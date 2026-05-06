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

        private Dictionary<int, int> _unitCurrentTU = new Dictionary<int, int>();
        private Dictionary<int, int> _unitPlannedTU = new Dictionary<int, int>();
        private List<ICommand> _commandQueue = new List<ICommand>();

        // 【🔥新增】历史记忆栈
        private Stack<ICommand> _commandHistory = new Stack<ICommand>();

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
                // 【修改】录入时立即触发预演(Execute)，展现虚影和逻辑位移
                cmd.Execute();

                _commandQueue.Add(cmd);
                _commandHistory.Push(cmd); // 压入记忆栈

                int uid = cmd.GetUnitID();
                _unitPlannedTU[uid] += cmd.GetCost();
                Debug.Log($"<color=green>[决策] 指令录入。角色 {uid} 已规划 TU: {_unitPlannedTU[uid]}/{MaxTUPerTurn}</color>");
            }
        }

        // 【🔥新增】供 UI 调用的撤回系统
        public void UndoLastCommand()
        {
            if (CurrentState != BattleState.Planning) return;

            if (_commandHistory.Count > 0)
            {
                ICommand lastCmd = _commandHistory.Pop();
                _commandQueue.Remove(lastCmd);

                // 退还 TU 规划值
                int uid = lastCmd.GetUnitID();
                _unitPlannedTU[uid] -= lastCmd.GetCost();

                // 触发指令本身的撤回逻辑
                lastCmd.Undo();
            }
            else
            {
                Debug.LogWarning("[撤销] 已经是初始状态，没有可撤回的操作了！");
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

            // 结算开始，清空所有规划和记忆
            _commandQueue.Clear();
            _commandHistory.Clear();

            foreach (var cmd in snapshot)
            {
                cmd.Settle(); // 【修改】这里改为调用真实结算 Settle
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