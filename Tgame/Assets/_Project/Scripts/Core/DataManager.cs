using System.Collections.Generic;
using UnityEngine;
using TGame.Data;

namespace TGame.Core
{
    public class DataManager : IGameSystem
    {
        public static DataManager Instance { get; private set; }

        // 字典的值类型改为了普通的 CharacterData
        private Dictionary<int, CharacterData> _characterDict = new Dictionary<int, CharacterData>();
        private Dictionary<int, LevelDataSO> _levelDict = new Dictionary<int, LevelDataSO>();

        public void OnInit()
        {
            Instance = this;
            LoadCharacterDatabase();
            LoadAllLevelData();
        }

        private void LoadCharacterDatabase()
        {
            // 直接读取那唯一的一张大表
            CharacterTableSO table = Resources.Load<CharacterTableSO>("DataConfigs/CharacterTable");
            if (table != null && table.characters != null)
            {
                foreach (var charData in table.characters)
                {
                    if (!_characterDict.ContainsKey(charData.characterID))
                    {
                        _characterDict.Add(charData.characterID, charData);
                    }
                }
                Debug.Log($"[DataManager] 成功从单表架构加载 {_characterDict.Count} 名角色。");
            }
            else
            {
                Debug.LogError("[DataManager] 加载失败！请确认是否已生成 Assets/Resources/DataConfigs/CharacterTable.asset");
            }
        }

        private void LoadAllLevelData()
        {
            LevelDataSO[] levels = Resources.LoadAll<LevelDataSO>("DataConfigs/Levels");
            foreach (var lvl in levels)
            {
                if (lvl != null && !_levelDict.ContainsKey(lvl.levelID))
                {
                    _levelDict.Add(lvl.levelID, lvl);
                }
            }
        }

        // 返回值变为 CharacterData
        public CharacterData GetCharacterData(int id)
        {
            if (_characterDict.TryGetValue(id, out var data)) return data;
            Debug.LogWarning($"[DataManager] 找不到 ID 为 {id} 的角色数据！");
            return null;
        }

        public LevelDataSO GetLevelData(int id)
        {
            if (_levelDict.TryGetValue(id, out var data)) return data;
            return null;
        }

        public void OnUpdate(float deltaTime) { }
        public void OnDestroy() { if (Instance == this) Instance = null; }
    }
}