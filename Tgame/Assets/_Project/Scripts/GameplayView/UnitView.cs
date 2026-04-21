using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;

public class UnitView : MonoBehaviour
{
    public RuntimeUnit LogicUnit { get; private set; }
    private Queue<Vector3> _pathQueue = new Queue<Vector3>();
    private Vector3 _currentTargetPos;
    private bool _isMoving = false;
    public float moveSpeed = 10f;

    // 【删除】不再需要 tuFillTransform，彻底和 UI 解耦

    public void Init(RuntimeUnit logicUnit)
    {
        LogicUnit = logicUnit;
        if (GridSystem.Instance != null)
            transform.position = GridSystem.Instance.CellToWorld(LogicUnit.GridPosition);
    }

    public void MoveAlongPath(List<GridCell> path)
    {
        if (path == null || path.Count == 0) return;
        if (_pathQueue == null) _pathQueue = new Queue<Vector3>();
        _pathQueue.Clear();
        foreach (var cell in path) _pathQueue.Enqueue(GridSystem.Instance.CellToWorld(cell.Position));
        if (_pathQueue.Count > 0 && Vector3.Distance(transform.position, _pathQueue.Peek()) < 0.1f) _pathQueue.Dequeue();

        if (_pathQueue.Count > 0)
        {
            _currentTargetPos = _pathQueue.Dequeue();
            _isMoving = true;
        }
    }

    private void Update()
    {
        // 只有纯粹的移动表现逻辑
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _currentTargetPos, Time.deltaTime * moveSpeed);
            if (Vector3.Distance(transform.position, _currentTargetPos) < 0.001f)
            {
                if (_pathQueue.Count > 0) _currentTargetPos = _pathQueue.Dequeue();
                else _isMoving = false;
            }
        }
    }
}