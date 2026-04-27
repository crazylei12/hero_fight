using System;
using System.Collections.Generic;
using System.IO;
using Fight.Data;
using Fight.UI;
using Fight.UI.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class MonkVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Monk Visual";
        private const string SourceSheetPath = "Assets/Art/Heroes/support_003_monk/monk_source_sheet.png";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/support_003_monk";
        private const string ResourcesPrefix = "HeroPreview/support_003_monk";
        private const string MonkPrefabPath = "Assets/Prefabs/Heroes/support_003_monk/Monk.prefab";
        private const string MonkPortraitPath = "Assets/Prefabs/Heroes/support_003_monk/Monk_idle_front.png";
        private const string MonkHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_003_monk/Monk.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/MonkVisualBuilder.cs";
        private const int ExpectedFrameColumns = 9;
        private const int ExpectedActionRows = 7;
        private const float GridLineCoverageRatio = 0.70f;
        private const int GridLinePaddingPixels = 1;
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.07f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 0, "idle", 7f, true),
            new ClipBuildSpec("Run", 1, "run", 12f, true),
            new ClipBuildSpec("Attack1", 2, "attack", 15f, false),
            new ClipBuildSpec("Skill", 3, "renewing_pulse", 12f, false),
            new ClipBuildSpec("Ult", 4, "guardian_mantra", 10f, false),
            new ClipBuildSpec("Hit", 5, "hit", 10f, false),
            new ClipBuildSpec("Death", 6, "death", 8f, false),
        };

        private static bool autoBuildScheduled;

        [InitializeOnLoadMethod]
        private static void ScheduleAutoBuildIfNeeded()
        {
            if (Application.isBatchMode || autoBuildScheduled)
            {
                return;
            }

            autoBuildScheduled = true;
            EditorApplication.delayCall += TryAutoBuildIfNeeded;
        }

        [MenuItem(BuildMenuPath)]
        public static void BuildMonkVisual()
        {
            BuildAll();
            Debug.Log("Monk sprite-sheet visual prefab rebuilt.");
        }

        public static void BuildMonkVisualBatch()
        {
            try
            {
                BuildAll();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void TryAutoBuildIfNeeded()
        {
            autoBuildScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoBuildIfNeeded();
                return;
            }

            if (NeedsRebuild())
            {
                BuildMonkVisual();
                return;
            }

            SyncMonkHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MonkPrefabPath) == null)
            {
                return true;
            }

            foreach (var spec in ClipBuildSpecs)
            {
                if (!AssetDatabase.IsValidFolder($"{ResourcesRoot}/{spec.ClipKey}"))
                {
                    return true;
                }

                for (var i = 0; i < ExpectedFrameColumns; i++)
                {
                    if (!File.Exists(ToAbsolutePath($"{ResourcesRoot}/{spec.ClipKey}/{spec.FilePrefix}_{i:00}.png")))
                    {
                        return true;
                    }
                }
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SourceSheetPath)
                > GetLatestTimestampUtc(MonkPrefabPath, ResourcesRoot, MonkPortraitPath);
        }

        private static void BuildAll()
        {
            AssetDatabase.ImportAsset(SourceSheetPath, ImportAssetOptions.ForceUpdate);
            GenerateFrameFolders();
            CreateMonkPortrait();
            CreateMonkPrefab();
            SyncMonkHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing Monk source sheet: {SourceSheetPath}");
            }

            try
            {
                EnsureFolder(ResourcesRoot);
                var grid = DetectGrid(sourceTexture);
                foreach (var spec in ClipBuildSpecs)
                {
                    var folderPath = $"{ResourcesRoot}/{spec.ClipKey}";
                    EnsureFolder(folderPath);
                    ClearGeneratedPngs(folderPath);

                    var frameRects = BuildFrameRects(grid, spec.SourceRow);
                    var outputFrameWidth = 1;
                    var outputFrameHeight = 1;
                    foreach (var rect in frameRects)
                    {
                        outputFrameWidth = Mathf.Max(outputFrameWidth, rect.width);
                        outputFrameHeight = Mathf.Max(outputFrameHeight, rect.height);
                    }

                    for (var i = 0; i < frameRects.Length; i++)
                    {
                        var frameTexture = CropFrame(sourceTexture, frameRects[i], outputFrameWidth, outputFrameHeight);
                        var assetPath = $"{folderPath}/{spec.FilePrefix}_{i:00}.png";
                        File.WriteAllBytes(ToAbsolutePath(assetPath), frameTexture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(frameTexture);
                        ApplyFrameTextureImporter(assetPath);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceTexture);
            }
        }

        private static void CreateMonkPortrait()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_003_monk");
            var sourcePath = $"{ResourcesRoot}/Idle/idle_00.png";
            var absoluteSourcePath = ToAbsolutePath(sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                return;
            }

            File.Copy(absoluteSourcePath, ToAbsolutePath(MonkPortraitPath), true);
            ApplyFrameTextureImporter(MonkPortraitPath);
        }

        private static void CreateMonkPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_003_monk");

            var root = new GameObject("Monk");
            root.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            root.AddComponent<SortingGroup>();

            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 0;

            var idlePreview = root.AddComponent<SpriteTextureFrameAnimator>();
            idlePreview.Configure(
                $"{ResourcesPrefix}/Idle",
                7f,
                PixelsPerUnit,
                FootPivot,
                loop: true);

            var visualConfig = root.AddComponent<SpriteSheetBattleVisualConfig>();
            ConfigureVisualConfig(visualConfig, spriteRenderer);

            PrefabUtility.SaveAsPrefabAsset(root, MonkPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureVisualConfig(SpriteSheetBattleVisualConfig visualConfig, SpriteRenderer spriteRenderer)
        {
            var serialized = new SerializedObject(visualConfig);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("resourcesRoot").stringValue = ResourcesPrefix;
            serialized.FindProperty("pixelsPerUnit").floatValue = PixelsPerUnit;
            serialized.FindProperty("spritePivot").vector2Value = FootPivot;

            var clipsProperty = serialized.FindProperty("clips");
            clipsProperty.arraySize = ClipBuildSpecs.Length;
            for (var i = 0; i < ClipBuildSpecs.Length; i++)
            {
                var spec = ClipBuildSpecs[i];
                var clipProperty = clipsProperty.GetArrayElementAtIndex(i);
                clipProperty.FindPropertyRelative("key").stringValue = spec.ClipKey;
                clipProperty.FindPropertyRelative("resourcesFolder").stringValue = spec.ClipKey;
                clipProperty.FindPropertyRelative("framesPerSecond").floatValue = spec.FramesPerSecond;
                clipProperty.FindPropertyRelative("loop").boolValue = spec.Loop;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SyncMonkHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(MonkHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonkPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(MonkPortraitPath);
            if (portrait != null)
            {
                hero.visualConfig.portrait = portrait;
            }

            hero.visualConfig.battlePrefab = prefab;
            hero.visualConfig.animatorController = null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            EditorUtility.SetDirty(hero);
        }

        private static RectInt[] BuildFrameRects(GridSpec grid, int sourceRow)
        {
            if (sourceRow < 0 || sourceRow >= grid.HorizontalLines.Length - 1)
            {
                throw new InvalidOperationException($"Monk source row {sourceRow} is outside the detected grid.");
            }

            var rects = new RectInt[ExpectedFrameColumns];
            var yMin = grid.HorizontalLines[sourceRow].Max + 1 + GridLinePaddingPixels;
            var yMax = grid.HorizontalLines[sourceRow + 1].Min - 1 - GridLinePaddingPixels;
            for (var i = 0; i < rects.Length; i++)
            {
                var xMin = grid.VerticalLines[i].Max + 1 + GridLinePaddingPixels;
                var xMax = grid.VerticalLines[i + 1].Min - 1 - GridLinePaddingPixels;
                rects[i] = new RectInt(
                    xMin,
                    yMin,
                    Mathf.Max(1, xMax - xMin + 1),
                    Mathf.Max(1, yMax - yMin + 1));
            }

            return rects;
        }

        private static Texture2D CropFrame(Texture2D sourceTexture, RectInt pngRect, int outputFrameWidth, int outputFrameHeight)
        {
            var topLeftPixels = new Color32[outputFrameWidth * outputFrameHeight];
            var horizontalPadding = Mathf.Max(0, (outputFrameWidth - pngRect.width) / 2);

            for (var y = 0; y < pngRect.height && y < outputFrameHeight; y++)
            {
                var sourceY = pngRect.y + y;
                for (var x = 0; x < pngRect.width && x + horizontalPadding < outputFrameWidth; x++)
                {
                    var sourceXPosition = pngRect.x + x;
                    var outputIndex = y * outputFrameWidth + x + horizontalPadding;
                    if (sourceXPosition < 0
                        || sourceXPosition >= sourceTexture.width
                        || sourceY < 0
                        || sourceY >= sourceTexture.height)
                    {
                        topLeftPixels[outputIndex] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var unityY = sourceTexture.height - 1 - sourceY;
                    topLeftPixels[outputIndex] = sourceTexture.GetPixel(sourceXPosition, unityY);
                }
            }

            RemoveCheckerBackground(topLeftPixels, outputFrameWidth, outputFrameHeight);

            var unityPixels = new Color32[topLeftPixels.Length];
            for (var y = 0; y < outputFrameHeight; y++)
            {
                var unityY = outputFrameHeight - 1 - y;
                Array.Copy(
                    topLeftPixels,
                    y * outputFrameWidth,
                    unityPixels,
                    unityY * outputFrameWidth,
                    outputFrameWidth);
            }

            var frameTexture = new Texture2D(outputFrameWidth, outputFrameHeight, TextureFormat.RGBA32, false);
            frameTexture.SetPixels32(unityPixels);
            frameTexture.filterMode = FilterMode.Point;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.Apply();
            return frameTexture;
        }

        private static GridSpec DetectGrid(Texture2D sourceTexture)
        {
            var verticalLines = DetectLineGroups(
                sourceTexture,
                scanColumns: true,
                ExpectedFrameColumns + 1,
                Mathf.RoundToInt(sourceTexture.height * GridLineCoverageRatio));
            var horizontalLines = DetectLineGroups(
                sourceTexture,
                scanColumns: false,
                ExpectedActionRows + 1,
                Mathf.RoundToInt(sourceTexture.width * GridLineCoverageRatio));
            return new GridSpec(verticalLines, horizontalLines);
        }

        private static GridLine[] DetectLineGroups(
            Texture2D sourceTexture,
            bool scanColumns,
            int expectedCount,
            int threshold)
        {
            var majorLength = scanColumns ? sourceTexture.width : sourceTexture.height;
            var minorLength = scanColumns ? sourceTexture.height : sourceTexture.width;
            var groups = new List<GridLine>();
            var currentMin = -1;
            var currentMax = -1;
            var currentPeak = 0;

            for (var major = 0; major < majorLength; major++)
            {
                var count = 0;
                for (var minor = 0; minor < minorLength; minor++)
                {
                    var x = scanColumns ? major : minor;
                    var yTop = scanColumns ? minor : major;
                    if (IsGridLinePixel(GetPixelTopLeft(sourceTexture, x, yTop)))
                    {
                        count++;
                    }
                }

                if (count >= threshold)
                {
                    if (currentMin < 0)
                    {
                        currentMin = major;
                    }

                    currentMax = major;
                    currentPeak = Mathf.Max(currentPeak, count);
                    continue;
                }

                AddCurrentGroup();
            }

            AddCurrentGroup();

            if (groups.Count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Monk source grid detection expected {expectedCount} {(scanColumns ? "vertical" : "horizontal")} lines, found {groups.Count}.");
            }

            return groups.ToArray();

            void AddCurrentGroup()
            {
                if (currentMin < 0)
                {
                    return;
                }

                groups.Add(new GridLine(currentMin, currentMax, currentPeak));
                currentMin = -1;
                currentMax = -1;
                currentPeak = 0;
            }
        }

        private static Color32 GetPixelTopLeft(Texture2D sourceTexture, int x, int yTop)
        {
            return sourceTexture.GetPixel(x, sourceTexture.height - 1 - yTop);
        }

        private static bool IsGridLinePixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            return average <= 125f && max - min <= 70;
        }

        private static void RemoveCheckerBackground(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (var x = 0; x < width; x++)
            {
                EnqueueIfBackground(x, 0, width, height, pixels, visited, queue);
                EnqueueIfBackground(x, height - 1, width, height, pixels, visited, queue);
            }

            for (var y = 0; y < height; y++)
            {
                EnqueueIfBackground(0, y, width, height, pixels, visited, queue);
                EnqueueIfBackground(width - 1, y, width, height, pixels, visited, queue);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                pixels[index].a = 0;

                var x = index % width;
                var y = index / width;
                EnqueueIfBackground(x + 1, y, width, height, pixels, visited, queue);
                EnqueueIfBackground(x - 1, y, width, height, pixels, visited, queue);
                EnqueueIfBackground(x, y + 1, width, height, pixels, visited, queue);
                EnqueueIfBackground(x, y - 1, width, height, pixels, visited, queue);
            }

        }

        private static void EnqueueIfBackground(
            int x,
            int y,
            int width,
            int height,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            var index = y * width + x;
            if (visited[index] || !IsCheckerBackgroundPixel(pixels[index]))
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(index);
        }

        private static bool IsCheckerBackgroundPixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            var isCheckerFill = average >= 168f && max - min <= 34;
            var isGridBorderLine = average >= 120f && average <= 220f && max - min <= 12;
            return isCheckerFill || isGridBorderLine;
        }

        private static bool TryLoadTexture(string assetPath, out Texture2D texture)
        {
            texture = null;
            var absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return false;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (texture.LoadImage(File.ReadAllBytes(absolutePath), markNonReadable: false))
            {
                return true;
            }

            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
            return false;
        }

        private static void ApplyFrameTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ClearGeneratedPngs(string folderPath)
        {
            var absoluteFolder = ToAbsolutePath(folderPath);
            if (!Directory.Exists(absoluteFolder))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(absoluteFolder, "*.png"))
            {
                File.Delete(path);
                // Keep .meta files so replacing frames does not churn stable sprite asset GUIDs.
            }
        }

        private static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/');
            if (assetFolder == "Assets" || AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var slashIndex = assetFolder.LastIndexOf('/');
            var parent = slashIndex > 0 ? assetFolder.Substring(0, slashIndex) : "Assets";
            var folder = slashIndex > 0 ? assetFolder.Substring(slashIndex + 1) : assetFolder;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static DateTime GetLatestTimestampUtc(params string[] assetPaths)
        {
            var latest = DateTime.MinValue;
            if (assetPaths == null)
            {
                return latest;
            }

            foreach (var assetPath in assetPaths)
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var fullPath = ToAbsolutePath(assetPath);
                if (File.Exists(fullPath))
                {
                    latest = Max(latest, File.GetLastWriteTimeUtc(fullPath));
                    continue;
                }

                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                foreach (var filePath in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                {
                    latest = Max(latest, File.GetLastWriteTimeUtc(filePath));
                }
            }

            return latest;
        }

        private static DateTime Max(DateTime left, DateTime right)
        {
            return left >= right ? left : right;
        }

        private readonly struct GridSpec
        {
            public GridSpec(GridLine[] verticalLines, GridLine[] horizontalLines)
            {
                VerticalLines = verticalLines ?? Array.Empty<GridLine>();
                HorizontalLines = horizontalLines ?? Array.Empty<GridLine>();
            }

            public GridLine[] VerticalLines { get; }
            public GridLine[] HorizontalLines { get; }
        }

        private readonly struct GridLine
        {
            public GridLine(int min, int max, int peak)
            {
                Min = min;
                Max = max;
                Peak = peak;
            }

            public int Min { get; }
            public int Max { get; }
            public int Peak { get; }
        }

        private sealed class ClipBuildSpec
        {
            public ClipBuildSpec(
                string clipKey,
                int sourceRow,
                string filePrefix,
                float framesPerSecond,
                bool loop)
            {
                ClipKey = clipKey;
                SourceRow = sourceRow;
                FilePrefix = filePrefix;
                FramesPerSecond = framesPerSecond;
                Loop = loop;
            }

            public string ClipKey { get; }
            public int SourceRow { get; }
            public string FilePrefix { get; }
            public float FramesPerSecond { get; }
            public bool Loop { get; }
        }
    }
}
