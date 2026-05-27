using System;
using UnityEngine;

namespace TGame.Data
{
    // ==========================================
    // AI 专属定位标签
    // ==========================================
    public enum EnemyRole
    {
        Assassin,   // 刺客 (追求击杀)
        AOE,        // 范围输出 (追求最大伤害覆盖)
        Tank,       // 肉盾 (追求承伤与卡位)
        Support     // 辅助 (追求强化刺客与AOE)
    }

    // ==========================================
    // 敌人单体数据结构
    // ==========================================
    [Serializable]
    public class EnemyData
    {
        [Header("基础信息")]
        public int enemyID;
        public string enemyName;

        [Tooltip("AI 大脑定位：决定它的行动偏好公式")]
        public EnemyRole aiRole;

        [Header("美术资源")]
        public Sprite portraitSprite;
        public GameObject prefab;

        public string attackVFXID = "Hit_Default";
        public float attackHitDelay = 0.35f;
        public float damagePopupDelay = 0.15f;

        [Header("战斗属性")]
        public int maxHP;
        public int attack;
        public int defense;
        public int attackRange;
        public float critRate;
        public int postureValue;

        [Header("AI专属机制")]
        public int maxMoveDistance = 4;
        public int speed;
    }

    // ==========================================
    // 敌人总表
    // ==========================================
    [CreateAssetMenu(fileName = "EnemyTable", menuName = "TGame/敌人总表 (Enemy Table)")]
    public class EnemyTableSO : ScriptableObject
    {
        public EnemyData[] enemies;
    }
}