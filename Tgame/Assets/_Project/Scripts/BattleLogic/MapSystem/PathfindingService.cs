using System.Collections.Generic;
using UnityEngine;

// 寻路节点内部类，用于 A* 计算
public class PathNode
{
    public GridCell Cell;
    public int GCost; // 从起点到当前点的移动耗时总和
    public int HCost; // 从当前点到终点的预估耗时 (曼哈顿距离)
    public int FCost => GCost + HCost;
    public PathNode ParentNode;

    public PathNode(GridCell cell)
    {
        Cell = cell;
    }
}

public static class PathfindingService
{
    /// <summary>
    /// 获取两点之间的最短路径（返回经过的地块列表）
    /// </summary>
    public static List<GridCell> GetPath(GridSystem gridSystem, Vector3Int startPos, Vector3Int targetPos)
    {
        GridCell startCell = gridSystem.GetCell(startPos);
        GridCell targetCell = gridSystem.GetCell(targetPos);

        if (startCell == null || targetCell == null) return null;
        if (!targetCell.CanEnter()) return null; // 终点不可进入

        List<PathNode> openList = new List<PathNode>();
        HashSet<GridCell> closedList = new HashSet<GridCell>();

        PathNode startNode = new PathNode(startCell)
        {
            GCost = 0,
            HCost = CalculateDistance(startCell, targetCell)
        };
        openList.Add(startNode);

        // 为了避免每次循环重新创建 PathNode，我们可以加一个字典缓存当次寻路的 Node
        Dictionary<Vector3Int, PathNode> nodeMap = new Dictionary<Vector3Int, PathNode>
        {
            { startPos, startNode }
        };

        while (openList.Count > 0)
        {
            // 找出 F 值最小的节点
            PathNode currentNode = GetLowestFCostNode(openList);

            // 到达终点，回溯路径
            if (currentNode.Cell == targetCell)
            {
                return CalculatePath(currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.Cell);

            // 遍历相邻地块
            foreach (GridCell neighbor in gridSystem.GetNeighbors(currentNode.Cell))
            {
                if (closedList.Contains(neighbor) || !neighbor.CanEnter()) continue;

                // 计算将要消耗的总时素：当前累积耗时 + 目标地块的耗时
                int tentativeGCost = currentNode.GCost + neighbor.MoveCost;

                if (!nodeMap.TryGetValue(neighbor.Position, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(neighbor);
                    nodeMap[neighbor.Position] = neighborNode;
                }

                if (!openList.Contains(neighborNode))
                {
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = CalculateDistance(neighbor, targetCell);
                    neighborNode.ParentNode = currentNode;
                    openList.Add(neighborNode);
                }
                else if (tentativeGCost < neighborNode.GCost)
                {
                    // 找到更优路径
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.ParentNode = currentNode;
                }
            }
        }

        // 遍历完未找到路径
        return null;
    }

    /// <summary>
    /// 计算消耗路径总和（如需求所述：总耗时 = PathSum(moveCost)）
    /// </summary>
    public static int CalculateTotalMoveCost(List<GridCell> path)
    {
        if (path == null || path.Count == 0) return 0;
        int totalCost = 0;
        // 起点不算入移动消耗
        for (int i = 1; i < path.size; i++)
        {
            totalCost += path[i].MoveCost;
        }
        return totalCost;
    }

    // 计算曼哈顿距离 (禁用斜向移动)
    private static int CalculateDistance(GridCell a, GridCell b)
    {
        int xDistance = Mathf.Abs(a.Position.x - b.Position.x);
        int yDistance = Mathf.Abs(a.Position.y - b.Position.y);
        return xDistance + yDistance;
    }

    private static PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].FCost < lowestFCostNode.FCost)
            {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }

    private static List<GridCell> CalculatePath(PathNode endNode)
    {
        List<GridCell> path = new List<GridCell>();
        PathNode currentNode = endNode;
        while (currentNode.ParentNode != null)
        {
            path.Add(currentNode.Cell);
            currentNode = currentNode.ParentNode;
        }
        // 反转列表，确保顺序是从起点到终点
        path.Reverse();
        return path;
    }
}