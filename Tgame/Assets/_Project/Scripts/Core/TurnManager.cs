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

        public int CurrentTimeUnitsUsed { get; private set; } = 0;   // 实际已扣除（结算用）
        public int PlannedTimeUnitsUsed { get; private set; } = 0;   // 决策已规划（UI展示用）

        public int MaxTimeUnitsPerTurn { get; private set; } = 13;
        public int CurrentRound { get; private set; } = 1;

        private List<ICommand> _commandQueue = new List<ICommand>();

        public void OnInit()
        {
            Instance = this;
            Debug.Log("[TurnManager] 就绪。");
        }

        public void OnUpdate(float deltaTime) { }
        public void OnDestroy() { if (Instance == this) Instance = null; }

        public void AddCommand(ICommand cmd)
        {
            if (cmd != null && cmd.Validate())
            {
                _commandQueue.Add(cmd);
                // 规划时先累加预估消耗
                PlannedTimeUnitsUsed += cmd.GetCost();
                Debug.Log($"<color=green>[决策] 指令录入。当前总规划消耗: {PlannedTimeUnitsUsed} TU</color>");
            }
        }

        public void EndPlayerTurn()
        {
            if (CurrentState != BattleState.Planning) return;
            BattleManager.Instance.StartSettleRoutine(ResolveTurnRoutine());
        }

        private IEnumerator ResolveTurnRoutine()
        {
            CurrentState = BattleState.Settling;
            Debug.Log("<color=yellow>======= 结算开始 =======</color>");

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
            CurrentTimeUnitsUsed = 0;
            PlannedTimeUnitsUsed = 0; // 重置规划
            CurrentState = BattleState.Planning;
            Debug.Log($"<color=green>======= 第 {CurrentRound} 轮 =======</color>");
        }

        // 校验是否还能承载该消耗
        public bool CanScheduleAction(int requiredTime) => (PlannedTimeUnitsUsed + requiredTime) <= MaxTimeUnitsPerTurn;

        // 正式扣费
        public void AdvanceTime(int timeCost) => CurrentTimeUnitsUsed += timeCost;
    }
}