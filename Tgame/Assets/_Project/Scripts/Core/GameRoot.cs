using System.Collections.Generic;
using UnityEngine;

public interface IGameSystem
{
    void OnInit();
    void OnUpdate(float deltaTime);
    void OnDestroy();
}

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }
    private readonly List<IGameSystem> _systems = new List<IGameSystem>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 严格按照依赖顺序注册子系统
        RegisterSystem(new DataManager());
        // 后续可注册 BattleManager, TimelineManager 等

        foreach (var system in _systems)
        {
            system.OnInit();
        }
    }

    private void RegisterSystem(IGameSystem system)
    {
        if (!_systems.Contains(system))
        {
            _systems.Add(system);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].OnUpdate(dt);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                _systems[i].OnDestroy();
            }
            _systems.Clear();
        }
    }
}