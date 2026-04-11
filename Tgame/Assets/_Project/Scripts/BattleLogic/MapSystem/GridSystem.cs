using System.Collections.Generic;
using UnityEngine;
using TGame.Data;

public class GridSystem : IGameSystem
{
    public static GridSystem Instance { get; private set; }
    private readonly Dictionary<Vector3Int, GridCell> _grid = new Dictionary<Vector3Int, GridCell>();
    public float HexSize { get; private set; } = 1.0f;

    public void OnInit()
    {
        Instance = this;
        Debug.Log("[GridSystem] 就绪。");
    }

    public void OnUpdate(float deltaTime) { }
    public void OnDestroy()
    {
        _grid.Clear();
        if (Instance == this) Instance = null;
    }

    public void LoadLevel(LevelDataSO levelData)
    {
        _grid.Clear();
        for (int x = -levelData.mapRadius; x <= levelData.mapRadius; x++)
        {
            int y1 = Mathf.Max(-levelData.mapRadius, -x - levelData.mapRadius);
            int y2 = Mathf.Min(levelData.mapRadius, -x + levelData.mapRadius);
            for (int y = y1; y <= y2; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, -x - y);
                _grid[pos] = new GridCell(pos, true, 1);
            }
        }

        foreach (var obsPos in levelData.obstacles)
        {
            if (_grid.TryGetValue(obsPos, out GridCell cell)) cell.IsWalkable = false;
        }
    }

    public GridCell GetCell(Vector3Int cellPosition)
    {
        _grid.TryGetValue(cellPosition, out GridCell cell);
        return cell;
    }

    // ==========================================
    // 核心修复：把被我不小心漏掉的 GetNeighbors 补回来！
    // 这是 PathfindingService (A*寻路) 必须依赖的方法
    // ==========================================
    public List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new List<GridCell>();
        Vector3Int[] directions = {
            new Vector3Int(1, -1, 0), new Vector3Int(1, 0, -1), new Vector3Int(0, 1, -1),
            new Vector3Int(-1, 1, 0), new Vector3Int(-1, 0, 1), new Vector3Int(0, -1, 1)
        };

        foreach (var dir in directions)
        {
            GridCell neighbor = GetCell(cell.Position + dir);
            if (neighbor != null) neighbors.Add(neighbor);
        }
        return neighbors;
    }

    public Vector3 CellToWorld(Vector3Int hexPos)
    {
        float xWorld = HexSize * Mathf.Sqrt(3f) * (hexPos.x + hexPos.y / 2f);
        float yWorld = HexSize * (3f / 2f) * hexPos.y;
        return new Vector3(xWorld, yWorld, 0f);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        float q = (Mathf.Sqrt(3f) / 3f * worldPos.x - 1f / 3f * worldPos.y) / HexSize;
        float r = (2f / 3f * worldPos.y) / HexSize;
        return CubeRound(q, r, -q - r);
    }

    private Vector3Int CubeRound(float fracQ, float fracR, float fracS)
    {
        int q = Mathf.RoundToInt(fracQ);
        int r = Mathf.RoundToInt(fracR);
        int s = Mathf.RoundToInt(fracS);
        float qDiff = Mathf.Abs(q - fracQ);
        float rDiff = Mathf.Abs(r - fracR);
        float sDiff = Mathf.Abs(s - fracS);
        if (qDiff > rDiff && qDiff > sDiff) q = -r - s;
        else if (rDiff > sDiff) r = -q - s;
        else s = -q - r;
        return new Vector3Int(q, r, s);
    }
}