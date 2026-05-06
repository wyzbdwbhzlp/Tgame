using System;
using UnityEngine;

namespace TGame.Data
{
    // 【🔥新增】角色职业枚举
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

        // 【🔥新增】职业与普攻范围
        public CharacterJob job;
        [Tooltip("普通攻击的格子距离，近战填1，远程填2或以上")]
        public int attackRange;

        [Header("美术资源 (直接拖拽赋值)")]
        public Sprite portraitSprite;
        public GameObject characterPrefab;

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
    }

    [CreateAssetMenu(fileName = "CharacterTable", menuName = "TGame/Character Table")]
    public class CharacterTableSO : ScriptableObject
    {
        public CharacterData[] characters;
    }
}