using UnityEngine;

namespace TGame.Data
{
    public class GridCell
    {
        public Vector3Int Position { get; private set; }
        public bool IsWalkable { get; set; }
        public int OccupantUnitID { get; set; } = -1;

        // 【🔥核心】逻辑层也要记录地面的两个 ID
        public int GroundVariantID { get; set; } = 0;
        public int ObstacleVariantID { get; set; } = -1;

        public GridCell(Vector3Int position)
        {
            Position = position;
            IsWalkable = true;
        }

        public GridCell(Vector3Int position, bool isWalkable, int cost = 1)
        {
            Position = position;
            IsWalkable = isWalkable;
        }
    }
}