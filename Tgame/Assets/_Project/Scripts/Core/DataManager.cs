using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// 所有由导表工具生成的 C# 类必须实现此接口
public interface IConfigData
{
    int GetId();
}

[Serializable]
public class ConfigWrapper<T> where T : IConfigData
{
    public List<T> Items;
}

public class DataManager : IGameSystem
{
    // 嵌套字典缓存：字典<数据类型, 字典<ID, 数据实例>>
    private readonly Dictionary<Type, object> _dataCaches = new Dictionary<Type, object>();

    public void OnInit()
    {
        // 实际项目中可接入 Addressables 异步加载，此处以 Resources 同步加载演示
        // LoadConfig<CharacterConfig>("Configs/CharacterConfig");
        // LoadConfig<SkillConfig>("Configs/SkillConfig");
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        _dataCaches.Clear();
    }

    public void LoadConfig<T>(string resourcePath) where T : IConfigData
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
        if (jsonAsset == null)
        {
            Debug.LogError($"[DataManager] 无法找到配置文件: {resourcePath}");
            return;
        }

        try
        {
            var wrapper = JsonConvert.DeserializeObject<ConfigWrapper<T>>(jsonAsset.text);
            Dictionary<int, T> typeDict = new Dictionary<int, T>();

            if (wrapper != null && wrapper.Items != null)
            {
                foreach (var item in wrapper.Items)
                {
                    typeDict[item.GetId()] = item;
                }
            }

            _dataCaches[typeof(T)] = typeDict;
            Debug.Log($"[DataManager] 成功加载 {typeof(T).Name} 表，共 {typeDict.Count} 条数据。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] 解析 {resourcePath} 失败: {e.Message}");
        }
    }

    public T GetData<T>(int id) where T : class, IConfigData
    {
        Type type = typeof(T);
        if (_dataCaches.TryGetValue(type, out object cacheObj))
        {
            var dict = cacheObj as Dictionary<int, T>;
            if (dict != null && dict.TryGetValue(id, out T data))
            {
                return data;
            }
        }
        Debug.LogWarning($"[DataManager] 找不到类型为 {type.Name} 且 ID 为 {id} 的数据！");
        return null;
    }
}