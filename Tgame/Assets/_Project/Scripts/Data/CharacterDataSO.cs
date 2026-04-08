using UnityEngine;

namespace TGame.Data
{
    // 这行代码让你可以在 Unity 菜单中直接右键创建这个 SO（虽然我们主要用工具自动生成）
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "TGame/Character Data")]
    public class CharacterDataSO : ScriptableObject
    {
        [Header("基础信息")]
        public int characterID;
        public string characterName;

        [Header("战斗属性")]
        public int maxHP;
        public int maxMP;
        public int attack;
        public int defense;
        public int speed;

        [Header("高级机制")]
        public int postureValue; // 躯干值
        public float dodgeRate;  // 闪避率
        public float critRate;   // 暴击率
    }
}