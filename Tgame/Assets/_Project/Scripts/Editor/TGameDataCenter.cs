#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TGame.Data;
using System.IO;
using System.Collections.Generic;
using System;

namespace TGame.EditorTools
{
    public class TGameDataCenter : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("TGame/TGame Data Center (单表定制版)", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<TGameDataCenter>("Data Center");
            window.minSize = new Vector2(400, 700);
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

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // 玩家为绿色
            if (GUILayout.Button("📥 从 CSV 导入【玩家】数据", GUILayout.Height(40)))
            {
                ImportCharactersFromCSV();
            }
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

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 敌人为红色
            if (GUILayout.Button("📥 从 CSV 导入【敌人】数据", GUILayout.Height(40)))
            {
                ImportEnemiesFromCSV();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            // ==========================================
            // 【🔥新增】模块 3：技能数据管理
            // ==========================================
            GUILayout.BeginVertical("box");
            GUILayout.Label("🔥 技能与魔法数据", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("读写 Assets/Resources/DataConfigs/SkillTable", MessageType.Info);

            if (GUILayout.Button("定位 / 生成技能总表", GUILayout.Height(30)))
            {
                CreateOrSelectAssetInResources<SkillTableSO>("DataConfigs", "SkillTable");
            }

            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // 技能用蓝色按钮
            if (GUILayout.Button("📥 从 CSV 导入【技能】数据", GUILayout.Height(40)))
            {
                ImportSkillsFromCSV();
            }
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
                CreateOrSelectAssetInResources<LevelTable>("DataConfigs", "MainLevelTable");
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

            if (table == null)
            {
                Debug.LogError($"[数据中心] 找不到角色总表！请先点击上面的【定位 / 生成玩家总表】按钮。");
                return;
            }

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

                // cols[4] 是立绘，cols[5] 是预制体，代码跳过读取，走下面的智能保留逻辑

                data.maxHP = int.Parse(cols[6]);
                data.maxMP = int.Parse(cols[7]);
                data.attack = int.Parse(cols[8]);
                data.defense = int.Parse(cols[9]);
                data.speed = int.Parse(cols[10]);
                data.postureValue = int.Parse(cols[11]);
                data.dodgeRate = float.Parse(cols[12]);
                data.critRate = float.Parse(cols[13]);

                data.attackVFXID = (cols.Length > 14 && !string.IsNullOrWhiteSpace(cols[14])) ? cols[14].Trim() : "Hit_Default";
                data.attackHitDelay = (cols.Length > 15 && float.TryParse(cols[15], out float hitDelay)) ? hitDelay : 0.35f;
                data.damagePopupDelay = (cols.Length > 16 && float.TryParse(cols[16], out float popDelay)) ? popDelay : 0.15f;

                // ==========================================
                // 【🔥新增】读取角色拥有的技能 ID 列表 (竖线分隔)
                // ==========================================
                data.skillIDs = new List<int>();
                if (cols.Length > 17 && !string.IsNullOrWhiteSpace(cols[17]))
                {
                    string[] skillStrings = cols[17].Split('|');
                    foreach (var s in skillStrings)
                    {
                        if (int.TryParse(s, out int sID)) data.skillIDs.Add(sID);
                    }
                }

                // 智能保留面板拖拽的美术资产
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
            Selection.activeObject = table;
            EditorGUIUtility.PingObject(table);
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

            if (table == null)
            {
                Debug.LogError($"[数据中心] 找不到敌人总表！请先点击上面的【定位 / 生成敌人总表】按钮。");
                return;
            }

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
            Selection.activeObject = table;
            EditorGUIUtility.PingObject(table);
        }

        // ==========================================
        // 【🔥新增】解析 CSV 导入 技能数据 (处理竖线黑科技)
        // ==========================================
        private void ImportSkillsFromCSV()
        {
            string csvPath = EditorUtility.OpenFilePanel("选择技能配置 CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(csvPath)) return;

            string tablePath = "Assets/Resources/DataConfigs/SkillTable.asset";
            SkillTableSO table = AssetDatabase.LoadAssetAtPath<SkillTableSO>(tablePath);

            if (table == null)
            {
                Debug.LogError($"[数据中心] 找不到技能总表！请先点击上面的【定位 / 生成技能总表】按钮。");
                return;
            }

            string[] lines = ReadCSVLinesSafely(csvPath);
            if (lines == null) return;

            Undo.RecordObject(table, "Import Skill CSV");
            List<SkillData> newSkillList = new List<SkillData>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = lines[i].Split(',');

                SkillData data = new SkillData();
                data.skillID = int.Parse(cols[0]);
                data.skillName = cols[1];
                data.slogan = cols[2];
                data.description = cols[3];

                if (Enum.TryParse(cols[4], true, out SkillCategory parsedCat)) data.category = parsedCat;

                // 读取词条 (支持 | 分隔)
                string tagString = cols[5].Replace("|", ",");
                if (Enum.TryParse(tagString, true, out SkillTag parsedTag)) data.tags = parsedTag;

                // 读取目标掩码 (支持 | 分隔)
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

                newSkillList.Add(data);
            }

            table.skills = newSkillList.ToArray();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=cyan>✨ 成功从 CSV 导入了 {newSkillList.Count} 个技能！</color>");
            Selection.activeObject = table;
            EditorGUIUtility.PingObject(table);
        }

        // ==========================================
        // 防崩溃的文件读取黑科技
        // ==========================================
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
                Debug.LogError($"[数据中心] 读取 CSV 失败: {e.Message}");
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