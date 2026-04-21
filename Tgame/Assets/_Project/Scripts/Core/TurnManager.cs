using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TGame.Battle
{
    public enum BattleState { Planning, Settling, EnemyTurn }

    public class TurnManager : IGameSystem
    {
        public static TurnManager Instance { get; private set; }
        public BattleState CurrentState { get; private set; } = BattleState.Planning;
        public int CurrentRound { get; private set; } = 1;
        public int MaxTUPerTurn { get; private set; } = 13;

        // 独立账本
        private Dictionary<int, int> _unitCurrentTU = new Dictionary<int, int>();
        private Dictionary<int, int> _unitPlannedTU = new Dictionary<int, int>();
        private List<ICommand> _commandQueue = new List<ICommand>();

        public void OnInit() { Instance = this; }
        public void OnUpdate(float deltaTime) { }
        public void OnDestroy() { if (Instance == this) Instance = null; }

        public void RegisterUnit(int unitID)
        {
            if (!_unitCurrentTU.ContainsKey(unitID))
            {
                _unitCurrentTU[unitID] = 0;
                _unitPlannedTU[unitID] = 0;
            }
        }

        public int GetUnitPlannedTU(int unitID) => _unitPlannedTU.ContainsKey(unitID) ? _unitPlannedTU[unitID] : 0;

        public bool CanScheduleAction(int unitID, int cost)
        {
            if (!_unitPlannedTU.ContainsKey(unitID)) RegisterUnit(unitID);
            return _unitPlannedTU[unitID] + cost <= MaxTUPerTurn;
        }

        public void AddCommand(ICommand cmd)
        {
            if (cmd != null && cmd.Validate())
            {
                _commandQueue.Add(cmd);
                int uid = cmd.GetUnitID();
                _unitPlannedTU[uid] += cmd.GetCost();
                Debug.Log($"<color=green>[决策] 指令录入。角色 {uid} 已规划 TU: {_unitPlannedTU[uid]}/{MaxTUPerTurn}</color>");
            }
        }

        public void AdvanceTime(int unitID, int cost)
        {
            if (!_unitCurrentTU.ContainsKey(unitID)) RegisterUnit(unitID);
            _unitCurrentTU[unitID] += cost;
        }

        public void EndPlayerTurn()
        {
            if (CurrentState != BattleState.Planning) return;
            BattleManager.Instance.StartSettleRoutine(ResolveTurnRoutine());
        }

        private IEnumerator ResolveTurnRoutine()
        {
            CurrentState = BattleState.Settling;
            List<ICommand> snapshot = new List<ICommand>(_commandQueue);
            _commandQueue.Clear();

            foreach (var cmd in snapshot)
            {
                cmd.Execute();
                yield return new WaitForSeconds(1.1f);
            }

            CurrentState = BattleState.EnemyTurn;
            yield return new WaitForSeconds(1.0f);
            StartNewRound();
        }

        private void StartNewRound()
        {
            CurrentRound++;
            List<int> keys = new List<int>(_unitCurrentTU.Keys);
            foreach (var k in keys)
            {
                _unitCurrentTU[k] = 0;
                _unitPlannedTU[k] = 0;
            }
            CurrentState = BattleState.Planning;
            Debug.Log($"<color=green>======= 第 {CurrentRound} 轮 =======</color>");
        }
    }
}