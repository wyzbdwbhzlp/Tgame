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
        public Sprite portraitSprite;
        public GameObject characterPrefab;

        // ==========================================
        // 【🔥核心修改】增加攻击时序配置
        // ==========================================
        [Header("战斗表现")]
        public string attackVFXID = "Hit_Default";

        [Tooltip("从发起攻击到特效产生的延迟(秒)")]
        public float attackHitDelay = 0.35f;

        [Tooltip("从特效产生到伤害跳字的延迟(秒)")]
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
    }

    [CreateAssetMenu(fileName = "CharacterTable", menuName = "TGame/Character Table")]
    public class CharacterTableSO : ScriptableObject
    {
        public CharacterData[] characters;
    }
}