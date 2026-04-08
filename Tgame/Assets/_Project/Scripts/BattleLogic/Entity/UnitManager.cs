using System;
using System.Collections.Generic;
using UnityEngine;
using TGame.Data;

public class UnitManager : IGameSystem
{
    public static UnitManager Instance { get; private set; }

    // ================= 新增：实体生成事件广播 =================
    // 表现层会监听这个事件，从而在屏幕上生成 3D 模型
    public event Action<RuntimeUnit> OnUnitSpawned;

    private readonly Dictionary<int, RuntimeUnit> _allUnits = new Dictionary<int, RuntimeUnit>();
    private int _instanceCounter = 10000;

    public void OnInit()
    {
        Instance = this;
        Debug.Log("[UnitManager] 实体管理器准备就绪。");
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        _allUnits.Clear();
        if (Instance == this) Instance = null;
    }

    public RuntimeUnit SpawnUnit(int configID, Vector3Int spawnPos)
    {
        CharacterDataSO config = DataManager.Instance.GetCharacterData(configID);
        if (config == null) return null;

        GridCell cell = GridSystem.Instance.GetCell(spawnPos);
        if (cell == null || !cell.IsWalkable || cell.OccupantUnitID != -1)
        {
            Debug.LogWarning($"[UnitManager] 无法在 {spawnPos} 生成 {config.characterName}，该地块不存在或已被占用！");
            return null;
        }

        int newInstanceID = _instanceCounter++;
        RuntimeUnit newUnit = new RuntimeUnit(newInstanceID, config, spawnPos);
        _allUnits.Add(newInstanceID, newUnit);
        cell.OccupantUnitID = newInstanceID;

        Debug.Log($"[UnitManager] 成功在 {spawnPos} 生成单位: 【{config.characterName}】");

        // ================= 触发广播 =================
        OnUnitSpawned?.Invoke(newUnit);

        return newUnit;
    }

    public RuntimeUnit GetUnit(int instanceID)
    {
        _allUnits.TryGetValue(instanceID, out RuntimeUnit unit);
        return unit;
    }

    // ================= 新增：获取所有实体 =================
    public IEnumerable<RuntimeUnit> GetAllUnits()
    {
        return _allUnits.Values;
    }
}