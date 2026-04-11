using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;

public class GameRoot : MonoBehaviour
{
    private List<IGameSystem> _systems = new List<IGameSystem>();

    private void Awake()
    {
        // 1. 注册顺序非常重要
        Register(new DataManager());
        Register(new GridSystem());
        Register(new TGame.Battle.TurnManager());
        Register(new UnitManager());

        // 2. 获取场景中的挂载脚本
        BattleManager bm = Object.FindAnyObjectByType<BattleManager>();
        if (bm != null) Register(bm);

        // 3. 启动
        foreach (var s in _systems) s.OnInit();
        Debug.Log("<color=white>[GameRoot] 全系统初始化完毕</color>");
    }

    private void Register(IGameSystem s) => _systems.Add(s);

    private void Update() { foreach (var s in _systems) s.OnUpdate(Time.deltaTime); }

    private void OnDestroy() { foreach (var s in _systems) s.OnDestroy(); _systems.Clear(); }
}