using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

// 所有由导表工具生成的 C# 类必须实现此接口
public interface IConfigData
{
    int GetId();
}

public class DataManager : IGameSystem
{
    // 嵌套字典缓存：字典<数据类型, 字典<ID, 数据实例>>
    private readonly Dictionary<Type, object> _dataCaches = new Dictionary<Type, object>();

    public void OnInit()
    {
        LoadConfigCSV<CharacterConfigData>("Configs/CharacterConfig");

        var yejin = GetData<CharacterConfigData>(1001);
        if (yejin != null)
        {
            Debug.Log($"[测试成功] CSV 读取到角色: {yejin.Name}, 攻击力: {yejin.Attack}");
        }
    }

    public void OnUpdate(float deltaTime)
    {
        // 数据管理器通常不需要按帧更新，保留空实现
    }

    public void OnDestroy()
    {
        _dataCaches.Clear();
    }

    /// <summary>
    /// 加载并解析 CSV 配置表
    /// </summary>
    public void LoadConfigCSV<T>(string resourcePath) where T : class, IConfigData, new()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>(resourcePath);
        if (csvAsset == null)
        {
            Debug.LogError($"[DataManager] 无法找到 CSV 配置文件: {resourcePath}");
            return;
        }

        try
        {
            // 调用核心解析逻辑
            Dictionary<int, T> typeDict = ParseCSV<T>(csvAsset.text);

            // 存入全局缓存
            _dataCaches[typeof(T)] = typeDict;
            Debug.Log($"[DataManager] 成功加载 CSV 配置表: {typeof(T).Name}, 共 {typeDict.Count} 条数据。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] 解析 CSV 配置文件失败: {resourcePath}. 异常: {e.Message}\n堆栈: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 核心 CSV 解析引擎 (基于反射自动装配字段)
    /// </summary>
    private Dictionary<int, T> ParseCSV<T>(string csvText) where T : class, IConfigData, new()
    {
        Dictionary<int, T> dictionary = new Dictionary<int, T>();

        // 兼容 Windows(\r\n) 和 Mac(\n) 的换行符
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            Debug.LogWarning($"[DataManager] CSV 数据为空或缺少正文行。");
            return dictionary;
        }

        // 1. 解析表头 (假设第一行是字段名)
        string[] headers = lines[0].Split(',');

        // 2. 缓存反射字段，建立“列索引”到“C#字段”的映射表，大幅提升循环中的性能
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        Dictionary<int, FieldInfo> columnToField = new Dictionary<int, FieldInfo>();

        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim();
            foreach (var field in fields)
            {
                // 忽略大小写匹配表头与变量名
                if (field.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    columnToField[i] = field;
                    break;
                }
            }
        }

        // 3. 处理正文数据
        // 正则表达式魔法：匹配逗号，但安全地忽略双引号内部的逗号 (例如技能描述中的标点)
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";

        // 从第二行开始遍历数据
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = Regex.Split(lines[i], pattern);
            T instance = new T();

            for (int j = 0; j < values.Length; j++)
            {
                if (columnToField.TryGetValue(j, out FieldInfo field))
                {
                    string val = values[j].Trim();

                    // 去除 CSV 导出时可能自动包裹的首尾双引号，并将转义的双引号还原
                    if (val.StartsWith("\"") && val.EndsWith("\""))
                    {
                        val = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
                    }

                    // 数据类型转换
                    object parsedValue = ParseValue(field.FieldType, val);
                    if (parsedValue != null)
                    {
                        field.SetValue(instance, parsedValue);
                    }
                }
            }

            // 使用 GetId() 建立字典索引
            dictionary[instance.GetId()] = instance;
        }

        return dictionary;
    }

    /// <summary>
    /// 支持基础类型、布尔值和枚举的自动转换
    /// </summary>
    private object ParseValue(Type type, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        try
        {
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(float)) return float.Parse(value);
            if (type == typeof(string)) return value;
            if (type == typeof(bool))
            {
                // 兼容配表习惯：填 1 或 TRUE 为真，0 或 FALSE 为假
                if (value == "1") return true;
                if (value == "0") return false;
                return bool.Parse(value);
            }
            if (type.IsEnum) return Enum.Parse(type, value, true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] 字段转换错误! 尝试将 '{value}' 转换为 {type.Name} 失败。");
        }

        return null;
    }

    /// <summary>
    /// 获取单条数据 (全系统通用接口)
    /// </summary>
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