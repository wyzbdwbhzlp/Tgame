using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;

namespace TGame.UI
{
    public class UnitHUD : MonoBehaviour
    {
        [Header("生命值 (HP)")]
        public Image imgHPFill;
        public TextMeshProUGUI txtHP;

        [Header("魔法值 (MP)")]
        public GameObject mpGroup; // 用来整体隐藏怪物的蓝条
        public Image imgMPFill;
        public TextMeshProUGUI txtMP;

        private RuntimeUnit _unit;

        public void Init(RuntimeUnit unit)
        {
            _unit = unit;
            if (_unit == null) return;

            // 玩家阵营 (1001) 显示 MP，否则隐藏 MP 节点
            if (mpGroup != null)
            {
                mpGroup.SetActive(_unit.Side == 1001);
            }

            Refresh();
        }

        private void LateUpdate()
        {
            // 实时刷新数值（使用 LateUpdate 确保在所有战斗结算完毕后更新）
            Refresh();
        }

        private void Refresh()
        {
            if (_unit == null || _unit.ConfigData == null) return;

            // 刷新 HP
            if (imgHPFill) imgHPFill.fillAmount = (float)_unit.CurrentHP / _unit.ConfigData.maxHP;
            if (txtHP) txtHP.text = $"{_unit.CurrentHP}/{_unit.ConfigData.maxHP}";

            // 刷新 MP (仅玩家)
            if (_unit.Side == 1001)
            {
                if (imgMPFill) imgMPFill.fillAmount = (float)_unit.CurrentMP / _unit.ConfigData.maxMP;
                if (txtMP) txtMP.text = $"{_unit.CurrentMP}/{_unit.ConfigData.maxMP}";
            }
        }
    }
}