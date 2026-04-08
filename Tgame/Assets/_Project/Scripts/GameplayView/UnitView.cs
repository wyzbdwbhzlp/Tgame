using System.Collections.Generic;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    public RuntimeUnit LogicUnit { get; private set; }

    // ==========================================
    // 动画控制核心变量
    // ==========================================
    private Queue<Vector3> _pathQueue = new Queue<Vector3>();
    private Vector3 _currentTargetPos;
    private bool _isMoving = false;
    public float moveSpeed = 8f; // 角色走路的速度

    public void Init(RuntimeUnit logicUnit)
    {
        LogicUnit = logicUnit;
        transform.position = GridSystem.Instance.CellToWorld(LogicUnit.GridPosition);
    }

    /// <summary>
    /// 接收寻路路径，并开始逐格移动
    /// </summary>
    public void MoveAlongPath(List<GridCell> path)
    {
        _pathQueue.Clear();

        // 将逻辑格子转化为世界坐标并排队
        foreach (var cell in path)
        {
            _pathQueue.Enqueue(GridSystem.Instance.CellToWorld(cell.Position));
        }

        // 如果有路要走，启动移动状态机
        if (_pathQueue.Count > 0)
        {
            _currentTargetPos = _pathQueue.Dequeue();
            _isMoving = true;
        }
    }

    private void Update()
    {
        if (_isMoving)
        {
            // 使用匀速直线运动，走向当前队列中的目标点
            transform.position = Vector3.MoveTowards(transform.position, _currentTargetPos, Time.deltaTime * moveSpeed);

            // 如果到达了当前节点（误差小于0.01）
            if (Vector3.Distance(transform.position, _currentTargetPos) < 0.01f)
            {
                if (_pathQueue.Count > 0)
                {
                    // 还有下一格，拿出来继续走
                    _currentTargetPos = _pathQueue.Dequeue();
                }
                else
                {
                    // 队列走完了，停止移动，并精准吸附到最终位置防止浮点数误差
                    _isMoving = false;
                    transform.position = GridSystem.Instance.CellToWorld(LogicUnit.GridPosition);
                }
            }
        }
    }
}