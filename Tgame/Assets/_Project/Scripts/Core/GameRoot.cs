using System.Collections.Generic;
using UnityEngine;

// ========================================================
// 核心系统生命周期接口
// 强制规范：所有非 MonoBehaviour 的全局系统/管理器都需要实现此接口
// ========================================================
public interface IGameSystem
{
    void OnInit();
    void OnUpdate(float deltaTime);
    void OnDestroy();
}

// ========================================================
// 全局统一生命周期入口 (单例 MonoBehaviour)
// 负责接管所有子系统的初始化、轮询和销毁，杜绝散落的 Update
// ========================================================
public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }

    // 存储所有注册的子系统，用于统一生命周期轮询
    private readonly List<IGameSystem> _systems = new List<IGameSystem>();

    private void Awake()
    {
        // 经典的单例模式，确保全局唯一
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 切换场景时不销毁此节点，保证底层基建一直存活
        DontDestroyOnLoad(gameObject);

        // 启动时自动初始化所有系统
        InitializeSystems();
    }

    /// <summary>
    /// 集中注册与初始化所有子系统
    /// 架构原则：请严格注意注册的先后顺序，底层依赖（如数据读取）必须在最前面注册
    /// </summary>
    private void InitializeSystems()
    {
        RegisterSystem(new DataManager());
        RegisterSystem(new GridSystem());  
        RegisterSystem(new TurnManager());
        RegisterSystem(new BattleManager());
        // 统一触发 OnInit
        foreach (var system in _systems)
        {
            system.OnInit();
        }

        Debug.Log("[GameRoot] 所有核心系统初始化完毕。");
    }

    /// <summary>
    /// 将系统加入生命周期轮询列表
    /// </summary>
    private void RegisterSystem(IGameSystem system)
    {
        if (!_systems.Contains(system))
        {
            _systems.Add(system);
        }
    }

    /// <summary>
    /// 统一驱动 Update
    /// 摒弃散落各处的 MonoBehaviour Update，由这里集中轮询，提升性能并确保时序可控
    /// </summary>
    private void Update()
    {
        float dt = Time.deltaTime;

        // 使用 for 循环避免 foreach 可能产生的极微量 GC
        for (int i = 0; i < _systems.Count; i++)
        {
            _systems[i].OnUpdate(dt);
        }
    }

    /// <summary>
    /// 游戏退出或该节点被强制销毁时，统一释放资源
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 倒序触发销毁，防止底层基建被提前卸载导致上层业务报错
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                _systems[i].OnDestroy();
            }
            _systems.Clear();

            Debug.Log("[GameRoot] 所有核心系统已安全卸载。");
        }
    }
}