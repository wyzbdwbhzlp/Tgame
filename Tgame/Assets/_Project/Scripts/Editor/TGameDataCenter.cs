using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TGame.Data;

namespace TGame.EditorTools
{
    public class TGameDataCenter : EditorWindow
    {
        private static readonly Regex csvParser = new Regex("[,\\t](?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        [MenuItem("Tools/TGame 数据中台")]
        public static void ShowWindow()
        {
            GetWindow<TGameDataCenter>("数据中台");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("📊 角色与数值导入", EditorStyles.boldLabel);
            if (GUILayout.Button("📂 选择【角色数据】CSV 并导入", GUILayout.Height(40)))
            {
                EditorApplication.delayCall += ImportCharacterCSV;
            }

            GUILayout.Space(20);
            GUILayout.Label("🗺️ 关卡与战斗配置", EditorStyles.boldLabel);
            if (GUILayout.Button("📂 选择【关卡数据】CSV 并导入", GUILayout.Height(40)))
            {
                EditorApplication.delayCall += ImportLevelCSV;
            }
        }

        #region --- 1. 角色数据导入 ---
        private void ImportCharacterCSV()
        {
            string filePath = EditorUtility.OpenFilePanel("选择角色配置CSV", "Assets/", "csv");
            if (string.IsNullOrEmpty(filePath)) return;

            List<string> lines = ReadFileLines(filePath);
            if (lines.Count <= 1) return;

            string savePath = "Assets/Resources/DataConfigs";
            EnsureFolderExists(savePath);

            string dbPath = $"{savePath}/CharacterTable.asset";
            CharacterTableSO table = AssetDatabase.LoadAssetAtPath<CharacterTableSO>(dbPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<CharacterTableSO>();
                AssetDatabase.CreateAsset(table, dbPath);
            }

            List<CharacterData> charList = new List<CharacterData>();

            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] row = csvParser.Split(lines[i]);

                // 【🔥核心修改】现在一共需要 14 列数据了
                if (row.Length < 14) continue;

                CharacterData tempData = new CharacterData();

                int.TryParse(CleanCSVString(row[0]), out tempData.characterID);
                tempData.characterName = CleanCSVString(row[1]);

                // 【🔥核心修改】解析职业和攻击距离 (索引2和3)
                tempData.job = ParseJob(CleanCSVString(row[2]));
                int.TryParse(CleanCSVString(row[3]), out tempData.attackRange);

                // 美术资源顺延到 4 和 5
                string portraitStr = CleanCSVString(row[4]);
                string prefabStr = CleanCSVString(row[5]);

                if (!string.IsNullOrEmpty(portraitStr))
                {
                    Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/{portraitStr}.png");
                    if (sp == null) sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/{portraitStr}.jpg");
                    tempData.portraitSprite = sp;
                }

                if (!string.IsNullOrEmpty(prefabStr))
                {
                    tempData.characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/{prefabStr}.prefab");
                }

                // 数值属性顺延 (从 6 开始)
                int.TryParse(CleanCSVString(row[6]), out tempData.maxHP);
                int.TryParse(CleanCSVString(row[7]), out tempData.maxMP);
                int.TryParse(CleanCSVString(row[8]), out tempData.attack);
                int.TryParse(CleanCSVString(row[9]), out tempData.defense);
                int.TryParse(CleanCSVString(row[10]), out tempData.speed);
                int.TryParse(CleanCSVString(row[11]), out tempData.postureValue);
                float.TryParse(CleanCSVString(row[12]), out tempData.dodgeRate);
                float.TryParse(CleanCSVString(row[13]), out tempData.critRate);

                charList.Add(tempData);
            }

            table.characters = charList.ToArray();

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ [角色导表] 成功将 {charList.Count} 个角色导入到 CharacterTable (已包含职业与攻击距离)！");
        }
        #endregion

        #region --- 2. 关卡数据导入 ---
        private void ImportLevelCSV()
        {
            string filePath = EditorUtility.OpenFilePanel("选择关卡配置CSV", "Assets/", "csv");
            if (string.IsNullOrEmpty(filePath)) return;

            List<string> lines = ReadFileLines(filePath);
            if (lines.Count <= 1) return;

            string savePath = "Assets/Resources/DataConfigs/Levels";
            EnsureFolderExists(savePath);

            int successCount = 0;
            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] row = csvParser.Split(lines[i]);
                if (row.Length < 6) continue;

                LevelDataSO tempData = ScriptableObject.CreateInstance<LevelDataSO>();

                int.TryParse(CleanCSVString(row[0]), out tempData.levelID);
                tempData.levelName = CleanCSVString(row[1]);
                int.TryParse(CleanCSVString(row[2]), out tempData.mapRadius);

                tempData.obstacles = ParseVectorList(CleanCSVString(row[3]));
                tempData.playerSpawns = ParseSpawnList(CleanCSVString(row[4]));
                tempData.enemySpawns = ParseSpawnList(CleanCSVString(row[5]));

                tempData.name = $"{tempData.levelID}_{tempData.levelName}";
                string assetPath = $"{savePath}/{tempData.name}.asset";

                LevelDataSO existingAsset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
                if (existingAsset != null)
                {
                    EditorUtility.CopySerialized(tempData, existingAsset);
                    EditorUtility.SetDirty(existingAsset);
                    DestroyImmediate(tempData, true);
                }
                else
                {
                    AssetDatabase.CreateAsset(tempData, assetPath);
                }
                successCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"✅ [关卡导表] 成功生成/更新了 {successCount} 个关卡数据！");
        }

        private List<Vector3Int> ParseVectorList(string input)
        {
            List<Vector3Int> list = new List<Vector3Int>();
            if (string.IsNullOrEmpty(input) || input == "无") return list;

            string[] parts = input.Split(';');
            foreach (var p in parts)
            {
                string[] coords = p.Split('|');
                if (coords.Length >= 2 && int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                    list.Add(new Vector3Int(x, y, -x - y));
            }
            return list;
        }

        private List<UnitSpawnInfo> ParseSpawnList(string input)
        {
            List<UnitSpawnInfo> list = new List<UnitSpawnInfo>();
            if (string.IsNullOrEmpty(input) || input == "无") return list;

            string[] parts = input.Split(';');
            foreach (var p in parts)
            {
                string[] idAndPos = p.Split(':');
                if (idAndPos.Length >= 2 && int.TryParse(idAndPos[0], out int id))
                {
                    string[] coords = idAndPos[1].Split('|');
                    if (coords.Length >= 2 && int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                        list.Add(new UnitSpawnInfo { characterID = id, spawnPos = new Vector3Int(x, y, -x - y) });
                }
            }
            return list;
        }
        #endregion

        #region --- 辅助方法 ---
        // 【🔥新增】将表中的中文文字转化为枚举
        private CharacterJob ParseJob(string input)
        {
            switch (input.Trim())
            {
                case "法师": return CharacterJob.Mage;
                case "牧师": return CharacterJob.Priest;
                case "弓箭手": return CharacterJob.Archer;
                case "战士":
                default: return CharacterJob.Warrior; // 默认战士
            }
        }

        private List<string> ReadFileLines(string path)
        {
            List<string> lines = new List<string>();
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null) lines.Add(line);
            }
            return lines;
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    currentPath += "/" + folders[i];
                }
            }
        }

        private static string CleanCSVString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            input = input.Trim();
            if (input.StartsWith("\"") && input.EndsWith("\""))
                input = input.Substring(1, input.Length - 2).Replace("\"\"", "\"");
            return input;
        }
        #endregion
    }
}