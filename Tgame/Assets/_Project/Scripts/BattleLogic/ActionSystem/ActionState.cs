public enum ActionState
{
    Idle,               // 空闲状态 (等待选择角色)
    SelectingAction,    // 正在选择动作 (如打开了技能菜单)
    SelectingTarget,    // 正在选择目标 (如鼠标在地图上悬停选格子)
    Confirming          // 确认阶段 (准备生成 TimelineEvent)
}