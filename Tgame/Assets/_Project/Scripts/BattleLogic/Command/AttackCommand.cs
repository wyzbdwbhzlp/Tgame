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

        // --- 接口实现：获取消耗与执行者 ---
        public int GetCost() => _timeCost;
        public int GetUnitID() => _attackerID;

        public bool Validate()
        {
            return TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_attackerID, _timeCost);
        }

        // ==========================================
        // 1. 规划阶段 (生成虚影时触发)
        // ==========================================
        public void Execute()
        {
            var attacker = UnitManager.Instance.GetUnit(_attackerID);
            var target = UnitManager.Instance.GetUnit(_targetID);

            if (attacker != null && target != null)
            {
                // 规划阶段不扣除真实属性，只做表现或日志
                Debug.Log($"<color=orange>[规划] {attacker.ConfigData.characterName} 锁定了 {target.ConfigData.characterName} 准备攻击。</color>");

                // 以后这里可以加上：在目标头上显示一个红色的“准星”UI
            }
        }

        // ==========================================
        // 2. 撤销阶段 (玩家点击撤回按钮时触发)
        // ==========================================
        public void Undo()
        {
            var attacker = UnitManager.Instance.GetUnit(_attackerID);
            if (attacker != null)
            {
                Debug.Log($"<color=yellow>[撤销] {attacker.ConfigData.characterName} 取消了攻击锁定。</color>");

                // 以后这里可以加上：清除目标头上的“准星”UI
            }
        }

        // ==========================================
        // 3. 真实结算阶段 (回合结束，正式播放动画时触发)
        // ==========================================
        public void Settle()
        {
            var attacker = UnitManager.Instance.GetUnit(_attackerID);
            var target = UnitManager.Instance.GetUnit(_targetID);

            if (attacker != null && target != null)
            {
                // 正式结算时，扣除真实的物理时素
                TurnManager.Instance.AdvanceTime(_attackerID, _timeCost);

                // 播报真实结算信息 (未来这里替换为播放攻击动画、扣血和飘字)
                Debug.Log($"<color=red>⚔️ [结算] {attacker.ConfigData.characterName} 真实攻击了 {target.ConfigData.characterName}！消耗了 {_timeCost} TU。</color>");
            }
        }
    }
}