using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TGame.Data; // 引入刚才写的 SO 命名空间

namespace TGame.EditorTools
{
    public class TGameDataCenter : EditorWindow
    {
        // 核心 CSV 解析正则：完美解决技能描述等文本中自带逗号被错误截断的问题
        private static readonly Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        [MenuItem("Tools/TGame 数据中台")]
        public static void ShowWindow()
        {
            GetWindow<TGameDataCenter>("数据中台");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("📊 CSV 智能导入系统", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("请选择包含角色面板数据的 CSV 文件，将自动生成或更新 ScriptableObject。", MessageType.Info);

            if (GUILayout.Button("📂 选择【角色数据】CSV 并导入", GUILayout.Height(40)))
            {
                EditorApplication.delayCall += ImportCharacterCSV;
            }
        }

        private void ImportCharacterCSV()
        {
            string filePath = EditorUtility.OpenFilePanel("选择角色配置CSV", "Assets/", "csv");
            if (string.IsNullOrEmpty(filePath)) return;

            // 1. 读取所有行 (使用 FileShare.ReadWrite 防止 Excel 未关闭时读取报错)
            List<string> lines = new List<string>();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null) lines.Add(line);
            }

            if (lines.Count <= 1)
            {
                Debug.LogWarning("CSV 文件为空或只有表头！");
                return;
            }

            // 2. 确保输出目录存在
            string savePath = "Assets/Resources/DataConfigs/Characters";
            EnsureFolderExists(savePath);

            int successCount = 0;

            // 3. 从第二行开始解析数据（跳过表头）
            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                // 使用正则分割当前行
                string[] row = csvParser.Split(lines[i]);
                if (row.Length < 10) continue; // 确保列数对得上

                // 4. 在内存中实例化一个临时 SO 并赋予数据
                CharacterDataSO tempData = ScriptableObject.CreateInstance<CharacterDataSO>();

                // 解析数据 (注意：CleanCSVString 用于去除多余的引号和空格)
                int.TryParse(CleanCSVString(row[0]), out tempData.characterID);
                tempData.characterName = CleanCSVString(row[1]);
                int.TryParse(CleanCSVString(row[2]), out tempData.maxHP);
                int.TryParse(CleanCSVString(row[3]), out tempData.maxMP);
                int.TryParse(CleanCSVString(row[4]), out tempData.attack);
                int.TryParse(CleanCSVString(row[5]), out tempData.defense);
                int.TryParse(CleanCSVString(row[6]), out tempData.speed);
                int.TryParse(CleanCSVString(row[7]), out tempData.postureValue);
                float.TryParse(CleanCSVString(row[8]), out tempData.dodgeRate);
                float.TryParse(CleanCSVString(row[9]), out tempData.critRate);

                // 5. 核心逻辑：生成或更新 Asset
                // 命名规范：ID_角色名.asset (如：1001_夜烬.asset)
                tempData.name = $"{tempData.characterID}_{tempData.characterName}";
                string assetPath = $"{savePath}/{tempData.name}.asset";

                // 检查是否已经存在同名 SO
                CharacterDataSO existingAsset = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(assetPath);

                if (existingAsset != null)
                {
                    // 【关键技巧】如果存在，绝对不要删了重建！否则场景里挂载的引用会全部丢失（Missing）。
                    // 我们使用 CopySerialized 将新数据“克隆”到老资产上。
                    EditorUtility.CopySerialized(tempData, existingAsset);
                    EditorUtility.SetDirty(existingAsset);
                    DestroyImmediate(tempData, true); // 数据刷完，销毁临时内存对象
                }
                else
                {
                    // 如果不存在，直接在对应路径创建新资产
                    AssetDatabase.CreateAsset(tempData, assetPath);
                }

                successCount++;
            }

            // 6. 统一保存与刷新引擎
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"✅ [导表成功] 生成/更新了 {successCount} 个角色数据资产！");
        }

        #region --- 辅助方法 ---

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                // 递归创建文件夹
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

        private static string CleanCSVString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            input = input.Trim();
            // 处理 Excel 导出时自动为含有逗号的内容添加的首尾双引号
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                input = input.Substring(1, input.Length - 2).Replace("\"\"", "\"");
            }
            return input;
        }

        #endregion
    }
}