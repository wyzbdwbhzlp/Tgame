using UnityEngine;

public class TurnManager : IGameSystem
{
    // ==========================================
    // 核心修复：加上单例访问器，让表现层能拿到时素数据
    // ==========================================
    public static TurnManager Instance { get; private set; }

    // 当前回合已经使用的时素
    public int CurrentTimeUnitsUsed { get; private set; } = 0;

    // 按照设定，每回合最大 13 时素
    public int MaxTimeUnitsPerTurn { get; private set; } = 13;

    public void OnInit()
    {
        // 初始化时赋值单例
        Instance = this;
        Debug.Log("[TurnManager] 初始化完成，准备就绪。");
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 校验本回合剩余时素是否足够执行该动作
    /// </summary>
    public bool CanScheduleAction(int requiredTime)
    {
        return (CurrentTimeUnitsUsed + requiredTime) <= MaxTimeUnitsPerTurn;
    }

    /// <summary>
    /// 正式扣除时素（在真正执行动作后调用）
    /// </summary>
    public void AdvanceTime(int timeCost)
    {
        CurrentTimeUnitsUsed += timeCost;
        Debug.Log($"[TurnManager] 消耗了 {timeCost} 时素，当前回合已用: {CurrentTimeUnitsUsed}/{MaxTimeUnitsPerTurn}");
    }

    /// <summary>
    /// 回合结束，重置时素
    /// </summary>
    public void ResetTurn()
    {
        CurrentTimeUnitsUsed = 0;
        Debug.Log("[TurnManager] 回合重置，时素恢复。");
    }
}