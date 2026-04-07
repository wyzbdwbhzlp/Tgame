using System.Collections.Generic;
using UnityEngine;

public class ActionManager : IGameSystem
{
    public ActionState CurrentState { get; private set; } = ActionState.Idle;

    // 当前正在构建的动作指令
    private IActionCommand _currentCommand;

    // 选中的操作角色 ID
    private int _activeUnitID = -1;

    public void OnInit()
    {
        Debug.Log("[ActionManager] 动作管理器初始化完成。");
    }

    public void OnUpdate(float deltaTime)
    {
        // 这里的 Update 逻辑未来可以用来处理“SelectingTarget”状态下的鼠标悬停射线检测
    }

    public void OnDestroy() { }

    /// <summary>
    /// 开始为特定角色分配动作
    /// </summary>
    public void StartActionSelection(int unitID)
    {
        _activeUnitID = unitID;
        ChangeState(ActionState.SelectingAction);
        Debug.Log($"[ActionManager] 开始为角色 {unitID} 选择动作。");
    }

    /// <summary>
    /// 玩家点击了具体的动作（如：移动、攻击、技能）
    /// </summary>
    public void SetCommand(IActionCommand command)
    {
        if (CurrentState != ActionState.SelectingAction) return;

        _currentCommand = command;
        ChangeState(ActionState.SelectingTarget);
    }

    /// <summary>
    /// 确认并提交当前动作
    /// </summary>
    public void ConfirmAction()
    {
        if (CurrentState != ActionState.SelectingTarget || _currentCommand == null) return;

        if (_currentCommand.Validate())
        {
            ChangeState(ActionState.Confirming);

            // 生成所有 TimelineEvent 数据
            List<TimelineEvent> events = _currentCommand.GenerateEvents();

            Debug.Log($"[ActionManager] 动作确认完毕，生成了 {events.Count} 个时间轴事件块！");

            // 动作结束，回归空闲
            ResetState();
        }
        else
        {
            Debug.LogWarning("[ActionManager] 动作校验失败（可能目标非法或时素不足）。");
        }
    }

    /// <summary>
    /// 取消当前操作（如玩家点击右键退回）
    /// </summary>
    public void CancelAction()
    {
        ResetState();
    }

    private void ChangeState(ActionState newState)
    {
        CurrentState = newState;
    }

    private void ResetState()
    {
        _activeUnitID = -1;
        _currentCommand = null;
        ChangeState(ActionState.Idle);
    }
}