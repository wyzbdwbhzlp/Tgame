using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;
using Game.UI;

namespace TGame.Core
{
    public class GlobalManager : MonoBehaviour
    {
        private static GlobalManager _instance;
        public static GlobalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GlobalManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[GlobalManager]");
                        _instance = go.AddComponent<GlobalManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("===== 表现层管理器 (MonoBehaviour) =====")]
        public UIManager uiManager;
        public BattleManager battleManager;
        public UnitViewManager unitViewManager;
        public HexMapView hexMapView;

        [Header("===== 逻辑层系统 (纯 C#) =====")]
        // 暴露出去供外部直接访问（可选，如果不想写单例的话）
        public DataManager dataManager { get; private set; }
        public GridSystem gridSystem { get; private set; }
        public TurnManager turnManager { get; private set; }
        public UnitManager unitManager { get; private set; }

        // 统一管理逻辑层的生命周期
        private List<IGameSystem> _logicSystems = new List<IGameSystem>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAllSystems();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 核心：统一初始化所有子系统
        /// </summary>
        private void InitializeAllSystems()
        {
            Debug.Log("<color=cyan>[GlobalManager] 开始挂载与初始化核心框架...</color>");

            // 1. 初始化纯 C# 逻辑系统 (按严格顺序)
            dataManager = new DataManager();
            gridSystem = new GridSystem();
            turnManager = new TurnManager();
            unitManager = new UnitManager();

            RegisterLogicSystem(dataManager);
            RegisterLogicSystem(gridSystem);
            RegisterLogicSystem(turnManager);
            RegisterLogicSystem(unitManager);

            foreach (var sys in _logicSystems) sys.OnInit();

            // 2. 挂载与初始化 MonoBehaviour 表现层系统
            if (uiManager == null) uiManager = gameObject.AddComponent<UIManager>();

            // 战棋特有管理器
            if (battleManager == null) battleManager = gameObject.AddComponent<BattleManager>();
            if (unitViewManager == null) unitViewManager = gameObject.AddComponent<UnitViewManager>();

            // HexMapView 因为需要 LineRenderer，我们用一种安全的方式挂载
            if (hexMapView == null)
            {
                GameObject mapGo = new GameObject("HexMapView");
                mapGo.transform.SetParent(this.transform);
                hexMapView = mapGo.AddComponent<HexMapView>();
            }

            // 手动触发 BattleManager 的战斗初始化（以前是在 Start 里）
            battleManager.OnInit();

            Debug.Log("<color=cyan>[GlobalManager] 框架搭建完毕，系统运转正常！</color>");
        }

        private void RegisterLogicSystem(IGameSystem system)
        {
            _logicSystems.Add(system);
        }

        private void Update()
        {
            // 统一驱动逻辑层的 Update
            float dt = Time.deltaTime;
            foreach (var sys in _logicSystems)
            {
                sys.OnUpdate(dt);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                foreach (var sys in _logicSystems) sys.OnDestroy();
                _logicSystems.Clear();
                _instance = null;
            }
        }
    }
}