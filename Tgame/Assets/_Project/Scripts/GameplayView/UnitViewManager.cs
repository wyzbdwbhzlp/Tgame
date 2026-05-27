using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;
using TGame.Data;

namespace TGame.Core
{
    public class UnitViewManager : MonoBehaviour
    {
        public static UnitViewManager Instance { get; private set; }

        // 存储所有场上角色的表现层(GameObject)引用
        private Dictionary<int, UnitView> _viewDict = new Dictionary<int, UnitView>();

        private void Awake()
        {
            Instance = this;
            Debug.Log("[UnitViewManager] 表现层角色管理器就绪。");
        }

        // ==========================================
        // 【🔥核心修复】这就是刚才报错提示找不到的方法！
        // 它的作用是拿着底层的数据，去生成真实的 3D/2D 模型。
        // ==========================================
        public UnitView CreateUnitView(RuntimeUnit logicUnit)
        {
            if (logicUnit == null || logicUnit.ConfigData == null)
            {
                Debug.LogError("[UnitViewManager] 创建失败：逻辑数据为空！");
                return null;
            }

            if (logicUnit.ConfigData.characterPrefab == null)
            {
                Debug.LogWarning($"<color=yellow>[UnitViewManager] 角色【{logicUnit.ConfigData.characterName}】没有配置预制体(Prefab)，无法显示外观！</color>");
                return null;
            }

            // 1. 实例化真正的游戏物体
            GameObject obj = Instantiate(logicUnit.ConfigData.characterPrefab);
            obj.name = $"[Unit] {logicUnit.ConfigData.characterName}_{logicUnit.InstanceID}";

            // 2. 确保它挂载了我们之前写好的 UnitView 脚本
            UnitView view = obj.GetComponent<UnitView>();
            if (view == null)
            {
                view = obj.AddComponent<UnitView>();
            }

            // 3. 将底层逻辑灌入给模型，UnitView 内部会自动把模型移动到正确的格子上
            view.Init(logicUnit);

            // 4. 存入字典，方便后续攻击、移动时直接找到对应的模型去播动画
            _viewDict[logicUnit.InstanceID] = view;

            return view;
        }

        /// <summary>
        /// 提供给战斗系统查询模型引用的接口
        /// </summary>
        public UnitView GetView(int instanceID)
        {
            _viewDict.TryGetValue(instanceID, out var view);
            return view;
        }

        public void RemoveView(int instanceID)
        {
            if (_viewDict.TryGetValue(instanceID, out var view))
            {
                if (view != null && view.gameObject != null)
                {
                    Destroy(view.gameObject);
                }
                _viewDict.Remove(instanceID);
            }
        }

        public void ClearAllViews()
        {
            foreach (var view in _viewDict.Values)
            {
                if (view != null && view.gameObject != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _viewDict.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}