using Unity.VisualScripting;
using UnityEngine;

public class TurnManager : IGameSystem
{
    public int CurrentTurn { get; private set; }
    public int MaxTimeUnitsPerTurn { get; private set; } = 13; // 默认 13 时素
    public int CurrentTimeUnitsUsed { get; private set; }

    public void OnInit()
    {
        CurrentTurn = 0;
        CurrentTimeUnitsUsed = 0;
        Debug.Log("[TurnManager] 初始化完成，准备就绪。");
    }

    public void OnUpdate(float deltaTime)
    {
        // 回合管理主要依靠事件驱动，Update 留空
    }

    public void OnDestroy()
    {
        // 清理逻辑
    }

    /// <summary>
    /// 开始新的一回合
    /// </summary>
    public void StartNewTurn()
    {
        CurrentTurn++;
        CurrentTimeUnitsUsed = 0;

        Debug.Log($"[TurnManager] 第 {CurrentTurn} 回合开始！");
        EventBus.Publish(new TurnStartEvent { TurnCount = CurrentTurn });
    }

    /// <summary>
    /// 校验安排的动作是否超出了当前回合的时素上限
    /// </summary>
    public bool CanScheduleAction(int durationUnits)
    {
        return (CurrentTimeUnitsUsed + durationUnits) <= MaxTimeUnitsPerTurn;
    }

    /// <summary>
    /// 消耗当前回合的时素 (通常在解析时间轴时调用)
    /// </summary>
    public void ConsumeTimeUnits(int units)
    {
        CurrentTimeUnitsUsed += units;
        if (CurrentTimeUnitsUsed > MaxTimeUnitsPerTurn)
        {
            CurrentTimeUnitsUsed = MaxTimeUnitsPerTurn;
        }
    }
}