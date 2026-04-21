using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Game.UI
{
    public enum UILayer
    {
        Background = 0, // 最底层（如主城背景）
        Normal = 1,     // 普通全屏界面（如背包、角色面板）
        Popup = 2,      // 弹窗界面（如确认框、提示框）
        Top = 3         // 顶层界面（如跑马灯、断线重连提示）
    }
    public class UIBase : MonoBehaviour
    {
        [Header("弹窗动画配置")]
        public bool usePopupAnimation = true;
        public float animationDuration = 0.3f;

        private Vector3 _baseOriginScale;
        private bool _isScaleInitialized = false;

        // 【新增】标识该 UI 所在的层级，可在 Prefab 的 Inspector 中配置
        public UILayer uiLayer = UILayer.Normal;

        /// <summary>
        /// 生命周期：初始化 (仅在 UI 预制体首次实例化时调用一次)
        /// 适合在这里执行：GetComponent、绑定按钮事件等
        /// </summary>
        public virtual void OnInit() { }

        public virtual void Show()
        {
            gameObject.SetActive(true);

            if (usePopupAnimation)
            {
                if (!_isScaleInitialized)
                {
                    _baseOriginScale = transform.localScale;
                    _isScaleInitialized = true;
                }
                // 假设你有 UITweenHelper
                // UITweenHelper.PlayPopupShow(transform, _baseOriginScale, animationDuration);
            }

            OnShow();
        }

        public virtual void Hide()
        {
            if (usePopupAnimation && _isScaleInitialized && gameObject.activeInHierarchy)
            {
                // UITweenHelper.PlayPopupHide(transform, _baseOriginScale, () =>
                // {
                //     gameObject.SetActive(false);
                //     OnHide();
                // }, animationDuration);
            }
            else
            {
                gameObject.SetActive(false);
                OnHide();
            }
        }

        /// <summary>
        /// 生命周期：每次打开时调用
        /// 适合在这里执行：刷新数据、播放音效
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// 生命周期：每次隐藏时调用
        /// 适合在这里执行：停止内部协程、清理临时数据
        /// </summary>
        protected virtual void OnHide() { }
    }
}