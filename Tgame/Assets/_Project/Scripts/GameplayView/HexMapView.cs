using UnityEngine;

public class HexMapView : MonoBehaviour
{
    // 当前鼠标悬停的逻辑格子坐标
    private Vector3Int _hoveredCellPos;

    private void Update()
    {
        // 确保逻辑层的地图已经初始化
        if (GridSystem.Instance == null) return;

        // 1. 获取鼠标在 2D 世界中的坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 强制拍平到 2D 平面

        // 2. 将世界坐标转换为六边形逻辑坐标
        _hoveredCellPos = GridSystem.Instance.WorldToCell(mouseWorldPos);

        // 3. 可以在这里接入你 ActionManager 的 Target 选择逻辑
        if (Input.GetMouseButtonDown(0))
        {
            GridCell clickedCell = GridSystem.Instance.GetCell(_hoveredCellPos);
            if (clickedCell != null)
            {
                Debug.Log($"[HexMapView] 点击了地块: 坐标 {_hoveredCellPos}, 移动消耗: {clickedCell.MoveCost}, 是否可行走: {clickedCell.IsWalkable}");
            }
            else
            {
                Debug.LogWarning($"[HexMapView] 点击了地图边界外！");
            }
        }
    }

    /// <summary>
    /// 利用 Gizmos 在 Scene 窗口绘制六边形网格
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && GridSystem.Instance != null)
        {
            // 遍历逻辑地图大小，画出所有格子
            // 注意：这里为了测试方便写死了遍历范围，实际应从 GridSystem 获取所有 Valid Cells
            int mapRadius = 5;
            for (int x = -mapRadius; x <= mapRadius; x++)
            {
                int y1 = Mathf.Max(-mapRadius, -x - mapRadius);
                int y2 = Mathf.Min(mapRadius, -x + mapRadius);
                for (int y = y1; y <= y2; y++)
                {
                    int z = -x - y;
                    Vector3Int cellPos = new Vector3Int(x, y, z);

                    // 获取格子的中心世界坐标
                    Vector3 centerPos = GridSystem.Instance.CellToWorld(cellPos);
                    GridCell cellData = GridSystem.Instance.GetCell(cellPos);

                    // 绘制六边形线框
                    if (cellData != null && !cellData.IsWalkable)
                    {
                        Gizmos.color = Color.black; // 障碍物画黑色
                    }
                    else
                    {
                        Gizmos.color = Color.white; // 正常地块画白色
                    }

                    DrawHexagonGizmo(centerPos, GridSystem.Instance.HexSize);
                }
            }

            // 额外高亮鼠标当前悬停的格子 (画红色)
            Vector3 hoveredCenter = GridSystem.Instance.CellToWorld(_hoveredCellPos);
            Gizmos.color = Color.red;
            DrawHexagonGizmo(hoveredCenter, GridSystem.Instance.HexSize);
        }
    }

    // 绘制单个正六边形的数学方法
    private void DrawHexagonGizmo(Vector3 center, float size)
    {
        Vector3[] corners = new Vector3[6];
        // 尖顶朝上 (Pointy-top) 的顶点计算
        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i - 30; // 减去30度让尖角朝上
            float angle_rad = Mathf.PI / 180 * angle_deg;
            corners[i] = new Vector3(center.x + size * Mathf.Cos(angle_rad), center.y + size * Mathf.Sin(angle_rad), 0);
        }

        // 用线段连接 6 个顶点
        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
        }
    }
}