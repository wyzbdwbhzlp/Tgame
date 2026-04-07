using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : IActionCommand
{
    private int _unitID;
    private Vector3Int _startPos;
    private Vector3Int _targetPos;

    // 依赖注入：需要网格系统和回合管理器的数据支持
    private GridSystem _gridSystem;
    private TurnManager _turnManager;

    // 缓存计算好的路径
    private List<GridCell> _calculatedPath;
    private int _requiredTimeUnits;

    public MoveCommand(int unitID, Vector3Int startPos, Vector3Int targetPos, GridSystem gridSystem, TurnManager turnManager)
    {
        _unitID = unitID;
        _startPos = startPos;
        _targetPos = targetPos;
        _gridSystem = gridSystem;
        _turnManager = turnManager;
    }

    /// <summary>
    /// 校验这个移动指令是否合法
    /// </summary>
    public bool Validate()
    {
        // 1. 调用昨天写的 A* 寻路获取路径
        _calculatedPath = PathfindingService.GetPath(_gridSystem, _startPos, _targetPos);

        if (_calculatedPath == null || _calculatedPath.Count == 0)
        {
            Debug.LogWarning("无法到达目标地点！");
            return false;
        }

        // 2. 根据地形消耗计算需要的总时素
        _requiredTimeUnits = PathfindingService.CalculateTotalMoveCost(_calculatedPath);

        // 3. 校验本回合剩余时素是否足够
        if (!_turnManager.CanScheduleAction(_requiredTimeUnits))
        {
            Debug.LogWarning($"时素不足！需要 {_requiredTimeUnits} 时素，剩余不够。");
            return false;
        }

        return true;
    }

    public void Execute()
    {
        // 纯逻辑层的 Command 通常不直接执行动画表现，而是交由后续的 Timeline 解析器执行
    }

    /// <summary>
    /// 转换成时间轴系统能识别的标准化数据
    /// </summary>
    public List<TimelineEvent> GenerateEvents()
    {
        List<TimelineEvent> events = new List<TimelineEvent>();

        TimelineEvent moveEvent = new TimelineEvent
        {
            unitID = _unitID,
            actionType = ActionType.Move,
            // 开始时间 = 当前回合已经用掉的时素
            startTime = _turnManager.CurrentTimeUnitsUsed,
            duration = _requiredTimeUnits,
            targetGrid = _targetPos,
            targetID = -1,
            isComposite = false
        };

        events.Add(moveEvent);
        return events;
    }
}