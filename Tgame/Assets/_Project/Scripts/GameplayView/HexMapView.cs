using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;

[RequireComponent(typeof(LineRenderer))]
public class HexMapView : MonoBehaviour
{
    private Vector3Int _hoveredCellPos;
    private LineRenderer _pathLineRenderer;
    private Vector3Int _lastHoveredPos = new Vector3Int(999, 999, 999);
    private RuntimeUnit _selectedUnit = null;

    // 存放生成的网格显示物体，方便后续管理
    private Dictionary<Vector3Int, GameObject> _cellVisuals = new Dictionary<Vector3Int, GameObject>();

    private void Start()
    {
        // 1. 初始化路径画线 (LineRenderer)
        _pathLineRenderer = GetComponent<LineRenderer>();
        _pathLineRenderer.positionCount = 0;
        _pathLineRenderer.startWidth = 0.08f;
        _pathLineRenderer.endWidth = 0.08f;
        _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _pathLineRenderer.startColor = Color.yellow;
        _pathLineRenderer.endColor = Color.white;
        _pathLineRenderer.sortingOrder = 5;

        // 2. 核心：生成 Game 视角可见的网格
        // 我们等一小会儿确保 GridSystem 已经 LoadLevel 完成
        Invoke("CreateGridVisuals", 0.2f);
    }

    private void CreateGridVisuals()
    {
        if (GridSystem.Instance == null) return;

        // 生成一张六边形边框贴图 (由代码动态生成，省去美术资源)
        Sprite hexSprite = CreateHexFrameSprite();

        // 遍历逻辑层的所有格子
        // 注意：如果你之前的 GridSystem 没有提供获取所有格子的公开方法，
        // 我们暂时通过半径暴力遍历（和你 OnDrawGizmos 逻辑一致）
        int radius = 5;
        for (int x = -radius; x <= radius; x++)
        {
            int y1 = Mathf.Max(-radius, -x - radius);
            int y2 = Mathf.Min(radius, -x + radius);
            for (int y = y1; y <= y2; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, -x - y);
                GridCell cellData = GridSystem.Instance.GetCell(pos);
                if (cellData == null) continue;

                // 创建显示物体
                GameObject cellObj = new GameObject($"Cell_{x}_{y}");
                cellObj.transform.SetParent(this.transform);
                cellObj.transform.position = GridSystem.Instance.CellToWorld(pos);

                SpriteRenderer sr = cellObj.AddComponent<SpriteRenderer>();
                sr.sprite = hexSprite;

                // 设置颜色：障碍物黑色，空地半透明白色边框
                sr.color = cellData.IsWalkable ? new Color(1, 1, 1, 0.3f) : new Color(0, 0, 0, 0.8f);
                sr.sortingOrder = 1; // 确保在最底层

                _cellVisuals[pos] = cellObj;
            }
        }
        Debug.Log($"[HexMapView] Game视图网格渲染完毕，共生成 {_cellVisuals.Count} 个格视觉体。");
    }

    // --- 动态生成六边形边框贴图的魔法函数 ---
    private Sprite CreateHexFrameSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);

        // 先填充透明
        for (int i = 0; i < size * size; i++) tex.SetPixel(i % size, i / size, transparent);

        // 计算六边形的 6 个点
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float r = size * 0.48f;
        Vector2[] points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60 * i - 30);
            points[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        // 画线段（简单的像素画法）
        for (int i = 0; i < 6; i++) DrawLine(tex, points[i], points[(i + 1) % 6], Color.white);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f / (GridSystem.Instance.HexSize * 2f));
    }

    private void DrawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col)
    {
        float dist = Vector2.Distance(p1, p2);
        for (float t = 0; t < 1; t += 1f / dist)
        {
            Vector2 p = Vector2.Lerp(p1, p2, t);
            tex.SetPixel((int)p.x, (int)p.y, col);
            // 加粗一点
            tex.SetPixel((int)p.x + 1, (int)p.y, col);
            tex.SetPixel((int)p.x, (int)p.y + 1, col);
        }
    }

    private void Update()
    {
        if (GridSystem.Instance == null || TurnManager.Instance == null) return;
        if (TurnManager.Instance.CurrentState != TGame.Battle.BattleState.Planning)
        {
            _pathLineRenderer.positionCount = 0;
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        _hoveredCellPos = GridSystem.Instance.WorldToCell(mouseWorldPos);

        if (_hoveredCellPos != _lastHoveredPos)
        {
            _lastHoveredPos = _hoveredCellPos;
            if (_selectedUnit != null) UpdatePathVisualization();
            else _pathLineRenderer.positionCount = 0;
        }

        if (Input.GetMouseButtonDown(0)) HandleLeftClick();
        if (Input.GetMouseButtonDown(1)) CancelSelection();
    }

    private void HandleLeftClick()
    {
        GridCell clickedCell = GridSystem.Instance.GetCell(_hoveredCellPos);
        if (clickedCell == null) return;

        if (_selectedUnit == null)
        {
            if (clickedCell.OccupantUnitID != -1)
            {
                _selectedUnit = UnitManager.Instance.GetUnit(clickedCell.OccupantUnitID);
                if (_selectedUnit != null) Debug.Log($"[交互] 选中: {_selectedUnit.ConfigData.characterName}");
            }
        }
        else
        {
            MoveCommand moveCmd = new MoveCommand(_selectedUnit.InstanceID, _selectedUnit.GridPosition, _hoveredCellPos);
            if (moveCmd.Validate())
            {
                TurnManager.Instance.AddCommand(moveCmd);
                CancelSelection();
            }
            else
            {
                Debug.LogWarning("[交互] TU不足！");
            }
        }
    }

    private void CancelSelection()
    {
        _selectedUnit = null;
        _pathLineRenderer.positionCount = 0;
    }

    private void UpdatePathVisualization()
    {
        if (_selectedUnit == null) return;
        List<GridCell> path = PathfindingService.GetPath(GridSystem.Instance, _selectedUnit.GridPosition, _hoveredCellPos);
        if (path == null || path.Count <= 1)
        {
            _pathLineRenderer.positionCount = 0;
            return;
        }
        _pathLineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            _pathLineRenderer.SetPosition(i, GridSystem.Instance.CellToWorld(path[i].Position));
        }
    }

    // 依然保留 Gizmos，双重保障
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || GridSystem.Instance == null) return;
        if (UnitManager.Instance != null)
        {
            foreach (var unit in UnitManager.Instance.GetAllUnits())
            {
                Vector3 pos = GridSystem.Instance.CellToWorld(unit.GridPosition);
                Gizmos.color = (unit.ConfigData.characterID == 1001) ? Color.cyan : Color.red;
                Gizmos.DrawSphere(pos, 0.3f);
            }
        }
    }
}