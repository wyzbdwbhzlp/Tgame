using System.Collections.Generic;
using UnityEngine;

namespace TGame.Data
{
    // ==========================================
    // 关卡排期总表
    // ==========================================
    [CreateAssetMenu(fileName = "LevelTable", menuName = "TGame/关卡排期表 (Level Table)")]
    public class LevelTable : ScriptableObject
    {
        [Header("将关卡资产拖拽到这里排列顺序")]
        public List<LevelDataSO> levels = new List<LevelDataSO>();

        /// <summary>
        /// 安全获取关卡数据
        /// </summary>
        public LevelDataSO GetLevel(int levelIndex)
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogError("[LevelTable] 关卡排期表为空！请放入关卡数据！");
                return null;
            }

            if (levelIndex >= 0 && levelIndex < levels.Count)
            {
                return levels[levelIndex];
            }

            Debug.LogError($"[LevelTable] 试图获取不存在的关卡索引：{levelIndex}，当前总关卡数：{levels.Count}");
            return null;
        }

        /// <summary>
        /// 获取总关卡数量
        /// </summary>
        public int GetTotalLevelCount()
        {
            return levels != null ? levels.Count : 0;
        }
    }
}