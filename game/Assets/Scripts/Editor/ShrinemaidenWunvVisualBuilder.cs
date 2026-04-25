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
    public static class ShrinemaidenWunvVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Shrinemaiden Wunv Visual";
        private const string SourceSheetPath = "Assets/Art/Heroes/support_004_shrinemaiden/wunv_source_sheet.png";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/support_004_shrinemaiden_wunv";
        private const string ResourcesPrefix = "HeroPreview/support_004_shrinemaiden_wunv";
        private const string ShrinemaidenPrefabPath = "Assets/Prefabs/Heroes/support_004_shrinemaiden/ShrinemaidenWunv.prefab";
        private const string ShrinemaidenHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_004_shrinemaiden/Shrinemaiden.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/ShrinemaidenWunvVisualBuilder.cs";
        private const int SourceColumns = 8;
        private const int SourceRows = 8;
        private const int OutputFrameCount = 8;
        private const int OutputFrameWidth = 167;
        private const int OutputFrameHeight = 148;
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.06f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 0, "idle", 7f, true),
            new ClipBuildSpec("Run", 1, "run", 12f, true),
            new ClipBuildSpec("Jump", 2, "jump", 12f, false),
            new ClipBuildSpec("Attack1", 3, "attack_damage", 16f, false, composeLastFrameWithPreviousBody: true),
            new ClipBuildSpec("Attack2", 4, "attack_heal", 16f, false, composeLastFrameWithPreviousBody: true),
            new ClipBuildSpec("Skill", 5, "prayer_bloom", 14f, false),
            new ClipBuildSpec("Ult", 5, "ult", 14f, false),
            new ClipBuildSpec("Hit", 6, "hurt", 12f, false),
            new ClipBuildSpec("Death", 7, "defeated", 9f, false),
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
        public static void BuildShrinemaidenWunvVisual()
        {
            BuildAll();
            Debug.Log("Shrinemaiden Wunv visual prefab rebuilt.");
        }

        public static void BuildShrinemaidenWunvVisualBatch()
        {
            BuildAll();
            EditorApplication.Exit(0);
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
                BuildShrinemaidenWunvVisual();
                return;
            }

            SyncShrinemaidenHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ShrinemaidenPrefabPath) == null)
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
                > GetLatestTimestampUtc(ShrinemaidenPrefabPath, ResourcesRoot);
        }

        private static void BuildAll()
        {
            GenerateFrameFolders();
            CreateShrinemaidenPrefab();
            SyncShrinemaidenHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateFrameFolders()
        {
            if (!TryLoadTexture(SourceSheetPath, out var sourceTexture))
            {
                throw new FileNotFoundException($"Missing Shrinemaiden Wunv source sheet: {SourceSheetPath}");
            }

            EnsureFolder(ResourcesRoot);
            foreach (var spec in ClipBuildSpecs)
            {
                var folderPath = $"{ResourcesRoot}/{spec.ClipKey}";
                EnsureFolder(folderPath);
                ClearGeneratedPngs(folderPath);

                for (var i = 0; i < OutputFrameCount; i++)
                {
                    var sourceFrame = Mathf.Min(i, SourceColumns - 1);
                    var frameTexture = CropFrame(sourceTexture, spec.SourceRow, sourceFrame);
                    if (spec.ComposeLastFrameWithPreviousBody && i == OutputFrameCount - 1)
                    {
                        var previousFrameTexture = CropFrame(sourceTexture, spec.SourceRow, sourceFrame - 1);
                        var projectileFrameTexture = frameTexture;
                        frameTexture = CompositeFrame(previousFrameTexture, projectileFrameTexture);
                        UnityEngine.Object.DestroyImmediate(previousFrameTexture);
                        UnityEngine.Object.DestroyImmediate(projectileFrameTexture);
                    }

                    var assetPath = $"{folderPath}/{spec.FilePrefix}_{i:00}.png";
                    File.WriteAllBytes(ToAbsolutePath(assetPath), frameTexture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                    ApplyFrameTextureImporter(assetPath);
                }
            }

            UnityEngine.Object.DestroyImmediate(sourceTexture);
        }

        private static void CreateShrinemaidenPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_004_shrinemaiden");

            var root = new GameObject("ShrinemaidenWunv");
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

            PrefabUtility.SaveAsPrefabAsset(root, ShrinemaidenPrefabPath);
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

        private static void SyncShrinemaidenHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ShrinemaidenHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShrinemaidenPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            if (hero.visualConfig.battlePrefab == prefab
                && hero.visualConfig.animatorController == null
                && !hero.visualConfig.battlePrefabFacesLeftByDefault)
            {
                return;
            }

            hero.visualConfig.battlePrefab = prefab;
            hero.visualConfig.animatorController = null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            EditorUtility.SetDirty(hero);
        }

        private static Texture2D CropFrame(Texture2D sourceTexture, int sourceRow, int sourceFrame)
        {
            var sourceX = Mathf.RoundToInt(sourceFrame * sourceTexture.width / (float)SourceColumns);
            var sourceYTop = Mathf.RoundToInt(sourceRow * sourceTexture.height / (float)SourceRows);
            var topLeftPixels = new Color32[OutputFrameWidth * OutputFrameHeight];

            for (var y = 0; y < OutputFrameHeight; y++)
            {
                var sourceY = sourceYTop + y;
                for (var x = 0; x < OutputFrameWidth; x++)
                {
                    var sourceXPosition = sourceX + x;
                    var outputIndex = y * OutputFrameWidth + x;
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

            RemoveConnectedCheckerBackground(topLeftPixels, OutputFrameWidth, OutputFrameHeight);

            var unityPixels = new Color32[topLeftPixels.Length];
            for (var y = 0; y < OutputFrameHeight; y++)
            {
                var unityY = OutputFrameHeight - 1 - y;
                Array.Copy(
                    topLeftPixels,
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

        private static Texture2D CompositeFrame(Texture2D baseTexture, Texture2D overlayTexture)
        {
            var basePixels = baseTexture.GetPixels32();
            var overlayPixels = overlayTexture.GetPixels32();
            for (var i = 0; i < basePixels.Length && i < overlayPixels.Length; i++)
            {
                var overlay = overlayPixels[i];
                if (overlay.a == 0)
                {
                    continue;
                }

                if (overlay.a == byte.MaxValue)
                {
                    basePixels[i] = overlay;
                    continue;
                }

                var alpha = overlay.a / 255f;
                var inverseAlpha = 1f - alpha;
                var baseColor = basePixels[i];
                basePixels[i] = new Color32(
                    (byte)Mathf.RoundToInt(overlay.r * alpha + baseColor.r * inverseAlpha),
                    (byte)Mathf.RoundToInt(overlay.g * alpha + baseColor.g * inverseAlpha),
                    (byte)Mathf.RoundToInt(overlay.b * alpha + baseColor.b * inverseAlpha),
                    (byte)Mathf.Max(overlay.a, baseColor.a));
            }

            var texture = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.RGBA32, false);
            texture.SetPixels32(basePixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        private static void RemoveConnectedCheckerBackground(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (var x = 0; x < width; x++)
            {
                EnqueueIfBackground(x, 0, width, pixels, visited, queue);
                EnqueueIfBackground(x, height - 1, width, pixels, visited, queue);
            }

            for (var y = 0; y < height; y++)
            {
                EnqueueIfBackground(0, y, width, pixels, visited, queue);
                EnqueueIfBackground(width - 1, y, width, pixels, visited, queue);
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

            EnqueueIfBackground(x, y, width, pixels, visited, queue);
        }

        private static void EnqueueIfBackground(
            int x,
            int y,
            int width,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue)
        {
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
            return min >= 224 && max - min <= 20;
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
                bool loop,
                bool composeLastFrameWithPreviousBody = false)
            {
                ClipKey = clipKey;
                SourceRow = sourceRow;
                FilePrefix = filePrefix;
                FramesPerSecond = framesPerSecond;
                Loop = loop;
                ComposeLastFrameWithPreviousBody = composeLastFrameWithPreviousBody;
            }

            public string ClipKey { get; }
            public int SourceRow { get; }
            public string FilePrefix { get; }
            public float FramesPerSecond { get; }
            public bool Loop { get; }
            public bool ComposeLastFrameWithPreviousBody { get; }
        }
    }
}
