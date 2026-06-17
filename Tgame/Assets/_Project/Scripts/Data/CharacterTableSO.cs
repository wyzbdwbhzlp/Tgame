using System;
using System.Collections.Generic;
using UnityEngine;

namespace TGame.Data
{
    public enum CharacterJob
    {
        Warrior = 0,  // 战士
        Mage = 1,     // 法师
        Priest = 2,   // 牧师
        Archer = 3    // 弓箭手
    }

    [Serializable]
    public class CharacterData
    {
        [Header("基础信息")]
        public int characterID;
        public string characterName;
        public CharacterJob job;

        [Tooltip("普通攻击的格子距离，近战填1，远程填2或以上")]
        public int attackRange;

        [Header("美术资源 (直接拖拽赋值)")]
        public Sprite portraitSprite; // 半身大立绘
        public Sprite headIcon;       // 【🔥新增】UI状态栏专用小圆头像
        public GameObject characterPrefab;

        [Header("战斗表现")]
        public string attackVFXID = "Hit_Default";
        public float attackHitDelay = 0.35f;
        public float damagePopupDelay = 0.15f;

        [Header("战斗属性")]
        public int maxHP;
        public int maxMP;
        public int attack;
        public int defense;
        public int speed;

        [Header("高级机制")]
        public int postureValue;
        public float dodgeRate;
        public float critRate;

        [Header("技能配置")]
        [Tooltip("填入 SkillTable 中的技能 ID")]
        public List<int> skillIDs = new List<int>();

        [Header("元素克质属性 (对应SkillTag中的元素)")]
        public TGame.Data.SkillTag weakness;   // 弱点属性
        public TGame.Data.SkillTag resistance; // 抗性属性
        public float evasionRate = 0.05f;      // 基础闪避率 (0.05 = 5%)
    }

    [CreateAssetMenu(fileName = "CharacterTable", menuName = "TGame/Character Table")]
    public class CharacterTableSO : ScriptableObject
    {
        public CharacterData[] characters;
    }
}