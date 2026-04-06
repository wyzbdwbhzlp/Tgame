using UnityEngine;

public class GridCell
{
    public Vector3Int Position { get; private set; }

    // 是否可行走（如墙壁、深渊为 false）
    public bool IsWalkable { get; set; }

    // 经过该地块需要消耗的时素（地形消耗）
    public int MoveCost { get; set; }

    // 当前占据该地块的角色 ID (-1 表示为空)
    public int OccupantUnitID { get; set; } = -1;

    public GridCell(Vector3Int position, bool isWalkable = true, int moveCost = 1)
    {
        Position = position;
        IsWalkable = isWalkable;
        MoveCost = moveCost;
    }

    /// <summary>
    /// 检查该地块是否允许移动进入
    /// </summary>
    public bool CanEnter()
    {
        return IsWalkable && OccupantUnitID == -1;
    }
}