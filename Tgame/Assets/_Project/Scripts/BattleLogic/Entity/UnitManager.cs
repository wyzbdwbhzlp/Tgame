using System;
using System.Collections.Generic;
using UnityEngine;
using TGame.Core;
using TGame.Data;

// 【🔥核心修改1】统一归入战斗命名空间，这样就能认识 RuntimeUnit 了
namespace TGame.Battle
{
    public class UnitManager : IGameSystem
    {
        public static UnitManager Instance { get; private set; }

        private Dictionary<int, RuntimeUnit> _unitDict = new Dictionary<int, RuntimeUnit>();
        private int _idCounter = 10000;

        public event Action<RuntimeUnit> OnUnitSpawned;
        public event Action<RuntimeUnit> OnUnitDied;

        public void OnInit()
        {
            Instance = this;
            Debug.Log("[UnitManager] 就绪。");
        }

        public RuntimeUnit SpawnUnit(int characterID, Vector3Int spawnPos)
        {
            // 【🔥核心修改2】这里从 CharacterDataSO 改成了 CharacterData
            CharacterData config = DataManager.Instance.GetCharacterData(characterID);
            if (config == null)
            {
                Debug.LogError($"[UnitManager] 生成失败！找不到 ID {characterID} 的角色配置表数据。");
                return null;
            }

            int side = (characterID == 1001) ? 1001 : 1002; // 临时判断阵营
            RuntimeUnit newUnit = new RuntimeUnit(++_idCounter, side, config, spawnPos);

            _unitDict.Add(newUnit.InstanceID, newUnit);

            if (GridSystem.Instance != null && GridSystem.Instance.GetCell(spawnPos) != null)
            {
                GridSystem.Instance.GetCell(spawnPos).OccupantUnitID = newUnit.InstanceID;
            }

            OnUnitSpawned?.Invoke(newUnit);
            return newUnit;
        }

        public RuntimeUnit GetUnit(int instanceID)
        {
            _unitDict.TryGetValue(instanceID, out var unit);
            return unit;
        }

        public IEnumerable<RuntimeUnit> GetAllUnits()
        {
            return _unitDict.Values;
        }

        public void RemoveUnit(int instanceID)
        {
            if (_unitDict.TryGetValue(instanceID, out var unit))
            {
                if (GridSystem.Instance != null)
                {
                    GridSystem.Instance.GetCell(unit.GridPosition).OccupantUnitID = -1;
                }
                _unitDict.Remove(instanceID);
                OnUnitDied?.Invoke(unit);
            }
        }

        public void OnUpdate(float deltaTime) { }

        public void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}