using System.Collections.Generic;
using UnityEngine;
using TGame.Data;

namespace TGame.Core
{
    public class DataManager : IGameSystem
    {
        public static DataManager Instance { get; private set; }

        private Dictionary<int, CharacterData> _characterDict = new Dictionary<int, CharacterData>();

        // 【🔥核心新增】存放敌人的字典
        private Dictionary<int, EnemyData> _enemyDict = new Dictionary<int, EnemyData>();
        private Dictionary<int, LevelDataSO> _levelDict = new Dictionary<int, LevelDataSO>();
        private Dictionary<int, SkillData> _skillDict = new Dictionary<int, SkillData>();
        public void OnInit()
        {
            Instance = this;
            LoadSkillDatabase();
            LoadCharacterDatabase();
            LoadEnemyDatabase(); // 【🔥新增】加载敌人表
            LoadAllLevelData();
        }
        private void LoadSkillDatabase()
        {
            SkillTableSO table = Resources.Load<SkillTableSO>("DataConfigs/SkillTable");
            if (table != null && table.skills != null)
            {
                foreach (var skill in table.skills)
                {
                    if (!_skillDict.ContainsKey(skill.skillID))
                    {
                        _skillDict.Add(skill.skillID, skill);
                    }
                }
                Debug.Log($"[DataManager] 成功加载 {_skillDict.Count} 个技能。");
            }
            else
            {
                Debug.LogWarning("[DataManager] 找不到 SkillTable！");
            }
        }

        // 新增查询接口：
        public SkillData GetSkillData(int id)
        {
            if (_skillDict.TryGetValue(id, out var data)) return data;
            Debug.LogWarning($"[DataManager] 找不到 ID 为 {id} 的技能！");
            return null;
        }
        private void LoadCharacterDatabase()
        {
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
                Debug.Log($"[DataManager] 成功加载 {_characterDict.Count} 名玩家角色。");
            }
            else
            {
                Debug.LogError("[DataManager] 加载玩家表失败！请确认是否已生成 Assets/Resources/DataConfigs/CharacterTable.asset");
            }
        }

        // ==========================================
        // 【🔥核心新增】加载敌人数据库
        // ==========================================
        private void LoadEnemyDatabase()
        {
            EnemyTableSO table = Resources.Load<EnemyTableSO>("DataConfigs/EnemyTable");
            if (table != null && table.enemies != null)
            {
                foreach (var enemyData in table.enemies)
                {
                    if (!_enemyDict.ContainsKey(enemyData.enemyID))
                    {
                        _enemyDict.Add(enemyData.enemyID, enemyData);
                    }
                }
                Debug.Log($"[DataManager] 成功加载 {_enemyDict.Count} 名敌方角色。");
            }
            else
            {
                Debug.LogWarning("[DataManager] 加载敌人表失败！如果你还没创建敌人表，请去数据中心创建一个。");
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

        public CharacterData GetCharacterData(int id)
        {
            if (_characterDict.TryGetValue(id, out var data)) return data;
            Debug.LogWarning($"[DataManager] 找不到 ID 为 {id} 的玩家数据！");
            return null;
        }

        // ==========================================
        // 【🔥核心新增】获取敌人单体数据
        // ==========================================
        public EnemyData GetEnemyData(int id)
        {
            if (_enemyDict.TryGetValue(id, out var data)) return data;
            Debug.LogWarning($"[DataManager] 找不到 ID 为 {id} 的敌人数据！");
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