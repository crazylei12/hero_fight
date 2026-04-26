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
    public static class RiflemanVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Rifleman Visual";
        private const string SourceSheetPath = "Assets/Art/Heroes/marksman_002_rifleman/rifleman_source_sheet.png";
        private const string NormalizedSheetPath = "Assets/Art/Heroes/marksman_002_rifleman/rifleman_clean_sheet.png";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/marksman_002_rifleman";
        private const string ResourcesPrefix = "HeroPreview/marksman_002_rifleman";
        private const string RiflemanPrefabPath = "Assets/Prefabs/Heroes/marksman_002_rifleman/Rifleman.prefab";
        private const string RiflemanPortraitPath = "Assets/Prefabs/Heroes/marksman_002_rifleman/Rifleman_idle_front.png";
        private const string RiflemanHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/marksman_002_rifleman/Rifleman.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/RiflemanVisualBuilder.cs";
        private const int SourceColumns = 9;
        private const int SourceRows = 7;
        private const int OutputFrameWidth = 158;
        private const int OutputFrameHeight = 158;
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.06f);
        private static readonly int[] SourceColumnLefts = { 5, 162, 319, 476, 633, 791, 949, 1106, 1264 };
        private static readonly int[] SourceColumnRights = { 159, 317, 474, 631, 788, 946, 1104, 1261, 1418 };
        private static readonly int[] SourceRowTops = { 5, 158, 308, 459, 612, 762, 912 };
        private static readonly int[] SourceRowBottoms = { 155, 306, 457, 609, 759, 909, 1100 };

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 0, "idle", 7f, true),
            new ClipBuildSpec("Run", 1, "run", 12f, true),
            new ClipBuildSpec("Attack1", 2, "attack", 14f, false),
            new ClipBuildSpec("Skill", 3, "burst_fire", 16f, false),
            new ClipBuildSpec("Ult", 4, "frag_grenade", 12f, false),
            new ClipBuildSpec("Hit", 5, "hit", 12f, false),
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
        public static void BuildRiflemanVisual()
        {
            BuildAll();
            Debug.Log("Rifleman sprite-sheet visual prefab rebuilt.");
        }

        public static void BuildRiflemanVisualBatch()
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
                BuildRiflemanVisual();
                return;
            }

            SyncRiflemanHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanPrefabPath) == null)
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
                > GetLatestTimestampUtc(RiflemanPrefabPath, ResourcesRoot, RiflemanPortraitPath);
        }

        private static void BuildAll()
        {
            ApplySourceTextureImporter(SourceSheetPath);
            GenerateFrameFolders();
            CreateRiflemanPortrait();
            CreateRiflemanPrefab();
            SyncRiflemanHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing Rifleman source sheet: {SourceSheetPath}");
            }

            EnsureFolder(ResourcesRoot);
            var normalizedSheet = new Texture2D(
                SourceColumns * OutputFrameWidth,
                SourceRows * OutputFrameHeight,
                TextureFormat.RGBA32,
                false);
            normalizedSheet.filterMode = FilterMode.Point;
            normalizedSheet.wrapMode = TextureWrapMode.Clamp;
            FillTexture(normalizedSheet, new Color32(0, 0, 0, 0));

            foreach (var spec in ClipBuildSpecs)
            {
                var folderPath = $"{ResourcesRoot}/{spec.ClipKey}";
                EnsureFolder(folderPath);
                ClearGeneratedPngs(folderPath);

                for (var i = 0; i < SourceColumns; i++)
                {
                    var frameTexture = CropFrame(sourceTexture, spec.SourceRow, i);
                    var assetPath = $"{folderPath}/{spec.FilePrefix}_{i:00}.png";
                    File.WriteAllBytes(ToAbsolutePath(assetPath), frameTexture.EncodeToPNG());
                    WriteFrameToNormalizedSheet(normalizedSheet, frameTexture, spec.SourceRow, i);
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                    ApplyFrameTextureImporter(assetPath);
                }
            }

            File.WriteAllBytes(ToAbsolutePath(NormalizedSheetPath), normalizedSheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(normalizedSheet);
            ApplySourceTextureImporter(NormalizedSheetPath);
            UnityEngine.Object.DestroyImmediate(sourceTexture);
        }

        private static void CreateRiflemanPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/marksman_002_rifleman");

            var root = new GameObject("Rifleman");
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

            PrefabUtility.SaveAsPrefabAsset(root, RiflemanPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateRiflemanPortrait()
        {
            EnsureFolder("Assets/Prefabs/Heroes/marksman_002_rifleman");
            var sourcePath = $"{ResourcesRoot}/Idle/idle_00.png";
            var absoluteSourcePath = ToAbsolutePath(sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                return;
            }

            File.Copy(absoluteSourcePath, ToAbsolutePath(RiflemanPortraitPath), true);
            ApplyFrameTextureImporter(RiflemanPortraitPath);
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

        private static void SyncRiflemanHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(RiflemanHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(RiflemanPortraitPath);
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

        private static Texture2D CropFrame(Texture2D sourceTexture, int sourceRow, int sourceFrame)
        {
            var sourceX = SourceColumnLefts[sourceFrame];
            var sourceYTop = SourceRowTops[sourceRow];
            var sourceWidth = SourceColumnRights[sourceFrame] - sourceX;
            var sourceHeight = SourceRowBottoms[sourceRow] - sourceYTop;
            var topLeftPixels = new Color32[sourceWidth * sourceHeight];

            for (var y = 0; y < sourceHeight; y++)
            {
                var sourceY = sourceYTop + y;
                for (var x = 0; x < sourceWidth; x++)
                {
                    var sourceXPosition = sourceX + x;
                    var outputIndex = y * sourceWidth + x;
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

            ClearConnectedPreviewBackground(topLeftPixels, sourceWidth, sourceHeight);

            var scaledTopLeftPixels = new Color32[OutputFrameWidth * OutputFrameHeight];
            for (var y = 0; y < OutputFrameHeight; y++)
            {
                var sourceY = Mathf.Min(sourceHeight - 1, Mathf.FloorToInt(y * sourceHeight / (float)OutputFrameHeight));
                for (var x = 0; x < OutputFrameWidth; x++)
                {
                    var sourceXPosition = Mathf.Min(sourceWidth - 1, Mathf.FloorToInt(x * sourceWidth / (float)OutputFrameWidth));
                    scaledTopLeftPixels[y * OutputFrameWidth + x] = topLeftPixels[sourceY * sourceWidth + sourceXPosition];
                }
            }

            var unityPixels = new Color32[scaledTopLeftPixels.Length];
            for (var y = 0; y < OutputFrameHeight; y++)
            {
                var unityY = OutputFrameHeight - 1 - y;
                Array.Copy(
                    scaledTopLeftPixels,
                    y * OutputFrameWidth,
                    unityPixels,
                    unityY * OutputFrameWidth,
                    OutputFrameWidth);
            }

            var frameTexture = new Texture2D(OutputFrameWidth, OutputFrameHeight, TextureFormat.RGBA32, false);
            frameTexture.SetPixels32(unityPixels);
            frameTexture.filterMode = FilterMode.Point;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.Apply();
            return frameTexture;
        }

        private static void ClearConnectedPreviewBackground(Color32[] topLeftPixels, int width, int height)
        {
            if (topLeftPixels == null || width <= 0 || height <= 0)
            {
                return;
            }

            var visited = new bool[topLeftPixels.Length];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    return;
                }

                var index = y * width + x;
                if (visited[index] || !IsPreviewBackground(topLeftPixels[index]))
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var color = topLeftPixels[index];
                topLeftPixels[index] = new Color32(color.r, color.g, color.b, 0);

                var x = index % width;
                var y = index / width;
                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            for (var i = 0; i < topLeftPixels.Length; i++)
            {
                if (topLeftPixels[i].a == 0 || !IsBrightCheckerPixel(topLeftPixels[i]))
                {
                    continue;
                }

                var color = topLeftPixels[i];
                topLeftPixels[i] = new Color32(color.r, color.g, color.b, 0);
            }
        }

        private static bool IsPreviewBackground(Color32 color)
        {
            if (color.a == 0)
            {
                return true;
            }

            var max = Mathf.Max(color.r, color.g, color.b);
            var min = Mathf.Min(color.r, color.g, color.b);
            return min >= 205 && max - min <= 48;
        }

        private static bool IsBrightCheckerPixel(Color32 color)
        {
            var max = Mathf.Max(color.r, color.g, color.b);
            var min = Mathf.Min(color.r, color.g, color.b);
            return min >= 232 && max - min <= 30;
        }

        private static void FillTexture(Texture2D texture, Color32 color)
        {
            var pixels = new Color32[texture.width * texture.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static void WriteFrameToNormalizedSheet(Texture2D normalizedSheet, Texture2D frameTexture, int sourceRow, int sourceFrame)
        {
            var targetY = (SourceRows - 1 - sourceRow) * OutputFrameHeight;
            normalizedSheet.SetPixels32(
                sourceFrame * OutputFrameWidth,
                targetY,
                OutputFrameWidth,
                OutputFrameHeight,
                frameTexture.GetPixels32());
            normalizedSheet.Apply();
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

        private static void ApplySourceTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
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
                var metaPath = $"{path}.meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
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
