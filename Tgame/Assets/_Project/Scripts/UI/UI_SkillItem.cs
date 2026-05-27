using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using TGame.Data;

namespace TGame.UI
{
    public class UI_SkillItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button btnExecute;
        public TextMeshProUGUI txtSkillName;
        public TextMeshProUGUI txtCost;

        private SkillData _skillData;
        private Action<int> _onClickCallback;
        private Action<SkillData, RectTransform> _onHoverEnter;
        private Action _onHoverExit;

        public void Init(SkillData data, Action<int> onClick, Action<SkillData, RectTransform> onHoverEnter, Action onHoverExit)
        {
            _skillData = data;
            _onClickCallback = onClick;
            _onHoverEnter = onHoverEnter;
            _onHoverExit = onHoverExit;

            if (txtSkillName) txtSkillName.text = data.skillName;

            // 显示时素和魔法消耗
            if (txtCost) txtCost.text = $"{data.tuCost} TU  <color=#00BFFF>{data.mpCost} MP</color>";

            btnExecute.onClick.RemoveAllListeners();
            btnExecute.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            _onClickCallback?.Invoke(_skillData.skillID);
        }

        // ==========================================
        // 鼠标悬停事件拦截
        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_skillData != null)
            {
                _onHoverEnter?.Invoke(_skillData, this.GetComponent<RectTransform>());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onHoverExit?.Invoke();
        }

        private void OnDisable()
        {
            _onHoverExit?.Invoke(); // 防止按钮突然被隐藏时，悬浮窗残留在屏幕上
        }
    }
}