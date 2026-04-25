using System.Collections.Generic;
using System.IO;
using Fight.Data;
using Fight.UI.Presentation.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class BoomerangerWheelVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Boomeranger Wheel VFX Prefabs";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string SourceArtFolder = "Assets/Art/VFX/Source";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string ProjectilePrefabsFolder = PrefabsRootFolder + "/Projectiles";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/BoomerangerWheelVfxPrefabBuilder.cs";

        private const string WheelSourceTexturePath = SourceArtFolder + "/BoomerangerWheelSource.png";
        private const string WheelSpritePath = GeneratedArtFolder + "/BoomerangerWheel.png";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string BasicProjectilePrefabPath = ProjectilePrefabsFolder + "/BoomerangerWheelProjectile.prefab";
        private const string BounceProjectilePrefabPath = ProjectilePrefabsFolder + "/BoomerangerWheelBounceProjectile.prefab";
        private const string ReturningProjectilePrefabPath = ProjectilePrefabsFolder + "/BoomerangerReturningWheelProjectile.prefab";
        private const string WheelstormOrbitPrefabPath = SkillPrefabsFolder + "/BoomerangerWheelstormOrbit.prefab";
        private const string BoomerangerHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/marksman_004_boomeranger/Boomeranger.asset";
        private const string BoomerangerActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/marksman_004_boomeranger/Returning Wheel.asset";
        private const string BoomerangerUltimateSkillAssetPath = "Assets/Data/Stage01Demo/Skills/marksman_004_boomeranger/Wheelstorm.asset";

        private const float WheelSpritePixelsPerUnit = 512f;
        private const int CheckerBackgroundAverageThreshold = 236;
        private const int CheckerBackgroundChannelDeltaThreshold = 24;
        private const int LooseBackgroundAverageThreshold = 224;
        private const int LooseBackgroundChannelDeltaThreshold = 32;
        private const byte MinimumVisibleAlpha = 12;
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
        public static void BuildBoomerangerWheelVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(SourceArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(ProjectilePrefabsFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            var wheelSprite = EnsureWheelSprite();
            BuildWheelProjectilePrefab(
                "BoomerangerWheelProjectile",
                BasicProjectilePrefabPath,
                wheelSprite,
                softCircleSprite,
                0.40f,
                0.40f,
                0.92f);
            BuildWheelProjectilePrefab(
                "BoomerangerWheelBounceProjectile",
                BounceProjectilePrefabPath,
                wheelSprite,
                softCircleSprite,
                0.30f,
                0.30f,
                0.78f);
            BuildWheelProjectilePrefab(
                "BoomerangerReturningWheelProjectile",
                ReturningProjectilePrefabPath,
                wheelSprite,
                softCircleSprite,
                0.46f,
                0.46f,
                1f);
            BuildWheelstormOrbitPrefab(wheelSprite, softCircleSprite);
            SyncStage01DemoAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Boomeranger wheel VFX prefabs rebuilt.");
        }

        public static void BuildBoomerangerWheelVfxPrefabsBatch()
        {
            BuildBoomerangerWheelVfxPrefabs();
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
                BuildBoomerangerWheelVfxPrefabs();
                return;
            }

            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            var latestInputTimestamp = GetLatestTimestampUtc(
                BuilderScriptAssetPath,
                WheelSourceTexturePath);
            return latestInputTimestamp > GetOldestTimestampUtc(
                WheelSpritePath,
                BasicProjectilePrefabPath,
                BounceProjectilePrefabPath,
                ReturningProjectilePrefabPath,
                WheelstormOrbitPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(SoftCircleSpritePath) != null
                && AssetDatabase.LoadAssetAtPath<Sprite>(WheelSpritePath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(BasicProjectilePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(BounceProjectilePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(ReturningProjectilePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(WheelstormOrbitPrefabPath) != null;
        }

        private static void SyncStage01DemoAssets()
        {
            var basicProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicProjectilePrefabPath);
            var bounceProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BounceProjectilePrefabPath);
            var returningProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReturningProjectilePrefabPath);
            var wheelstormOrbitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WheelstormOrbitPrefabPath);

            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(BoomerangerHeroAssetPath);
            if (hero != null)
            {
                hero.visualConfig ??= new HeroVisualConfig();
                hero.visualConfig.projectilePrefab = basicProjectilePrefab;
                hero.visualConfig.projectileAlignToMovement = basicProjectilePrefab != null;
                hero.visualConfig.projectileEulerAngles = Vector3.zero;
                hero.visualConfig.basicAttackVariantVisuals = new[]
                {
                    new BasicAttackVariantVisualConfig
                    {
                        variantKey = "bounce",
                        projectilePrefab = bounceProjectilePrefab,
                        hitVfxPrefab = null,
                    },
                };
                EditorUtility.SetDirty(hero);
            }

            var activeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(BoomerangerActiveSkillAssetPath);
            if (activeSkill != null)
            {
                activeSkill.castProjectileVfxPrefab = returningProjectilePrefab;
                activeSkill.persistentAreaVfxPrefab = null;
                activeSkill.persistentAreaVfxScaleMultiplier = 1f;
                activeSkill.persistentAreaVfxEulerAngles = Vector3.zero;
                activeSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(activeSkill);
            }

            var ultimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(BoomerangerUltimateSkillAssetPath);
            if (ultimateSkill != null)
            {
                ultimateSkill.castProjectileVfxPrefab = null;
                ultimateSkill.persistentAreaVfxPrefab = wheelstormOrbitPrefab;
                ultimateSkill.persistentAreaVfxScaleMultiplier = 1f;
                ultimateSkill.persistentAreaVfxEulerAngles = Vector3.zero;
                ultimateSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(ultimateSkill);
            }
        }

        private static void BuildWheelProjectilePrefab(
            string prefabName,
            string prefabPath,
            Sprite wheelSprite,
            Sprite softCircleSprite,
            float wheelScale,
            float glowScale,
            float alphaMultiplier)
        {
            var root = new GameObject(prefabName);
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "TrailBloom",
                softCircleSprite,
                new Color(1f, 0.78f, 0.30f, 0.16f * alphaMultiplier),
                1,
                new Vector3(-0.12f, 0f, 0f),
                new Vector3(glowScale * 0.92f, glowScale * 0.42f, 1f));
            CreateSprite(
                root.transform,
                "WheelGlow",
                softCircleSprite,
                new Color(1f, 0.92f, 0.58f, 0.18f * alphaMultiplier),
                2,
                Vector3.zero,
                new Vector3(glowScale * 0.58f, glowScale * 0.58f, 1f));

            var wheel = CreateSprite(
                root.transform,
                "Wheel",
                wheelSprite,
                new Color(1f, 1f, 1f, alphaMultiplier),
                10,
                Vector3.zero,
                Vector3.one * wheelScale);
            wheel.gameObject.AddComponent<LoopingRotationVfx>().Configure(new Vector3(0f, 0f, -900f));

            SavePrefab(root, prefabPath);
        }

        private static void BuildWheelstormOrbitPrefab(Sprite wheelSprite, Sprite softCircleSprite)
        {
            var root = new GameObject("BoomerangerWheelstormOrbit");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "AreaGlow",
                softCircleSprite,
                new Color(1f, 0.72f, 0.22f, 0.08f),
                -4,
                Vector3.zero,
                new Vector3(0.78f, 0.78f, 1f));
            CreateSprite(
                root.transform,
                "InnerHeat",
                softCircleSprite,
                new Color(1f, 0.92f, 0.45f, 0.10f),
                -2,
                Vector3.zero,
                new Vector3(0.34f, 0.34f, 1f));

            var orbitRoot = new GameObject("OrbitRoot");
            orbitRoot.transform.SetParent(root.transform, false);
            orbitRoot.AddComponent<LoopingRotationVfx>().Configure(new Vector3(0f, 0f, 115f));

            const int wheelCount = 6;
            const float orbitRadius = 0.16f;
            for (var i = 0; i < wheelCount; i++)
            {
                var angle = (360f / wheelCount) * i;
                var radians = angle * Mathf.Deg2Rad;
                var localPosition = new Vector3(
                    Mathf.Cos(radians) * orbitRadius,
                    Mathf.Sin(radians) * orbitRadius,
                    0f);

                var wheel = CreateSprite(
                    orbitRoot.transform,
                    $"OrbitWheel_{i + 1:00}",
                    wheelSprite,
                    new Color(1f, 1f, 1f, 0.96f),
                    12 + i,
                    localPosition,
                    Vector3.one * 0.037f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                wheel.gameObject.AddComponent<LoopingRotationVfx>().Configure(new Vector3(0f, 0f, -760f));
            }

            SavePrefab(root, WheelstormOrbitPrefabPath);
        }

        private static Sprite EnsureWheelSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(WheelSourceTexturePath)))
            {
                throw new FileNotFoundException($"Missing boomeranger wheel source texture at {WheelSourceTexturePath}");
            }

            AssetDatabase.ImportAsset(WheelSourceTexturePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureWheelSourceImporter();

            var sourceTexture = LoadRequiredAsset<Texture2D>(WheelSourceTexturePath);
            var processedTexture = BuildTransparentWheelTexture(sourceTexture);
            try
            {
                File.WriteAllBytes(GetAbsoluteProjectPath(WheelSpritePath), processedTexture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(processedTexture);
            }

            AssetDatabase.ImportAsset(WheelSpritePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureWheelSpriteImporter();
            return LoadRequiredAsset<Sprite>(WheelSpritePath);
        }

        private static void ConfigureWheelSourceImporter()
        {
            if (AssetImporter.GetAtPath(WheelSourceTexturePath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureWheelSpriteImporter()
        {
            if (AssetImporter.GetAtPath(WheelSpritePath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = WheelSpritePixelsPerUnit;
            importer.SaveAndReimport();
        }

        private static Texture2D BuildTransparentWheelTexture(Texture2D sourceTexture)
        {
            var width = sourceTexture.width;
            var height = sourceTexture.height;
            var sourcePixels = sourceTexture.GetPixels32();
            var edgeBackground = FindEdgeConnectedBackground(sourcePixels, width, height);
            var transparentPixels = new Color32[sourcePixels.Length];

            for (var i = 0; i < sourcePixels.Length; i++)
            {
                var pixel = sourcePixels[i];
                if (edgeBackground[i] || IsCheckerBackgroundPixel(pixel))
                {
                    pixel.a = 0;
                }
                else if (IsSoftCheckerFringePixel(pixel))
                {
                    pixel.a = (byte)Mathf.Min(pixel.a, 96);
                }

                transparentPixels[i] = pixel;
            }

            var bounds = FindOpaqueBounds(transparentPixels, width, height);
            if (!bounds.HasValue)
            {
                throw new FileNotFoundException("Could not isolate visible wheel pixels from BoomerangerWheelSource.png.");
            }

            var paddedBounds = bounds.Value;
            paddedBounds.xMin = Mathf.Max(0, paddedBounds.xMin - 10);
            paddedBounds.yMin = Mathf.Max(0, paddedBounds.yMin - 10);
            paddedBounds.xMax = Mathf.Min(width, paddedBounds.xMax + 10);
            paddedBounds.yMax = Mathf.Min(height, paddedBounds.yMax + 10);

            var result = new Texture2D(paddedBounds.width, paddedBounds.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var croppedPixels = new Color32[paddedBounds.width * paddedBounds.height];
            for (var y = 0; y < paddedBounds.height; y++)
            {
                var sourceY = paddedBounds.yMin + y;
                for (var x = 0; x < paddedBounds.width; x++)
                {
                    var sourceX = paddedBounds.xMin + x;
                    croppedPixels[(y * paddedBounds.width) + x] = transparentPixels[(sourceY * width) + sourceX];
                }
            }

            result.SetPixels32(croppedPixels);
            result.Apply();
            return result;
        }

        private static bool[] FindEdgeConnectedBackground(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    return;
                }

                var index = (y * width) + x;
                if (visited[index] || !IsLooseBackgroundPixel(pixels[index]))
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
                var x = index % width;
                var y = index / width;
                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            return visited;
        }

        private static RectInt? FindOpaqueBounds(Color32[] pixels, int width, int height)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[(y * width) + x];
                    if (pixel.a <= MinimumVisibleAlpha)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            return maxX < minX || maxY < minY
                ? null
                : new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static bool IsCheckerBackgroundPixel(Color32 pixel)
        {
            return pixel.a > MinimumVisibleAlpha
                && GetAverage(pixel) >= CheckerBackgroundAverageThreshold
                && GetChannelDelta(pixel) <= CheckerBackgroundChannelDeltaThreshold;
        }

        private static bool IsLooseBackgroundPixel(Color32 pixel)
        {
            return pixel.a <= MinimumVisibleAlpha
                || (GetAverage(pixel) >= LooseBackgroundAverageThreshold
                    && GetChannelDelta(pixel) <= LooseBackgroundChannelDeltaThreshold);
        }

        private static bool IsSoftCheckerFringePixel(Color32 pixel)
        {
            return pixel.a > MinimumVisibleAlpha
                && GetAverage(pixel) >= 220
                && GetChannelDelta(pixel) <= 28;
        }

        private static float GetAverage(Color32 pixel)
        {
            return (pixel.r + pixel.g + pixel.b) / 3f;
        }

        private static int GetChannelDelta(Color32 pixel)
        {
            return Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) - Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                return LoadRequiredAsset<Sprite>(SoftCircleSpritePath);
            }

            var texture = BuildSoftCircleTexture(128);
            try
            {
                File.WriteAllBytes(GetAbsoluteProjectPath(SoftCircleSpritePath), texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(SoftCircleSpritePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(SoftCircleSpritePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return LoadRequiredAsset<Sprite>(SoftCircleSpritePath);
        }

        private static Texture2D BuildSoftCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = distance <= radius
                        ? 1f - Mathf.Clamp01((distance - (radius * 0.62f)) / Mathf.Max(1f, radius * 0.38f))
                        : 0f;
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            int sortingOrder,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static T LoadRequiredAsset<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new FileNotFoundException($"Missing asset at path: {assetPath}");
            }

            return asset;
        }

        private static void SavePrefab(GameObject root, string assetPath)
        {
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string folderPath)
        {
            var segments = folderPath.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath);
        }

        private static System.DateTime GetLatestTimestampUtc(params string[] assetPaths)
        {
            var latest = System.DateTime.MinValue;
            for (var i = 0; i < assetPaths.Length; i++)
            {
                var assetPath = assetPaths[i];
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var absolutePath = GetAbsoluteProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    continue;
                }

                var timestamp = File.GetLastWriteTimeUtc(absolutePath);
                if (timestamp > latest)
                {
                    latest = timestamp;
                }
            }

            return latest;
        }

        private static System.DateTime GetOldestTimestampUtc(params string[] assetPaths)
        {
            var oldest = System.DateTime.MaxValue;
            for (var i = 0; i < assetPaths.Length; i++)
            {
                var assetPath = assetPaths[i];
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var absolutePath = GetAbsoluteProjectPath(assetPath);
                if (!File.Exists(absolutePath))
                {
                    return System.DateTime.MinValue;
                }

                var timestamp = File.GetLastWriteTimeUtc(absolutePath);
                if (timestamp < oldest)
                {
                    oldest = timestamp;
                }
            }

            return oldest == System.DateTime.MaxValue ? System.DateTime.MinValue : oldest;
        }
    }
}
