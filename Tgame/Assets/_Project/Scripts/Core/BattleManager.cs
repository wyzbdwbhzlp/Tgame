using UnityEngine;
using System.Collections;
using TGame.Data;
using TGame.Battle;
using Game.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public void OnInit()
    {
        Instance = this;
        Debug.Log("<color=green>[BattleManager] 战斗初始化启动...</color>");
        StartLevel(2001); // 加载关卡
    }

    private void StartLevel(int levelID)
    {
        LevelDataSO levelData = DataManager.Instance.GetLevelData(levelID);
        if (levelData == null)
        {
            Debug.LogError($"[BattleManager] 找不到关卡配置 {levelID}！");
            return;
        }

        GridSystem.Instance.LoadLevel(levelData);

        foreach (var spawn in levelData.playerSpawns)
            UnitManager.Instance.SpawnUnit(spawn.characterID, spawn.spawnPos);

        foreach (var spawn in levelData.enemySpawns)
            UnitManager.Instance.SpawnUnit(spawn.characterID, spawn.spawnPos);

        Debug.Log($"🎉 [BattleManager] 关卡【{levelData.levelName}】部署完毕！");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.Show<UI_BattleMain>("UI_BattleMain");
        }
    }

    public void StartSettleRoutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }
}