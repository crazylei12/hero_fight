using System.Collections.Generic;
using System.IO;
using Fight.Data;
using Fight.UI.Presentation.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class ButcherHookVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Butcher Hook VFX Prefab";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string SourceArtFolder = "Assets/Art/VFX/Source";
        private const string ProjectilePrefabsFolder = "Assets/Prefabs/VFX/Projectiles";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/ButcherHookVfxPrefabBuilder.cs";

        private const string HookSourceTexturePath = SourceArtFolder + "/ButcherHookChainSource.png";
        private const string HookHeadSpritePath = GeneratedArtFolder + "/ButcherHookHead.png";
        private const string HookChainSpritePath = GeneratedArtFolder + "/ButcherHookChainSegment.png";
        private const string HookChainPrefabPath = ProjectilePrefabsFolder + "/ButcherHookChainProjectile.prefab";
        private const string ButcherActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/assassin_003_butcher/Gore Hook.asset";
        private const string ButcherUltimateSkillAssetPath = "Assets/Data/Stage01Demo/Skills/assassin_003_butcher/Carnage Reel.asset";

        private const float HookPixelsPerUnit = 512f;
        private const int MinimumVisibleAlpha = 8;
        private static readonly RectInt HookHeadSourceRect = new RectInt(7, 6, 437, 430);
        private static readonly RectInt ChainSegmentSourceRect = new RectInt(1340, 6, 436, 430);
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
        public static void BuildButcherHookVfxPrefab()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(SourceArtFolder);
            EnsureFolder(ProjectilePrefabsFolder);

            var hookHeadSprite = EnsureHookSprite(HookHeadSpritePath, HookHeadSourceRect, trimBounds: true, padding: 8);
            var chainSprite = EnsureHookSprite(HookChainSpritePath, ChainSegmentSourceRect, trimBounds: true, padding: 4);
            BuildHookChainPrefab(hookHeadSprite, chainSprite);
            SyncStage01DemoAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Butcher hook VFX prefab rebuilt.");
        }

        public static void BuildButcherHookVfxPrefabBatch()
        {
            BuildButcherHookVfxPrefab();
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
                BuildButcherHookVfxPrefab();
                return;
            }

            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(HookHeadSpritePath) == null
                || AssetDatabase.LoadAssetAtPath<Sprite>(HookChainSpritePath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(HookChainPrefabPath) == null)
            {
                return true;
            }

            var latestInputTimestamp = GetLatestTimestampUtc(BuilderScriptAssetPath, HookSourceTexturePath);
            return latestInputTimestamp > GetOldestTimestampUtc(HookHeadSpritePath, HookChainSpritePath, HookChainPrefabPath);
        }

        private static void SyncStage01DemoAssets()
        {
            var hookChainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HookChainPrefabPath);

            var activeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ButcherActiveSkillAssetPath);
            if (activeSkill != null)
            {
                activeSkill.castProjectileVfxPrefab = hookChainPrefab;
                activeSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(activeSkill);
            }

            var ultimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ButcherUltimateSkillAssetPath);
            if (ultimateSkill != null)
            {
                ultimateSkill.castProjectileVfxPrefab = hookChainPrefab;
                ultimateSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(ultimateSkill);
            }
        }

        private static void BuildHookChainPrefab(Sprite hookHeadSprite, Sprite chainSprite)
        {
            var root = new GameObject("ButcherHookChainProjectile");
            var sortingGroup = root.AddComponent<SortingGroup>();

            var chainShadow = CreateSprite(
                root.transform,
                "ChainShadow",
                chainSprite,
                new Color(0f, 0f, 0f, 0.24f),
                -2,
                Vector3.zero,
                Vector3.one,
                SpriteDrawMode.Tiled,
                new Vector2(0.8f, 0.34f));
            var chain = CreateSprite(
                root.transform,
                "Chain",
                chainSprite,
                new Color(1f, 1f, 1f, 0.96f),
                2,
                Vector3.zero,
                Vector3.one,
                SpriteDrawMode.Tiled,
                new Vector2(0.8f, 0.25f));
            var hookHead = CreateSprite(
                root.transform,
                "HookHead",
                hookHeadSprite,
                Color.white,
                8,
                Vector3.zero,
                Vector3.one * 0.42f);

            root.AddComponent<ButcherHookChainVfx>().Configure(
                chain,
                chainShadow,
                hookHead,
                sortingGroup,
                0.25f,
                0.16f,
                0.26f,
                0.52f,
                180f,
                0.05f);

            SavePrefab(root, HookChainPrefabPath);
        }

        private static Sprite EnsureHookSprite(string outputPath, RectInt sourceRect, bool trimBounds, int padding)
        {
            if (!File.Exists(GetAbsoluteProjectPath(HookSourceTexturePath)))
            {
                throw new FileNotFoundException($"Missing butcher hook source texture at {HookSourceTexturePath}");
            }

            AssetDatabase.ImportAsset(HookSourceTexturePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureSourceImporter();

            var sourceTexture = LoadRequiredAsset<Texture2D>(HookSourceTexturePath);
            var processedTexture = BuildTransparentCroppedTexture(sourceTexture, sourceRect, trimBounds, padding);
            try
            {
                File.WriteAllBytes(GetAbsoluteProjectPath(outputPath), processedTexture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(processedTexture);
            }

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureGeneratedSpriteImporter(outputPath);
            return LoadRequiredAsset<Sprite>(outputPath);
        }

        private static void ConfigureSourceImporter()
        {
            if (AssetImporter.GetAtPath(HookSourceTexturePath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureGeneratedSpriteImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = assetPath == HookChainSpritePath ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = HookPixelsPerUnit;
            importer.SaveAndReimport();
        }

        private static Texture2D BuildTransparentCroppedTexture(Texture2D sourceTexture, RectInt sourceRect, bool trimBounds, int padding)
        {
            var width = sourceTexture.width;
            var height = sourceTexture.height;
            var clampedRect = new RectInt(
                Mathf.Clamp(sourceRect.xMin, 0, width - 1),
                Mathf.Clamp(sourceRect.yMin, 0, height - 1),
                Mathf.Clamp(sourceRect.width, 1, width),
                Mathf.Clamp(sourceRect.height, 1, height));
            clampedRect.width = Mathf.Min(clampedRect.width, width - clampedRect.xMin);
            clampedRect.height = Mathf.Min(clampedRect.height, height - clampedRect.yMin);

            var sourcePixels = sourceTexture.GetPixels32();
            var transparentPixels = new Color32[clampedRect.width * clampedRect.height];
            for (var y = 0; y < clampedRect.height; y++)
            {
                var sourceY = clampedRect.yMin + y;
                for (var x = 0; x < clampedRect.width; x++)
                {
                    var sourceX = clampedRect.xMin + x;
                    var pixel = sourcePixels[(sourceY * width) + sourceX];
                    if (IsCheckerBackgroundPixel(pixel))
                    {
                        pixel.a = 0;
                    }
                    else if (IsSoftCheckerFringePixel(pixel))
                    {
                        pixel.a = (byte)Mathf.Min(pixel.a, 72);
                    }

                    transparentPixels[(y * clampedRect.width) + x] = pixel;
                }
            }

            var outputRect = trimBounds
                ? FindOpaqueBounds(transparentPixels, clampedRect.width, clampedRect.height, padding)
                : new RectInt(0, 0, clampedRect.width, clampedRect.height);
            if (outputRect.width <= 0 || outputRect.height <= 0)
            {
                throw new FileNotFoundException($"Could not isolate visible hook pixels from {HookSourceTexturePath}.");
            }

            var result = new Texture2D(outputRect.width, outputRect.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var croppedPixels = new Color32[outputRect.width * outputRect.height];
            for (var y = 0; y < outputRect.height; y++)
            {
                var sourceY = outputRect.yMin + y;
                for (var x = 0; x < outputRect.width; x++)
                {
                    var sourceX = outputRect.xMin + x;
                    croppedPixels[(y * outputRect.width) + x] = transparentPixels[(sourceY * clampedRect.width) + sourceX];
                }
            }

            result.SetPixels32(croppedPixels);
            result.Apply();
            return result;
        }

        private static RectInt FindOpaqueBounds(Color32[] pixels, int width, int height, int padding)
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

            if (maxX < minX || maxY < minY)
            {
                return new RectInt(0, 0, 0, 0);
            }

            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);
            return new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static bool IsCheckerBackgroundPixel(Color32 pixel)
        {
            if (pixel.a <= MinimumVisibleAlpha)
            {
                return true;
            }

            var average = GetAverage(pixel);
            var delta = GetChannelDelta(pixel);
            return (average >= 216f && delta <= 34)
                || (average >= 206f && delta <= 20);
        }

        private static bool IsSoftCheckerFringePixel(Color32 pixel)
        {
            return pixel.a > MinimumVisibleAlpha
                && GetAverage(pixel) >= 198f
                && GetChannelDelta(pixel) <= 24;
        }

        private static float GetAverage(Color32 pixel)
        {
            return (pixel.r + pixel.g + pixel.b) / 3f;
        }

        private static int GetChannelDelta(Color32 pixel)
        {
            return Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) - Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            int sortingOrder,
            Vector3 localPosition,
            Vector3 localScale,
            SpriteDrawMode drawMode = SpriteDrawMode.Simple,
            Vector2? size = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = drawMode;
            if (drawMode == SpriteDrawMode.Tiled)
            {
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size ?? renderer.size;
            }

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
