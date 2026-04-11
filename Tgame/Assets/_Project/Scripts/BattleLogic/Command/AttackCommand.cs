using UnityEngine;
using TGame.Battle;

namespace TGame.Battle
{
    public class AttackCommand : ICommand
    {
        private int _attackerID;
        private int _targetID;
        private int _timeCost = 3; // 攻击固定消耗 3 时素

        public AttackCommand(int attackerID, int targetID)
        {
            _attackerID = attackerID;
            _targetID = targetID;
        }

        // --- 核心修复：实现接口要求的方法 ---
        public int GetCost()
        {
            return _timeCost;
        }

        public bool Validate()
        {
            // 校验当前规划的总时素是否允许发起这次攻击
            return TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_timeCost);
        }

        public void Execute()
        {
            var attacker = UnitManager.Instance.GetUnit(_attackerID);
            var target = UnitManager.Instance.GetUnit(_targetID);

            if (attacker != null && target != null)
            {
                // 正式扣除时素
                TurnManager.Instance.AdvanceTime(_timeCost);

                // 播报结算信息
                Debug.Log($"<color=orange>⚔️ [结算] {attacker.ConfigData.characterName} 攻击了 {target.ConfigData.characterName}！消耗了 {_timeCost} TU。</color>");

                // 这里以后可以加入战斗动画触发逻辑
            }
        }
    }
}