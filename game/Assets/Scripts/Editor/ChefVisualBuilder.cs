using System;
using System.IO;
using Fight.Data;
using Fight.UI;
using Fight.UI.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class ChefVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Chef Visual";
        private const string SourceSplitRoot = "Assets/Art/Heroes/support_005_chef/split";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/support_005_chef";
        private const string ResourcesPrefix = "HeroPreview/support_005_chef";
        private const string ChefPrefabPath = "Assets/Prefabs/Heroes/support_005_chef/Chef.prefab";
        private const string ChefPortraitPath = "Assets/Prefabs/Heroes/support_005_chef/Chef_idle_front.png";
        private const string ChefHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_005_chef/Chef.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/ChefVisualBuilder.cs";
        private const float PixelsPerUnit = 64f;
        private const int DividerScanPixels = 24;
        private const int DividerClearRadius = 1;
        private const int DividerEdgeAnchorPixels = 4;
        private const int LeftEdgeSliverMaxWidthPixels = 12;
        private const int LeftEdgeSliverMaxPixels = 220;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.07f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", "Idle", "Idle", "idle", 7f, true),
            new ClipBuildSpec("Run", "Run", "Run", "run", 12f, true),
            new ClipBuildSpec("Attack1", "Attack1", "Attack1", "attack", 15f, false),
            new ClipBuildSpec("Skill", "SkillBurger", "Skill", "burger", 14f, false),
            new ClipBuildSpec("Skill_burger", "SkillBurger", "SkillBurger", "burger", 14f, false),
            new ClipBuildSpec("Skill_hotdog", "SkillHotdog", "SkillHotdog", "hotdog", 14f, false),
            new ClipBuildSpec("Skill_fries", "SkillFries", "SkillFries", "fries", 14f, false),
            new ClipBuildSpec("Ult", "Ult", "Ult", "bigmac", 12f, false),
            new ClipBuildSpec("Hit", "Hit", "Hit", "hit", 10f, false),
            new ClipBuildSpec("Death", "Death", "Death", "death", 8f, false),
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
        public static void BuildChefVisual()
        {
            BuildAll();
            Debug.Log("Chef sprite-sheet visual prefab rebuilt.");
        }

        public static void BuildChefVisualBatch()
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
                BuildChefVisual();
                return;
            }

            SyncChefHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ChefPrefabPath) == null)
            {
                return true;
            }

            foreach (var spec in ClipBuildSpecs)
            {
                if (!AssetDatabase.IsValidFolder($"{ResourcesRoot}/{spec.ResourcesFolder}"))
                {
                    return true;
                }
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SourceSplitRoot)
                > GetLatestTimestampUtc(ChefPrefabPath, ResourcesRoot, ChefPortraitPath);
        }

        private static void BuildAll()
        {
            EnsureFolder(ResourcesRoot);
            CopyClipFrames();
            CreateChefPortrait();
            CreateChefPrefab();
            SyncChefHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CopyClipFrames()
        {
            foreach (var spec in ClipBuildSpecs)
            {
                var sourceFolder = $"{SourceSplitRoot}/{spec.SourceFolder}";
                var sourceAbsoluteFolder = ToAbsolutePath(sourceFolder);
                if (!Directory.Exists(sourceAbsoluteFolder))
                {
                    throw new DirectoryNotFoundException($"Missing Chef split frame folder: {sourceFolder}");
                }

                var sourceFiles = Directory.GetFiles(sourceAbsoluteFolder, $"{spec.FilePrefix}_*.png");
                Array.Sort(sourceFiles, StringComparer.OrdinalIgnoreCase);
                if (sourceFiles.Length == 0)
                {
                    throw new FileNotFoundException($"Chef split frame folder has no PNG frames: {sourceFolder}");
                }

                var resourcesFolder = $"{ResourcesRoot}/{spec.ResourcesFolder}";
                EnsureFolder(resourcesFolder);
                ClearGeneratedPngs(resourcesFolder);

                for (var i = 0; i < sourceFiles.Length; i++)
                {
                    var assetPath = $"{resourcesFolder}/{spec.FilePrefix}_{i:00}.png";
                    CopyCleanedFrame(sourceFiles[i], ToAbsolutePath(assetPath));
                    ApplyFrameTextureImporter(assetPath);
                }
            }
        }

        private static void CopyCleanedFrame(string sourceAbsolutePath, string destinationAbsolutePath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(sourceAbsolutePath)))
                {
                    throw new InvalidDataException($"Could not load Chef frame PNG: {sourceAbsolutePath}");
                }

                RemoveDividerArtifacts(texture);
                File.WriteAllBytes(destinationAbsolutePath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void RemoveDividerArtifacts(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            var width = texture.width;
            var height = texture.height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var pixels = texture.GetPixels32();
            ClearDividerLineBands(pixels, width, height);
            ClearLeftEdgeSlivers(pixels, width, height);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static void ClearDividerLineBands(Color32[] pixels, int width, int height)
        {
            var scanColumns = Mathf.Min(DividerScanPixels, Mathf.Max(0, width / 2));
            var clearColumns = new bool[width];
            for (var x = 0; x < scanColumns; x++)
            {
                if (HasLongDividerRunInColumn(pixels, width, height, x))
                {
                    MarkColumnForClearing(clearColumns, x);
                }
            }

            for (var x = Mathf.Max(scanColumns, width - scanColumns); x < width; x++)
            {
                if (HasLongDividerRunInColumn(pixels, width, height, x))
                {
                    MarkColumnForClearing(clearColumns, x);
                }
            }

            var scanRows = Mathf.Min(DividerScanPixels, Mathf.Max(0, height / 2));
            var clearRows = new bool[height];
            for (var y = 0; y < scanRows; y++)
            {
                if (HasLongDividerRunInRow(pixels, width, height, y))
                {
                    MarkRowForClearing(clearRows, y);
                }
            }

            for (var y = Mathf.Max(scanRows, height - scanRows); y < height; y++)
            {
                if (HasLongDividerRunInRow(pixels, width, height, y))
                {
                    MarkRowForClearing(clearRows, y);
                }
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (clearColumns[x] || clearRows[y])
                    {
                        ClearPixel(pixels, width, x, y);
                    }
                }
            }
        }

        private static void ClearLeftEdgeSlivers(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queueX = new int[pixels.Length];
            var queueY = new int[pixels.Length];

            for (var y = 0; y < height; y++)
            {
                var index = ToIndex(width, 0, y);
                if (visited[index] || !IsVisible(pixels[index]))
                {
                    continue;
                }

                var head = 0;
                var tail = 0;
                var count = 0;
                var maxX = 0;
                queueX[tail] = 0;
                queueY[tail] = y;
                tail++;
                visited[index] = true;

                while (head < tail)
                {
                    var currentX = queueX[head];
                    var currentY = queueY[head];
                    head++;
                    count++;
                    maxX = Mathf.Max(maxX, currentX);

                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (var offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                continue;
                            }

                            var nextX = currentX + offsetX;
                            var nextY = currentY + offsetY;
                            if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                            {
                                continue;
                            }

                            var nextIndex = ToIndex(width, nextX, nextY);
                            if (visited[nextIndex] || !IsVisible(pixels[nextIndex]))
                            {
                                continue;
                            }

                            visited[nextIndex] = true;
                            queueX[tail] = nextX;
                            queueY[tail] = nextY;
                            tail++;
                        }
                    }
                }

                if (maxX <= LeftEdgeSliverMaxWidthPixels || count <= LeftEdgeSliverMaxPixels)
                {
                    for (var i = 0; i < tail; i++)
                    {
                        ClearPixel(pixels, width, queueX[i], queueY[i]);
                    }
                }
            }
        }

        private static bool HasLongDividerRunInColumn(Color32[] pixels, int width, int height, int x)
        {
            return HasEdgeAnchoredDividerRunInColumn(pixels, width, height, x, height * 0.32f)
                || HasEdgeAnchoredVisibleRunInColumn(pixels, width, height, x, height * 0.85f);
        }

        private static bool HasLongDividerRunInRow(Color32[] pixels, int width, int height, int y)
        {
            return HasEdgeAnchoredDividerRunInRow(pixels, width, height, y, width * 0.32f)
                || HasEdgeAnchoredVisibleRunInRow(pixels, width, height, y, width * 0.85f);
        }

        private static bool HasEdgeAnchoredDividerRunInColumn(Color32[] pixels, int width, int height, int x, float minLength)
        {
            return HasEdgeAnchoredRun(height, y => IsDividerPixel(pixels[ToIndex(width, x, y)]), minLength);
        }

        private static bool HasEdgeAnchoredVisibleRunInColumn(Color32[] pixels, int width, int height, int x, float minLength)
        {
            return HasEdgeAnchoredRun(height, y => IsVisible(pixels[ToIndex(width, x, y)]), minLength);
        }

        private static bool HasEdgeAnchoredDividerRunInRow(Color32[] pixels, int width, int height, int y, float minLength)
        {
            return HasEdgeAnchoredRun(width, x => IsDividerPixel(pixels[ToIndex(width, x, y)]), minLength);
        }

        private static bool HasEdgeAnchoredVisibleRunInRow(Color32[] pixels, int width, int height, int y, float minLength)
        {
            return HasEdgeAnchoredRun(width, x => IsVisible(pixels[ToIndex(width, x, y)]), minLength);
        }

        private static bool HasEdgeAnchoredRun(int length, Func<int, bool> predicate, float minLength)
        {
            var runStart = -1;
            for (var i = 0; i < length; i++)
            {
                if (predicate(i))
                {
                    if (runStart < 0)
                    {
                        runStart = i;
                    }

                    continue;
                }

                if (IsEdgeAnchoredRun(runStart, i - 1, length, minLength))
                {
                    return true;
                }

                runStart = -1;
            }

            return IsEdgeAnchoredRun(runStart, length - 1, length, minLength);
        }

        private static bool IsEdgeAnchoredRun(int start, int end, int length, float minLength)
        {
            if (start < 0 || end < start)
            {
                return false;
            }

            var runLength = end - start + 1;
            if (runLength < minLength)
            {
                return false;
            }

            return start <= DividerEdgeAnchorPixels || end >= length - 1 - DividerEdgeAnchorPixels;
        }

        private static void MarkColumnForClearing(bool[] clearColumns, int center)
        {
            for (var x = Mathf.Max(0, center - DividerClearRadius);
                 x <= Mathf.Min(clearColumns.Length - 1, center + DividerClearRadius);
                 x++)
            {
                clearColumns[x] = true;
            }
        }

        private static void MarkRowForClearing(bool[] clearRows, int center)
        {
            for (var y = Mathf.Max(0, center - DividerClearRadius);
                 y <= Mathf.Min(clearRows.Length - 1, center + DividerClearRadius);
                 y++)
            {
                clearRows[y] = true;
            }
        }

        private static bool IsVisible(Color32 color)
        {
            return color.a > 16;
        }

        private static bool IsDividerPixel(Color32 color)
        {
            if (!IsVisible(color))
            {
                return false;
            }

            var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            return max <= 64 || (min >= 178 && max - min <= 54);
        }

        private static void ClearPixel(Color32[] pixels, int width, int x, int y)
        {
            var index = ToIndex(width, x, y);
            var color = pixels[index];
            color.a = 0;
            pixels[index] = color;
        }

        private static int ToIndex(int width, int x, int y)
        {
            return (y * width) + x;
        }

        private static void CreateChefPortrait()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_005_chef");
            var sourcePath = $"{ResourcesRoot}/Idle/idle_00.png";
            var absoluteSourcePath = ToAbsolutePath(sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                return;
            }

            File.Copy(absoluteSourcePath, ToAbsolutePath(ChefPortraitPath), true);
            ApplyFrameTextureImporter(ChefPortraitPath);
        }

        private static void CreateChefPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_005_chef");

            var root = new GameObject("Chef");
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

            PrefabUtility.SaveAsPrefabAsset(root, ChefPrefabPath);
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
                clipProperty.FindPropertyRelative("resourcesFolder").stringValue = spec.ResourcesFolder;
                clipProperty.FindPropertyRelative("framesPerSecond").floatValue = spec.FramesPerSecond;
                clipProperty.FindPropertyRelative("loop").boolValue = spec.Loop;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SyncChefHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ChefHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChefPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(ChefPortraitPath);
            if (portrait != null)
            {
                hero.visualConfig.portrait = portrait;
            }

            hero.visualConfig.battlePrefab = prefab;
            hero.visualConfig.animatorController = null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            EditorUtility.SetDirty(hero);
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

        private sealed class ClipBuildSpec
        {
            public ClipBuildSpec(
                string clipKey,
                string sourceFolder,
                string resourcesFolder,
                string filePrefix,
                float framesPerSecond,
                bool loop)
            {
                ClipKey = clipKey;
                SourceFolder = sourceFolder;
                ResourcesFolder = resourcesFolder;
                FilePrefix = filePrefix;
                FramesPerSecond = framesPerSecond;
                Loop = loop;
            }

            public string ClipKey { get; }
            public string SourceFolder { get; }
            public string ResourcesFolder { get; }
            public string FilePrefix { get; }
            public float FramesPerSecond { get; }
            public bool Loop { get; }
        }
    }
}
