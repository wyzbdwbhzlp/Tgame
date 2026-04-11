using System.Collections.Generic;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    public RuntimeUnit LogicUnit { get; private set; }
    private Queue<Vector3> _pathQueue = new Queue<Vector3>();
    private Vector3 _currentTargetPos;
    private bool _isMoving = false;
    public float moveSpeed = 10f; // 稍微提点速，演示更有力

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

        // 将路径格转化为世界坐标
        foreach (var cell in path)
        {
            _pathQueue.Enqueue(GridSystem.Instance.CellToWorld(cell.Position));
        }

        // 如果第一格就是当前站立的位置，直接弹掉，防止原地卡死
        if (_pathQueue.Count > 0 && Vector3.Distance(transform.position, _pathQueue.Peek()) < 0.1f)
        {
            _pathQueue.Dequeue();
        }

        if (_pathQueue.Count > 0)
        {
            _currentTargetPos = _pathQueue.Dequeue();
            _isMoving = true;
            Debug.Log($"[UnitView] 动画启动！目标点: {_currentTargetPos}");
        }
    }

    private void Update()
    {
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _currentTargetPos, Time.deltaTime * moveSpeed);

            if (Vector3.Distance(transform.position, _currentTargetPos) < 0.001f)
            {
                if (_pathQueue.Count > 0)
                {
                    _currentTargetPos = _pathQueue.Dequeue();
                }
                else
                {
                    _isMoving = false;
                    Debug.Log("[UnitView] 动画播放完毕。");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (LogicUnit == null) return;
        Gizmos.color = (LogicUnit.ConfigData.characterID == 1001) ? Color.cyan : Color.red;
        Gizmos.DrawSphere(transform.position, 0.4f);
    }
}