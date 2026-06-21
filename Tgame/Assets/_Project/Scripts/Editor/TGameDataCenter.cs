#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TGame.Data;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;

namespace TGame.EditorTools
{
    public class TGameDataCenter : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("TGame/TGame Data Center (双向同步版)", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<TGameDataCenter>("Data Center");
            window.minSize = new Vector2(450, 750);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("TGame 数据管理中心", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ==========================================
            // 模块 1：玩家角色管理
            // ==========================================
            GUILayout.BeginVertical("box");
            GUILayout.Label("🤺 玩家英雄数据", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("读写 Assets/Resources/DataConfigs/CharacterTable", MessageType.Info);

            if (GUILayout.Button("定位 / 生成玩家总表", GUILayout.Height(30)))
            {
                CreateOrSelectAssetInResources<CharacterTableSO>("DataConfigs", "CharacterTable");
            }

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("📥 导入 (CSV -> SO)", GUILayout.Height(40))) ImportCharactersFromCSV();
            GUI.backgroundColor = new Color(1f, 0.9f, 0.6f);
            if (GUILayout.Button("📤 反写 (SO -> CSV)", GUILayout.Height(40))) ExportCharactersToCSV();
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            // ==========================================
            // 模块 2：敌人数据管理
            // ==========================================
            GUILayout.BeginVertical("box");
            GUILayout.Label("👹 敌方怪物数据", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("读写 Assets/Resources/DataConfigs/EnemyTable", MessageType.Info);

            if (GUILayout.Button("定位 / 生成敌人总表", GUILayout.Height(30)))
            {
                CreateOrSelectAssetInResources<EnemyTableSO>("DataConfigs", "EnemyTable");
            }

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("📥 导入 (CSV -> SO)", GUILayout.Height(40))) ImportEnemiesFromCSV();
            GUI.backgroundColor = new Color(1f, 0.9f, 0.6f);
            if (GUILayout.Button("📤 反写 (SO -> CSV)", GUILayout.Height(40))) ExportEnemiesToCSV();
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            // ==========================================
            // 模块 3：技能数据管理
            // ==========================================
            GUILayout.BeginVertical("box");
            GUILayout.Label("🔥 技能与魔法数据", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("读写 Assets/Resources/DataConfigs/SkillTable", MessageType.Info);

            if (GUILayout.Button("定位 / 生成技能总表", GUILayout.Height(30)))
            {
                CreateOrSelectAssetInResources<SkillTableSO>("DataConfigs", "SkillTable");
            }

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Button("📥 导入 (CSV -> SO)", GUILayout.Height(40))) ImportSkillsFromCSV();
            GUI.backgroundColor = new Color(1f, 0.9f, 0.6f);
            if (GUILayout.Button("📤 反写 (SO -> CSV)", GUILayout.Height(40))) ExportSkillsToCSV();
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            // ==========================================
            // 模块 4：关卡管理
            // ==========================================
            GUILayout.BeginVertical("box");
            GUILayout.Label("🗺️ 关卡数据", EditorStyles.boldLabel);

            if (GUILayout.Button("创建新关卡 (LevelDataSO)", GUILayout.Height(30)))
            {
                CreateAssetInSpecificFolder<LevelDataSO>("Assets/Resources/DataConfigs/Levels", "NewLevel");
            }
            if (GUILayout.Button("定位 / 生成关卡排期总表", GUILayout.Height(30)))
            {
                // 【🔥修改】将 MainLevelTable 改成了 LevelTable
                CreateOrSelectAssetInResources<LevelTable>("DataConfigs", "LevelTable");
            }
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            // ==========================================
            // 模块 5：特效管理
            // ==========================================
            GUILayout.BeginVertical("box");
            GUILayout.Label("✨ 特效管理 (VFX)", EditorStyles.boldLabel);

            if (GUILayout.Button("创建单体特效资产 (VFXDataSO)", GUILayout.Height(30)))
            {
                CreateAssetInSpecificFolder<VFXDataSO>("Assets/Data/VFXs", "NewVFX");
            }
            if (GUILayout.Button("定位 / 生成特效总表 (VFXTable)", GUILayout.Height(30)))
            {
                CreateOrSelectAssetInResources<VFXTable>("", "VFXTable");
            }
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        // ==========================================
        // 解析 CSV 导入 玩家数据
        // ==========================================
        private void ImportCharactersFromCSV()
        {
            string csvPath = EditorUtility.OpenFilePanel("选择玩家配置 CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(csvPath)) return;
            string tablePath = "Assets/Resources/DataConfigs/CharacterTable.asset";
            CharacterTableSO table = AssetDatabase.LoadAssetAtPath<CharacterTableSO>(tablePath);
            if (table == null) { Debug.LogError("找不到总表！"); return; }
            string[] lines = ReadCSVLinesSafely(csvPath);
            if (lines == null) return;

            Undo.RecordObject(table, "Import Player CSV");
            List<CharacterData> newCharList = new List<CharacterData>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = lines[i].Split(',');

                CharacterData data = new CharacterData();
                data.characterID = int.Parse(cols[0]);
                data.characterName = cols[1];
                if (Enum.TryParse(cols[2], true, out CharacterJob parsedJob)) data.job = parsedJob;
                data.attackRange = int.Parse(cols[3]);

                data.maxHP = int.Parse(cols[6]);
                data.maxMP = int.Parse(cols[7]);
                data.attack = int.Parse(cols[8]);
                data.defense = int.Parse(cols[9]);
                data.speed = int.Parse(cols[10]);
                data.postureValue = int.Parse(cols[11]);
                data.evasionRate = float.Parse(cols[12]);
                data.critRate = float.Parse(cols[13]);

                data.attackVFXID = (cols.Length > 14 && !string.IsNullOrWhiteSpace(cols[14])) ? cols[14].Trim() : "Hit_Default";
                data.attackHitDelay = (cols.Length > 15 && float.TryParse(cols[15], out float hitDelay)) ? hitDelay : 0.35f;
                data.damagePopupDelay = (cols.Length > 16 && float.TryParse(cols[16], out float popDelay)) ? popDelay : 0.15f;

                data.skillIDs = new List<int>();
                if (cols.Length > 17 && !string.IsNullOrWhiteSpace(cols[17]))
                {
                    string[] skillStrings = cols[17].Split('|');
                    foreach (var s in skillStrings) if (int.TryParse(s, out int sID)) data.skillIDs.Add(sID);
                }

                if (cols.Length > 18 && !string.IsNullOrWhiteSpace(cols[18]))
                    if (Enum.TryParse(cols[18].Replace("|", ","), true, out SkillTag parsedW)) data.weakness = parsedW;
                if (cols.Length > 19 && !string.IsNullOrWhiteSpace(cols[19]))
                    if (Enum.TryParse(cols[19].Replace("|", ","), true, out SkillTag parsedR)) data.resistance = parsedR;

                if (table.characters != null)
                {
                    CharacterData existingData = Array.Find(table.characters, c => c.characterID == data.characterID);
                    if (existingData != null)
                    {
                        data.portraitSprite = existingData.portraitSprite;
                        data.characterPrefab = existingData.characterPrefab;
                    }
                }
                newCharList.Add(data);
            }
            table.characters = newCharList.ToArray();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>🎉 成功从 CSV 导入了 {newCharList.Count} 名英雄！</color>");
        }

        // ==========================================
        // 【🔥新增】反写 玩家数据 到 CSV
        // ==========================================
        private void ExportCharactersToCSV()
        {
            string tablePath = "Assets/Resources/DataConfigs/CharacterTable.asset";
            CharacterTableSO table = AssetDatabase.LoadAssetAtPath<CharacterTableSO>(tablePath);
            if (table == null || table.characters == null) { Debug.LogError("找不到玩家表或数据为空！"); return; }

            string savePath = EditorUtility.SaveFilePanel("反写玩家数据到 CSV", Application.dataPath, "CharacterTable_Export", "csv");
            if (string.IsNullOrEmpty(savePath)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID,名字,职业,攻击距离,立绘(无),预制体(无),最大HP,最大MP,攻击力,防御力,速度,躯干值,闪避率,暴击率,攻击特效,命中延迟,飘字延迟,技能列表,弱点,抗性");

            foreach (var c in table.characters)
            {
                string skills = c.skillIDs != null ? string.Join("|", c.skillIDs) : "";
                string weaknessStr = c.weakness.ToString().Replace(", ", "|");
                string resStr = c.resistance.ToString().Replace(", ", "|");

                sb.AppendLine($"{c.characterID},{EscapeCSV(c.characterName)},{c.job},{c.attackRange},-,," +
                              $"{c.maxHP},{c.maxMP},{c.attack},{c.defense},{c.speed},{c.postureValue},{c.evasionRate},{c.critRate}," +
                              $"{EscapeCSV(c.attackVFXID)},{c.attackHitDelay},{c.damagePopupDelay},{skills},{weaknessStr},{resStr}");
            }
            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"<color=orange>📤 玩家数据已成功反写至：{savePath}</color>");
        }

        // ==========================================
        // 解析 CSV 导入 敌人数据
        // ==========================================
        private void ImportEnemiesFromCSV()
        {
            string csvPath = EditorUtility.OpenFilePanel("选择敌人配置 CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(csvPath)) return;
            string tablePath = "Assets/Resources/DataConfigs/EnemyTable.asset";
            EnemyTableSO table = AssetDatabase.LoadAssetAtPath<EnemyTableSO>(tablePath);
            if (table == null) { Debug.LogError("找不到敌人总表！"); return; }
            string[] lines = ReadCSVLinesSafely(csvPath);
            if (lines == null) return;

            Undo.RecordObject(table, "Import Enemy CSV");
            List<EnemyData> newEnemyList = new List<EnemyData>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = lines[i].Split(',');

                EnemyData data = new EnemyData();
                data.enemyID = int.Parse(cols[0]);
                data.enemyName = cols[1];
                if (Enum.TryParse(cols[2], true, out EnemyRole parsedRole)) data.aiRole = parsedRole;
                data.attackRange = int.Parse(cols[3]);

                data.maxHP = int.Parse(cols[6]);
                data.attack = int.Parse(cols[7]);
                data.defense = int.Parse(cols[8]);
                data.speed = int.Parse(cols[9]);
                data.postureValue = int.Parse(cols[10]);
                data.critRate = float.Parse(cols[11]);
                data.maxMoveDistance = int.Parse(cols[12]);

                data.attackVFXID = (cols.Length > 13 && !string.IsNullOrWhiteSpace(cols[13])) ? cols[13].Trim() : "Hit_Default";
                data.attackHitDelay = (cols.Length > 14 && float.TryParse(cols[14], out float hitDelay)) ? hitDelay : 0.35f;
                data.damagePopupDelay = (cols.Length > 15 && float.TryParse(cols[15], out float popDelay)) ? popDelay : 0.15f;

                if (cols.Length > 16 && !string.IsNullOrWhiteSpace(cols[16]))
                    if (Enum.TryParse(cols[16].Replace("|", ","), true, out SkillTag parsedW)) data.weakness = parsedW;
                if (cols.Length > 17 && !string.IsNullOrWhiteSpace(cols[17]))
                    if (Enum.TryParse(cols[17].Replace("|", ","), true, out SkillTag parsedR)) data.resistance = parsedR;

                data.evasionRate = (cols.Length > 18 && float.TryParse(cols[18], out float eva)) ? eva : 0.05f;

                if (table.enemies != null)
                {
                    EnemyData existingData = Array.Find(table.enemies, e => e.enemyID == data.enemyID);
                    if (existingData != null)
                    {
                        data.portraitSprite = existingData.portraitSprite;
                        data.prefab = existingData.prefab;
                    }
                }
                newEnemyList.Add(data);
            }
            table.enemies = newEnemyList.ToArray();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=red>👿 成功从 CSV 导入了 {newEnemyList.Count} 名敌人怪物！</color>");
        }

        // ==========================================
        // 【🔥新增】反写 敌人数据 到 CSV
        // ==========================================
        private void ExportEnemiesToCSV()
        {
            string tablePath = "Assets/Resources/DataConfigs/EnemyTable.asset";
            EnemyTableSO table = AssetDatabase.LoadAssetAtPath<EnemyTableSO>(tablePath);
            if (table == null || table.enemies == null) { Debug.LogError("找不到敌人表或数据为空！"); return; }

            string savePath = EditorUtility.SaveFilePanel("反写敌人数据到 CSV", Application.dataPath, "EnemyTable_Export", "csv");
            if (string.IsNullOrEmpty(savePath)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID,名字,AI定位,攻击距离,立绘(无),预制体(无),最大HP,攻击力,防御力,速度,躯干值,暴击率,最大移动,攻击特效,命中延迟,飘字延迟,弱点,抗性,闪避率");

            foreach (var e in table.enemies)
            {
                string weaknessStr = e.weakness.ToString().Replace(", ", "|");
                string resStr = e.resistance.ToString().Replace(", ", "|");

                sb.AppendLine($"{e.enemyID},{EscapeCSV(e.enemyName)},{e.aiRole},{e.attackRange},-,," +
                              $"{e.maxHP},{e.attack},{e.defense},{e.speed},{e.postureValue},{e.critRate},{e.maxMoveDistance}," +
                              $"{EscapeCSV(e.attackVFXID)},{e.attackHitDelay},{e.damagePopupDelay},{weaknessStr},{resStr},{e.evasionRate}");
            }
            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"<color=orange>📤 敌人数据已成功反写至：{savePath}</color>");
        }

        // ==========================================
        // 解析 CSV 导入 技能数据 
        // ==========================================
        private void ImportSkillsFromCSV()
        {
            string csvPath = EditorUtility.OpenFilePanel("选择技能配置 CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(csvPath)) return;
            string tablePath = "Assets/Resources/DataConfigs/SkillTable.asset";
            SkillTableSO table = AssetDatabase.LoadAssetAtPath<SkillTableSO>(tablePath);
            if (table == null) { Debug.LogError("找不到技能总表！"); return; }
            string[] lines = ReadCSVLinesSafely(csvPath);
            if (lines == null) return;

            Undo.RecordObject(table, "Import Skill CSV");
            List<SkillData> newSkillList = new List<SkillData>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = ParseCSVLine(lines[i]); // 使用安全切割法

                SkillData data = new SkillData();
                data.skillID = int.Parse(cols[0]);
                data.skillName = cols[1];
                data.slogan = cols[2];
                data.description = cols[3];

                if (Enum.TryParse(cols[4], true, out SkillCategory parsedCat)) data.category = parsedCat;

                string tagString = cols[5].Replace("|", ",");
                if (Enum.TryParse(tagString, true, out SkillTag parsedTag)) data.tags = parsedTag;

                string targetString = cols[6].Replace("|", ",");
                if (Enum.TryParse(targetString, true, out SkillTargetMask parsedMask)) data.targetMask = parsedMask;

                data.castRange = int.Parse(cols[7]);
                data.aoeRadius = int.Parse(cols[8]);

                if (Enum.TryParse(cols[9], true, out SkillEffectType parsedEff)) data.effectType = parsedEff;

                data.baseEffectValue = int.Parse(cols[10]);
                data.effectMultiplier = float.Parse(cols[11]);
                data.mpCost = int.Parse(cols[12]);
                data.tuCost = int.Parse(cols[13]);
                data.vfxID = (cols.Length > 14 && !string.IsNullOrWhiteSpace(cols[14])) ? cols[14].Trim() : "Hit_Default";

                data.hitRate = (cols.Length > 15 && float.TryParse(cols[15], out float hr)) ? hr : 1.0f;
                data.penetration = (cols.Length > 16 && float.TryParse(cols[16], out float pen)) ? pen : 0f;

                newSkillList.Add(data);
            }
            table.skills = newSkillList.ToArray();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=cyan>✨ 成功从 CSV 导入了 {newSkillList.Count} 个技能！</color>");
        }

        // ==========================================
        // 【🔥新增】反写 技能数据 到 CSV
        // ==========================================
        private void ExportSkillsToCSV()
        {
            string tablePath = "Assets/Resources/DataConfigs/SkillTable.asset";
            SkillTableSO table = AssetDatabase.LoadAssetAtPath<SkillTableSO>(tablePath);
            if (table == null || table.skills == null) { Debug.LogError("找不到技能表或数据为空！"); return; }

            string savePath = EditorUtility.SaveFilePanel("反写技能数据到 CSV", Application.dataPath, "SkillTable_Export", "csv");
            if (string.IsNullOrEmpty(savePath)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID,技能名,喊话,描述,分类,词条,目标掩码,施法距离,AOE半径,效果类型,基础数值,效果倍率,耗蓝,耗时,特效ID,基础命中率,穿透力");

            foreach (var s in table.skills)
            {
                string tagStr = s.tags.ToString().Replace(", ", "|");
                string maskStr = s.targetMask.ToString().Replace(", ", "|");

                sb.AppendLine($"{s.skillID},{EscapeCSV(s.skillName)},{EscapeCSV(s.slogan)},{EscapeCSV(s.description)},{s.category}," +
                              $"{tagStr},{maskStr},{s.castRange},{s.aoeRadius},{s.effectType},{s.baseEffectValue},{s.effectMultiplier}," +
                              $"{s.mpCost},{s.tuCost},{EscapeCSV(s.vfxID)},{s.hitRate},{s.penetration}");
            }
            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"<color=orange>📤 技能数据已成功反写至：{savePath}</color>");
        }

        // ==========================================
        // 底层黑科技：CSV 安全处理 (防逗号断行截断)
        // ==========================================
        private string EscapeCSV(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
            {
                return "\"" + str.Replace("\"", "\"\"") + "\""; // 将双引号转义，并用双引号包裹整体
            }
            return str;
        }

        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentVal = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        currentVal.Append('\"'); // 处理转义的双引号
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes; // 切换引号状态
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentVal.ToString());
                    currentVal.Clear();
                }
                else
                {
                    currentVal.Append(c);
                }
            }
            result.Add(currentVal.ToString());
            return result.ToArray();
        }

        private string[] ReadCSVLinesSafely(string csvPath)
        {
            try
            {
                using (FileStream fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs))
                {
                    string content = sr.ReadToEnd();
                    return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("读取失败", "无法读取 CSV 文件！\n可能是文件权限问题，请尝试关闭 Excel 后重试。", "确定");
                Debug.LogError($"读取 CSV 失败: {e.Message}");
                return null;
            }
        }

        // ==========================================
        // 底层逻辑
        // ==========================================
        private void CreateAssetInSpecificFolder<T>(string folderPath, string defaultName) where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) CreateFolderRecursive(folderPath);
            string path = EditorUtility.SaveFilePanelInProject($"创建 {typeof(T).Name}", defaultName, "asset", "", folderPath);
            if (string.IsNullOrEmpty(path)) return;
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void CreateOrSelectAssetInResources<T>(string subFolder, string requiredName) where T : ScriptableObject
        {
            string basePath = "Assets/Resources";
            string path = string.IsNullOrEmpty(subFolder) ? basePath : $"{basePath}/{subFolder}";
            if (!AssetDatabase.IsValidFolder(path)) CreateFolderRecursive(path);

            string fullPath = $"{path}/{requiredName}.asset";
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(fullPath);

            if (existingAsset != null)
            {
                Selection.activeObject = existingAsset;
                EditorGUIUtility.PingObject(existingAsset);
            }
            else
            {
                T asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void CreateFolderRecursive(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath += "/" + folders[i];
            }
        }
    }
}
#endif