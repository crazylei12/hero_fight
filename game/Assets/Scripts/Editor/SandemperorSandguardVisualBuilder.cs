using System;
using System.Collections.Generic;
using System.IO;
using Fight.Data;
using Fight.UI.Presentation.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class SandemperorSandguardVisualBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/VFX/Skills/SandemperorSandguard.prefab";

        private const string BuildMenuPath = "Fight/Stage 01/Build Sandemperor Sandguard Visual";
        private const string SourceSheetPath = "Assets/Art/VFX/Source/SandemperorSandguardSource.png";
        private const string ResourcesRoot = "Assets/Resources/Stage01Demo/VFX/Deployables/SandemperorSandguard";
        private const string ResourcesPrefix = "Stage01Demo/VFX/Deployables/SandemperorSandguard";
        private const string ActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/mage_003_sandemperor/Raise Sandguard.asset";
        private const string UltimateSkillAssetPath = "Assets/Data/Stage01Demo/Skills/mage_003_sandemperor/Imperial Encirclement.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/SandemperorSandguardVisualBuilder.cs";
        private const string AnimatorScriptAssetPath = "Assets/Scripts/UI/Presentation/Skills/DeployableProxySpriteSheetAnimator.cs";
        private const int ExpectedFrameColumns = 8;
        private const int ExpectedActionRows = 2;
        private const float GridLineCoverageRatio = 0.70f;
        private const int GridLinePaddingPixels = 1;
        private const int EdgeLineMaxDistancePixels = 4;
        private const int GridLineMergeGapPixels = 2;
        private const int InteriorCheckerBackgroundMinPixels = 16;
        private const float InteriorCheckerBackgroundMinBrightRatio = 0.18f;
        private const float InteriorCheckerBackgroundMaxBrightRatio = 0.92f;
        private const float PixelsPerUnit = 64f;
        private const float RootScale = 0.65f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.07f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 0, "idle", 7f, true),
            new ClipBuildSpec("Attack", 1, "attack", 13f, false),
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
        public static void BuildSandguardVisual()
        {
            BuildAll();
            Debug.Log("Sandemperor sandguard sprite visual prefab rebuilt.");
        }

        public static void BuildSandguardVisualBatch()
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

        public static void ConfigureDeployableEffectVisuals(SkillEffectData effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.deployableProxySpawnVfxPrefab = null;
            effect.deployableProxyLoopVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            effect.deployableProxyRemovalVfxPrefab = null;
            effect.deployableProxyVfxLocalOffset = Vector3.zero;
            effect.deployableProxyVfxEulerAngles = Vector3.zero;
            effect.deployableProxyVfxScaleMultiplier = Vector3.one;
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
                BuildSandguardVisual();
                return;
            }

            SyncSkillAssets();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
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

            return GetLatestTimestampUtc(SourceSheetPath, BuilderScriptAssetPath, AnimatorScriptAssetPath)
                > GetLatestTimestampUtc(PrefabPath, ResourcesRoot);
        }

        private static void BuildAll()
        {
            AssetDatabase.ImportAsset(SourceSheetPath, ImportAssetOptions.ForceUpdate);
            GenerateFrameFolders();
            CreatePrefab();
            SyncSkillAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing Sandemperor sandguard source sheet: {SourceSheetPath}");
            }

            try
            {
                EnsureFolder(ResourcesRoot);
                var grid = DetectGrid(sourceTexture);
                var allFrameRects = new List<RectInt>();
                foreach (var spec in ClipBuildSpecs)
                {
                    allFrameRects.AddRange(BuildFrameRects(grid, spec.SourceRow));
                }

                var outputFrameWidth = 1;
                var outputFrameHeight = 1;
                foreach (var rect in allFrameRects)
                {
                    outputFrameWidth = Mathf.Max(outputFrameWidth, rect.width);
                    outputFrameHeight = Mathf.Max(outputFrameHeight, rect.height);
                }

                foreach (var spec in ClipBuildSpecs)
                {
                    var folderPath = $"{ResourcesRoot}/{spec.ClipKey}";
                    EnsureFolder(folderPath);
                    ClearGeneratedPngs(folderPath);

                    var frameRects = BuildFrameRects(grid, spec.SourceRow);
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

        private static void CreatePrefab()
        {
            EnsureFolder("Assets/Prefabs/VFX/Skills");

            var root = new GameObject("SandemperorSandguard");
            root.transform.localScale = new Vector3(RootScale, RootScale, 1f);
            root.AddComponent<SortingGroup>();

            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 0;

            var animator = root.AddComponent<DeployableProxySpriteSheetAnimator>();
            animator.Configure(
                spriteRenderer,
                ResourcesPrefix,
                "Idle",
                "Attack",
                ClipBuildSpecs[0].FramesPerSecond,
                ClipBuildSpecs[1].FramesPerSecond,
                PixelsPerUnit,
                FootPivot,
                facesLeftByDefault: false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SyncSkillAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            SyncSkillAsset(ActiveSkillAssetPath, prefab);
            SyncSkillAsset(UltimateSkillAssetPath, prefab);
        }

        private static void SyncSkillAsset(string skillAssetPath, GameObject prefab)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(skillAssetPath);
            if (skill?.effects == null)
            {
                return;
            }

            var changed = false;
            foreach (var effect in skill.effects)
            {
                if (effect == null || effect.effectType != SkillEffectType.CreateDeployableProxy)
                {
                    continue;
                }

                if (effect.deployableProxySpawnVfxPrefab != null
                    || effect.deployableProxyLoopVfxPrefab != prefab
                    || effect.deployableProxyRemovalVfxPrefab != null
                    || effect.deployableProxyVfxLocalOffset != Vector3.zero
                    || effect.deployableProxyVfxEulerAngles != Vector3.zero
                    || effect.deployableProxyVfxScaleMultiplier != Vector3.one)
                {
                    effect.deployableProxySpawnVfxPrefab = null;
                    effect.deployableProxyLoopVfxPrefab = prefab;
                    effect.deployableProxyRemovalVfxPrefab = null;
                    effect.deployableProxyVfxLocalOffset = Vector3.zero;
                    effect.deployableProxyVfxEulerAngles = Vector3.zero;
                    effect.deployableProxyVfxScaleMultiplier = Vector3.one;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(skill);
            }
        }

        private static RectInt[] BuildFrameRects(GridSpec grid, int sourceRow)
        {
            if (sourceRow < 0 || sourceRow >= grid.HorizontalLines.Length - 1)
            {
                throw new InvalidOperationException($"Sandemperor sandguard source row {sourceRow} is outside the detected grid.");
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
            NormalizeDetectedLines(groups, expectedCount, majorLength);

            if (groups.Count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Sandemperor sandguard source grid detection expected {expectedCount} {(scanColumns ? "vertical" : "horizontal")} lines, found {groups.Count}.");
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

        private static void NormalizeDetectedLines(List<GridLine> groups, int expectedCount, int majorLength)
        {
            MergeNearbyLineGroups(groups);

            if (groups.Count != expectedCount - 1 || groups.Count == 0)
            {
                return;
            }

            if (groups[0].Min > EdgeLineMaxDistancePixels)
            {
                groups.Insert(0, new GridLine(0, GridLinePaddingPixels, 0));
                return;
            }

            var lastGroup = groups[groups.Count - 1];
            if (lastGroup.Max < majorLength - 1 - EdgeLineMaxDistancePixels)
            {
                groups.Add(new GridLine(majorLength - 1 - GridLinePaddingPixels, majorLength - 1, 0));
            }
        }

        private static void MergeNearbyLineGroups(List<GridLine> groups)
        {
            for (var i = 0; i < groups.Count - 1; i++)
            {
                var current = groups[i];
                var next = groups[i + 1];
                if (next.Min - current.Max - 1 > GridLineMergeGapPixels)
                {
                    continue;
                }

                groups[i] = new GridLine(
                    current.Min,
                    next.Max,
                    Mathf.Max(current.Peak, next.Peak));
                groups.RemoveAt(i + 1);
                i--;
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
            var isDarkLine = average <= 110f && max - min <= 45;
            var isMediumLine = average >= 120f && average <= 220f && max - min <= 12;
            return isDarkLine || isMediumLine;
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
            public ClipBuildSpec(string clipKey, int sourceRow, string filePrefix, float framesPerSecond, bool loop)
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
