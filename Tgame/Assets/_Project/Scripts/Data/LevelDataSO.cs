using System;
using System.Collections.Generic;
using UnityEngine;

namespace TGame.Data
{
    // 用于记录角色部署信息的数据结构
    [Serializable]
    public class UnitSpawnInfo
    {
        public int characterID;
        public Vector3Int spawnPos;
    }

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "TGame/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public int levelID;
        public string levelName;
        public int mapRadius = 5; // 关卡地图半径

        [Header("地图配置")]
        public List<Vector3Int> obstacles = new List<Vector3Int>(); // 障碍物坐标

        [Header("单位部署")]
        public List<UnitSpawnInfo> playerSpawns = new List<UnitSpawnInfo>(); // 玩家部署
        public List<UnitSpawnInfo> enemySpawns = new List<UnitSpawnInfo>();  // 敌人部署 (后续使用)
    }
}