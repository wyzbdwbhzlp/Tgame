using UnityEngine;

public class GridCell
{
    // 六边形网格的立方体坐标 (Cube Coordinates): x, y, z
    public Vector3Int Position { get; private set; }

    public bool IsWalkable { get; set; }
    public int MoveCost { get; set; }
    public int OccupantUnitID { get; set; } = -1;

    public GridCell(Vector3Int position, bool isWalkable = true, int moveCost = 1)
    {
        Position = position;
        IsWalkable = isWalkable;
        MoveCost = moveCost;
    }

    public bool CanEnter()
    {
        return IsWalkable && OccupantUnitID == -1;
    }
}