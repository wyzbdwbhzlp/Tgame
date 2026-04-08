using System.Collections.Generic;
using UnityEngine;

public class GridSystem : IGameSystem
{
    // ==========================================
    // 新增：单例访问器，让表现层能方便地读取地图数据
    // ==========================================
    public static GridSystem Instance { get; private set; }

    private readonly Dictionary<Vector3Int, GridCell> _grid = new Dictionary<Vector3Int, GridCell>();

    // 六边形外接圆的半径（中心点到顶点的距离）
    public float HexSize { get; private set; } = 1.0f;

    public void OnInit()
    {
        // 赋值单例
        Instance = this;

        // 模拟生成一个半径为 5 的大六边形战场
        GenerateHexMap(5);
        Debug.Log($"[GridSystem] 六边形地图初始化完成，共生成 {_grid.Count} 个逻辑网格。");
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        _grid.Clear();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 基于立方体坐标生成正六边形形状的地图
    private void GenerateHexMap(int mapRadius)
    {
        for (int x = -mapRadius; x <= mapRadius; x++)
        {
            int y1 = Mathf.Max(-mapRadius, -x - mapRadius);
            int y2 = Mathf.Min(mapRadius, -x + mapRadius);
            for (int y = y1; y <= y2; y++)
            {
                int z = -x - y; // 强制满足 x + y + z = 0
                Vector3Int pos = new Vector3Int(x, y, z);
                _grid[pos] = new GridCell(pos, true, 1);
            }
        }

        
    }

    public GridCell GetCell(Vector3Int cellPosition)
    {
        if (_grid.TryGetValue(cellPosition, out GridCell cell))
        {
            return cell;
        }
        return null;
    }

    /// <summary>
    /// 六边形立方体坐标 转 世界坐标 (采用尖顶朝上 Pointy-top 布局)
    /// </summary>
    public Vector3 CellToWorld(Vector3Int hexPos)
    {
        // 修复：垂直方向(yWorld)应该对应立方体坐标的 y 轴，而不是 z 轴
        float xWorld = HexSize * Mathf.Sqrt(3f) * (hexPos.x + hexPos.y / 2f);
        float yWorld = HexSize * (3f / 2f) * hexPos.y;
        return new Vector3(xWorld, yWorld, 0f);
    }
    /// <summary>
    /// 获取六边形的 6 个相邻地块
    /// </summary>
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
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// 世界坐标 转 六边形立方体坐标 (用于鼠标点击检测)
    /// </summary>
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        // 尖顶朝上 (Pointy-top) 的逆向转换公式
        float q = (Mathf.Sqrt(3f) / 3f * worldPos.x - 1f / 3f * worldPos.y) / HexSize;
        float r = (2f / 3f * worldPos.y) / HexSize;
        return CubeRound(q, r, -q - r);
    }

    // 浮点数立方体坐标取整算法 (确保转换后依然满足 x+y+z=0)
    private Vector3Int CubeRound(float fracQ, float fracR, float fracS)
    {
        int q = Mathf.RoundToInt(fracQ);
        int r = Mathf.RoundToInt(fracR);
        int s = Mathf.RoundToInt(fracS);

        float qDiff = Mathf.Abs(q - fracQ);
        float rDiff = Mathf.Abs(r - fracR);
        float sDiff = Mathf.Abs(s - fracS);

        if (qDiff > rDiff && qDiff > sDiff)
            q = -r - s;
        else if (rDiff > sDiff)
            r = -q - s;
        else
            s = -q - r;

        return new Vector3Int(q, r, s);
    }
}