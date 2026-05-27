using System.Collections.Generic;
using UnityEngine;
using TGame.Data; // 【🔥修复1：引入底层格子数据】

namespace TGame.Battle // 【🔥修复2：统一战斗逻辑命名空间】
{
    public class PathNode
    {
        public GridCell Cell;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;
        public PathNode ParentNode;

        public PathNode(GridCell cell)
        {
            Cell = cell;
        }
    }

    public static class PathfindingService
    {
        public static List<GridCell> GetPath(GridSystem gridSystem, Vector3Int startPos, Vector3Int targetPos)
        {
            GridCell startCell = gridSystem.GetCell(startPos);
            GridCell targetCell = gridSystem.GetCell(targetPos);

            if (startCell == null || targetCell == null) return null;

            // 【🔥修复3：使用 IsWalkable 替代 CanEnter，且目标点不能有其他人站着】
            if (!targetCell.IsWalkable || (targetCell.OccupantUnitID != -1 && targetCell.OccupantUnitID != startCell.OccupantUnitID)) return null;

            List<PathNode> openList = new List<PathNode>();
            HashSet<GridCell> closedList = new HashSet<GridCell>();

            PathNode startNode = new PathNode(startCell)
            {
                GCost = 0,
                HCost = CalculateDistance(startCell, targetCell)
            };
            openList.Add(startNode);

            Dictionary<Vector3Int, PathNode> nodeMap = new Dictionary<Vector3Int, PathNode>
            {
                { startPos, startNode }
            };

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);

                if (currentNode.Cell == targetCell)
                {
                    return CalculatePath(currentNode);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode.Cell);

                foreach (GridCell neighbor in gridSystem.GetNeighbors(currentNode.Cell))
                {
                    // 【🔥修复4：判断是否可走，并且避开被其他角色占据的格子】
                    if (closedList.Contains(neighbor) || !neighbor.IsWalkable) continue;
                    if (neighbor.OccupantUnitID != -1 && neighbor != targetCell && neighbor.OccupantUnitID != startCell.OccupantUnitID) continue;

                    // 【🔥修复5：默认六边形每走一步的 Cost 为 1】
                    int moveCost = 1;
                    int tentativeGCost = currentNode.GCost + moveCost;

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
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.ParentNode = currentNode;
                    }
                }
            }
            return null;
        }

        public static int CalculateTotalMoveCost(List<GridCell> path)
        {
            if (path == null || path.Count == 0) return 0;

            // 【🔥修复6：因为每步 Cost 都是 1，总消耗直接返回路径节点数量即可】
            return path.Count;
        }

        // ==========================================
        // 六边形网格的距离计算算法 (保持完美)
        // ==========================================
        private static int CalculateDistance(GridCell a, GridCell b)
        {
            int xDistance = Mathf.Abs(a.Position.x - b.Position.x);
            int yDistance = Mathf.Abs(a.Position.y - b.Position.y);
            int zDistance = Mathf.Abs(a.Position.z - b.Position.z);
            return (xDistance + yDistance + zDistance) / 2;
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
            path.Reverse();
            return path;
        }
    }
}