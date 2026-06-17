using UnityEngine;
using TGame.Data;

namespace TGame.Battle
{
    // 定义异常状态枚举
    public enum PostureState
    {
        Normal,      // 正常
        Stagger,     // 破衡
        KnockUp,     // 击飞
        Stun         // 击晕
    }

    public struct DamageResult
    {
        public bool isMiss;             // 是否闪避
        public bool isCrit;             // 是否暴击
        public int finalHPDamage;        // 最终血条伤害 (30% + 溢出)
        public int finalPostureDamage;   // 最终躯干伤害 (70%)
        public PostureState nextState;  // 结算后触发的新状态
    }

    public static class DamageCalculator
    {
        public static DamageResult Calculate(RuntimeUnit attacker, RuntimeUnit defender, SkillData skill, int targetIndex = 0)
        {
            DamageResult result = new DamageResult();
            result.nextState = (PostureState)defender.CurrentState; // 默认维持原状态

            // 1. 闪避判定：c = 技能命中率 a - 敌方闪避率 b
            float hitChance = skill.hitRate - defender.ConfigData.evasionRate;
            if (Random.value > hitChance)
            {
                result.isMiss = true;
                return result;
            }

            // 2. 穿透伤害衰减率 S = 1 - 穿透力 / 100
            float S = 1f - (skill.penetration / 100f);

            // 3. 基础技能伤害 SkillA
            float skillA = skill.baseEffectValue + (attacker.ConfigData.attack * skill.effectMultiplier);

            // 4. 防御力 ED 减伤计算 FirstA
            // 【??核心修正】使用 100 / (ED + 100) 的减伤曲线，确保防御力越高伤害合理减少
            float DA = 100f / (defender.ConfigData.defense + 100f);
            float firstA = skillA * DA;

            // 5. 抗性与弱点判定 SecondA
            float secondA = firstA;
            // 遍历技能的所有 Tag，检查是否触发了受击方的弱点或抗性
            if ((skill.tags & defender.ConfigData.resistance) != 0)
                secondA = firstA * 0.6f;
            else if ((skill.tags & defender.ConfigData.weakness) != 0)
                secondA = firstA * 1.2f;

            // 6. 多人打击穿透衰减 ThirdA (当前经过 n 个敌人，n = targetIndex)
            float thirdA = secondA * Mathf.Pow(1f - S, targetIndex);

            // 7. 特殊状态词条判定 FourA
            float fourA = thirdA;
            PostureState currentDefState = (PostureState)defender.CurrentState;

            // 如果处于“破衡”状态，且技能带有击飞或击晕词条，或者敌人已经处于击飞/击晕状态，额外加成 10%
            bool hasControlTag = (skill.tags & (SkillTag.KnockUp | SkillTag.Stun | SkillTag.KnockDown)) != 0;
            if (currentDefState == PostureState.KnockUp || currentDefState == PostureState.Stun ||
               (currentDefState == PostureState.Stagger && hasControlTag))
            {
                fourA = thirdA * 1.1f; // 对应文档：额外加10%
            }

            // 8. 暴击判定 FinalA
            result.isCrit = Random.value < attacker.ConfigData.critRate;
            float finalA = result.isCrit ? fourA * 1.5f : fourA;

            // 12. 攻击者自身处于异常/特殊状态，伤害和躯干伤害再减去 50%
            if ((PostureState)attacker.CurrentState != PostureState.Normal)
            {
                finalA *= 0.5f;
            }

            // 9 & 10. 数值拆分：躯干损失 Q = FinalA * 0.7，血量伤害 Attack = FinalA * 0.3
            float Q = finalA * 0.7f;
            float attack = finalA * 0.3f;

            // 11. 躯干值扣除与 HP 溢出计算
            int currentPosture = defender.CurrentPosture;
            int finalPostureDmg = Mathf.RoundToInt(Q);
            int finalHPDmg = Mathf.RoundToInt(attack);

            if (currentPosture > 0)
            {
                if (finalPostureDmg >= currentPosture)
                {
                    // 躯干条打空！触发破衡，且剩余伤害计算到 HP 中
                    int overflowPostureDmg = finalPostureDmg - currentPosture;
                    result.finalPostureDamage = currentPosture;
                    result.finalHPDamage = finalHPDmg + overflowPostureDmg; // 躯干值清空后，余伤归入HP

                    // 状态转换判定
                    if (currentDefState == PostureState.Normal)
                    {
                        // 正常状态清空直接进入“破衡”
                        result.nextState = PostureState.Stagger;
                    }
                }
                else
                {
                    // 没打满躯干，正常扣除
                    result.finalPostureDamage = finalPostureDmg;
                    result.finalHPDamage = finalHPDmg;
                }
            }
            else
            {
                // 躯干本就是0，全额伤害转化为 HP 伤害
                result.finalPostureDamage = 0;
                result.finalHPDamage = finalHPDmg + finalPostureDmg;
            }

            // 在破衡状态下，受到特定词条攻击，转入更深层状态
            if (currentDefState == PostureState.Stagger)
            {
                if ((skill.tags & SkillTag.KnockUp) != 0) result.nextState = PostureState.KnockUp;
                else if ((skill.tags & SkillTag.Stun) != 0) result.nextState = PostureState.Stun;
                else if ((skill.tags & SkillTag.KnockDown) != 0) result.nextState = PostureState.KnockUp; // 击倒归入物理异常
            }

            return result;
        }
    }
}