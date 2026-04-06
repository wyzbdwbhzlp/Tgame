using System.Collections.Generic;
using UnityEngine;

public class GridSystem : IGameSystem
{
    // 使用字典管理地图，方便处理不规则形状的战棋地图
    private readonly Dictionary<Vector3Int, GridCell> _grid = new Dictionary<Vector3Int, GridCell>();

    // 渲染层面上每个格子的世界尺寸（用于坐标转换）
    public float CellSize { get; private set; } = 1.0f;

    public void OnInit()
    {
        // 此处为了测试，我们在内存中直接生成一个 10x10 的纯逻辑地图
        // 实际项目中，这里应该读取配置表或解析场景中的 Tilemap 数据
        GenerateTestMap(10, 10);
        Debug.Log($"[GridSystem] 地图初始化完成，共生成 {_grid.Count} 个逻辑网格。");
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        _grid.Clear();
    }

    private void GenerateTestMap(int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                // 模拟中间有个泥沼地形，移动消耗为 3
                int cost = (x == 5 && y == 5) ? 3 : 1;
                _grid[pos] = new GridCell(pos, true, cost);
            }
        }
        // 模拟一个障碍物
        _grid[new Vector3Int(3, 3, 0)].IsWalkable = false;
    }

    /// <summary>
    /// 获取指定坐标的地块数据
    /// </summary>
    public GridCell GetCell(Vector3Int cellPosition)
    {
        if (_grid.TryGetValue(cellPosition, out GridCell cell))
        {
            return cell;
        }
        return null;
    }

    /// <summary>
    /// 将逻辑网格坐标转换为世界中心点坐标 (便于表现层角色移动对齐)
    /// </summary>
    public Vector3 CellToWorld(Vector3Int cellPosition)
    {
        return new Vector3(
            cellPosition.x * CellSize + CellSize * 0.5f,
            cellPosition.y * CellSize + CellSize * 0.5f,
            0f
        );
    }

    /// <summary>
    /// 将世界坐标转换为逻辑网格坐标 (例如鼠标点击屏幕后转逻辑点)
    /// </summary>
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPosition.x / CellSize),
            Mathf.FloorToInt(worldPosition.y / CellSize),
            0
        );
    }

    /// <summary>
    /// 获取指定地块的相邻地块 (上下左右四向)
    /// </summary>
    public List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new List<GridCell>();
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

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
}