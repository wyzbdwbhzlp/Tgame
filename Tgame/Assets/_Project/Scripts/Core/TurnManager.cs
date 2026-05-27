using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TGame.Core;

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
            var unit = UnitManager.Instance.GetUnit(unitID);
            if (unit != null && unit.Side != 1001) return true;

            if (!_unitPlannedTU.ContainsKey(unitID)) RegisterUnit(unitID);
            return _unitPlannedTU[unitID] + cost <= MaxTUPerTurn;
        }

        public void AddCommand(ICommand cmd)
        {
            if (cmd != null && cmd.Validate())
            {
                cmd.Execute();
                _commandQueue.Add(cmd);
                _commandHistory.Push(cmd);

                int uid = cmd.GetUnitID();
                _unitPlannedTU[uid] += cmd.GetCost();
            }
        }

        public void UndoLastCommand()
        {
            if (CurrentState != BattleState.Planning) return;

            if (_commandHistory.Count > 0)
            {
                ICommand lastCmd = _commandHistory.Pop();
                _commandQueue.Remove(lastCmd);

                int uid = lastCmd.GetUnitID();
                _unitPlannedTU[uid] -= lastCmd.GetCost();

                lastCmd.Undo();
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
            _commandHistory.Clear();

            // 【🔥完美同步】不再写死时间，而是用 yield return 挂起等待该指令自己的协程完成！
            foreach (var cmd in snapshot)
            {
                yield return BattleManager.Instance.StartSettleRoutine(cmd.SettleRoutine());
            }

            CurrentState = BattleState.EnemyTurn;

            List<RuntimeUnit> enemies = UnitManager.Instance.GetAllUnits()
                .Where(u => u.Side != 1001 && u.CurrentHP > 0)
                .ToList();

            EnemyAIController aiBrain = new EnemyAIController();

            foreach (var enemy in enemies)
            {
                if (enemy.CurrentHP <= 0) continue;

                if (CameraController.Instance != null) CameraController.Instance.FocusOnExecution(enemy.InstanceID);

                ICommand enemyCmd = aiBrain.DecideNextAction(enemy);

                if (enemyCmd != null && enemyCmd.Validate())
                {
                    enemyCmd.Execute();
                    // 同样完美同步敌方的动作！
                    yield return BattleManager.Instance.StartSettleRoutine(enemyCmd.SettleRoutine());
                }
            }

            yield return new WaitForSeconds(0.5f);
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
        }
    }
}