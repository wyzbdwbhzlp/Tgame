using System;
using System.Collections.Generic;
using UnityEngine;

namespace TGame.Data
{
    public enum SkillCategory
    {
        Spell,  // 角色自身法术
        Weapon  // 武器技能
    }

    public enum SkillEffectType
    {
        Damage,     // 伤害技能 (扣血)
        Heal,       // 治疗技能 (加血)
        StatusOnly  // 纯状态技能 (不造成直接数值变化)
    }

    [Flags]
    public enum SkillTag
    {
        None = 0,
        Wood = 1 << 0,
        Fire = 1 << 1,
        Water = 1 << 2,
        Electric = 1 << 3,
        Wind = 1 << 4,
        KnockUp = 1 << 5,
        Stun = 1 << 6,
        KnockDown = 1 << 7
    }

    // ==========================================
    // 【🔥新增】目标选择掩码 (使用 Flags 允许组合)
    // ==========================================
    [Flags]
    public enum SkillTargetMask
    {
        None = 0,
        Ally = 1 << 0,     // 友军 (包含自身)
        Enemy = 1 << 1,    // 敌人
        Empty = 1 << 2,    // 空格子

        // 策划偷懒专用组合热键
        AllUnits = Ally | Enemy,               // 只要是个角色就能点 (不能点空地)
        Anywhere = Ally | Enemy | Empty        // 随便点哪里都能放 (如：地毯式轰炸)
    }

    [Serializable]
    public class SkillData
    {
        [Header("基础信息")]
        public int skillID;
        public string skillName;
        public string slogan;
        [TextArea(2, 4)]
        public string description;
        public SkillCategory category;

        [Header("核心规则")]
        public SkillTag tags;

        // 【🔥新增】填入该技能允许释放的目标类型
        [Tooltip("允许瞄准的对象 (Ally:友军, Enemy:敌人, Empty:空地)")]
        public SkillTargetMask targetMask = SkillTargetMask.Anywhere;

        public int castRange;
        public int aoeRadius;

        [Header("效果与收益")]
        public SkillEffectType effectType;
        public int baseEffectValue;
        public float effectMultiplier;

        [Header("消耗")]
        public int mpCost;
        public int tuCost;

        [Header("表现层")]
        public string vfxID = "Hit_Fire";
    }

    [CreateAssetMenu(fileName = "SkillTable", menuName = "TGame/技能总表 (Skill Table)")]
    public class SkillTableSO : ScriptableObject
    {
        public SkillData[] skills;
    }
}