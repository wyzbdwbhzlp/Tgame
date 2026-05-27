using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TGame.Data;
using TGame.Battle;
using Game.UI;

namespace TGame.Core
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        public void OnInit()
        {
            Instance = this;
            StartBattleSetup();
        }

        private void StartBattleSetup()
        {
            if (GlobalManager.Instance == null || GlobalManager.Instance.levelManager == null) return;

            LevelDataSO levelData = GlobalManager.Instance.levelManager.GetCurrentLevelData();
            if (levelData == null) return;

            if (HexMapView.Instance != null) HexMapView.Instance.CreateGridVisuals();

            foreach (var spawn in levelData.playerSpawns)
            {
                UnitManager.Instance.SpawnPlayer(spawn.characterID, spawn.spawnPos);
            }

            foreach (var spawn in levelData.enemySpawns)
            {
                UnitManager.Instance.SpawnEnemy(spawn.enemyID, spawn.spawnPos);
            }

            if (UIManager.Instance != null) UIManager.Instance.Show<UI_BattleMain>("UI_BattleMain");
        }

        // 【🔥核心修复】返回值从 void 变成 Coroutine，使得外部可以使用 yield return 等待它
        public Coroutine StartSettleRoutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }
}