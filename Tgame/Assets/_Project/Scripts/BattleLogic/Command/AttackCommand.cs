using UnityEngine;
using System.Collections;
using TGame.Core;
using TGame.Data;

namespace TGame.Battle
{
    public class AttackCommand : ICommand
    {
        private int _attackerID;
        private int _targetID;
        // 假设普攻固定消耗 4 TU，如果你有配表，可以从配置里读
        private int _cost = 4;

        public AttackCommand(int attackerID, int targetID)
        {
            _attackerID = attackerID;
            _targetID = targetID;
        }

        public int GetCost() => _cost;
        public int GetUnitID() => _attackerID;

        public bool Validate()
        {
            return TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_attackerID, _cost);
        }

        public void Execute()
        {
            Debug.Log($"<color=orange>[规划] {_attackerID} 准备攻击 {_targetID}</color>");
        }

        public void Undo() { }

        // ==========================================
        // 【🔥核心升级】把 Settle 变成协程 SettleRoutine，完美把控普攻节奏
        // ==========================================
        public IEnumerator SettleRoutine()
        {
            var attacker = UnitManager.Instance.GetUnit(_attackerID);
            var target = UnitManager.Instance.GetUnit(_targetID);

            if (attacker == null || target == null) yield break;

            // 1. 镜头聚焦 & 扣除TU
            if (CameraController.Instance != null) CameraController.Instance.FocusOnExecution(_attackerID);
            TurnManager.Instance.AdvanceTime(_attackerID, _cost);

            var attackerView = UnitViewManager.Instance.GetView(_attackerID);
            var targetView = UnitViewManager.Instance.GetView(_targetID);

            // 2. 播放攻击动作
            if (attackerView != null)
            {
                attackerView.PlayAttackAnimation(GridSystem.Instance.CellToWorld(target.GridPosition));
            }

            // 3. 等待前摇 (读取角色的 attackHitDelay，如果读不到默认给 0.35 秒)
            float hitDelay = attacker.ConfigData != null ? attacker.ConfigData.attackHitDelay : 0.35f;
            yield return new WaitForSeconds(hitDelay);

            // 4. 命中瞬间：受击闪白、震屏、爆特效
            if (targetView != null) targetView.PlayHitFlash();
            if (CameraController.Instance != null) CameraController.Instance.TriggerHitShake();

            if (VFXManager.Instance != null && attacker.ConfigData != null)
            {
                // 播放攻击者配置的攻击特效
                VFXManager.Instance.PlayVFX(attacker.ConfigData.attackVFXID, targetView.transform);
            }

            // 5. 伤害结算与飘字
            int damage = Mathf.Max(1, attacker.ConfigData.attack - target.ConfigData.defense);
            target.TakeDamage(damage);

            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.CreatePopup(GridSystem.Instance.CellToWorld(target.GridPosition), damage, false);
            }

            // 6. 表现展示期：等受击动作和特效播完
            yield return new WaitForSeconds(0.8f);

            // 7. 恢复镜头并缓冲
            if (CameraController.Instance != null) CameraController.Instance.ResetCameraZoom();
            yield return new WaitForSeconds(0.2f);
        }
    }
}