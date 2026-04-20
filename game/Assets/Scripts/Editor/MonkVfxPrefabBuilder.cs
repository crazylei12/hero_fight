using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class MonkVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Monk Guardian Mantra VFX Prefab";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/MonkVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string GuardianMantraBurstPrefabPath = SkillPrefabsFolder + "/MonkGuardianMantraBurst.prefab";
        private const string ShieldGoldSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Shields/Shield_gold.prefab";
        private const string LightBuffSourcePrefabPath = "Assets/Hun0FX/FX/BuffnDebuff_vol1/FX_Buff_01_light.prefab";

        private static readonly Quaternion TopDownRotation = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Color ShieldRingStartMinColor = new Color(0.88f, 0.66f, 0.18f, 0.24f);
        private static readonly Color ShieldRingStartMaxColor = new Color(1f, 0.92f, 0.52f, 0.66f);
        private static readonly Color ShieldFieldStartMinColor = new Color(0.94f, 0.78f, 0.28f, 0.10f);
        private static readonly Color ShieldFieldStartMaxColor = new Color(1f, 0.95f, 0.78f, 0.32f);
        private static readonly Color HolyLightStartMinColor = new Color(1f, 0.86f, 0.42f, 0.12f);
        private static readonly Color HolyLightStartMaxColor = new Color(1f, 0.98f, 0.88f, 0.36f);
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
        public static void BuildMonkGuardianMantraVfxPrefab()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildGuardianMantraBurstPrefab(softCircleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Monk Guardian Mantra VFX prefab rebuilt.");
        }

        public static void BuildMonkGuardianMantraVfxPrefabBatch()
        {
            BuildMonkGuardianMantraVfxPrefab();
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

            if (!NeedsRebuild())
            {
                return;
            }

            BuildMonkGuardianMantraVfxPrefab();
        }

        private static void BuildGuardianMantraBurstPrefab(Sprite softCircleSprite)
        {
            var shieldGoldPrefab = LoadRequiredAsset<GameObject>(ShieldGoldSourcePrefabPath);
            var lightBuffPrefab = LoadRequiredAsset<GameObject>(LightBuffSourcePrefabPath);

            var root = new GameObject("MonkGuardianMantraBurst");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "RearHalo",
                softCircleSprite,
                new Color(0.98f, 0.80f, 0.30f, 0.10f),
                -22,
                new Vector3(0f, -0.02f, 0f),
                new Vector3(1.28f, 1.16f, 1f));
            CreateSprite(
                root.transform,
                "GroundHalo",
                softCircleSprite,
                new Color(1f, 0.90f, 0.52f, 0.16f),
                -18,
                Vector3.zero,
                new Vector3(1.02f, 0.94f, 1f));
            CreateSprite(
                root.transform,
                "CoreHalo",
                softCircleSprite,
                new Color(1f, 0.97f, 0.78f, 0.18f),
                -14,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.62f, 0.56f, 1f));
            CreateSprite(
                root.transform,
                "EdgeAura",
                softCircleSprite,
                new Color(1f, 0.96f, 0.84f, 0.08f),
                -12,
                Vector3.zero,
                new Vector3(1.16f, 1.06f, 1f));

            var frontSweep = CreateSprite(
                root.transform,
                "FrontSweep",
                softCircleSprite,
                new Color(1f, 0.92f, 0.58f, 0.10f),
                -10,
                new Vector3(0f, 0.28f, 0f),
                new Vector3(0.52f, 0.13f, 1f));
            frontSweep.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);

            var rearSweep = CreateSprite(
                root.transform,
                "RearSweep",
                softCircleSprite,
                new Color(1f, 0.92f, 0.58f, 0.10f),
                -10,
                new Vector3(0f, -0.28f, 0f),
                new Vector3(0.52f, 0.13f, 1f));
            rearSweep.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);

            var leftSweep = CreateSprite(
                root.transform,
                "LeftSweep",
                softCircleSprite,
                new Color(0.98f, 0.86f, 0.42f, 0.08f),
                -10,
                new Vector3(-0.28f, 0f, 0f),
                new Vector3(0.15f, 0.44f, 1f));
            leftSweep.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            var rightSweep = CreateSprite(
                root.transform,
                "RightSweep",
                softCircleSprite,
                new Color(0.98f, 0.86f, 0.42f, 0.08f),
                -10,
                new Vector3(0.28f, 0f, 0f),
                new Vector3(0.15f, 0.44f, 1f));
            rightSweep.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var goldShield = InstantiateNestedPrefab(shieldGoldPrefab, root.transform, "GoldShield");
            goldShield.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            goldShield.transform.localScale = Vector3.one * 0.58f;
            goldShield.transform.localRotation = TopDownRotation;
            DisableChild(goldShield.transform, "shield_AB");
            DisableChild(goldShield.transform, "shield_add");
            ConfigureParticleSystems(goldShield, loop: false, prewarm: false, durationCap: 1.05f, simulationSpeedFloor: 1.08f);
            RetintShieldParticleSystems(goldShield);
            OffsetRendererOrders(goldShield, 8);

            var holyLight = InstantiateNestedPrefab(lightBuffPrefab, root.transform, "HolyLight");
            holyLight.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            holyLight.transform.localScale = Vector3.one * 0.18f;
            ConfigureParticleSystems(holyLight, loop: false, prewarm: false, durationCap: 1.1f, simulationSpeedFloor: 1.14f);
            RetintHolyLightParticleSystems(holyLight);
            OffsetRendererOrders(holyLight, 18);

            SavePrefab(root, GuardianMantraBurstPrefabPath);
        }

        private static void DisableChild(Transform root, string childName)
        {
            var child = FindChildRecursive(root, childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindChildRecursive(root.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void ConfigureParticleSystems(
            GameObject root,
            bool loop,
            bool prewarm,
            float durationCap,
            float simulationSpeedFloor)
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
                main.loop = loop;
                main.prewarm = loop && prewarm;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.duration = Mathf.Min(main.duration, durationCap);
                main.simulationSpeed = Mathf.Max(main.simulationSpeed, simulationSpeedFloor);
            }
        }

        private static void RetintShieldParticleSystems(GameObject root)
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

                var particleName = particleSystem.gameObject.name;
                var main = particleSystem.main;
                if (particleName == "circle" || particleName == "stroke")
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(ShieldRingStartMinColor, ShieldRingStartMaxColor);
                    ApplyLifetimeGradient(
                        particleSystem,
                        new Color(1f, 1f, 1f, 0f),
                        new Color(1f, 0.92f, 0.58f, 0.36f),
                        new Color(0.92f, 0.74f, 0.18f, 0f));
                }
                else
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(ShieldFieldStartMinColor, ShieldFieldStartMaxColor);
                    ApplyLifetimeGradient(
                        particleSystem,
                        new Color(1f, 1f, 1f, 0f),
                        new Color(1f, 0.96f, 0.84f, 0.20f),
                        new Color(0.96f, 0.82f, 0.38f, 0f));
                }
            }
        }

        private static void RetintHolyLightParticleSystems(GameObject root)
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
                main.startColor = new ParticleSystem.MinMaxGradient(HolyLightStartMinColor, HolyLightStartMaxColor);
                ApplyLifetimeGradient(
                    particleSystem,
                    new Color(1f, 1f, 1f, 0f),
                    new Color(1f, 0.96f, 0.74f, 0.24f),
                    new Color(1f, 0.86f, 0.32f, 0f));
            }
        }

        private static void ApplyLifetimeGradient(
            ParticleSystem particleSystem,
            Color startColor,
            Color midColor,
            Color endColor)
        {
            if (particleSystem == null)
            {
                return;
            }

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(midColor, 0.35f),
                    new GradientColorKey(endColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(midColor.a, 0.35f),
                    new GradientAlphaKey(endColor.a, 1f),
                });
            colorOverLifetime.color = gradient;
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

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            return GetLatestTimestampUtc(
                    BuilderScriptAssetPath,
                    SoftCircleSpritePath,
                    ShieldGoldSourcePrefabPath,
                    LightBuffSourcePrefabPath)
                > GetLatestTimestampUtc(GuardianMantraBurstPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(GuardianMantraBurstPrefabPath) != null;
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

                var fullPath = GetAbsoluteProjectPath(assetPath);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var timestamp = File.GetLastWriteTimeUtc(fullPath);
                if (timestamp > latest)
                {
                    latest = timestamp;
                }
            }

            return latest;
        }
    }
}
