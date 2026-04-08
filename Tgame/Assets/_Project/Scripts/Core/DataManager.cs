using System.Collections.Generic;
using UnityEngine;
using TGame.Data; // 引入你的数据结构命名空间

public class DataManager : IGameSystem
{
    // 提供单例访问，方便战斗逻辑层极速获取数据
    public static DataManager Instance { get; private set; }

    // 核心缓存字典：角色ID -> 角色SO数据
    private readonly Dictionary<int, CharacterDataSO> _characterCache = new Dictionary<int, CharacterDataSO>();

    public void OnInit()
    {
        Instance = this;

        // 启动时，一次性把所有角色数据加载到内存中建立索引
        LoadAllCharacterData();
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        _characterCache.Clear();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 从 Resources 目录加载所有由中台工具生成的角色 SO
    /// </summary>
    private void LoadAllCharacterData()
    {
        // 对应你导表工具里的路径：Assets/Resources/DataConfigs/Characters
        // 注意：Resources.LoadAll 只需要填 "Resources/" 后面的相对路径
        CharacterDataSO[] loadedCharacters = Resources.LoadAll<CharacterDataSO>("DataConfigs/Characters");

        if (loadedCharacters == null || loadedCharacters.Length == 0)
        {
            Debug.LogWarning("[DataManager] 未找到任何角色配置数据！请检查导表工具是否正常运行。");
            return;
        }

        foreach (var charSO in loadedCharacters)
        {
            _characterCache[charSO.characterID] = charSO;
        }

        Debug.Log($"[DataManager] 极速挂载完毕！成功缓存 {_characterCache.Count} 名角色数据。");
    }

    /// <summary>
    /// 全局通用接口：根据 ID 获取角色面板数据
    /// </summary>
    public CharacterDataSO GetCharacterData(int characterID)
    {
        if (_characterCache.TryGetValue(characterID, out CharacterDataSO data))
        {
            return data;
        }

        Debug.LogError($"[DataManager] 严重错误：试图获取不存在的角色数据！ID: {characterID}");
        return null;
    }
}