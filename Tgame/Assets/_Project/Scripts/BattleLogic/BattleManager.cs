using Unity.VisualScripting;
using UnityEngine;

public class BattleManager : IGameSystem
{
    public BattleState CurrentState { get; private set; } = BattleState.None;

    public void OnInit()
    {
        // 订阅时间轴确认事件：当玩家排好时间轴点击确认时，切入执行状态
        EventBus.Subscribe<TimelineConfirmedEvent>(OnTimelineConfirmed);

        Debug.Log("[BattleManager] 战斗控制器初始化完成。");
    }

    public void OnUpdate(float deltaTime)
    {
        // 根据当前状态执行不同的轮询逻辑 (如果有需要持续每帧检测的逻辑)
        switch (CurrentState)
        {
            case BattleState.Planning:
                // 等待玩家输入或 UI 操作...
                break;
            case BattleState.Execution:
                // 轮询 TimelinePlayer，检查是否播放完毕...
                break;
            case BattleState.EnemyTurn:
                // 轮询敌方 AI 决策...
                break;
        }
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<TimelineConfirmedEvent>(OnTimelineConfirmed);
    }

    /// <summary>
    /// 启动整场战斗的主入口
    /// </summary>
    public void StartBattle()
    {
        Debug.Log("=================================");
        Debug.Log("[BattleManager] 战斗正式开始！");
        Debug.Log("=================================");

        ChangeState(BattleState.Planning);
    }

    /// <summary>
    /// 核心状态流转方法
    /// </summary>
    private void ChangeState(BattleState newState)
    {
        if (CurrentState == newState) return;

        BattleState prevState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[BattleManager] 战斗状态流转: {prevState} -> {CurrentState}");
        EventBus.Publish(new BattleStateChangeEvent { PreviousState = prevState, CurrentState = CurrentState });

        // 执行进入新状态的初始化逻辑
        OnEnterState(CurrentState);
    }

    private void OnEnterState(BattleState state)
    {
        switch (state)
        {
            case BattleState.Planning:
                // 通知 TurnManager 增加回合数，重置时素
                // 通知 TimelineUI 显示可交互界面
                break;

            case BattleState.Execution:
                // 锁定 UI，开始解析 TimelineData 并触发角色表现
                Debug.Log("[BattleManager] 锁定时间轴，开始执行玩家指令！");
                // 临时模拟：假设执行了 2 秒后结束
                // 实际项目中应由 TimelinePlayer 播放完毕后回调 ChangeState(BattleState.EnemyTurn)
                break;

            case BattleState.EnemyTurn:
                Debug.Log("[BattleManager] 轮到敌方行动！");
                // 触发敌方 AI 逻辑...
                break;

            case BattleState.Settlement:
                Debug.Log("[BattleManager] 本回合结束，进行状态结算...");
                // 结算持续性伤害、躯干值恢复等...
                break;
        }
    }

    // ================= 事件回调 =================

    private void OnTimelineConfirmed(TimelineConfirmedEvent evt)
    {
        if (CurrentState == BattleState.Planning)
        {
            ChangeState(BattleState.Execution);
        }
        else
        {
            Debug.LogError("[BattleManager] 当前非规划阶段，无法确认时间轴！");
        }
    }
}