using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class SharedBlinkVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Shared Blink VFX";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string SharedPrefabFolder = "Assets/Prefabs/VFX/Shared";
        private const string SharedResourcesFolder = "Assets/Resources/Stage01Demo/VFX/Shared";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string BlinkFlashPrefabPath = SharedPrefabFolder + "/BlinkFlash.prefab";
        private const string BlinkFlashResourcesPrefabPath = SharedResourcesFolder + "/BlinkFlash.prefab";
        private const string BurstRingsSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_rings.prefab";
        private const string DarkMagicHitSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/Hit_dark_magic.prefab";
        private const string MagicSwirlSourcePrefabPath = "Assets/Super Pixel Effects Pack 2/Prefabs/fx2_magic_swirl_small_violet.prefab";
        private const string MagicHexSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_magic_hex_small_black.prefab";

        [MenuItem(BuildMenuPath)]
        public static void BuildSharedBlinkVfx()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(SharedPrefabFolder);
            EnsureFolder(SharedResourcesFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildBlinkFlashPrefab(softCircleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Shared blink VFX prefab rebuilt.");
        }

        public static void BuildSharedBlinkVfxBatch()
        {
            BuildSharedBlinkVfx();
        }

        private static void BuildBlinkFlashPrefab(Sprite softCircleSprite)
        {
            var burstRingsPrefab = LoadRequiredAsset<GameObject>(BurstRingsSourcePrefabPath);
            var darkMagicHitPrefab = LoadRequiredAsset<GameObject>(DarkMagicHitSourcePrefabPath);
            var magicSwirlPrefab = LoadRequiredAsset<GameObject>(MagicSwirlSourcePrefabPath);
            var magicHexPrefab = LoadRequiredAsset<GameObject>(MagicHexSourcePrefabPath);

            var root = new GameObject("BlinkFlash");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "ShadowFloor",
                softCircleSprite,
                new Color(0.12f, 0.06f, 0.18f, 0.34f),
                -20,
                Vector3.zero,
                new Vector3(0.94f, 0.76f, 1f));
            CreateSprite(
                root.transform,
                "CoreFloor",
                softCircleSprite,
                new Color(0.48f, 0.24f, 0.72f, 0.22f),
                -10,
                Vector3.zero,
                new Vector3(0.52f, 0.42f, 1f));
            CreateSprite(
                root.transform,
                "FadeRing",
                softCircleSprite,
                new Color(0.78f, 0.64f, 1f, 0.12f),
                -5,
                Vector3.zero,
                new Vector3(0.72f, 0.58f, 1f));

            var hex = InstantiateNestedPrefab(magicHexPrefab, root.transform, "ShadowHex");
            hex.transform.localScale = Vector3.one * 0.24f;
            hex.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            OffsetRendererOrders(hex, 2);

            var hit = InstantiateNestedPrefab(darkMagicHitPrefab, root.transform, "DarkHit");
            hit.transform.localScale = Vector3.one * 0.17f;
            hit.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            OffsetRendererOrders(hit, 8);

            var swirl = InstantiateNestedPrefab(magicSwirlPrefab, root.transform, "MagicSwirl");
            swirl.transform.localScale = Vector3.one * 0.18f;
            swirl.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            OffsetRendererOrders(swirl, 12);

            var rings = InstantiateNestedPrefab(burstRingsPrefab, root.transform, "BlinkRings");
            rings.transform.localScale = Vector3.one * 0.1f;
            rings.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            OffsetRendererOrders(rings, 16);

            SavePrefab(root, BlinkFlashPrefabPath);
            RefreshResourcesCopy(BlinkFlashPrefabPath, BlinkFlashResourcesPrefabPath);
        }

        private static void RefreshResourcesCopy(string sourcePrefabPath, string destinationPrefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(destinationPrefabPath);
            }

            if (!AssetDatabase.CopyAsset(sourcePrefabPath, destinationPrefabPath))
            {
                throw new IOException($"Could not copy prefab from {sourcePrefabPath} to {destinationPrefabPath}.");
            }
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                var texture = BuildSoftCircleTexture(128);
                File.WriteAllBytes(GetAbsoluteProjectPath(SoftCircleSpritePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SoftCircleSpritePath, ImportAssetOptions.ForceSynchronousImport);

                var importer = AssetImporter.GetAtPath(SoftCircleSpritePath) as TextureImporter;
                if (importer != null)
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

        private static GameObject InstantiateNestedPrefab(GameObject sourcePrefab, Transform parent, string name)
        {
            var instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                throw new IOException($"Could not instantiate prefab at {AssetDatabase.GetAssetPath(sourcePrefab)}.");
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void OffsetRendererOrders(GameObject root, int baseOrder)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var minOrder = renderers[0].sortingOrder;
            for (var i = 1; i < renderers.Length; i++)
            {
                minOrder = Mathf.Min(minOrder, renderers[i].sortingOrder);
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = baseOrder + (renderers[i].sortingOrder - minOrder);
            }
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
    }
}
