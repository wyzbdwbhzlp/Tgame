using System;
using UnityEngine;

namespace TGame.Data
{
    public enum EnemyRole
    {
        Assassin,   // 刺客 (追求击杀)
        AOE,        // 范围输出 (追求最大伤害覆盖)
        Tank,       // 肉盾 (追求承伤与卡位)
        Support     // 辅助 (追求强化刺客与AOE)
    }

    [Serializable]
    public class EnemyData
    {
        [Header("基础信息")]
        public int enemyID;
        public string enemyName;
        public EnemyRole aiRole;

        [Header("美术资源")]
        public Sprite portraitSprite; // 半身大立绘
        public Sprite headIcon;       // 【🔥新增】UI状态栏专用小圆头像
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

        [Header("元素克质属性 (对应SkillTag中的元素)")]
        public TGame.Data.SkillTag weakness;   // 弱点属性
        public TGame.Data.SkillTag resistance; // 抗性属性
        public float evasionRate = 0.05f;      // 基础闪避率
    }

    [CreateAssetMenu(fileName = "EnemyTable", menuName = "TGame/敌人总表 (Enemy Table)")]
    public class EnemyTableSO : ScriptableObject
    {
        public EnemyData[] enemies;
    }
}