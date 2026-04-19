using System.IO;
using Fight.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class WindchimeVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Windchime VFX Prefabs";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string SharedPrefabsFolder = PrefabsRootFolder + "/Shared";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/WindchimeVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string EchoCanopyGuardPrefabPath = SharedPrefabsFolder + "/WindchimeEchoCanopyGuard.prefab";
        private const string EchoCanopyBurstPrefabPath = SkillPrefabsFolder + "/WindchimeEchoCanopyBurst.prefab";
        private const string StillwindDomainPrefabPath = SkillPrefabsFolder + "/WindchimeStillwindDomainField.prefab";
        private const string WindchimeActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_002_windchime/Echo Canopy.asset";
        private const string ShieldWindSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Shields/Shield_wind.prefab";
        private const string HitWindSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/Hit_wind.prefab";
        private const string AreaGenericBlueSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Area_generic/Area_generic_blue.prefab";
        private const string AreaGenericBlueOutbreakSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Area_generic/Area_generic_blue_outbreak.prefab";
        private const float EchoCanopyGuardSourceScale = 0.54f;
        private const float EchoCanopyBurstSourceScale = 0.28f;
        private const float StillwindDomainFieldScale = 0.16f;
        private const float StillwindDomainPulseScale = 0.18f;
        private static readonly Quaternion TopDownRotation = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Vector3 ReactiveGuardLocalOffset = new Vector3(0f, 0.58f, 0f);
        private static readonly Color GuardHaloBackColor = new Color(0.72f, 0.92f, 1f, 0.18f);
        private static readonly Color GuardHaloCoreColor = new Color(0.94f, 1f, 1f, 0.22f);
        private static readonly Color GuardWakeColor = new Color(0.82f, 0.95f, 1f, 0.16f);
        private static readonly Color GuardHighlightColor = new Color(0.98f, 1f, 1f, 0.14f);
        private static readonly Color GuardWindStartMinColor = new Color(0.68f, 0.90f, 1f, 0.18f);
        private static readonly Color GuardWindStartMaxColor = new Color(0.98f, 1f, 1f, 0.42f);
        private static readonly Color GuardRingStartMinColor = new Color(0.70f, 0.92f, 1f, 0.26f);
        private static readonly Color GuardRingStartMaxColor = new Color(0.98f, 1f, 1f, 0.58f);
        private static readonly Color BurstStartMinColor = new Color(0.72f, 0.90f, 1f, 0.24f);
        private static readonly Color BurstStartMaxColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color StillwindBaseColor = new Color(0.54f, 0.84f, 0.98f, 0.16f);
        private static readonly Color StillwindCoreColor = new Color(0.90f, 0.98f, 1f, 0.14f);
        private static readonly Color StillwindEdgeTintColor = new Color(0.46f, 0.80f, 0.96f, 0.11f);
        private static readonly Color StillwindHaloColor = new Color(0.84f, 0.97f, 1f, 0.10f);
        private static readonly Color StillwindWakeColor = new Color(0.78f, 0.94f, 1f, 0.12f);
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
        public static void BuildWindchimeVfxPrefabs()
        {
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);
            EnsureFolder(SharedPrefabsFolder);

            BuildEchoCanopyGuardPrefab();
            BuildEchoCanopyBurstPrefab();
            BuildStillwindDomainPrefab();
            SyncStage01DemoAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Windchime VFX prefabs rebuilt.");
        }

        public static void BuildWindchimeVfxPrefabsBatch()
        {
            BuildWindchimeVfxPrefabs();
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
                BuildWindchimeVfxPrefabs();
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

            return GetLatestTimestampUtc(
                    BuilderScriptAssetPath,
                    SoftCircleSpritePath,
                    ShieldWindSourcePrefabPath,
                    HitWindSourcePrefabPath,
                    AreaGenericBlueSourcePrefabPath,
                    AreaGenericBlueOutbreakSourcePrefabPath)
                > GetLatestTimestampUtc(
                    EchoCanopyGuardPrefabPath,
                    EchoCanopyBurstPrefabPath,
                    StillwindDomainPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(EchoCanopyGuardPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(EchoCanopyBurstPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(StillwindDomainPrefabPath) != null;
        }

        private static void BuildEchoCanopyGuardPrefab()
        {
            SavePrefab(CreateEchoCanopyGuardRoot(), EchoCanopyGuardPrefabPath);
        }

        private static void BuildEchoCanopyBurstPrefab()
        {
            SavePrefab(CreateEchoCanopyBurstRoot(), EchoCanopyBurstPrefabPath);
        }

        private static void BuildStillwindDomainPrefab()
        {
            SavePrefab(CreateStillwindDomainRoot(), StillwindDomainPrefabPath);
        }

        private static GameObject CreateEchoCanopyGuardRoot()
        {
            var softCircleSprite = EnsureSoftCircleSprite();
            var shieldWindPrefab = LoadRequiredAsset<GameObject>(ShieldWindSourcePrefabPath);

            var root = new GameObject("WindchimeEchoCanopyGuard");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "RearVeil",
                softCircleSprite,
                GuardHaloBackColor,
                0,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(0.92f, 0.72f, 1f));
            CreateSprite(
                root.transform,
                "CoreVeil",
                softCircleSprite,
                GuardHaloCoreColor,
                2,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.64f, 0.52f, 1f));

            var leftWake = CreateSprite(
                root.transform,
                "LeftWake",
                softCircleSprite,
                GuardWakeColor,
                1,
                new Vector3(-0.24f, -0.03f, 0f),
                new Vector3(0.22f, 0.46f, 1f));
            leftWake.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            var rightWake = CreateSprite(
                root.transform,
                "RightWake",
                softCircleSprite,
                GuardWakeColor,
                1,
                new Vector3(0.24f, -0.03f, 0f),
                new Vector3(0.22f, 0.46f, 1f));
            rightWake.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            CreateSprite(
                root.transform,
                "UpperHighlight",
                softCircleSprite,
                GuardHighlightColor,
                4,
                new Vector3(0f, 0.14f, 0f),
                new Vector3(0.38f, 0.22f, 1f));

            var shieldWind = InstantiateNestedPrefab(shieldWindPrefab, root.transform, "ShieldWind");
            shieldWind.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            shieldWind.transform.localScale = Vector3.one * EchoCanopyGuardSourceScale;
            shieldWind.transform.localRotation = TopDownRotation;
            DisableChild(shieldWind.transform, "shield_AB");
            DisableChild(shieldWind.transform, "shield_add");
            DisableChild(shieldWind.transform, "leaves");
            ConfigureParticleSystems(shieldWind, loop: true, prewarm: true);
            RetintGuardParticleSystems(shieldWind);
            OffsetRendererOrders(shieldWind, 8);

            return root;
        }

        private static GameObject CreateEchoCanopyBurstRoot()
        {
            var hitWindPrefab = LoadRequiredAsset<GameObject>(HitWindSourcePrefabPath);

            var root = new GameObject("WindchimeEchoCanopyBurst");
            root.AddComponent<SortingGroup>();

            var windBurst = InstantiateNestedPrefab(hitWindPrefab, root.transform, "HitWind");
            windBurst.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            windBurst.transform.localScale = Vector3.one * EchoCanopyBurstSourceScale;
            ConfigureParticleSystems(windBurst, loop: false, prewarm: false);
            RetintBurstParticleSystems(windBurst);
            OffsetRendererOrders(windBurst, 18);

            return root;
        }

        private static GameObject CreateStillwindDomainRoot()
        {
            var softCircleSprite = EnsureSoftCircleSprite();
            var areaGenericBluePrefab = LoadRequiredAsset<GameObject>(AreaGenericBlueSourcePrefabPath);
            var areaGenericBlueOutbreakPrefab = LoadRequiredAsset<GameObject>(AreaGenericBlueOutbreakSourcePrefabPath);

            var root = new GameObject("WindchimeStillwindDomainField");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "StillwindBase",
                softCircleSprite,
                StillwindBaseColor,
                -30,
                Vector3.zero,
                new Vector3(0.98f, 0.94f, 1f));
            CreateSprite(
                root.transform,
                "StillwindCore",
                softCircleSprite,
                StillwindCoreColor,
                -24,
                new Vector3(0f, 0.01f, 0f),
                new Vector3(0.64f, 0.60f, 1f));
            CreateSprite(
                root.transform,
                "StillwindEdgeTint",
                softCircleSprite,
                StillwindEdgeTintColor,
                -18,
                Vector3.zero,
                new Vector3(0.84f, 0.80f, 1f));
            CreateSprite(
                root.transform,
                "StillwindHalo",
                softCircleSprite,
                StillwindHaloColor,
                -12,
                Vector3.zero,
                new Vector3(1.08f, 1.02f, 1f));

            var frontWake = CreateSprite(
                root.transform,
                "FrontWake",
                softCircleSprite,
                StillwindWakeColor,
                -8,
                new Vector3(0f, 0.23f, 0f),
                new Vector3(0.42f, 0.16f, 1f));
            frontWake.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);

            var rearWake = CreateSprite(
                root.transform,
                "RearWake",
                softCircleSprite,
                StillwindWakeColor,
                -8,
                new Vector3(0f, -0.23f, 0f),
                new Vector3(0.42f, 0.16f, 1f));
            rearWake.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);

            var leftWake = CreateSprite(
                root.transform,
                "LeftWake",
                softCircleSprite,
                StillwindWakeColor,
                -8,
                new Vector3(-0.25f, 0f, 0f),
                new Vector3(0.18f, 0.38f, 1f));
            leftWake.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            var rightWake = CreateSprite(
                root.transform,
                "RightWake",
                softCircleSprite,
                StillwindWakeColor,
                -8,
                new Vector3(0.25f, 0f, 0f),
                new Vector3(0.18f, 0.38f, 1f));
            rightWake.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var upperSweep = CreateSprite(
                root.transform,
                "UpperSweep",
                softCircleSprite,
                new Color(0.82f, 0.95f, 1f, 0.09f),
                -6,
                new Vector3(0f, 0.31f, 0f),
                new Vector3(0.56f, 0.12f, 1f));
            upperSweep.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);

            var lowerSweep = CreateSprite(
                root.transform,
                "LowerSweep",
                softCircleSprite,
                new Color(0.82f, 0.95f, 1f, 0.09f),
                -6,
                new Vector3(0f, -0.31f, 0f),
                new Vector3(0.56f, 0.12f, 1f));
            lowerSweep.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            var stillwindArea = InstantiateNestedPrefab(areaGenericBluePrefab, root.transform, "StillwindArea");
            ConfigureAreaSourceInstance(stillwindArea, Vector3.zero, Vector3.one * StillwindDomainFieldScale);
            ConfigureParticleSystems(stillwindArea, loop: true, prewarm: true);
            OffsetRendererOrders(stillwindArea, -4);

            var stillwindPulse = InstantiateNestedPrefab(areaGenericBlueOutbreakPrefab, root.transform, "StillwindPulse");
            ConfigureAreaSourceInstance(stillwindPulse, Vector3.zero, Vector3.one * StillwindDomainPulseScale);
            ConfigureParticleSystems(stillwindPulse, loop: true, prewarm: true);
            OffsetRendererOrders(stillwindPulse, -2);

            return root;
        }

        private static void SyncStage01DemoAssets()
        {
            var echoCanopyGuardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoCanopyGuardPrefabPath);
            var echoCanopyBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoCanopyBurstPrefabPath);
            var echoCanopySkill = AssetDatabase.LoadAssetAtPath<SkillData>(WindchimeActiveSkillAssetPath);
            if (echoCanopySkill == null || echoCanopyGuardPrefab == null || echoCanopyBurstPrefab == null)
            {
                return;
            }

            echoCanopySkill.reactiveGuard ??= new ReactiveGuardData();
            if (echoCanopySkill.reactiveGuard.guardLoopVfxPrefab == echoCanopyGuardPrefab
                && echoCanopySkill.reactiveGuard.guardLoopVfxLocalOffset == ReactiveGuardLocalOffset
                && echoCanopySkill.reactiveGuard.guardLoopVfxLocalScale == Vector3.one
                && echoCanopySkill.reactiveGuard.guardLoopVfxEulerAngles == Vector3.zero
                && echoCanopySkill.reactiveGuard.triggerVfxPrefab == echoCanopyBurstPrefab
                && echoCanopySkill.reactiveGuard.triggerVfxLocalOffset == ReactiveGuardLocalOffset
                && echoCanopySkill.reactiveGuard.triggerVfxLocalScale == Vector3.one
                && echoCanopySkill.reactiveGuard.triggerVfxEulerAngles == Vector3.zero)
            {
                return;
            }

            echoCanopySkill.reactiveGuard.guardLoopVfxPrefab = echoCanopyGuardPrefab;
            echoCanopySkill.reactiveGuard.guardLoopVfxLocalOffset = ReactiveGuardLocalOffset;
            echoCanopySkill.reactiveGuard.guardLoopVfxLocalScale = Vector3.one;
            echoCanopySkill.reactiveGuard.guardLoopVfxEulerAngles = Vector3.zero;
            echoCanopySkill.reactiveGuard.triggerVfxPrefab = echoCanopyBurstPrefab;
            echoCanopySkill.reactiveGuard.triggerVfxLocalOffset = ReactiveGuardLocalOffset;
            echoCanopySkill.reactiveGuard.triggerVfxLocalScale = Vector3.one;
            echoCanopySkill.reactiveGuard.triggerVfxEulerAngles = Vector3.zero;
            EditorUtility.SetDirty(echoCanopySkill);
        }

        private static void ConfigureParticleSystems(GameObject root, bool loop, bool prewarm)
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
            }
        }

        private static void ConfigureAreaSourceInstance(GameObject instance, Vector3 localPosition, Vector3 localScale)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;
            instance.transform.localRotation = TopDownRotation;
        }

        private static void RetintGuardParticleSystems(GameObject root)
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
                switch (particleName)
                {
                    case "circle":
                    case "stroke":
                        main.startColor = new ParticleSystem.MinMaxGradient(GuardRingStartMinColor, GuardRingStartMaxColor);
                        ApplyLifetimeGradient(
                            particleSystem,
                            new Color(1f, 1f, 1f, 0f),
                            new Color(0.90f, 0.98f, 1f, 0.34f),
                            new Color(0.66f, 0.90f, 1f, 0f));
                        break;
                    default:
                        main.startColor = new ParticleSystem.MinMaxGradient(GuardWindStartMinColor, GuardWindStartMaxColor);
                        ApplyLifetimeGradient(
                            particleSystem,
                            new Color(1f, 1f, 1f, 0f),
                            new Color(0.86f, 0.97f, 1f, 0.28f),
                            new Color(0.68f, 0.90f, 1f, 0f));
                        break;
                }
            }
        }

        private static void RetintBurstParticleSystems(GameObject root)
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
                main.startColor = new ParticleSystem.MinMaxGradient(BurstStartMinColor, BurstStartMaxColor);
                ApplyLifetimeGradient(
                    particleSystem,
                    new Color(1f, 1f, 1f, 0f),
                    new Color(0.86f, 0.96f, 1f, 0.42f),
                    new Color(0.64f, 0.88f, 1f, 0f));
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

            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            var child = Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(child))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
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

        private static void SavePrefab(GameObject root, string assetPath)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static T LoadRequiredAsset<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new FileNotFoundException($"Required asset missing at {assetPath}");
            }

            return asset;
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                EnsureFolder(GeneratedArtFolder);
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
                    alpha *= alpha;
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

            if (string.Equals(root.name, childName, System.StringComparison.Ordinal))
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

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static System.DateTime GetLatestTimestampUtc(params string[] assetPaths)
        {
            var latest = System.DateTime.MinValue;
            if (assetPaths == null)
            {
                return latest;
            }

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
