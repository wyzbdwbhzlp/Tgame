using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;
using System;

namespace TGame.UI
{
    public class UI_UnitStatusItem : MonoBehaviour
    {
        [Header("基础控件")]
        public Button btnSelect;
        public Image imgPortrait;
        public TextMeshProUGUI txtName;

        [Header("生命值 (球形血条)")]
        public Image imgHPFill;
        public TextMeshProUGUI txtHP;

        [Header("失衡值 (躯干条)")]
        public Image imgPostureFill;
        public TextMeshProUGUI txtPosture;

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

            // 【🔥安全修正】区分玩家和敌人，防止读取 Enemy 的 ConfigData 导致空指针崩溃！
            string uName = _unit.Side == 1001 ? _unit.ConfigData.characterName : _unit.EnemyConfig.enemyName;

            // 优先读取新的小头像，如果没有配置，则拿大立绘兜底
            Sprite uIcon = _unit.Side == 1001 ? _unit.ConfigData.headIcon : _unit.EnemyConfig.headIcon;
            if (uIcon == null)
                uIcon = _unit.Side == 1001 ? _unit.ConfigData.portraitSprite : _unit.EnemyConfig.portraitSprite;

            if (txtName) txtName.text = uName;
            if (imgPortrait) imgPortrait.sprite = uIcon;

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

            // 刷新生命值
            int maxHP = _unit.GetMaxHP();
            if (imgHPFill) imgHPFill.fillAmount = (float)_unit.CurrentHP / maxHP;
            if (txtHP) txtHP.text = $"{_unit.CurrentHP}/{maxHP}";

            // 【🔥新增】刷新躯干失衡值
            int maxPosture = _unit.GetMaxPosture();
            if (imgPostureFill) imgPostureFill.fillAmount = (float)_unit.CurrentPosture / maxPosture;
            if (txtPosture) txtPosture.text = $"{_unit.CurrentPosture}/{maxPosture}";

            // 刷新魔法值 (仅玩家)
            if (_unit.Side == 1001)
            {
                if (imgMPFill) imgMPFill.fillAmount = (float)_unit.CurrentMP / _unit.ConfigData.maxMP;
                if (txtMP) txtMP.text = $"{_unit.CurrentMP}/{_unit.ConfigData.maxMP}";
            }
        }
    }
}