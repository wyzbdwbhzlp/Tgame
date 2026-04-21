using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public class UIManager : MonoBehaviour
    {
        // BattleManager 正在找的就是这个 Instance！
        public static UIManager Instance { get; private set; }

        private Dictionary<Type, UIBase> _uiDict = new Dictionary<Type, UIBase>();
        private Dictionary<UILayer, Transform> _layerRoots = new Dictionary<UILayer, Transform>();

        private void Awake()
        {
            Instance = this;
            InitLayerRoots();
        }

        private void InitLayerRoots()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                GameObject layerObj = new GameObject($"Layer_{layer}");
                layerObj.transform.SetParent(this.transform, false);

                RectTransform rect = layerObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _layerRoots.Add(layer, layerObj.transform);
            }
        }

        public T Show<T>(string prefabName) where T : UIBase
        {
            Type uiType = typeof(T);

            if (_uiDict.TryGetValue(uiType, out UIBase uiPanel))
            {
                uiPanel.transform.SetAsLastSibling();
                uiPanel.Show();
                return uiPanel as T;
            }

            GameObject prefab = Resources.Load<GameObject>($"UI/{prefabName}");
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 加载失败！找不到路径: Resources/UI/{prefabName}");
                return null;
            }

            GameObject instObj = Instantiate(prefab);
            T newUI = instObj.GetComponent<T>();
            if (newUI == null)
            {
                Debug.LogError($"[UIManager] 预制体缺失 {uiType.Name} 组件！");
                Destroy(instObj);
                return null;
            }

            instObj.transform.SetParent(_layerRoots[newUI.uiLayer], false);
            _uiDict.Add(uiType, newUI);
            newUI.OnInit();
            newUI.Show();

            return newUI;
        }

        public void Hide<T>() where T : UIBase
        {
            if (_uiDict.TryGetValue(typeof(T), out UIBase panel)) panel.Hide();
        }

        public T Get<T>() where T : UIBase
        {
            if (_uiDict.TryGetValue(typeof(T), out var ui)) return ui as T;
            return null;
        }
    }
}