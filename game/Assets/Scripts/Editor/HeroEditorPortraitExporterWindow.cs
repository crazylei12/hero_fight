using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    [Serializable]
    public sealed class HeroEditorPortraitExportOptions
    {
        public const string DefaultRelativeOutputFolder = "Exports/HeroEditorPortraits";

        public int width = 1024;
        public int height = 1024;
        public float paddingMultiplier = 1.12f;
        public Vector2 framingOffset = new Vector2(0f, 0.1f);
        public bool includeShadow;
        public string relativeOutputFolder = DefaultRelativeOutputFolder;
        public string fileSuffix = "_idle_front";
    }

    public sealed class HeroEditorPortraitExporterWindow : EditorWindow
    {
        private const string MenuPath = "Fight/Tools/HeroEditor PNG Exporter";
        private const string OutputFolderPreferenceKey = "Fight.Editor.HeroEditorPortraitExporter.OutputFolder";
        private const string FileSuffixPreferenceKey = "Fight.Editor.HeroEditorPortraitExporter.FileSuffix";

        [SerializeField] private GameObject sourcePrefab;
        [SerializeField] private HeroEditorPortraitExportOptions options = new HeroEditorPortraitExportOptions();

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<HeroEditorPortraitExporterWindow>("HeroEditor PNG");
            window.minSize = new Vector2(520f, 330f);
            window.Show();
        }

        private void OnEnable()
        {
            options ??= new HeroEditorPortraitExportOptions();
            options.relativeOutputFolder = EditorPrefs.GetString(OutputFolderPreferenceKey, HeroEditorPortraitExportOptions.DefaultRelativeOutputFolder);
            options.fileSuffix = EditorPrefs.GetString(FileSuffixPreferenceKey, "_idle_front");
        }

        private void OnDisable()
        {
            if (options == null)
            {
                return;
            }

            EditorPrefs.SetString(OutputFolderPreferenceKey, options.relativeOutputFolder ?? HeroEditorPortraitExportOptions.DefaultRelativeOutputFolder);
            EditorPrefs.SetString(FileSuffixPreferenceKey, options.fileSuffix ?? "_idle_front");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HeroEditor 正面 Idle PNG 导出", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "这个工具会把包含 Character4D 的 prefab 放进临时 prefab 场景，自动切到正面朝向、Idle 状态，并输出透明背景 PNG。",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("源 Prefab", EditorStyles.boldLabel);
                sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", sourcePrefab, typeof(GameObject), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("使用当前选择"))
                    {
                        sourcePrefab = Selection.activeObject as GameObject;
                    }

                    if (GUILayout.Button("打开输出目录"))
                    {
                        var outputFolder = HeroEditorPortraitExporter.ResolveOutputFolderAbsolutePath(options);
                        Directory.CreateDirectory(outputFolder);
                        EditorUtility.RevealInFinder(outputFolder);
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("导出设置", EditorStyles.boldLabel);
                options.relativeOutputFolder = EditorGUILayout.TextField("相对项目根目录", options.relativeOutputFolder);
                EditorGUILayout.LabelField("绝对路径", HeroEditorPortraitExporter.ResolveOutputFolderAbsolutePath(options), EditorStyles.wordWrappedLabel);
                options.fileSuffix = EditorGUILayout.TextField("文件后缀", options.fileSuffix);
                options.width = EditorGUILayout.IntField("宽度", options.width);
                options.height = EditorGUILayout.IntField("高度", options.height);
                options.paddingMultiplier = EditorGUILayout.Slider("边缘留白", options.paddingMultiplier, 1.0f, 1.6f);
                options.framingOffset = EditorGUILayout.Vector2Field("镜头偏移", options.framingOffset);
                options.includeShadow = EditorGUILayout.Toggle("包含阴影", options.includeShadow);
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导出当前 Prefab", GUILayout.Height(36f)))
                {
                    ExportCurrentPrefab();
                }

                if (GUILayout.Button("批量导出当前选择", GUILayout.Height(36f)))
                {
                    ExportSelectedPrefabs();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "批量导出会扫描当前 Project 里选中的 prefab，并只处理包含 Character4D 的资源。默认文件名格式是 <PrefabName>_idle_front.png。",
                MessageType.None);
        }

        private void ExportCurrentPrefab()
        {
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("缺少 Prefab", "请先指定一个包含 Character4D 的 prefab。", "OK");
                return;
            }

            RunWithDialog(() =>
            {
                var outputPath = HeroEditorPortraitExporter.ExportPrefab(sourcePrefab, options);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GetAssetPath(sourcePrefab));
                Debug.Log($"[HeroEditorPortraitExporter] Exported portrait to {outputPath}");
            }, "导出完成");
        }

        private void ExportSelectedPrefabs()
        {
            var prefabs = HeroEditorPortraitExporter.GetSelectedPrefabAssets().ToList();
            if (prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("没有可导出的 Prefab", "请在 Project 视图中选择至少一个包含 Character4D 的 prefab。", "OK");
                return;
            }

            RunWithDialog(() =>
            {
                var exportedPaths = HeroEditorPortraitExporter.ExportPrefabs(prefabs, options).ToList();
                Debug.Log($"[HeroEditorPortraitExporter] Exported {exportedPaths.Count} portraits to {HeroEditorPortraitExporter.ResolveOutputFolderAbsolutePath(options)}");
            }, "批量导出完成");
        }

        private void RunWithDialog(Action action, string title)
        {
            try
            {
                action.Invoke();
                EditorUtility.DisplayDialog(title, HeroEditorPortraitExporter.ResolveOutputFolderAbsolutePath(options), "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("执行失败", exception.Message, "OK");
            }
        }
    }

    public static class HeroEditorPortraitExporter
    {
        private const string CommandLineMethodSourceArg = "-fightPortraitSource";
        private const string CommandLineMethodOutputArg = "-fightPortraitOutput";
        private const string CommandLineWidthArg = "-fightPortraitWidth";
        private const string CommandLineHeightArg = "-fightPortraitHeight";
        private const string CommandLinePaddingArg = "-fightPortraitPadding";
        private const string CommandLineIncludeShadowArg = "-fightPortraitIncludeShadow";
        private const string CommandLineOffsetXArg = "-fightPortraitOffsetX";
        private const string CommandLineOffsetYArg = "-fightPortraitOffsetY";

        public static IEnumerable<GameObject> GetSelectedPrefabAssets()
        {
            return Selection.objects
                .OfType<GameObject>()
                .Where(IsPrefabAsset)
                .Where(ContainsCharacter4D);
        }

        public static IEnumerable<string> ExportPrefabs(IEnumerable<GameObject> prefabs, HeroEditorPortraitExportOptions options)
        {
            var normalizedOptions = NormalizeOptions(options);
            var exportedPaths = new List<string>();

            foreach (var prefab in prefabs)
            {
                exportedPaths.Add(ExportPrefab(prefab, normalizedOptions));
            }

            AssetDatabase.Refresh();
            return exportedPaths;
        }

        public static string ExportPrefab(GameObject prefab, HeroEditorPortraitExportOptions options)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (!IsPrefabAsset(prefab))
            {
                throw new InvalidOperationException("只能导出 Project 里的 prefab 资源。");
            }

            if (!ContainsCharacter4D(prefab))
            {
                throw new InvalidOperationException($"Prefab [{prefab.name}] 不包含 Character4D，无法走 HeroEditor 正面 Idle 导出流程。");
            }

            var normalizedOptions = NormalizeOptions(options);
            var sourceAssetPath = AssetDatabase.GetAssetPath(prefab);
            var absoluteOutputFolder = ResolveOutputFolderAbsolutePath(normalizedOptions);
            Directory.CreateDirectory(absoluteOutputFolder);

            var outputPath = Path.Combine(
                absoluteOutputFolder,
                $"{SanitizeFileName(prefab.name)}{normalizedOptions.fileSuffix}.png");

            ExportPrefabAtAssetPath(sourceAssetPath, outputPath, normalizedOptions);
            AssetDatabase.Refresh();
            return outputPath;
        }

        public static string ResolveOutputFolderAbsolutePath(HeroEditorPortraitExportOptions options)
        {
            var normalizedOptions = NormalizeOptions(options);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, normalizedOptions.relativeOutputFolder));
        }

        public static void ExportFromCommandLine()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var sourceAssetPath = ReadArgument(arguments, CommandLineMethodSourceArg);
                if (string.IsNullOrWhiteSpace(sourceAssetPath))
                {
                    throw new InvalidOperationException($"Missing required argument {CommandLineMethodSourceArg}.");
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourceAssetPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Could not load prefab at path [{sourceAssetPath}].");
                }

                var options = NormalizeOptions(new HeroEditorPortraitExportOptions());
                options.width = ReadIntArgument(arguments, CommandLineWidthArg, options.width);
                options.height = ReadIntArgument(arguments, CommandLineHeightArg, options.height);
                options.paddingMultiplier = ReadFloatArgument(arguments, CommandLinePaddingArg, options.paddingMultiplier);
                options.includeShadow = ReadBoolArgument(arguments, CommandLineIncludeShadowArg, options.includeShadow);
                options.framingOffset = new Vector2(
                    ReadFloatArgument(arguments, CommandLineOffsetXArg, options.framingOffset.x),
                    ReadFloatArgument(arguments, CommandLineOffsetYArg, options.framingOffset.y));

                var outputPath = ReadArgument(arguments, CommandLineMethodOutputArg);
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    outputPath = Path.Combine("Temp", "HeroPortraitExports", $"{SanitizeFileName(prefab.name)}{options.fileSuffix}.png");
                }

                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                var absoluteOutputPath = Path.IsPathRooted(outputPath)
                    ? outputPath
                    : Path.GetFullPath(Path.Combine(projectRoot, outputPath));

                ExportPrefabAtAssetPath(sourceAssetPath, absoluteOutputPath, options);
                Debug.Log($"[HeroEditorPortraitExporter] Exported portrait to {absoluteOutputPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        private static void ExportPrefabAtAssetPath(string assetPath, string absoluteOutputPath, HeroEditorPortraitExportOptions options)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAsset == null)
            {
                throw new InvalidOperationException($"Could not load prefab asset at [{assetPath}].");
            }

            var prefabRoot = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (prefabRoot == null)
            {
                throw new InvalidOperationException($"Could not instantiate prefab asset at [{assetPath}].");
            }

            prefabRoot.hideFlags = HideFlags.HideAndDontSave;
            PreviewRenderUtility previewUtility = null;
            Texture2D texture = null;
            RenderTexture previewRenderTexture = null;

            try
            {
                var character = prefabRoot.GetComponentInChildren<Character4D>(true);
                if (character == null)
                {
                    throw new InvalidOperationException($"Prefab at [{assetPath}] does not contain a Character4D component.");
                }

                PrepareCharacterForPortraitCapture(character, options);

                var spriteRenderers = CollectCaptureRenderers(character, options.includeShadow);
                if (spriteRenderers.Count == 0)
                {
                    throw new InvalidOperationException($"Prefab at [{assetPath}] has no visible SpriteRenderer after switching to front idle state.");
                }

                var bounds = CalculateBounds(spriteRenderers);
                previewUtility = new PreviewRenderUtility(true, true);
                previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewUtility.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                previewUtility.lights[0].intensity = 0f;
                previewUtility.lights[1].intensity = 0f;
                ConfigureCaptureCamera(previewUtility.camera, bounds, options);
                previewUtility.AddSingleGO(prefabRoot);
                previewUtility.BeginPreview(new Rect(0f, 0f, options.width, options.height), GUIStyle.none);
                previewUtility.camera.Render();
                previewRenderTexture = previewUtility.EndPreview() as RenderTexture;
                if (previewRenderTexture == null)
                {
                    throw new InvalidOperationException("PreviewRenderUtility did not return a RenderTexture.");
                }

                texture = new Texture2D(options.width, options.height, TextureFormat.ARGB32, false);
                var previousActiveRenderTexture = RenderTexture.active;
                try
                {
                    RenderTexture.active = previewRenderTexture;
                    texture.ReadPixels(new Rect(0f, 0f, options.width, options.height), 0, 0);
                    texture.Apply(false, false);
                    RecenterOpaquePixels(texture);
                }
                finally
                {
                    RenderTexture.active = previousActiveRenderTexture;
                }

                var outputDirectory = Path.GetDirectoryName(absoluteOutputPath);
                if (string.IsNullOrWhiteSpace(outputDirectory))
                {
                    throw new InvalidOperationException($"Could not resolve output directory for [{absoluteOutputPath}].");
                }

                Directory.CreateDirectory(outputDirectory);
                File.WriteAllBytes(absoluteOutputPath, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                if (previewRenderTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(previewRenderTexture);
                }

                if (previewUtility != null)
                {
                    previewUtility.Cleanup();
                }

                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        private static void PrepareCharacterForPortraitCapture(Character4D character, HeroEditorPortraitExportOptions options)
        {
            character.Initialize();
            character.SetDirection(Vector2.down);
            character.SetExpression("Default");

            if (character.Shadows != null)
            {
                foreach (var shadow in character.Shadows.Where(shadow => shadow != null))
                {
                    shadow.SetActive(options.includeShadow);
                }
            }

            var animator = character.Animator != null ? character.Animator : character.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                return;
            }

            animator.Rebind();
            animator.Update(0f);

            if (character.AnimationManager != null)
            {
                character.AnimationManager.IsAction = false;
                character.AnimationManager.SetState(CharacterState.Idle);
            }
            else
            {
                animator.SetBool("Action", false);
                animator.SetInteger("State", (int)CharacterState.Idle);
            }

            animator.Update(0.05f);
            animator.Update(0f);
            animator.speed = 0f;
        }

        private static List<SpriteRenderer> CollectCaptureRenderers(Character4D character, bool includeShadow)
        {
            return character.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer != null)
                .Where(renderer => renderer.enabled)
                .Where(renderer => renderer.gameObject.activeInHierarchy)
                .Where(renderer => renderer.sprite != null)
                .Where(renderer => includeShadow || !renderer.name.Contains("Shadow", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static Bounds CalculateBounds(IReadOnlyList<SpriteRenderer> spriteRenderers)
        {
            var bounds = spriteRenderers[0].bounds;
            for (var i = 1; i < spriteRenderers.Count; i++)
            {
                bounds.Encapsulate(spriteRenderers[i].bounds);
            }

            return bounds;
        }

        private static void ConfigureCaptureCamera(Camera captureCamera, Bounds bounds, HeroEditorPortraitExportOptions options)
        {
            const float SafeFrameMultiplier = 1.25f;

            captureCamera.orthographic = true;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureCamera.nearClipPlane = 0.01f;
            captureCamera.farClipPlane = 100f;

            var aspect = options.width / (float)options.height;
            var paddedExtents = bounds.extents * Mathf.Max(1f, options.paddingMultiplier) * SafeFrameMultiplier;
            var sizeFromHeight = paddedExtents.y;
            var sizeFromWidth = paddedExtents.x / Mathf.Max(0.01f, aspect);

            captureCamera.orthographicSize = Mathf.Max(0.1f, sizeFromHeight, sizeFromWidth);
            captureCamera.transform.position = new Vector3(
                bounds.center.x + options.framingOffset.x,
                bounds.center.y + options.framingOffset.y,
                bounds.center.z - 10f);
            captureCamera.transform.rotation = Quaternion.identity;
        }

        private static void RecenterOpaquePixels(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();

            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var alpha = pixels[(y * width) + x].a;
                    if (alpha == 0)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return;
            }

            var contentWidth = maxX - minX + 1;
            var contentHeight = maxY - minY + 1;
            var targetX = Mathf.Max(0, (width - contentWidth) / 2);
            var targetY = Mathf.Max(0, (height - contentHeight) / 2);
            var centeredPixels = new Color32[pixels.Length];

            for (var y = 0; y < contentHeight; y++)
            {
                Array.Copy(
                    pixels,
                    ((minY + y) * width) + minX,
                    centeredPixels,
                    ((targetY + y) * width) + targetX,
                    contentWidth);
            }

            texture.SetPixels32(centeredPixels);
            texture.Apply(false, false);
        }

        private static HeroEditorPortraitExportOptions NormalizeOptions(HeroEditorPortraitExportOptions options)
        {
            var normalized = options ?? new HeroEditorPortraitExportOptions();
            normalized.width = Mathf.Max(64, normalized.width);
            normalized.height = Mathf.Max(64, normalized.height);
            normalized.paddingMultiplier = Mathf.Max(1f, normalized.paddingMultiplier);
            normalized.relativeOutputFolder = string.IsNullOrWhiteSpace(normalized.relativeOutputFolder)
                ? HeroEditorPortraitExportOptions.DefaultRelativeOutputFolder
                : normalized.relativeOutputFolder.Trim();
            normalized.fileSuffix = string.IsNullOrWhiteSpace(normalized.fileSuffix)
                ? "_idle_front"
                : normalized.fileSuffix.Trim();
            normalized.fileSuffix = SanitizeFileName(normalized.fileSuffix);
            return normalized;
        }

        private static bool IsPrefabAsset(GameObject prefab)
        {
            return prefab != null
                && EditorUtility.IsPersistent(prefab)
                && PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab;
        }

        private static bool ContainsCharacter4D(GameObject prefab)
        {
            return prefab != null && prefab.GetComponentInChildren<Character4D>(true) != null;
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            return new string(fileName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        }

        private static string ReadArgument(IReadOnlyList<string> arguments, string argumentName)
        {
            for (var i = 0; i < arguments.Count - 1; i++)
            {
                if (string.Equals(arguments[i], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        private static int ReadIntArgument(IReadOnlyList<string> arguments, string argumentName, int defaultValue)
        {
            var value = ReadArgument(arguments, argumentName);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        private static float ReadFloatArgument(IReadOnlyList<string> arguments, string argumentName, float defaultValue)
        {
            var value = ReadArgument(arguments, argumentName);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }

        private static bool ReadBoolArgument(IReadOnlyList<string> arguments, string argumentName, bool defaultValue)
        {
            var value = ReadArgument(arguments, argumentName);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
