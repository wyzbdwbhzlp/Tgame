using System;
using UnityEngine;

namespace TGame.Data
{
    // 【🔥核心】不再继承 ScriptableObject，而是变成一个可序列化的普通类
    [Serializable]
    public class CharacterData
    {
        [Header("基础信息")]
        public int characterID;
        public string characterName;

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

    // 【🔥核心】这张表才是真正的 ScriptableObject，它容纳了所有的角色数据
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "TGame/Character Table")]
    public class CharacterTableSO : ScriptableObject
    {
        public CharacterData[] characters;
    }
}