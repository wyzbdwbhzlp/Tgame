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

        // --- 接口实现：获取消耗 ---
        public int GetCost()
        {
            return _timeCost;
        }

        // --- 核心修复：实现接口要求的 GetUnitID 方法 ---
        public int GetUnitID()
        {
            return _attackerID; // 攻击者就是这个指令的发起人
        }

        public bool Validate()
        {
            // 核心修复：传入 _attackerID，检查攻击者自己的剩余时素
            return TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_attackerID, _timeCost);
        }

        public void Execute()
        {
            var attacker = UnitManager.Instance.GetUnit(_attackerID);
            var target = UnitManager.Instance.GetUnit(_targetID);

            if (attacker != null && target != null)
            {
                // 核心修复：正式扣除时素时，指定扣除攻击者的时素
                TurnManager.Instance.AdvanceTime(_attackerID, _timeCost);

                // 播报结算信息
                Debug.Log($"<color=orange>⚔️ [结算] {attacker.ConfigData.characterName} 攻击了 {target.ConfigData.characterName}！消耗了 {_timeCost} TU。</color>");

                // 这里以后可以加入战斗动画触发逻辑
            }
        }
    }
}