using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class FrostMageVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build FrostMage VFX Prefabs";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string BlizzardFieldPrefabPath = SkillPrefabsFolder + "/FrostMageBlizzardField.prefab";
        private const string IceLineSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Top_down_attack/top_down_ice_line.prefab";
        private const string IceCircleSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Top_down_attack/top_down_ice_circle.prefab";

        [MenuItem(BuildMenuPath)]
        public static void BuildFrostMageVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildBlizzardFieldPrefab(softCircleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FrostMage VFX prefabs rebuilt.");
        }

        public static void BuildFrostMageVfxPrefabsBatch()
        {
            BuildFrostMageVfxPrefabs();
        }

        private static void BuildBlizzardFieldPrefab(Sprite softCircleSprite)
        {
            var iceLinePrefab = LoadRequiredAsset<GameObject>(IceLineSourcePrefabPath);
            var iceCirclePrefab = LoadRequiredAsset<GameObject>(IceCircleSourcePrefabPath);

            var root = new GameObject("FrostMageBlizzardField");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "ColdFloor",
                softCircleSprite,
                new Color(0.55f, 0.84f, 1f, 0.20f),
                -30,
                Vector3.zero,
                new Vector3(0.98f, 0.94f, 1f));
            CreateSprite(
                root.transform,
                "ColdCore",
                softCircleSprite,
                new Color(0.86f, 0.97f, 1f, 0.14f),
                -20,
                Vector3.zero,
                new Vector3(0.56f, 0.52f, 1f));
            CreateSprite(
                root.transform,
                "ColdEdgeTint",
                softCircleSprite,
                new Color(0.42f, 0.74f, 1f, 0.12f),
                -10,
                Vector3.zero,
                new Vector3(0.84f, 0.80f, 1f));

            var outerRing = InstantiateNestedPrefab(iceCirclePrefab, root.transform, "OuterIceRing");
            ConfigureAreaSourceInstance(outerRing, Vector3.zero, new Vector3(0.104f, 0.104f, 0.104f), 0f);

            CreateIceLine(root.transform, iceLinePrefab, "LineNorthSouth", new Vector3(0f, 0f, 0f), 0.108f);
            CreateIceLine(root.transform, iceLinePrefab, "LineEastWest", new Vector3(-0.12f, 0f, 0f), 0.108f);
            CreateIceLine(root.transform, iceLinePrefab, "LineDiagonalA", new Vector3(0.12f, 0f, 0f), 0.092f);
            CreateIceLine(root.transform, iceLinePrefab, "LineDiagonalB", new Vector3(0f, 0.08f, 0f), 0.092f);

            SavePrefab(root, BlizzardFieldPrefabPath);
        }

        private static void CreateIceLine(Transform parent, GameObject sourcePrefab, string name, Vector3 localPosition, float uniformScale)
        {
            var line = InstantiateNestedPrefab(sourcePrefab, parent, name);
            ConfigureAreaSourceInstance(line, localPosition, new Vector3(uniformScale, uniformScale, uniformScale), 0f);
        }

        private static void ConfigureAreaSourceInstance(GameObject instance, Vector3 localPosition, Vector3 localScale, float zRotation = 0f)
        {
            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;
            instance.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
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
                throw new System.InvalidOperationException($"Could not instantiate prefab at {AssetDatabase.GetAssetPath(sourcePrefab)}");
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
