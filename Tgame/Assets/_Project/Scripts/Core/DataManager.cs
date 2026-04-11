using System.Collections.Generic;
using UnityEngine;
using TGame.Data;

public class DataManager : IGameSystem
{
    public static DataManager Instance { get; private set; }

    private readonly Dictionary<int, CharacterDataSO> _characterCache = new Dictionary<int, CharacterDataSO>();
    private readonly Dictionary<int, LevelDataSO> _levelCache = new Dictionary<int, LevelDataSO>();

    public void OnInit()
    {
        Instance = this;
        LoadAllCharacterData();
        LoadAllLevelData();
    }

    public void OnUpdate(float deltaTime) { }

    public void OnDestroy()
    {
        _characterCache.Clear();
        _levelCache.Clear();
        if (Instance == this) Instance = null;
    }

    private void LoadAllCharacterData()
    {
        CharacterDataSO[] loadedCharacters = Resources.LoadAll<CharacterDataSO>("DataConfigs/Characters");
        if (loadedCharacters != null)
        {
            foreach (var charSO in loadedCharacters) _characterCache[charSO.characterID] = charSO;
        }
        Debug.Log($"[DataManager] »º´æ {_characterCache.Count} Ãû½ÇÉ«ÅäÖÃ¡£");
    }

    private void LoadAllLevelData()
    {
        LevelDataSO[] loadedLevels = Resources.LoadAll<LevelDataSO>("DataConfigs/Levels");
        if (loadedLevels != null)
        {
            foreach (var lv in loadedLevels) _levelCache[lv.levelID] = lv;
        }
        Debug.Log($"[DataManager] »º´æ {_levelCache.Count} ¸ö¹Ø¿¨ÅäÖÃ¡£");
    }

    public CharacterDataSO GetCharacterData(int characterID)
    {
        _characterCache.TryGetValue(characterID, out CharacterDataSO data);
        return data;
    }

    public LevelDataSO GetLevelData(int levelID)
    {
        _levelCache.TryGetValue(levelID, out LevelDataSO data);
        return data;
    }
}