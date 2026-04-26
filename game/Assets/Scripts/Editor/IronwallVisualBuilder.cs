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
    public static class IronwallVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Ironwall Visual";
        private const string SourceSheetPath = "Assets/Art/Heroes/tank_001_ironwall/ironwall_source_sheet.png";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/tank_001_ironwall";
        private const string ResourcesPrefix = "HeroPreview/tank_001_ironwall";
        private const string IronwallPrefabPath = "Assets/Prefabs/Heroes/tank_001_ironwall/Ironwall.prefab";
        private const string IronwallPortraitPath = "Assets/Prefabs/Heroes/tank_001_ironwall/Ironwall_idle_front.png";
        private const string IronwallHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/tank_001_ironwall/Ironwall.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/IronwallVisualBuilder.cs";
        private const int ExpectedFrameColumns = 9;
        private const int ExpectedActionRows = 7;
        private const float GridLineCoverageRatio = 0.75f;
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.07f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 0, "idle", 7f, true),
            new ClipBuildSpec("Run", 1, "run", 12f, true),
            new ClipBuildSpec("Attack1", 2, "attack", 16f, false),
            new ClipBuildSpec("Skill", 3, "bulwark_bond", 14f, false),
            new ClipBuildSpec("Ult", 4, "iron_oath", 12f, false),
            new ClipBuildSpec("Hit", 5, "hit", 12f, false),
            new ClipBuildSpec("Death", 6, "death", 9f, false),
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
        public static void BuildIronwallVisual()
        {
            BuildAll();
            Debug.Log("Ironwall sprite-sheet visual prefab rebuilt.");
        }

        public static void BuildIronwallVisualBatch()
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
                BuildIronwallVisual();
                return;
            }

            SyncIronwallHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(IronwallPrefabPath) == null)
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
                > GetLatestTimestampUtc(IronwallPrefabPath, ResourcesRoot, IronwallPortraitPath);
        }

        private static void BuildAll()
        {
            ApplySourceTextureImporter(SourceSheetPath);
            GenerateFrameFolders();
            CreateIronwallPortrait();
            CreateIronwallPrefab();
            SyncIronwallHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing Ironwall source sheet: {SourceSheetPath}");
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

        private static void CreateIronwallPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/tank_001_ironwall");

            var root = new GameObject("Ironwall");
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

            PrefabUtility.SaveAsPrefabAsset(root, IronwallPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateIronwallPortrait()
        {
            EnsureFolder("Assets/Prefabs/Heroes/tank_001_ironwall");
            var sourcePath = $"{ResourcesRoot}/Idle/idle_00.png";
            var absoluteSourcePath = ToAbsolutePath(sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                return;
            }

            File.Copy(absoluteSourcePath, ToAbsolutePath(IronwallPortraitPath), true);
            ApplyFrameTextureImporter(IronwallPortraitPath);
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

        private static void SyncIronwallHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(IronwallHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IronwallPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(IronwallPortraitPath);
            if (hero.visualConfig.battlePrefab == prefab
                && hero.visualConfig.portrait == portrait
                && hero.visualConfig.animatorController == null
                && !hero.visualConfig.battlePrefabFacesLeftByDefault)
            {
                return;
            }

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
                throw new InvalidOperationException($"Ironwall source row {sourceRow} is outside the detected grid.");
            }

            var rects = new RectInt[ExpectedFrameColumns];
            var yMin = grid.HorizontalLines[sourceRow].Max + 1;
            var yMax = grid.HorizontalLines[sourceRow + 1].Min - 1;
            for (var i = 0; i < rects.Length; i++)
            {
                var xMin = grid.VerticalLines[i].Max + 1;
                var xMax = grid.VerticalLines[i + 1].Min - 1;
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

            RemoveConnectedFrameEdgeBackground(topLeftPixels, outputFrameWidth, outputFrameHeight);
            RemoveInteriorCheckerBackgroundComponents(topLeftPixels, outputFrameWidth, outputFrameHeight);
            RemoveLightMatteComponents(topLeftPixels, outputFrameWidth, outputFrameHeight);
            RemoveLowNeutralBottomLineComponents(topLeftPixels, outputFrameWidth, outputFrameHeight);
            ClearOuterFrameBorder(topLeftPixels, outputFrameWidth, outputFrameHeight);

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
                    $"Ironwall source grid detection expected {expectedCount} {(scanColumns ? "vertical" : "horizontal")} lines, found {groups.Count}.");
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
            return color.a != 0
                && color.r <= 55
                && color.g <= 55
                && color.b <= 55;
        }

        private static void RemoveConnectedFrameEdgeBackground(Color32[] pixels, int width, int height)
        {
            const int edgeSeedDepth = 4;
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var seedDepthX = Mathf.Min(edgeSeedDepth, width);
            var seedDepthY = Mathf.Min(edgeSeedDepth, height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < seedDepthY; y++)
                {
                    EnqueueIfBackground(x, y, width, height, pixels, visited, queue);
                    EnqueueIfBackground(x, height - 1 - y, width, height, pixels, visited, queue);
                }
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < seedDepthX; x++)
                {
                    EnqueueIfBackground(x, y, width, height, pixels, visited, queue);
                    EnqueueIfBackground(width - 1 - x, y, width, height, pixels, visited, queue);
                }
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

        private static void RemoveInteriorCheckerBackgroundComponents(Color32[] pixels, int width, int height)
        {
            const int minInteriorCheckerArea = 20;
            const int minLowerInteriorCheckerArea = 8;

            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var component = new List<int>();
            var lowerRegionMinY = Mathf.RoundToInt(height * 0.65f);

            for (var startIndex = 0; startIndex < pixels.Length; startIndex++)
            {
                if (visited[startIndex] || !IsCheckerBackgroundPixel(pixels[startIndex]))
                {
                    continue;
                }

                visited[startIndex] = true;
                queue.Enqueue(startIndex);
                component.Clear();
                var maxY = 0;

                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    component.Add(index);

                    var x = index % width;
                    var y = index / width;
                    maxY = Mathf.Max(maxY, y);

                    EnqueueComponentNeighbor(x + 1, y);
                    EnqueueComponentNeighbor(x - 1, y);
                    EnqueueComponentNeighbor(x, y + 1);
                    EnqueueComponentNeighbor(x, y - 1);
                }

                var isLargeCheckerPocket = component.Count >= minInteriorCheckerArea;
                var isLowerCheckerPocket = maxY >= lowerRegionMinY && component.Count >= minLowerInteriorCheckerArea;
                if (!isLargeCheckerPocket && !isLowerCheckerPocket)
                {
                    continue;
                }

                foreach (var index in component)
                {
                    pixels[index].a = 0;
                }
            }

            void EnqueueComponentNeighbor(int x, int y)
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
        }

        private static void RemoveLightMatteComponents(Color32[] pixels, int width, int height)
        {
            const int minLowerMatteArea = 8;
            const int maxSpeckleArea = 16;
            const int maxLowerEdgeSpeckleArea = 32;

            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var component = new List<int>();
            var lowerRegionMinY = Mathf.RoundToInt(height * 0.72f);
            var lowerRegionMaxY = Mathf.RoundToInt(height * 0.78f);
            var lowerEdgeCleanupMinY = Mathf.RoundToInt(height * 0.80f);

            for (var startIndex = 0; startIndex < pixels.Length; startIndex++)
            {
                if (visited[startIndex] || !IsLightNeutralMattePixel(pixels[startIndex]))
                {
                    continue;
                }

                visited[startIndex] = true;
                queue.Enqueue(startIndex);
                component.Clear();
                var minY = height;
                var maxY = 0;
                var touchesTransparent = false;

                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    component.Add(index);

                    var x = index % width;
                    var y = index / width;
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                    touchesTransparent |= HasTransparentNeighbor(x, y, width, height, pixels);

                    EnqueueComponentNeighbor(x + 1, y);
                    EnqueueComponentNeighbor(x - 1, y);
                    EnqueueComponentNeighbor(x, y + 1);
                    EnqueueComponentNeighbor(x, y - 1);
                }

                if (!touchesTransparent)
                {
                    continue;
                }

                var isLowerMattePocket = minY >= lowerRegionMinY
                    && maxY >= lowerRegionMaxY
                    && component.Count >= minLowerMatteArea;
                var isLightSpeckle = component.Count <= maxSpeckleArea;
                var isLowerEdgeSpeckle = maxY >= lowerEdgeCleanupMinY
                    && component.Count <= maxLowerEdgeSpeckleArea;
                if (!isLowerMattePocket && !isLightSpeckle && !isLowerEdgeSpeckle)
                {
                    continue;
                }

                foreach (var index in component)
                {
                    pixels[index].a = 0;
                }
            }

            void EnqueueComponentNeighbor(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    return;
                }

                var index = y * width + x;
                if (visited[index] || !IsLightNeutralMattePixel(pixels[index]))
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(index);
            }
        }

        private static bool HasTransparentNeighbor(int x, int y, int width, int height, Color32[] pixels)
        {
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0)
                    {
                        continue;
                    }

                    var neighborX = x + offsetX;
                    var neighborY = y + offsetY;
                    if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
                    {
                        continue;
                    }

                    if (pixels[neighborY * width + neighborX].a == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsLightNeutralMattePixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            return average >= 145f && max - min <= 45;
        }

        private static void RemoveLowNeutralBottomLineComponents(Color32[] pixels, int width, int height)
        {
            const int minLineWidth = 6;
            const int maxLineHeight = 2;
            const int maxLineArea = 64;

            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var component = new List<int>();
            var bottomLineMinY = Mathf.RoundToInt(height * 0.86f);

            for (var startIndex = 0; startIndex < pixels.Length; startIndex++)
            {
                if (visited[startIndex] || !IsLowNeutralMatteLinePixel(pixels[startIndex]))
                {
                    continue;
                }

                visited[startIndex] = true;
                queue.Enqueue(startIndex);
                component.Clear();
                var minX = width;
                var maxX = 0;
                var minY = height;
                var maxY = 0;
                var touchesTransparent = false;

                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    component.Add(index);

                    var x = index % width;
                    var y = index / width;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                    touchesTransparent |= HasTransparentNeighbor(x, y, width, height, pixels);

                    EnqueueComponentNeighbor(x + 1, y);
                    EnqueueComponentNeighbor(x - 1, y);
                    EnqueueComponentNeighbor(x, y + 1);
                    EnqueueComponentNeighbor(x, y - 1);
                }

                var componentWidth = maxX - minX + 1;
                var componentHeight = maxY - minY + 1;
                var isBottomLine = touchesTransparent
                    && minY >= bottomLineMinY
                    && componentWidth >= minLineWidth
                    && componentHeight <= maxLineHeight
                    && component.Count <= maxLineArea;
                if (!isBottomLine)
                {
                    continue;
                }

                foreach (var index in component)
                {
                    pixels[index].a = 0;
                }
            }

            void EnqueueComponentNeighbor(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    return;
                }

                var index = y * width + x;
                if (visited[index] || !IsLowNeutralMatteLinePixel(pixels[index]))
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(index);
            }
        }

        private static bool IsLowNeutralMatteLinePixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            return average >= 120f && max - min <= 65;
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
            if (visited[index] || !IsFrameEdgeBackgroundPixel(pixels[index]))
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
            return average >= 225f && max - min <= 35;
        }

        private static bool IsFrameEdgeBackgroundPixel(Color32 color)
        {
            if (color.a == 0)
            {
                return false;
            }

            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var average = (color.r + color.g + color.b) / 3f;
            return average >= 142f && max - min <= 52;
        }

        private static void ClearOuterFrameBorder(Color32[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            const int borderWidth = 6;
            var maxBorderX = Mathf.Min(borderWidth, width);
            var maxBorderY = Mathf.Min(borderWidth, height);

            for (var y = 0; y < maxBorderY; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x].a = 0;
                    pixels[(height - 1 - y) * width + x].a = 0;
                }
            }

            for (var x = 0; x < maxBorderX; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    pixels[y * width + x].a = 0;
                    pixels[y * width + width - 1 - x].a = 0;
                }
            }
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
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath), markNonReadable: false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                texture = null;
                return false;
            }

            return true;
        }

        private static void ApplySourceTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
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
