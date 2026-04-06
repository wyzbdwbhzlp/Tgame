using System;

// 战斗阶段状态枚举
public enum BattleState
{
    None,           // 初始化前
    Planning,       // 规划阶段 (玩家操作时间轴)
    Execution,      // 执行阶段 (按照时间轴播放动作)
    EnemyTurn,      // 敌方回合
    Settlement      // 结算阶段 (计算Buff/Debuff/胜负)
}

// ================= 全局战斗事件定义 =================
// 采用之前设计的强类型 EventBus 进行派发

public struct TurnStartEvent : IEvent
{
    public int TurnCount;
}

public struct BattleStateChangeEvent : IEvent
{
    public BattleState PreviousState;
    public BattleState CurrentState;
}

// 当玩家在时间轴点击“确认执行”时派发
public struct TimelineConfirmedEvent : IEvent
{
    // 这里未来会携带具体的 TimelineData
}