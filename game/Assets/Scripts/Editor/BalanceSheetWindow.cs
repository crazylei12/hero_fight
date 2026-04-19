using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    public sealed class BalanceSheetWindow : EditorWindow
    {
        private const string MenuPath = "Fight/Tools/Balance Sheets";
        private const string FolderPreferenceKey = "Fight.Editor.BalanceSheetWindow.RelativeFolder";

        [SerializeField] private string relativeFolder = BalanceSheetService.DefaultRelativeFolder;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<BalanceSheetWindow>("Balance Sheets");
            window.minSize = new Vector2(620f, 300f);
            window.Show();
        }

        public static void ExportDefaultFolderBatch()
        {
            BalanceSheetService.Export(GetDefaultAbsoluteFolderPath());
            EditorApplication.Exit(0);
        }

        private void OnEnable()
        {
            relativeFolder = EditorPrefs.GetString(FolderPreferenceKey, BalanceSheetService.DefaultRelativeFolder);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(FolderPreferenceKey, relativeFolder ?? BalanceSheetService.DefaultRelativeFolder);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stage-01 批量调数表", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "导出的表头和枚举值都带中文说明；导入时只识别左侧稳定键。空白单元格会被跳过，不会清空已有数值。",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("表格目录", EditorStyles.boldLabel);
                relativeFolder = EditorGUILayout.TextField("相对项目根目录", relativeFolder);
                EditorGUILayout.LabelField("绝对路径", GetAbsoluteFolderPath(), EditorStyles.wordWrappedLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("恢复默认目录"))
                    {
                        relativeFolder = BalanceSheetService.DefaultRelativeFolder;
                    }

                    if (GUILayout.Button("打开目录"))
                    {
                        var folderPath = GetAbsoluteFolderPath();
                        Directory.CreateDirectory(folderPath);
                        EditorUtility.RevealInFinder(folderPath);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "导出只会生成 heroes.csv 和 skills.csv 两张表。heroes 只放英雄基础属性与普攻数值；skills 只放技能主数值，以及现有 effect/status 的数值槽位说明。表格不会创建或删除结构，给不存在的槽位填值会直接报错。",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导出当前数值到表格", GUILayout.Height(36f)))
                {
                    RunWithDialog(() => BalanceSheetService.Export(GetAbsoluteFolderPath()), "导出完成");
                }

                if (GUILayout.Button("从表格导入并回写资产", GUILayout.Height(36f)))
                {
                    RunWithDialog(() => BalanceSheetService.Import(GetAbsoluteFolderPath()), "导入完成");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "安全规则：当前构建流程和 demo 内容确保流程不会覆盖你已经调好的数值。只有显式执行“覆盖重建 Demo 内容”的菜单时，才会回到默认样例数值。",
                MessageType.Warning);
        }

        private string GetAbsoluteFolderPath()
        {
            var safeRelativeFolder = string.IsNullOrWhiteSpace(relativeFolder)
                ? BalanceSheetService.DefaultRelativeFolder
                : relativeFolder.Trim();
            return GetAbsoluteFolderPath(safeRelativeFolder);
        }

        private static string GetDefaultAbsoluteFolderPath()
        {
            return GetAbsoluteFolderPath(BalanceSheetService.DefaultRelativeFolder);
        }

        private static string GetAbsoluteFolderPath(string safeRelativeFolder)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, safeRelativeFolder));
        }

        private void RunWithDialog(System.Action action, string title)
        {
            try
            {
                action.Invoke();
                EditorUtility.DisplayDialog(title, $"目录：{GetAbsoluteFolderPath()}", "OK");
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("执行失败", exception.Message, "OK");
            }
        }
    }
}
