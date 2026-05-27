using System;
using System.Collections.Generic;
using UnityEngine;

namespace TGame.Data
{
    // ==========================================
    // 玩家生成点
    // ==========================================
    [Serializable]
    public class UnitSpawnInfo
    {
        [Tooltip("对应 CharacterTable 里的英雄 ID")]
        public int characterID;
        public Vector3Int spawnPos;
    }

    // ==========================================
    // 敌人生成点
    // ==========================================
    [Serializable]
    public class EnemySpawnInfo
    {
        [Tooltip("对应 EnemyTable 里的敌人 ID")]
        public int enemyID;
        public Vector3Int spawnPos;
    }

    // ==========================================
    // 地图可视化与结构
    // ==========================================
    [Serializable]
    public class HexEditorCell
    {
        public Vector3Int position;
        public int groundVariantID = 0;
        public int obstacleVariantID = -1;
    }

    // ==========================================
    // 关卡总控数据
    // ==========================================
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "TGame/关卡数据 (Level Data)")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("基础配置")]
        public int levelID;
        public string levelName = "未命名关卡";

        [Header("地图可视化结构 (由画笔工具自动生成)")]
        public List<HexEditorCell> cells = new List<HexEditorCell>();

        [Header("单位部署")]
        public List<UnitSpawnInfo> playerSpawns = new List<UnitSpawnInfo>();
        public List<EnemySpawnInfo> enemySpawns = new List<EnemySpawnInfo>();
    }

    // ==========================================
    // 关卡排期表
    // ==========================================
    [CreateAssetMenu(fileName = "LevelTable", menuName = "TGame/关卡排期表 (Level Table)")]
    public class LevelTable : ScriptableObject
    {
        [Header("将关卡资产拖拽到这里排列顺序")]
        public List<LevelDataSO> levels = new List<LevelDataSO>();
    }
}