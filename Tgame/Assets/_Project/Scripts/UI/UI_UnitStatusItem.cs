using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;
using System;

namespace TGame.UI
{
    public class UI_UnitStatusItem : MonoBehaviour
    {
        public Button btnSelect;
        public Image imgPortrait;
        public TextMeshProUGUI txtName;

        [Header("生命值")]
        public Image imgHPFill;
        public TextMeshProUGUI txtHP;

        [Header("魔法值")]
        public GameObject mpGroup; // 用来整体隐藏敌人的蓝条
        public Image imgMPFill;
        public TextMeshProUGUI txtMP;

        private int _unitID;
        private Action<int> _onSelectCallback;
        private RuntimeUnit _unit;

        public void Init(RuntimeUnit unit, Action<int> onSelect)
        {
            _unit = unit;
            _unitID = unit.InstanceID;
            _onSelectCallback = onSelect;

            if (txtName) txtName.text = unit.ConfigData.characterName;
            if (imgPortrait) imgPortrait.sprite = unit.ConfigData.portraitSprite;

            // 只有玩家 (1001) 才显示魔法值区域
            if (mpGroup) mpGroup.SetActive(_unit.Side == 1001);

            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(() => _onSelectCallback?.Invoke(_unitID));

            Refresh();
        }

        private void LateUpdate()
        {
            // 使用 LateUpdate 确保战斗扣血结算后，UI 每一帧都能完美同步
            Refresh();
        }

        private void Refresh()
        {
            if (_unit == null) return;

            if (imgHPFill) imgHPFill.fillAmount = (float)_unit.CurrentHP / _unit.ConfigData.maxHP;
            if (txtHP) txtHP.text = $"{_unit.CurrentHP}/{_unit.ConfigData.maxHP}";

            if (_unit.Side == 1001)
            {
                if (imgMPFill) imgMPFill.fillAmount = (float)_unit.CurrentMP / _unit.ConfigData.maxMP;
                if (txtMP) txtMP.text = $"{_unit.CurrentMP}/{_unit.ConfigData.maxMP}";
            }
        }
    }
}