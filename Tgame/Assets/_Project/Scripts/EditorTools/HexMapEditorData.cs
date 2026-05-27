using UnityEngine;
using System.Collections.Generic;
using TGame.Data;

public enum BrushLayer { Ground, Obstacle }

public class HexMapEditorData : MonoBehaviour
{
    [Header("绑定目标关卡 (画好后将数据覆盖进这个文件)")]
    public LevelDataSO targetLevel;

    [Header("网格大小 (需与 GridSystem 保持一致)")]
    public float hexSize = 1f;

    [Header("样式菜单配置")]
    public string[] groundVariantNames = { "0: 默认草地", "1: 泥土", "2: 沙地", "3: 石板路" };
    public string[] obstacleVariantNames = { "0: 普通岩石", "1: 大树", "2: 废墟墙壁", "3: 水坑" };

    [Header("美术资产预览 (拖入真实贴图即可在场景中预览)")]
    public Sprite[] groundSprites;
    public Sprite[] obstacleSprites;

    [Header("当前编辑器缓存的地图数据")]
    public List<HexEditorCell> cells = new List<HexEditorCell>();

    public void SetGround(Vector3Int pos, int variantID)
    {
        var cell = cells.Find(c => c.position == pos);
        if (cell != null) cell.groundVariantID = variantID;
        else cells.Add(new HexEditorCell { position = pos, groundVariantID = variantID, obstacleVariantID = -1 });
    }

    public void SetObstacle(Vector3Int pos, int variantID)
    {
        var cell = cells.Find(c => c.position == pos);
        if (cell != null) cell.obstacleVariantID = variantID;
        else cells.Add(new HexEditorCell { position = pos, groundVariantID = 0, obstacleVariantID = variantID });
    }

    public void RemoveObstacle(Vector3Int pos)
    {
        var cell = cells.Find(c => c.position == pos);
        if (cell != null) cell.obstacleVariantID = -1;
    }

    public void RemoveCell(Vector3Int pos)
    {
        cells.RemoveAll(c => c.position == pos);
    }
}