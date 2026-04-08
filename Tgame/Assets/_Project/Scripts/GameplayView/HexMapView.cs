using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HexMapView : MonoBehaviour
{
    private Vector3Int _hoveredCellPos;
    private LineRenderer _pathLineRenderer;

    private Vector3Int _lastHoveredPos = new Vector3Int(999, 999, 999);

    // ==========================================
    // 核心变动：动态选中的实体引用
    // ==========================================
    private RuntimeUnit _selectedUnit = null;

    private void Start()
    {
        _pathLineRenderer = GetComponent<LineRenderer>();
        _pathLineRenderer.positionCount = 0;
        _pathLineRenderer.startWidth = 0.15f;
        _pathLineRenderer.endWidth = 0.15f;
        _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _pathLineRenderer.startColor = Color.cyan;
        _pathLineRenderer.endColor = Color.blue;
        _pathLineRenderer.sortingOrder = 5; // 确保线画在方块下面
    }

    private void Update()
    {
        if (GridSystem.Instance == null || TurnManager.Instance == null) return;

        // 1. 获取鼠标并转换逻辑坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        _hoveredCellPos = GridSystem.Instance.WorldToCell(mouseWorldPos);

        // 2. 鼠标悬停逻辑 (只有选中角色时，才绘制寻路轨迹)
        if (_hoveredCellPos != _lastHoveredPos)
        {
            _lastHoveredPos = _hoveredCellPos;
            if (_selectedUnit != null)
            {
                UpdatePathVisualization();
            }
            else
            {
                _pathLineRenderer.positionCount = 0; // 没选中时清空画线
            }
        }

        // ==========================================
        // 3. 鼠标交互核心逻辑：左键点击
        // ==========================================
        if (Input.GetMouseButtonDown(0))
        {
            GridCell clickedCell = GridSystem.Instance.GetCell(_hoveredCellPos);
            if (clickedCell == null) return;

            if (_selectedUnit == null)
            {
                // 【状态一：空闲】尝试点击格子上的角色
                if (clickedCell.OccupantUnitID != -1)
                {
                    _selectedUnit = UnitManager.Instance.GetUnit(clickedCell.OccupantUnitID);
                    if (_selectedUnit != null)
                    {
                        Debug.Log($"[交互] 👆 选中了单位: 【{_selectedUnit.ConfigData.characterName}】");
                        // 选中后立刻刷新一下当前位置到鼠标的线
                        UpdatePathVisualization();
                    }
                }
            }
            else
            {
                // 【状态二：已选中】尝试对目标格子下达移动指令
                ConfirmMoveAction();
            }
        }

        // ==========================================
        // 4. 鼠标交互补充：右键取消选择
        // ==========================================
        if (Input.GetMouseButtonDown(1))
        {
            if (_selectedUnit != null)
            {
                Debug.Log($"[交互] ❌ 取消选择: 【{_selectedUnit.ConfigData.characterName}】");
                _selectedUnit = null;
                _pathLineRenderer.positionCount = 0;
            }
        }
    }

    private void UpdatePathVisualization()
    {
        if (_selectedUnit == null) return;

        // 注意这里：起点变成了选中角色的真实坐标
        List<GridCell> path = PathfindingService.GetPath(GridSystem.Instance, _selectedUnit.GridPosition, _hoveredCellPos);

        if (path == null || path.Count == 0)
        {
            _pathLineRenderer.positionCount = 0;
            return;
        }

        _pathLineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 worldPoint = GridSystem.Instance.CellToWorld(path[i].Position);
            worldPoint.z = 0f;
            _pathLineRenderer.SetPosition(i, worldPoint);
        }
    }

    private void ConfirmMoveAction()
    {
        GridCell targetCell = GridSystem.Instance.GetCell(_hoveredCellPos);
        if (targetCell == null || !targetCell.CanEnter())
        {
            Debug.LogWarning("[交互] 目标格子不可达或已被占用！");
            return;
        }

        MoveCommand moveCmd = new MoveCommand(
            _selectedUnit.InstanceID,
            _selectedUnit.GridPosition,
            _hoveredCellPos,
            GridSystem.Instance,
            TurnManager.Instance
        );

        if (moveCmd.Validate())
        {
            // ==========================================
            // 魔法在此：在改变逻辑坐标前，拿到底层的 A* 路径
            // 然后立刻通知表现层的 2D 方块去播放动画！
            // ==========================================
            List<GridCell> path = PathfindingService.GetPath(GridSystem.Instance, _selectedUnit.GridPosition, _hoveredCellPos);
            if (UnitViewManager.Instance != null && path != null)
            {
                UnitView view = UnitViewManager.Instance.GetView(_selectedUnit.InstanceID);
                if (view != null)
                {
                    view.MoveAlongPath(path);
                }
            }

            // ------------------------------------------------

            List<TimelineEvent> generatedEvents = moveCmd.GenerateEvents();
            Debug.Log($"✅ [动作系统] 移动指令生效！{_selectedUnit.ConfigData.characterName} 前往: {_hoveredCellPos}");

            // 逻辑层：底层瞬间完成数据移交
            GridCell oldCell = GridSystem.Instance.GetCell(_selectedUnit.GridPosition);
            oldCell.OccupantUnitID = -1;

            _selectedUnit.GridPosition = _hoveredCellPos;
            targetCell.OccupantUnitID = _selectedUnit.InstanceID;

            // 走完之后自动取消选中状态
            _selectedUnit = null;
            _pathLineRenderer.positionCount = 0;
        }
        else
        {
            Debug.LogWarning("❌ [动作系统] 时素不足或路径无法到达。");
        }
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || GridSystem.Instance == null) return;

        int mapRadius = 5;
        for (int x = -mapRadius; x <= mapRadius; x++)
        {
            int y1 = Mathf.Max(-mapRadius, -x - mapRadius);
            int y2 = Mathf.Min(mapRadius, -x + mapRadius);
            for (int y = y1; y <= y2; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, -x - y);
                Vector3 centerPos = GridSystem.Instance.CellToWorld(cellPos);
                GridCell cellData = GridSystem.Instance.GetCell(cellPos);

                Gizmos.color = (cellData != null && !cellData.IsWalkable) ? Color.black : Color.white;
                DrawHexagonGizmo(centerPos, GridSystem.Instance.HexSize);
            }
        }

        // 高亮选中角色的底格为绿色
        if (_selectedUnit != null)
        {
            Gizmos.color = Color.green;
            DrawHexagonGizmo(GridSystem.Instance.CellToWorld(_selectedUnit.GridPosition), GridSystem.Instance.HexSize);
        }

        Gizmos.color = Color.red;
        DrawHexagonGizmo(GridSystem.Instance.CellToWorld(_hoveredCellPos), GridSystem.Instance.HexSize);
    }

    private void DrawHexagonGizmo(Vector3 center, float size)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle_rad = Mathf.PI / 180 * (60 * i - 30);
            corners[i] = new Vector3(center.x + size * Mathf.Cos(angle_rad), center.y + size * Mathf.Sin(angle_rad), 0);
        }
        for (int i = 0; i < 6; i++) Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
    }
}