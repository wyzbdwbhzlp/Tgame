using UnityEngine;
using TGame.Data; // 引入导表数据命名空间

public class RuntimeUnit
{
    // ================= 核心标识 =================
    public int InstanceID { get; private set; } // 场上唯一标识符 (防止同名怪物冲突)
    public CharacterDataSO ConfigData { get; private set; } // 指向静态配置表的引用
    public Vector3Int GridPosition { get; set; } // 当前所在的六边形逻辑坐标

    // ================= 运行时动态数值 =================
    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public int CurrentPosture { get; private set; } // 当前躯干值

    public RuntimeUnit(int instanceID, CharacterDataSO config, Vector3Int startPos)
    {
        InstanceID = instanceID;
        ConfigData = config;
        GridPosition = startPos;

        // 角色生成时，以配置表中的最大值为准，初始化状态
        CurrentHP = config.maxHP;
        CurrentMP = config.maxMP;
        CurrentPosture = config.postureValue;
    }

    /// <summary>
    /// 受到伤害的方法示例
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        CurrentHP -= damageAmount;
        if (CurrentHP < 0) CurrentHP = 0;

        Debug.Log($"[实体状态] {ConfigData.characterName} 受到了 {damageAmount} 点伤害！剩余 HP: {CurrentHP}");

        if (CurrentHP == 0)
        {
            Debug.Log($"[实体状态] {ConfigData.characterName} 阵亡！");
            // 这里未来可以触发 OnUnitDead 事件
        }
    }
}