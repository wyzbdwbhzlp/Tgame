using UnityEngine;
using System.Collections.Generic;
using TGame.Data;

// 【🔥新增】增加了 Player 和 Enemy 两种画笔图层
public enum BrushLayer { Ground, Obstacle, Player, Enemy }

// 【🔥新增】用于在编辑器中缓存出生点数据的结构
[System.Serializable]
public class EditorUnitSpawn
{
    public Vector3Int position;
    public int unitID;
}

public class HexMapEditorData : MonoBehaviour
{
    [Header("绑定目标关卡 (画好后将数据覆盖进这个文件)")]
    public LevelDataSO targetLevel;

    [Header("网格大小 (需与 GridSystem 保持一致)")]
    public float hexSize = 1f;

    [Header("统一地图美术配置")]
    public MapVisualConfigSO visualConfig;

    [Header("样式菜单配置")]
    public string[] groundVariantNames = { "0: 默认草地", "1: 泥土", "2: 沙地", "3: 石板路" };
    public string[] obstacleVariantNames = { "0: 普通岩石", "1: 大树", "2: 废墟墙壁", "3: 水坑" };

    // ==========================================
    // 【🔥新增】配置你的角色和怪物 ID (需与表格一致)
    // ==========================================
    [Header("出生点菜单配置 (名字与对应的ID)")]
    public string[] playerVariantNames = { "主角 (ID:1001)", "弓箭手 (ID:1002)" };
    public int[] playerVariantIDs = { 1001, 1002 };

    public string[] enemyVariantNames = { "史莱姆 (ID:2001)", "哥布林 (ID:2002)" };
    public int[] enemyVariantIDs = { 2001, 2002 };

    [Header("当前编辑器缓存的数据")]
    public List<HexEditorCell> cells = new List<HexEditorCell>();
    public List<EditorUnitSpawn> playerSpawns = new List<EditorUnitSpawn>();
    public List<EditorUnitSpawn> enemySpawns = new List<EditorUnitSpawn>();

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
        RemoveUnitSpawn(pos); // 删掉格子的同时删掉上面的人
    }

    // ==========================================
    // 【🔥新增】绘制和擦除出生点的方法
    // ==========================================
    public void SetPlayer(Vector3Int pos, int unitID)
    {
        RemoveUnitSpawn(pos); // 确保一个格子只站一个人
        playerSpawns.Add(new EditorUnitSpawn { position = pos, unitID = unitID });
    }

    public void SetEnemy(Vector3Int pos, int unitID)
    {
        RemoveUnitSpawn(pos);
        enemySpawns.Add(new EditorUnitSpawn { position = pos, unitID = unitID });
    }

    public void RemoveUnitSpawn(Vector3Int pos)
    {
        playerSpawns.RemoveAll(p => p.position == pos);
        enemySpawns.RemoveAll(e => e.position == pos);
    }
}