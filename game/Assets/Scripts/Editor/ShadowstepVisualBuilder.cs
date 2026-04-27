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
    public static class ShadowstepVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Shadowstep Visual";
        private const string SourceSheetPath = "Assets/Art/Heroes/assassin_001_shadowstep/shadowstep_source_sheet.png";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/assassin_001_shadowstep";
        private const string ResourcesPrefix = "HeroPreview/assassin_001_shadowstep";
        private const string ShadowstepPrefabPath = "Assets/Prefabs/Heroes/assassin_001_shadowstep/Shadowstep.prefab";
        private const string ShadowstepPortraitPath = "Assets/Prefabs/Heroes/assassin_001_shadowstep/Shadowstep_idle_front.png";
        private const string ShadowstepHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/assassin_001_shadowstep/Shadowstep.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/ShadowstepVisualBuilder.cs";
        private const int ExpectedFrameColumns = 9;
        private const int ExpectedActionRows = 7;
        private const int GridLinePaddingPixels = 4;
        private const int EdgeArtifactScanPixels = 12;
        private const float EdgeArtifactLineCoverageRatio = 0.55f;
        private const int InteriorCheckerBackgroundMinPixels = 16;
        private const float InteriorCheckerBackgroundMinBrightRatio = 0.18f;
        private const float InteriorCheckerBackgroundMaxBrightRatio = 0.96f;
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.07f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 0, "idle", 7f, true),
            new ClipBuildSpec("Run", 1, "run", 12f, true),
            new ClipBuildSpec("Attack1", 2, "attack", 15f, false),
            new ClipBuildSpec("Skill", 3, "shadow_blink", 12f, false),
            new ClipBuildSpec("Ult", 4, "smoke_veil", 10f, false),
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
        public static void BuildShadowstepVisual()
        {
            BuildAll();
            Debug.Log("Shadowstep sprite-sheet visual prefab rebuilt.");
        }

        public static void BuildShadowstepVisualBatch()
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
                BuildShadowstepVisual();
                return;
            }

            SyncShadowstepHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ShadowstepPrefabPath) == null)
            {
                return true;
            }

            foreach (var spec in ClipBuildSpecs)
            {
                if (!AssetDatabase.IsValidFolder($"{ResourcesRoot}/{spec.ClipKey}"))
                {
                    return true;
                }
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SourceSheetPath)
                > GetLatestTimestampUtc(ShadowstepPrefabPath, ResourcesRoot, ShadowstepPortraitPath);
        }

        private static void BuildAll()
        {
            AssetDatabase.ImportAsset(SourceSheetPath, ImportAssetOptions.ForceUpdate);
            GenerateFrameFolders();
            CreateShadowstepPortrait();
            CreateShadowstepPrefab();
            SyncShadowstepHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing Shadowstep source sheet: {SourceSheetPath}");
            }

            try
            {
                EnsureFolder(ResourcesRoot);
                var grid = BuildFixedGrid(sourceTexture);
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

        private static void CreateShadowstepPortrait()
        {
            EnsureFolder("Assets/Prefabs/Heroes/assassin_001_shadowstep");
            var sourcePath = $"{ResourcesRoot}/Idle/idle_00.png";
            var absoluteSourcePath = ToAbsolutePath(sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                return;
            }

            File.Copy(absoluteSourcePath, ToAbsolutePath(ShadowstepPortraitPath), true);
            ApplyFrameTextureImporter(ShadowstepPortraitPath);
        }

        private static void CreateShadowstepPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/assassin_001_shadowstep");

            var root = new GameObject("Shadowstep");
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

            PrefabUtility.SaveAsPrefabAsset(root, ShadowstepPrefabPath);
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

        private static void SyncShadowstepHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ShadowstepHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowstepPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowstepPortraitPath);
            if (portrait != null)
            {
                hero.visualConfig.portrait = portrait;
            }

            hero.visualConfig.battlePrefab = prefab;
            hero.visualConfig.animatorController = null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            EditorUtility.SetDirty(hero);
        }

        private static GridSpec BuildFixedGrid(Texture2D sourceTexture)
        {
            if (sourceTexture.width % ExpectedFrameColumns != 0 || sourceTexture.height % ExpectedActionRows != 0)
            {
                throw new InvalidOperationException(
                    $"Shadowstep source sheet must be an exact {ExpectedFrameColumns}x{ExpectedActionRows} grid. Current size: {sourceTexture.width}x{sourceTexture.height}.");
            }

            var cellWidth = sourceTexture.width / ExpectedFrameColumns;
            var cellHeight = sourceTexture.height / ExpectedActionRows;
            return new GridSpec(cellWidth, cellHeight);
        }

        private static RectInt[] BuildFrameRects(GridSpec grid, int sourceRow)
        {
            if (sourceRow < 0 || sourceRow >= ExpectedActionRows)
            {
                throw new InvalidOperationException($"Shadowstep source row {sourceRow} is outside the fixed grid.");
            }

            var rects = new RectInt[ExpectedFrameColumns];
            var yMin = sourceRow * grid.CellHeight + GridLinePaddingPixels;
            var yMax = (sourceRow + 1) * grid.CellHeight - 1 - GridLinePaddingPixels;
            for (var i = 0; i < rects.Length; i++)
            {
                var xMin = i * grid.CellWidth + GridLinePaddingPixels;
                var xMax = (i + 1) * grid.CellWidth - 1 - GridLinePaddingPixels;
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
            RemoveEdgeDividerArtifacts(topLeftPixels, outputFrameWidth, outputFrameHeight);

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

            RemoveInteriorCheckerBackground(pixels, width, height);
        }

        private static void RemoveEdgeDividerArtifacts(Color32[] pixels, int width, int height)
        {
            var scanRows = Mathf.Min(EdgeArtifactScanPixels, height);
            var scanColumns = Mathf.Min(EdgeArtifactScanPixels, width);
            for (var y = 0; y < scanRows; y++)
            {
                if (GetRowDividerCoverage(pixels, width, y) >= EdgeArtifactLineCoverageRatio)
                {
                    ClearRowDividerPixels(pixels, width, y);
                }
            }

            for (var y = height - scanRows; y < height; y++)
            {
                if (GetRowDividerCoverage(pixels, width, y) >= EdgeArtifactLineCoverageRatio)
                {
                    ClearRowDividerPixels(pixels, width, y);
                }
            }

            for (var x = 0; x < scanColumns; x++)
            {
                if (GetColumnDividerCoverage(pixels, width, height, x) >= EdgeArtifactLineCoverageRatio)
                {
                    ClearColumnDividerPixels(pixels, width, height, x);
                }
            }

            for (var x = width - scanColumns; x < width; x++)
            {
                if (GetColumnDividerCoverage(pixels, width, height, x) >= EdgeArtifactLineCoverageRatio)
                {
                    ClearColumnDividerPixels(pixels, width, height, x);
                }
            }
        }

        private static float GetRowDividerCoverage(Color32[] pixels, int width, int y)
        {
            var count = 0;
            var rowStart = y * width;
            for (var x = 0; x < width; x++)
            {
                if (IsGridDividerArtifactPixel(pixels[rowStart + x]))
                {
                    count++;
                }
            }

            return count / (float)width;
        }

        private static float GetColumnDividerCoverage(Color32[] pixels, int width, int height, int x)
        {
            var count = 0;
            for (var y = 0; y < height; y++)
            {
                if (IsGridDividerArtifactPixel(pixels[y * width + x]))
                {
                    count++;
                }
            }

            return count / (float)height;
        }

        private static void ClearRowDividerPixels(Color32[] pixels, int width, int y)
        {
            var rowStart = y * width;
            for (var x = 0; x < width; x++)
            {
                ClearDividerPixel(pixels, rowStart + x);
            }
        }

        private static void ClearColumnDividerPixels(Color32[] pixels, int width, int height, int x)
        {
            for (var y = 0; y < height; y++)
            {
                ClearDividerPixel(pixels, y * width + x);
            }
        }

        private static void ClearDividerPixel(Color32[] pixels, int index)
        {
            if (!IsGridDividerArtifactPixel(pixels[index]))
            {
                return;
            }

            var color = pixels[index];
            color.a = 0;
            pixels[index] = color;
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
            return average >= 168f && max - min <= 34;
        }

        private static bool IsGridDividerArtifactPixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            var isPurpleDivider = color.b >= color.r + 8
                && color.b >= color.g + 6
                && color.r >= 70
                && color.g >= 45
                && average >= 80f
                && average <= 230f;
            var isNeutralDivider = average >= 120f && average <= 235f && max - min <= 22;
            return isPurpleDivider || isNeutralDivider;
        }

        private static void RemoveInteriorCheckerBackground(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var component = new List<int>();

            for (var i = 0; i < pixels.Length; i++)
            {
                if (visited[i] || !IsCheckerBackgroundPixel(pixels[i]))
                {
                    continue;
                }

                CollectBackgroundComponent(i, width, height, pixels, visited, queue, component);
                if (!ShouldRemoveInteriorCheckerBackground(component, pixels))
                {
                    continue;
                }

                for (var componentIndex = 0; componentIndex < component.Count; componentIndex++)
                {
                    pixels[component[componentIndex]].a = 0;
                }
            }
        }

        private static void CollectBackgroundComponent(
            int startIndex,
            int width,
            int height,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue,
            List<int> component)
        {
            queue.Clear();
            component.Clear();

            visited[startIndex] = true;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                component.Add(index);

                var x = index % width;
                var y = index / width;
                EnqueueComponentPixel(x + 1, y, width, height, pixels, visited, queue);
                EnqueueComponentPixel(x - 1, y, width, height, pixels, visited, queue);
                EnqueueComponentPixel(x, y + 1, width, height, pixels, visited, queue);
                EnqueueComponentPixel(x, y - 1, width, height, pixels, visited, queue);
            }
        }

        private static void EnqueueComponentPixel(
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

        private static bool ShouldRemoveInteriorCheckerBackground(List<int> component, Color32[] pixels)
        {
            if (component.Count < InteriorCheckerBackgroundMinPixels)
            {
                return false;
            }

            var brightPixelCount = 0;
            for (var i = 0; i < component.Count; i++)
            {
                if (IsBrightCheckerBackgroundPixel(pixels[component[i]]))
                {
                    brightPixelCount++;
                }
            }

            var brightRatio = brightPixelCount / (float)component.Count;
            return brightRatio >= InteriorCheckerBackgroundMinBrightRatio
                && brightRatio <= InteriorCheckerBackgroundMaxBrightRatio;
        }

        private static bool IsBrightCheckerBackgroundPixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            return average >= 218f && max - min <= 35;
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
            public GridSpec(int cellWidth, int cellHeight)
            {
                CellWidth = cellWidth;
                CellHeight = cellHeight;
            }

            public int CellWidth { get; }
            public int CellHeight { get; }
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
