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
            Debug.Log("<color=green>[BattleManager] 战斗初始化启动...</color>");

            StartBattleSetup();
        }

        private void StartBattleSetup()
        {
            if (GlobalManager.Instance == null || GlobalManager.Instance.levelManager == null)
            {
                Debug.LogError("[BattleManager] 找不到 GlobalManager 或 LevelManager！");
                return;
            }

            LevelDataSO levelData = GlobalManager.Instance.levelManager.GetCurrentLevelData();
            if (levelData == null)
            {
                Debug.LogError("[BattleManager] 获取不到当前关卡数据，无法生成角色！");
                return;
            }

            // ==========================================
            // 先把关卡里的地图数据喂给底层网格！
            // ==========================================
            LoadMapDataToGrid(levelData);

            // 数据喂好之后，再让 HexMapView 根据有了数据的网格生成美术贴图
            if (HexMapView.Instance != null)
            {
                HexMapView.Instance.CreateGridVisuals();
            }

            // 部署玩家角色
            foreach (var spawn in levelData.playerSpawns)
            {
                UnitManager.Instance.SpawnPlayer(spawn.characterID, spawn.spawnPos);
            }

            // 部署敌人角色
            foreach (var spawn in levelData.enemySpawns)
            {
                UnitManager.Instance.SpawnEnemy(spawn.enemyID, spawn.spawnPos);
            }

            Debug.Log($"🎉 [BattleManager] 关卡【{levelData.levelName}】地图与角色部署完毕！");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.Show<UI_BattleMain>("UI_BattleMain");
            }
        }

        private void LoadMapDataToGrid(LevelDataSO levelData)
        {
            if (GridSystem.Instance == null) return;

            // ==========================================
            // 【🔥核心修复】把 mapCells 改成了 cells！与你的数据结构完美对齐！
            // ==========================================
            if (levelData.cells != null && levelData.cells.Count > 0)
            {
                foreach (var editorCell in levelData.cells)
                {
                    var gridCell = GridSystem.Instance.GetCell(editorCell.position);

                    // 如果底层的纯逻辑网格里还没有这个坐标的格子，我们就动态生成一个塞进去
                    if (gridCell == null)
                    {
                        gridCell = new GridCell(editorCell.position);
                        GridSystem.Instance.AddCell(gridCell);
                    }

                    // 将编辑器里的美术表现数据，同步给底层的逻辑格子
                    gridCell.GroundVariantID = editorCell.groundVariantID;
                    gridCell.ObstacleVariantID = editorCell.obstacleVariantID;

                    // 同步寻路阻挡逻辑：如果有障碍物（ID != -1），就不允许走上去！
                    gridCell.IsWalkable = (editorCell.obstacleVariantID == -1);
                }
                Debug.Log($"<color=cyan>[BattleManager] 成功加载 {levelData.cells.Count} 个地块的真实美术与阻挡数据！</color>");
            }
            else
            {
                Debug.LogWarning("[BattleManager] 当前关卡的地图数据为空，将使用默认空白地图！请检查是否在编辑器里保存了笔刷数据！");
            }
        }

        public Coroutine StartSettleRoutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }
}