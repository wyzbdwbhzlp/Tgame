using System;
using System.Collections.Generic;
using UnityEngine;
using TGame.Core;
using TGame.Data;

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

        // ==========================================
        // 【🔥核心修改】玩家专属生成通道
        // ==========================================
        public RuntimeUnit SpawnPlayer(int characterID, Vector3Int spawnPos)
        {
            CharacterData config = DataManager.Instance.GetCharacterData(characterID);
            if (config == null) return null;

            RuntimeUnit newUnit = new RuntimeUnit(++_idCounter, 1001, config, null, spawnPos);
            RegisterAndInstantiate(newUnit, spawnPos);
            return newUnit;
        }

        // ==========================================
        // 【🔥核心修改】敌人专属生成通道 (含数据适配器黑科技)
        // ==========================================
        public RuntimeUnit SpawnEnemy(int enemyID, Vector3Int spawnPos)
        {
            EnemyData enemyConfig = DataManager.Instance.GetEnemyData(enemyID);
            if (enemyConfig == null) return null;

            // 💡 数据桥接：临时捏一个 CharacterData 喂给战斗公式，完美兼容旧代码！
            CharacterData adapterData = new CharacterData
            {
                characterID = enemyConfig.enemyID,
                characterName = enemyConfig.enemyName,
                characterPrefab = enemyConfig.prefab,
                maxHP = enemyConfig.maxHP,
                attack = enemyConfig.attack,
                defense = enemyConfig.defense,
                speed = enemyConfig.speed,
                attackRange = enemyConfig.attackRange,
                critRate = enemyConfig.critRate,
                postureValue = enemyConfig.postureValue,
                attackVFXID = enemyConfig.attackVFXID,
                attackHitDelay = enemyConfig.attackHitDelay,
                damagePopupDelay = enemyConfig.damagePopupDelay
            };

            // 生成时，把转换好的通用数据和专属的 EnemyData 一并塞进去
            RuntimeUnit newUnit = new RuntimeUnit(++_idCounter, 2001, adapterData, enemyConfig, spawnPos);
            RegisterAndInstantiate(newUnit, spawnPos);
            return newUnit;
        }

        // 提取出的公共注册逻辑
        private void RegisterAndInstantiate(RuntimeUnit newUnit, Vector3Int spawnPos)
        {
            _unitDict.Add(newUnit.InstanceID, newUnit);

            if (GridSystem.Instance != null && GridSystem.Instance.GetCell(spawnPos) != null)
            {
                GridSystem.Instance.GetCell(spawnPos).OccupantUnitID = newUnit.InstanceID;
            }

            if (UnitViewManager.Instance != null)
            {
                UnitViewManager.Instance.CreateUnitView(newUnit);
            }

            OnUnitSpawned?.Invoke(newUnit);
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