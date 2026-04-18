using System.IO;
using Fight.UI.Presentation.Statuses;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class AttackPowerDebuffStatusVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Attack Power Debuff Status VFX";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string StatusIconsFolder = "Assets/Art/VFX/StatusIcons";
        private const string SharedPrefabsFolder = "Assets/Prefabs/VFX/Shared";
        private const string StatusResourcesFolder = "Assets/Resources/Stage01Demo/VFX/Statuses";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/AttackPowerDebuffStatusVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string AttackDebuffIconSpritePath = StatusIconsFolder + "/AttackDebuffEffect.png";
        private const string SharedPrefabPath = SharedPrefabsFolder + "/AttackPowerDownStatusLoop.prefab";
        private const string ResourcesPrefabPath = StatusResourcesFolder + "/AttackPowerDownStatusLoop.prefab";

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
            var debuffIconSprite = EnsureAttackDebuffIconSprite();

            SavePrefab(CreateAttackPowerDownStatusRoot(softCircleSprite, debuffIconSprite), SharedPrefabPath);
            SavePrefab(CreateAttackPowerDownStatusRoot(softCircleSprite, debuffIconSprite), ResourcesPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Attack power debuff status VFX prefabs rebuilt.");
        }

        public static void BuildAttackPowerDebuffStatusVfxBatch()
        {
            BuildAttackPowerDebuffStatusVfx();
            EditorApplication.Exit(0);
        }

        private static GameObject CreateAttackPowerDownStatusRoot(Sprite softCircleSprite, Sprite debuffIconSprite)
        {
            var root = new GameObject("AttackPowerDownStatusLoop");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "DebuffHaloOuter",
                softCircleSprite,
                new Color(0.88f, 0.15f, 0.22f, 0.16f),
                -8,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.38f, 0.24f, 1f));
            CreateSprite(
                root.transform,
                "DebuffHaloInner",
                softCircleSprite,
                new Color(1f, 0.42f, 0.42f, 0.12f),
                -6,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.24f, 0.16f, 1f));

            var orbitAnchor = new GameObject("AttackDebuffOrbit").transform;
            orbitAnchor.SetParent(root.transform, false);
            orbitAnchor.localPosition = new Vector3(0.34f, 0.02f, 0f);

            CreateSprite(
                orbitAnchor,
                "OrbitGlow",
                softCircleSprite,
                new Color(0.98f, 0.18f, 0.26f, 0.28f),
                10,
                Vector3.zero,
                new Vector3(0.18f, 0.12f, 1f));
            CreateSprite(
                orbitAnchor,
                "IconShadow",
                debuffIconSprite,
                new Color(0f, 0f, 0f, 0.45f),
                11,
                new Vector3(0.02f, -0.02f, 0f),
                Vector3.one * 0.9f);
            CreateSprite(
                orbitAnchor,
                "Icon",
                debuffIconSprite,
                Color.white,
                14,
                Vector3.zero,
                Vector3.one * 0.9f);

            var orbitController = root.AddComponent<OrbitingStatusIconVfx>();
            orbitController.Configure(
                orbitAnchor,
                orbitSpeedDegreesPerSecond: 108f,
                keepAnchorUpright: true,
                randomizeStartingAngle: true);

            return root;
        }

        private static Sprite EnsureAttackDebuffIconSprite()
        {
            var fullPath = GetAbsoluteProjectPath(AttackDebuffIconSpritePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Missing source attack debuff icon at {AttackDebuffIconSpritePath}");
            }

            AssetDatabase.ImportAsset(AttackDebuffIconSpritePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(AttackDebuffIconSpritePath) is TextureImporter importer)
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

            return LoadRequiredAsset<Sprite>(AttackDebuffIconSpritePath);
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
