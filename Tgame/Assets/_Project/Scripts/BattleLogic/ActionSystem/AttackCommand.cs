using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackCommand : IActionCommand
{
    public int targetID;
    public bool isComposite;

    private int _unitID;
    private Vector3Int _startPos;
    private Vector3Int _targetPos;

    private GridSystem _gridSystem;
    private TurnManager _turnManager;

    private List<GridCell> _calculatedPath;
    private int _moveTimeCost = 0;
    private int _attackTimeCost = 7;

    public AttackCommand(int unitID, int targetID, Vector3Int startPos, Vector3Int targetPos, GridSystem gridSystem, TurnManager turnManager)
    {
        _unitID = unitID;
        this.targetID = targetID;
        _startPos = startPos;
        _targetPos = targetPos;
        _gridSystem = gridSystem;
        _turnManager = turnManager;
    }

    public bool Validate()
    {
        int attackRange = 1;
        int distance = CalculateHexDistance(_startPos, _targetPos);

        if (distance <= attackRange)
        {
            isComposite = false;
            _moveTimeCost = 0;
            return _turnManager.CanScheduleAction(_attackTimeCost);
        }
        else
        {
            isComposite = true;

            GridCell attackStandCell = FindNearestWalkableNeighbor(_startPos, _targetPos);
            if (attackStandCell == null)
            {
                Debug.LogWarning("[AttackCommand] 目标周围没有可站立的地块！");
                return false;
            }

            _calculatedPath = PathfindingService.GetPath(_gridSystem, _startPos, attackStandCell.Position);
            if (_calculatedPath == null || _calculatedPath.Count == 0) return false;

            _moveTimeCost = PathfindingService.CalculateTotalMoveCost(_calculatedPath);

            return _turnManager.CanScheduleAction(_moveTimeCost + _attackTimeCost);
        }
    }

    public void Execute()
    {
    }

    public List<TimelineEvent> GenerateEvents()
    {
        List<TimelineEvent> events = new List<TimelineEvent>();
        float currentStartTime = _turnManager.CurrentTimeUnitsUsed;

        if (isComposite)
        {
            events.Add(new TimelineEvent
            {
                unitID = _unitID,
                actionType = ActionType.Move,
                startTime = currentStartTime,
                duration = _moveTimeCost,
                targetGrid = _calculatedPath[_calculatedPath.Count - 1].Position,
                targetID = -1,
                isComposite = true
            });
            currentStartTime += _moveTimeCost;
        }

        events.Add(new TimelineEvent
        {
            unitID = _unitID,
            actionType = ActionType.Attack,
            startTime = currentStartTime,
            duration = _attackTimeCost,
            targetGrid = _targetPos,
            targetID = targetID,
            isComposite = isComposite
        });

        return events;
    }

    private int CalculateHexDistance(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
    }

    private GridCell FindNearestWalkableNeighbor(Vector3Int start, Vector3Int target)
    {
        GridCell targetCell = _gridSystem.GetCell(target);
        if (targetCell == null) return null;

        List<GridCell> neighbors = _gridSystem.GetNeighbors(targetCell);
        GridCell bestCell = null;
        int minDistance = int.MaxValue;

        foreach (var cell in neighbors)
        {
            if (cell.CanEnter())
            {
                int dist = CalculateHexDistance(start, cell.Position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCell = cell;
                }
            }
        }
        return bestCell;
    }
}