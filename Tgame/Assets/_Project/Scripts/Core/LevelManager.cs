using UnityEngine;
using TGame.Data;

namespace TGame.Battle
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("核心配置")]
        public LevelTable masterLevelTable;
        public int currentLevelIndex = 0;

        private void Awake()
        {
            Instance = this;
        }

        // 【🔥核心修复】删掉原有的 Start() 方法！
        // 绝对不允许它自己擅自加载地图，必须等 GlobalManager 传唤！

        public void LoadCurrentLevel()
        {
            if (masterLevelTable == null || masterLevelTable.levels.Count <= currentLevelIndex)
            {
                Debug.LogError("[LevelManager] 关卡表为空，或者索引越界！");
                return;
            }

            LevelDataSO levelData = masterLevelTable.levels[currentLevelIndex];
            Debug.Log($"<color=orange>[LevelManager] 正在启动关卡：{levelData.levelName}</color>");

            // 1. 启动底层逻辑网格
            if (GridSystem.Instance != null)
            {
                GridSystem.Instance.LoadLevel(levelData);
            }
            else
            {
                Debug.LogError("[LevelManager] GridSystem 未初始化！");
            }

            // 2. 启动表现层渲染
            if (HexMapView.Instance != null)
            {
                HexMapView.Instance.CreateGridVisuals();
            }
        }

        // 【🔥新增】辅助工具接口，方便 BattleManager 生成怪物时拿数据
        public LevelDataSO GetCurrentLevelData()
        {
            if (masterLevelTable != null && currentLevelIndex < masterLevelTable.levels.Count)
            {
                return masterLevelTable.levels[currentLevelIndex];
            }
            return null;
        }
    }
}