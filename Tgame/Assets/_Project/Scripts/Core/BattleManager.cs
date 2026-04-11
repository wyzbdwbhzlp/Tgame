using UnityEngine;
using TGame.Data;
using TGame.Battle;
using System.Collections;

public class BattleManager : MonoBehaviour, IGameSystem
{
    public static BattleManager Instance { get; private set; }

    public void OnInit()
    {
        Instance = this;
        Debug.Log("<color=green>[BattleManager] 战斗初始化：正在加载关卡 2001...</color>");

        // 核心：启动关卡加载流程
        StartLevel(2001);
    }

    public void OnUpdate(float deltaTime) { }
    public void OnDestroy() { if (Instance == this) Instance = null; }

    private void StartLevel(int levelID)
    {
        LevelDataSO levelData = DataManager.Instance.GetLevelData(levelID);
        if (levelData == null)
        {
            Debug.LogError($"[BattleManager] 加载失败：DataManager 中找不到 ID 为 {levelID} 的关卡配置！请检查 CSV 导表是否成功。");
            return;
        }

        // 1. 驱动地形生成
        GridSystem.Instance.LoadLevel(levelData);

        // 2. 驱动单位生成
        foreach (var spawn in levelData.playerSpawns)
        {
            UnitManager.Instance.SpawnUnit(spawn.characterID, spawn.spawnPos);
        }

        foreach (var spawn in levelData.enemySpawns)
        {
            UnitManager.Instance.SpawnUnit(spawn.characterID, spawn.spawnPos);
        }

        Debug.Log($"🎉 [BattleManager] 关卡【{levelData.levelName}】地图与角色部署完毕！");
    }

    // 供 TurnManager 调用的结算协程入口
    public void StartSettleRoutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }
}