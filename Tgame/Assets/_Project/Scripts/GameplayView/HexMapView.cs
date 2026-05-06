using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;

[RequireComponent(typeof(LineRenderer))]
public class HexMapView : MonoBehaviour
{
    public static HexMapView Instance { get; private set; }

    private Vector3Int _hoveredCellPos;
    private LineRenderer _pathLineRenderer;
    private Vector3Int _lastHoveredPos = new Vector3Int(999, 999, 999);

    private RuntimeUnit _selectedUnit = null;
    public RuntimeUnit SelectedUnit => _selectedUnit;

    private Dictionary<Vector3Int, GameObject> _cellVisuals = new Dictionary<Vector3Int, GameObject>();

    private GameObject _phantomObj;

    private void Awake() { Instance = this; }

    private void Start()
    {
        _pathLineRenderer = GetComponent<LineRenderer>();
        _pathLineRenderer.positionCount = 0;
        _pathLineRenderer.startWidth = 0.08f;
        _pathLineRenderer.endWidth = 0.08f;
        _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _pathLineRenderer.startColor = Color.yellow;
        _pathLineRenderer.endColor = Color.white;
        _pathLineRenderer.sortingOrder = 5;

        Invoke("CreateGridVisuals", 0.2f);
    }

    private void CreateGridVisuals()
    {
        if (GridSystem.Instance == null) return;
        Sprite hexSprite = CreateHexFrameSprite();
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

                GameObject cellObj = new GameObject($"Cell_{x}_{y}");
                cellObj.transform.SetParent(this.transform);
                cellObj.transform.position = GridSystem.Instance.CellToWorld(pos);

                SpriteRenderer sr = cellObj.AddComponent<SpriteRenderer>();
                sr.sprite = hexSprite;
                sr.color = cellData.IsWalkable ? new Color(1, 1, 1, 0.3f) : new Color(0, 0, 0, 0.8f);
                sr.sortingOrder = 1;

                _cellVisuals[pos] = cellObj;
            }
        }
    }

    private Sprite CreateHexFrameSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);
        for (int i = 0; i < size * size; i++) tex.SetPixel(i % size, i / size, transparent);

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float r = size * 0.48f;
        Vector2[] points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60 * i - 30);
            points[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }
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
            tex.SetPixel((int)p.x + 1, (int)p.y, col);
            tex.SetPixel((int)p.x, (int)p.y + 1, col);
        }
    }

    private void Update()
    {
        if (GridSystem.Instance == null || TurnManager.Instance == null) return;
        if (TurnManager.Instance.CurrentState != TGame.Battle.BattleState.Planning)
        {
            if (_pathLineRenderer != null) _pathLineRenderer.positionCount = 0;
            return;
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        _hoveredCellPos = GridSystem.Instance.WorldToCell(mouseWorldPos);

        if (_hoveredCellPos != _lastHoveredPos)
        {
            _lastHoveredPos = _hoveredCellPos;

            if (_selectedUnit != null && _phantomObj == null)
            {
                UpdatePathVisualization();
            }
            else if (_phantomObj == null)
            {
                _pathLineRenderer.positionCount = 0;
            }
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
            // 【🔥核心修复】防止玩家点击自己站立的格子，产生 0 消耗的无限虚影
            if (_hoveredCellPos == _selectedUnit.GridPosition)
            {
                Debug.LogWarning("[交互] 无法在原地进行移动规划！");
                return;
            }

            MoveCommand moveCmd = new MoveCommand(_selectedUnit.InstanceID, _selectedUnit.GridPosition, _hoveredCellPos);
            if (moveCmd.Validate())
            {
                TurnManager.Instance.AddCommand(moveCmd);
            }
            else
            {
                Debug.LogWarning("[交互] 该角色剩余时素不足或路径不可达！");
            }
        }
    }

    private void CancelSelection()
    {
        _selectedUnit = null;
        if (_phantomObj == null) _pathLineRenderer.positionCount = 0;
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
        for (int i = 0; i < path.Count; i++) _pathLineRenderer.SetPosition(i, GridSystem.Instance.CellToWorld(path[i].Position));
    }

    public void ShowPhantom(int unitID, Vector3Int targetPos, List<GridCell> path)
    {
        ClearPhantom();

        var unit = UnitManager.Instance.GetUnit(unitID);
        if (unit == null || unit.ConfigData.characterPrefab == null) return;

        _phantomObj = Instantiate(unit.ConfigData.characterPrefab);
        _phantomObj.name = $"[Phantom] {unit.ConfigData.characterName}";
        _phantomObj.transform.position = GridSystem.Instance.CellToWorld(targetPos);

        var unitView = _phantomObj.GetComponent<UnitView>();
        if (unitView != null) Destroy(unitView);

        SpriteRenderer[] renderers = _phantomObj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
            sr.sortingOrder = 15;
        }

        if (path != null && path.Count > 0)
        {
            _pathLineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                _pathLineRenderer.SetPosition(i, GridSystem.Instance.CellToWorld(path[i].Position));
            }
        }
    }

    public void ClearPhantom()
    {
        if (_phantomObj != null) Destroy(_phantomObj);
        if (_pathLineRenderer != null) _pathLineRenderer.positionCount = 0;
    }

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