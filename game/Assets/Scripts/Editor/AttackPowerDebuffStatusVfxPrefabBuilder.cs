using System.Collections.Generic;
using System.IO;
using Fight.UI.Presentation.Statuses;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class AttackPowerDebuffStatusVfxPrefabBuilder
    {
        private readonly struct StatModifierStatusVfxBuildSpec
        {
            public StatModifierStatusVfxBuildSpec(
                string rootName,
                string orbitAnchorName,
                string iconSpritePath,
                string sharedPrefabPath,
                string resourcesPrefabPath,
                Color outerHaloColor,
                Color innerHaloColor,
                Color orbitGlowColor)
            {
                RootName = rootName;
                OrbitAnchorName = orbitAnchorName;
                IconSpritePath = iconSpritePath;
                SharedPrefabPath = sharedPrefabPath;
                ResourcesPrefabPath = resourcesPrefabPath;
                OuterHaloColor = outerHaloColor;
                InnerHaloColor = innerHaloColor;
                OrbitGlowColor = orbitGlowColor;
            }

            public string RootName { get; }

            public string OrbitAnchorName { get; }

            public string IconSpritePath { get; }

            public string SharedPrefabPath { get; }

            public string ResourcesPrefabPath { get; }

            public Color OuterHaloColor { get; }

            public Color InnerHaloColor { get; }

            public Color OrbitGlowColor { get; }
        }

        private const string BuildMenuPath = "Fight/Stage 01/Build Stat Modifier Status VFX";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string StatusIconsFolder = "Assets/Art/VFX/StatusIcons";
        private const string SharedPrefabsFolder = "Assets/Prefabs/VFX/Shared";
        private const string StatusResourcesFolder = "Assets/Resources/Stage01Demo/VFX/Statuses";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string AttackBuffIconSpritePath = StatusIconsFolder + "/AttackBuffEffect.png";
        private const string AttackDebuffIconSpritePath = StatusIconsFolder + "/AttackDebuffEffect.png";
        private const string DefenseBuffIconSpritePath = StatusIconsFolder + "/DefenceBuffEffect.png";
        private const string DefenseDebuffIconSpritePath = StatusIconsFolder + "/DefenceDebuffEffect.png";
        private const string AttackSpeedBuffIconSpritePath = StatusIconsFolder + "/AttackSpeedBuffEffect.png";
        private const string AttackSpeedDebuffIconSpritePath = StatusIconsFolder + "/AttackSpeedDebuffEffect.png";
        private const string MoveSpeedBuffIconSpritePath = StatusIconsFolder + "/MoveSpeedBuffEffect.png";
        private const string MoveSpeedDebuffIconSpritePath = StatusIconsFolder + "/MoveSpeedDebuffEffect.png";

        private static readonly StatModifierStatusVfxBuildSpec[] BuildSpecs =
        {
            new StatModifierStatusVfxBuildSpec(
                "AttackPowerUpStatusLoop",
                "AttackBuffOrbit",
                AttackBuffIconSpritePath,
                SharedPrefabsFolder + "/AttackPowerUpStatusLoop.prefab",
                StatusResourcesFolder + "/AttackPowerUpStatusLoop.prefab",
                new Color(1f, 0.72f, 0.2f, 0.16f),
                new Color(1f, 0.9f, 0.42f, 0.12f),
                new Color(1f, 0.8f, 0.28f, 0.26f)),
            new StatModifierStatusVfxBuildSpec(
                "AttackPowerDownStatusLoop",
                "AttackDebuffOrbit",
                AttackDebuffIconSpritePath,
                SharedPrefabsFolder + "/AttackPowerDownStatusLoop.prefab",
                StatusResourcesFolder + "/AttackPowerDownStatusLoop.prefab",
                new Color(0.88f, 0.15f, 0.22f, 0.16f),
                new Color(1f, 0.42f, 0.42f, 0.12f),
                new Color(0.98f, 0.18f, 0.26f, 0.28f)),
            new StatModifierStatusVfxBuildSpec(
                "DefenseUpStatusLoop",
                "DefenseBuffOrbit",
                DefenseBuffIconSpritePath,
                SharedPrefabsFolder + "/DefenseUpStatusLoop.prefab",
                StatusResourcesFolder + "/DefenseUpStatusLoop.prefab",
                new Color(0.18f, 0.76f, 1f, 0.16f),
                new Color(0.56f, 0.9f, 1f, 0.12f),
                new Color(0.3f, 0.82f, 1f, 0.24f)),
            new StatModifierStatusVfxBuildSpec(
                "DefenseDownStatusLoop",
                "DefenseDebuffOrbit",
                DefenseDebuffIconSpritePath,
                SharedPrefabsFolder + "/DefenseDownStatusLoop.prefab",
                StatusResourcesFolder + "/DefenseDownStatusLoop.prefab",
                new Color(0.16f, 0.48f, 0.8f, 0.16f),
                new Color(0.5f, 0.78f, 1f, 0.12f),
                new Color(0.34f, 0.68f, 1f, 0.24f)),
            new StatModifierStatusVfxBuildSpec(
                "AttackSpeedUpStatusLoop",
                "AttackSpeedBuffOrbit",
                AttackSpeedBuffIconSpritePath,
                SharedPrefabsFolder + "/AttackSpeedUpStatusLoop.prefab",
                StatusResourcesFolder + "/AttackSpeedUpStatusLoop.prefab",
                new Color(1f, 0.84f, 0.28f, 0.16f),
                new Color(1f, 0.94f, 0.58f, 0.12f),
                new Color(1f, 0.88f, 0.38f, 0.26f)),
            new StatModifierStatusVfxBuildSpec(
                "AttackSpeedDownStatusLoop",
                "AttackSpeedDebuffOrbit",
                AttackSpeedDebuffIconSpritePath,
                SharedPrefabsFolder + "/AttackSpeedDownStatusLoop.prefab",
                StatusResourcesFolder + "/AttackSpeedDownStatusLoop.prefab",
                new Color(0.84f, 0.24f, 0.5f, 0.16f),
                new Color(0.96f, 0.52f, 0.72f, 0.12f),
                new Color(0.9f, 0.34f, 0.58f, 0.24f)),
            new StatModifierStatusVfxBuildSpec(
                "MoveSpeedUpStatusLoop",
                "MoveSpeedBuffOrbit",
                MoveSpeedBuffIconSpritePath,
                SharedPrefabsFolder + "/MoveSpeedUpStatusLoop.prefab",
                StatusResourcesFolder + "/MoveSpeedUpStatusLoop.prefab",
                new Color(0.22f, 0.82f, 0.46f, 0.16f),
                new Color(0.56f, 0.96f, 0.7f, 0.12f),
                new Color(0.32f, 0.92f, 0.54f, 0.24f)),
            new StatModifierStatusVfxBuildSpec(
                "MoveSpeedDownStatusLoop",
                "MoveSpeedDebuffOrbit",
                MoveSpeedDebuffIconSpritePath,
                SharedPrefabsFolder + "/MoveSpeedDownStatusLoop.prefab",
                StatusResourcesFolder + "/MoveSpeedDownStatusLoop.prefab",
                new Color(0.9f, 0.46f, 0.16f, 0.16f),
                new Color(1f, 0.68f, 0.34f, 0.12f),
                new Color(1f, 0.58f, 0.24f, 0.24f)),
        };

        [MenuItem(BuildMenuPath)]
        public static void BuildAttackPowerDebuffStatusVfx()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(StatusIconsFolder);
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/VFX");
            EnsureFolder(SharedPrefabsFolder);
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Stage01Demo");
            EnsureFolder("Assets/Resources/Stage01Demo/VFX");
            EnsureFolder(StatusResourcesFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            var iconSprites = new Dictionary<string, Sprite>();
            for (var i = 0; i < BuildSpecs.Length; i++)
            {
                var spec = BuildSpecs[i];
                if (!iconSprites.TryGetValue(spec.IconSpritePath, out var iconSprite))
                {
                    iconSprite = EnsureStatusIconSprite(spec.IconSpritePath);
                    iconSprites.Add(spec.IconSpritePath, iconSprite);
                }

                SavePrefab(CreateStatModifierStatusRoot(spec, softCircleSprite, iconSprite), spec.SharedPrefabPath);
                SavePrefab(CreateStatModifierStatusRoot(spec, softCircleSprite, iconSprite), spec.ResourcesPrefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Stat modifier status VFX prefabs rebuilt.");
        }

        public static void BuildAttackPowerDebuffStatusVfxBatch()
        {
            BuildAttackPowerDebuffStatusVfx();
            EditorApplication.Exit(0);
        }

        private static GameObject CreateStatModifierStatusRoot(
            StatModifierStatusVfxBuildSpec spec,
            Sprite softCircleSprite,
            Sprite iconSprite)
        {
            var root = new GameObject(spec.RootName);
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "DebuffHaloOuter",
                softCircleSprite,
                spec.OuterHaloColor,
                -8,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.38f, 0.24f, 1f));
            CreateSprite(
                root.transform,
                "DebuffHaloInner",
                softCircleSprite,
                spec.InnerHaloColor,
                -6,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.24f, 0.16f, 1f));

            var orbitAnchor = new GameObject(spec.OrbitAnchorName).transform;
            orbitAnchor.SetParent(root.transform, false);
            orbitAnchor.localPosition = Vector3.zero;

            CreateSprite(
                orbitAnchor,
                "OrbitGlow",
                softCircleSprite,
                spec.OrbitGlowColor,
                10,
                Vector3.zero,
                new Vector3(0.18f, 0.12f, 1f));
            CreateSprite(
                orbitAnchor,
                "IconShadow",
                iconSprite,
                new Color(0f, 0f, 0f, 0.45f),
                11,
                new Vector3(0.02f, -0.02f, 0f),
                Vector3.one * 0.9f);
            CreateSprite(
                orbitAnchor,
                "Icon",
                iconSprite,
                Color.white,
                14,
                Vector3.zero,
                Vector3.one * 0.9f);

            var orbitController = root.AddComponent<OrbitingStatusIconVfx>();
            orbitController.ConfigureBodyOrbit(
                orbitAnchor,
                orbitSpeedDegreesPerSecond: 108f,
                keepAnchorUpright: true,
                randomizeStartingAngle: false,
                orbitRadius: new Vector2(0.45f, 0.165f),
                backScaleMultiplier: 0.72f,
                backAlphaMultiplier: 0.24f,
                backSortingOrderOffset: -158);

            return root;
        }

        private static Sprite EnsureStatusIconSprite(string iconSpritePath)
        {
            var fullPath = GetAbsoluteProjectPath(iconSpritePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Missing source status icon at {iconSpritePath}");
            }

            AssetDatabase.ImportAsset(iconSpritePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(iconSpritePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 32f;
                importer.SaveAndReimport();
            }

            return LoadRequiredAsset<Sprite>(iconSpritePath);
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                var texture = BuildSoftCircleTexture(128);
                File.WriteAllBytes(GetAbsoluteProjectPath(SoftCircleSpritePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SoftCircleSpritePath, ImportAssetOptions.ForceSynchronousImport);

                if (AssetImporter.GetAtPath(SoftCircleSpritePath) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
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
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            var name = Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
