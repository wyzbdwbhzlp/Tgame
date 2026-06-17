using System;

namespace TGame.Data
{
    public enum ElementType { Physical, Fire, Ice, Thunder, Light, Dark }

    public enum AbnormalState { None, Stagger, Knockback, Stun } // Õý³£, ÆÆºâ, »÷·É, »÷ÔÎ

    [Flags]
    public enum SkillTags
    {
        None = 0,
        Knockback = 1 << 0,  // »÷·É´ÊÌõ
        Stun = 1 << 1        // »÷ÔÎ´ÊÌõ
    }
}