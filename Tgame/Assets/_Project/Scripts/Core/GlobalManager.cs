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

        // 【🔥新增】将 LevelManager 也纳入 Global 的统一管理
        public LevelManager levelManager;

        [Header("===== 逻辑层系统 (纯 C#) =====")]
        public DataManager dataManager { get; private set; }
        public GridSystem gridSystem { get; private set; }
        public TurnManager turnManager { get; private set; }
        public UnitManager unitManager { get; private set; }

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
            if (gameObject.GetComponent<VFXManager>() == null) gameObject.AddComponent<VFXManager>();
            // 2. 挂载与初始化 MonoBehaviour 表现层系统
            if (uiManager == null) uiManager = gameObject.AddComponent<UIManager>();
            if (battleManager == null) battleManager = gameObject.AddComponent<BattleManager>();
            if (unitViewManager == null) unitViewManager = gameObject.AddComponent<UnitViewManager>();

            // 安全挂载 LevelManager
            if (levelManager == null) levelManager = GetComponent<LevelManager>();
            if (levelManager == null) levelManager = gameObject.AddComponent<LevelManager>();
            if (gameObject.GetComponent<DamagePopupManager>() == null) gameObject.AddComponent<DamagePopupManager>();
            if (hexMapView == null)
            {
                GameObject mapGo = new GameObject("HexMapView");
                mapGo.transform.SetParent(this.transform);
                hexMapView = mapGo.AddComponent<HexMapView>();
            }

            // ==========================================
            // 【🔥核心修复】严格控制生命周期顺序！
            // ==========================================

            // 第一步：先让 LevelManager 把底层地图建好，并画出表现层的地块
            levelManager.LoadCurrentLevel();

            // 第二步：再让 BattleManager 初始化并生成角色
            // 此时由于地图已就绪，角色生成时，底层的 OccupantUnitID 才能被正确记录！
            battleManager.OnInit();

            Debug.Log("<color=cyan>[GlobalManager] 框架搭建完毕，系统运转正常！</color>");
        }

        private void RegisterLogicSystem(IGameSystem system)
        {
            _logicSystems.Add(system);
        }

        private void Update()
        {
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