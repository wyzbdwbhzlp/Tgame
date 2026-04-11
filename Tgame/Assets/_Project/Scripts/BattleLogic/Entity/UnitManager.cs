using System;
using System.Collections.Generic;
using UnityEngine;
using TGame.Data;

public class UnitManager : IGameSystem
{
    public static UnitManager Instance { get; private set; }
    public event Action<RuntimeUnit> OnUnitSpawned;
    private readonly Dictionary<int, RuntimeUnit> _allUnits = new Dictionary<int, RuntimeUnit>();
    private int _instanceCounter = 10000;

    public void OnInit()
    {
        Instance = this;
        Debug.Log("[UnitManager] ¾ÍÐ÷¡£");
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
        if (cell == null || !cell.IsWalkable || cell.OccupantUnitID != -1) return null;

        int newInstanceID = _instanceCounter++;
        RuntimeUnit newUnit = new RuntimeUnit(newInstanceID, config, spawnPos);
        _allUnits.Add(newInstanceID, newUnit);
        cell.OccupantUnitID = newInstanceID;

        OnUnitSpawned?.Invoke(newUnit);
        return newUnit;
    }

    public RuntimeUnit GetUnit(int instanceID)
    {
        _allUnits.TryGetValue(instanceID, out RuntimeUnit unit);
        return unit;
    }

    public IEnumerable<RuntimeUnit> GetAllUnits() => _allUnits.Values;
}