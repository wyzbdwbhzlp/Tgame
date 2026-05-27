using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TGame.Data;
using TGame.Core;

namespace TGame.Battle
{
    public class SkillCommand : ICommand
    {
        private int _casterID;
        private Vector3Int _targetHex;
        private SkillData _skillData;

        public SkillCommand(int casterID, Vector3Int targetHex, int skillID)
        {
            _casterID = casterID;
            _targetHex = targetHex;
            _skillData = DataManager.Instance.GetSkillData(skillID);
        }

        public int GetCost() => _skillData != null ? _skillData.tuCost : 0;
        public int GetUnitID() => _casterID;

        public bool Validate()
        {
            if (_skillData == null) return false;
            var caster = UnitManager.Instance.GetUnit(_casterID);
            if (caster == null || caster.CurrentMP < _skillData.mpCost) return false;
            return TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_casterID, _skillData.tuCost);
        }

        public void Execute() { }
        public void Undo() { }

        // 【🔥核心升级】直接通过协程处理时序
        public IEnumerator SettleRoutine()
        {
            var caster = UnitManager.Instance.GetUnit(_casterID);
            if (caster == null || _skillData == null) yield break;

            caster.ConsumeMP(_skillData.mpCost);
            TurnManager.Instance.AdvanceTime(_casterID, _skillData.tuCost);

            UnitView casterView = UnitViewManager.Instance.GetView(_casterID);
            if (casterView == null) yield break;

            Vector3 targetWorldPos = GridSystem.Instance.CellToWorld(_targetHex);

            // 1. 镜头特写与前摇
            if (CameraController.Instance != null) CameraController.Instance.ActionZoomIn(targetWorldPos);
            casterView.PlaySkillAnimation(targetWorldPos);

            yield return new WaitForSeconds(0.4f); // 施法前摇

            // 2. AOE 目标获取
            List<RuntimeUnit> hitTargets = new List<RuntimeUnit>();
            foreach (var unit in UnitManager.Instance.GetAllUnits())
            {
                if (GetHexDistance(_targetHex, unit.GridPosition) <= _skillData.aoeRadius)
                {
                    hitTargets.Add(unit);
                }
            }

            // 3. 震屏与地毯式特效
            if (_skillData.effectType == SkillEffectType.Damage && CameraController.Instance != null)
                CameraController.Instance.TriggerHitShake();

            if (VFXManager.Instance != null && !string.IsNullOrEmpty(_skillData.vfxID))
            {
                foreach (var kvp in GridSystem.Instance.GetAllCells())
                {
                    if (GetHexDistance(_targetHex, kvp.Key) <= _skillData.aoeRadius)
                    {
                        VFXManager.Instance.PlayVFXAtPosition(_skillData.vfxID, GridSystem.Instance.CellToWorld(kvp.Key));
                    }
                }
            }

            // 4. 结算数值
            foreach (var target in hitTargets)
            {
                UnitView targetView = UnitViewManager.Instance.GetView(target.InstanceID);
                int rawValue = _skillData.baseEffectValue + Mathf.RoundToInt(caster.ConfigData.attack * _skillData.effectMultiplier);

                if (_skillData.effectType == SkillEffectType.Damage)
                {
                    if (targetView != null) targetView.PlayHitFlash();
                    int finalDamage = Mathf.Max(1, rawValue - target.ConfigData.defense);
                    target.TakeDamage(finalDamage);

                    if (DamagePopupManager.Instance != null)
                        DamagePopupManager.Instance.CreatePopup(GridSystem.Instance.CellToWorld(target.GridPosition), finalDamage, false);
                }
                else if (_skillData.effectType == SkillEffectType.Heal)
                {
                    int finalHeal = Mathf.Max(1, rawValue);
                    target.Heal(finalHeal);

                    if (DamagePopupManager.Instance != null)
                        DamagePopupManager.Instance.CreatePopup(GridSystem.Instance.CellToWorld(target.GridPosition), finalHeal, false);
                }
            }

            // 【🔥表现展示期】等火烧完，等飘字飘完！
            yield return new WaitForSeconds(1.2f);

            if (CameraController.Instance != null) CameraController.Instance.ResetCameraZoom();

            // 【🔥收尾缓冲】退回原位后，再停顿 0.3 秒，然后才切给下一个角色
            yield return new WaitForSeconds(0.3f);
        }

        private int GetHexDistance(Vector3Int a, Vector3Int b)
        {
            return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
        }
    }
}