using System.IO;
using Fight.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class RiflemanVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Rifleman VFX Prefabs";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/RiflemanVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";

        private const string FragGrenadePrefabPath = SkillPrefabsFolder + "/RiflemanFragGrenadeBurst.prefab";
        private const string FragGrenadeSkillAssetPath = "Assets/Data/Stage01Demo/Skills/marksman_002_rifleman/Frag Grenade.asset";

        private const string BurstRingsSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_rings.prefab";
        private const string BurstSharpSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_sharp.prefab";
        private const string FireExplosionEarthSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Fire/Fire_explosion_earth.prefab";

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
        public static void BuildRiflemanVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildFragGrenadeBurstPrefab(softCircleSprite);
            SyncStage01DemoAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rifleman VFX prefabs rebuilt.");
        }

        public static void BuildRiflemanVfxPrefabsBatch()
        {
            BuildRiflemanVfxPrefabs();
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
                BuildRiflemanVfxPrefabs();
                return;
            }

            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadePrefabPath) != null;
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SoftCircleSpritePath)
                > GetLatestTimestampUtc(FragGrenadePrefabPath);
        }

        private static void SyncStage01DemoAssets()
        {
            var fragGrenadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadePrefabPath);
            var fragGrenadeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(FragGrenadeSkillAssetPath);
            if (fragGrenadeSkill == null)
            {
                return;
            }

            fragGrenadeSkill.persistentAreaVfxPrefab = fragGrenadePrefab;
            fragGrenadeSkill.persistentAreaVfxScaleMultiplier = 1f;
            fragGrenadeSkill.persistentAreaVfxEulerAngles = Vector3.zero;
            fragGrenadeSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
            EditorUtility.SetDirty(fragGrenadeSkill);
        }

        private static void BuildFragGrenadeBurstPrefab(Sprite softCircleSprite)
        {
            var burstRingsPrefab = LoadRequiredAsset<GameObject>(BurstRingsSourcePrefabPath);
            var burstSharpPrefab = LoadRequiredAsset<GameObject>(BurstSharpSourcePrefabPath);
            var fireExplosionEarthPrefab = LoadRequiredAsset<GameObject>(FireExplosionEarthSourcePrefabPath);

            var root = new GameObject("RiflemanFragGrenadeBurst");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "BlastShadow",
                softCircleSprite,
                new Color(0.11f, 0.09f, 0.08f, 0.16f),
                -4,
                Vector3.zero,
                new Vector3(1.04f, 0.96f, 1f));
            CreateSprite(
                root.transform,
                "ScorchTint",
                softCircleSprite,
                new Color(0.34f, 0.17f, 0.08f, 0.12f),
                -3,
                Vector3.zero,
                new Vector3(0.92f, 0.86f, 1f));
            CreateSprite(
                root.transform,
                "ShockwaveTint",
                softCircleSprite,
                new Color(1f, 0.47f, 0.14f, 0.12f),
                -2,
                Vector3.zero,
                new Vector3(0.74f, 0.68f, 1f));
            CreateSprite(
                root.transform,
                "HeatCore",
                softCircleSprite,
                new Color(1f, 0.88f, 0.56f, 0.14f),
                -1,
                Vector3.zero,
                new Vector3(0.34f, 0.30f, 1f));

            var outerRing = InstantiateNestedPrefab(burstRingsPrefab, root.transform, "OuterRing");
            outerRing.transform.localScale = Vector3.one * 0.22f;
            outerRing.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            TuneBurstParticleSystems(outerRing, 0.45f, 1.35f);
            OffsetRendererOrders(outerRing, 8);

            var fragmentBurst = InstantiateNestedPrefab(burstSharpPrefab, root.transform, "FragmentBurst");
            fragmentBurst.transform.localScale = Vector3.one * 0.18f;
            fragmentBurst.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            TuneBurstParticleSystems(fragmentBurst, 0.45f, 1.45f);
            OffsetRendererOrders(fragmentBurst, 10);

            var centerExplosion = InstantiateNestedPrefab(fireExplosionEarthPrefab, root.transform, "CenterExplosion");
            centerExplosion.transform.localScale = Vector3.one * 0.18f;
            centerExplosion.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            TuneBurstParticleSystems(centerExplosion, 0.45f, 1.4f);
            OffsetRendererOrders(centerExplosion, 12);

            SavePrefab(root, FragGrenadePrefabPath);
        }

        private static void TuneBurstParticleSystems(GameObject root, float maxDurationSeconds, float minSimulationSpeed)
        {
            if (root == null)
            {
                return;
            }

            var particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                var main = particleSystem.main;
                main.loop = false;
                main.prewarm = false;
                main.duration = Mathf.Min(main.duration, maxDurationSeconds);
                main.simulationSpeed = Mathf.Max(main.simulationSpeed, minSimulationSpeed);
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
    }
}
