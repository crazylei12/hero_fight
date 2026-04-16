using System.IO;
using Fight.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class SunpriestVfxPrefabBuilder
    {
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string ProjectilePrefabsFolder = PrefabsRootFolder + "/Projectiles";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string SharedPrefabsFolder = PrefabsRootFolder + "/Shared";
        private const string SharedVfxResourcesFolder = "Assets/Resources/Stage01Demo/VFX/Shared";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/SunpriestVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";

        private const string ProjectilePrefabPath = ProjectilePrefabsFolder + "/SunpriestBasicAttackProjectile.prefab";
        private const string HealImpactPrefabPath = SharedPrefabsFolder + "/SunpriestHealImpact.prefab";
        private const string HealImpactResourcesPrefabPath = SharedVfxResourcesFolder + "/HealReceivedImpact.prefab";
        private const string SunBlessingFieldPrefabPath = SkillPrefabsFolder + "/SunpriestSunBlessingField.prefab";

        private const string SunpriestHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_001_sunpriest/Sunpriest.asset";
        private const string SunBlessingSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_001_sunpriest/Sun Blessing.asset";

        private const string LightProjectileSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/Projectiles_light.prefab";
        private const string RegenerationHealthLoopSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_loop.prefab";
        private const string RegenerationHealthAreaSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_area.prefab";
        private const string RegenerationHealthAreaLoopSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_area_loop.prefab";
        private const string OrbsGoldSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Orbs/Orbs_gold.prefab";
        private const string LightMissileSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_magic_missile_small_yellow.prefab";
        private const string LightSparkSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_light_spark_small_yellow.prefab";
        private const string HealImpactSourceChildName = "plus";
        private static readonly Vector3 HealImpactLocalOffset = new Vector3(-0.36f, 0.04f, 0f);
        private const float HealImpactLoopScale = 0.54f;
        private const float HealImpactLoopDurationSeconds = 0.6f;
        private const float HealImpactLoopSimulationSpeed = 1.2f;
        private static readonly Quaternion HealImpactAdaptedRotation = Quaternion.Euler(90f, 0f, 0f);

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

        public static void BuildSunpriestVfxPrefabsBatch()
        {
            BuildSunpriestVfxPrefabs();
            EditorApplication.Exit(0);
        }

        public static void BuildSunpriestVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(ProjectilePrefabsFolder);
            EnsureFolder(SkillPrefabsFolder);
            EnsureFolder(SharedPrefabsFolder);
            EnsureFolder(SharedVfxResourcesFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildProjectilePrefab(softCircleSprite);
            BuildHealImpactPrefabs();
            BuildSunBlessingFieldPrefab(softCircleSprite);
            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sunpriest VFX prefabs rebuilt.");
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
                BuildSunpriestVfxPrefabs();
                return;
            }

            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(HealImpactPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(HealImpactResourcesPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(SunBlessingFieldPrefabPath) != null;
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SoftCircleSpritePath)
                > GetLatestTimestampUtc(ProjectilePrefabPath, HealImpactPrefabPath, HealImpactResourcesPrefabPath, SunBlessingFieldPrefabPath);
        }

        private static void SyncStage01DemoAssets()
        {
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var sunBlessingFieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunBlessingFieldPrefabPath);

            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(SunpriestHeroAssetPath);
            if (hero != null)
            {
                hero.visualConfig ??= new HeroVisualConfig();
                hero.visualConfig.projectilePrefab = projectilePrefab;
                hero.visualConfig.projectileAlignToMovement = projectilePrefab != null;
                hero.visualConfig.projectileEulerAngles = Vector3.zero;
                hero.visualConfig.hitVfxPrefab = null;
                EditorUtility.SetDirty(hero);
            }

            var sunBlessing = AssetDatabase.LoadAssetAtPath<SkillData>(SunBlessingSkillAssetPath);
            if (sunBlessing != null)
            {
                sunBlessing.persistentAreaVfxPrefab = sunBlessingFieldPrefab;
                sunBlessing.persistentAreaVfxScaleMultiplier = 1f;
                sunBlessing.persistentAreaVfxEulerAngles = Vector3.zero;
                sunBlessing.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(sunBlessing);
            }
        }

        private static void BuildProjectilePrefab(Sprite softCircleSprite)
        {
            var lightProjectilePrefab = LoadRequiredAsset<GameObject>(LightProjectileSourcePrefabPath);
            var lightMissilePrefab = LoadRequiredAsset<GameObject>(LightMissileSourcePrefabPath);
            var lightSparkPrefab = LoadRequiredAsset<GameObject>(LightSparkSourcePrefabPath);
            var orbsGoldPrefab = LoadRequiredAsset<GameObject>(OrbsGoldSourcePrefabPath);

            var root = new GameObject("SunpriestBasicAttackProjectile");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "OuterGlow",
                softCircleSprite,
                new Color(1f, 0.9f, 0.42f, 0.26f),
                2,
                Vector3.zero,
                Vector3.one * 0.34f);
            CreateSprite(
                root.transform,
                "InnerGlow",
                softCircleSprite,
                new Color(1f, 0.98f, 0.84f, 0.7f),
                4,
                Vector3.zero,
                Vector3.one * 0.2f);

            var trailOrb = InstantiateNestedPrefab(orbsGoldPrefab, root.transform, "TrailOrb");
            trailOrb.transform.localPosition = new Vector3(-0.08f, 0f, 0f);
            trailOrb.transform.localScale = Vector3.one * 0.07f;
            OffsetRendererOrders(trailOrb, 5);

            var lightSpark = InstantiateNestedPrefab(lightSparkPrefab, root.transform, "LightSpark");
            lightSpark.transform.localPosition = new Vector3(-0.03f, 0f, 0f);
            lightSpark.transform.localScale = Vector3.one * 0.24f;
            OffsetRendererOrders(lightSpark, 8);

            var projectile = InstantiateNestedPrefab(lightProjectilePrefab, root.transform, "LightTrail");
            projectile.transform.localScale = Vector3.one * 0.16f;
            projectile.transform.localPosition = new Vector3(-0.01f, 0f, 0f);
            OffsetRendererOrders(projectile, 11);

            var missile = InstantiateNestedPrefab(lightMissilePrefab, root.transform, "MissileCore");
            missile.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            missile.transform.localScale = Vector3.one * 0.34f;
            OffsetRendererOrders(missile, 15);

            SavePrefab(root, ProjectilePrefabPath);
        }

        private static void BuildHealImpactPrefabs()
        {
            SavePrefab(CreateHealImpactRoot(), HealImpactPrefabPath);
            SavePrefab(CreateHealImpactRoot(), HealImpactResourcesPrefabPath);
        }

        private static GameObject CreateHealImpactRoot()
        {
            var regenerationHealthLoopPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthLoopSourcePrefabPath);

            var root = new GameObject("HealReceivedImpact");
            root.AddComponent<SortingGroup>();

            var healLoop = InstantiateNestedPrefab(regenerationHealthLoopPrefab, root.transform, "HealLoop");
            healLoop.transform.localScale = Vector3.one * HealImpactLoopScale;
            healLoop.transform.localPosition = HealImpactLocalOffset;
            healLoop.transform.localRotation = HealImpactAdaptedRotation;
            KeepOnlyDirectChild(healLoop.transform, HealImpactSourceChildName);
            TuneHealImpactLoop(healLoop);
            OffsetRendererOrders(healLoop, 10);

            return root;
        }

        private static void TuneHealImpactLoop(GameObject healLoop)
        {
            if (healLoop == null)
            {
                return;
            }

            var particleSystems = healLoop.GetComponentsInChildren<ParticleSystem>(true);
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
                main.duration = Mathf.Min(main.duration, HealImpactLoopDurationSeconds);
                main.simulationSpeed = Mathf.Max(main.simulationSpeed, HealImpactLoopSimulationSpeed);
            }
        }

        private static void KeepOnlyDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == childName)
                {
                    child.name = "HealSpark";
                    continue;
                }

                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void BuildSunBlessingFieldPrefab(Sprite softCircleSprite)
        {
            var regenerationAreaLoopPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthAreaLoopSourcePrefabPath);
            var regenerationAreaPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthAreaSourcePrefabPath);
            var adaptedAreaRotation = Quaternion.Euler(90f, 0f, 0f);

            var root = new GameObject("SunpriestSunBlessingField");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "OuterSanctuary",
                softCircleSprite,
                new Color(1f, 0.93f, 0.48f, 0.1f),
                0,
                Vector3.zero,
                new Vector3(1.02f, 1.02f, 1f));
            CreateSprite(
                root.transform,
                "SanctuaryEdge",
                softCircleSprite,
                new Color(1f, 0.98f, 0.8f, 0.14f),
                1,
                Vector3.zero,
                new Vector3(0.86f, 0.86f, 1f));
            CreateSprite(
                root.transform,
                "InnerGlow",
                softCircleSprite,
                new Color(0.96f, 1f, 0.82f, 0.08f),
                2,
                Vector3.zero,
                new Vector3(0.64f, 0.64f, 1f));
            CreateSprite(
                root.transform,
                "BlessingCore",
                softCircleSprite,
                new Color(0.84f, 1f, 0.76f, 0.05f),
                3,
                Vector3.zero,
                new Vector3(0.34f, 0.34f, 1f));

            var sanctuaryLoop = InstantiateNestedPrefab(regenerationAreaLoopPrefab, root.transform, "SanctuaryLoop");
            sanctuaryLoop.transform.localScale = Vector3.one * 0.128f;
            sanctuaryLoop.transform.localPosition = Vector3.zero;
            sanctuaryLoop.transform.localRotation = adaptedAreaRotation;
            OffsetRendererOrders(sanctuaryLoop, 8);

            var sanctuaryLoopInner = InstantiateNestedPrefab(regenerationAreaLoopPrefab, root.transform, "SanctuaryLoopInner");
            sanctuaryLoopInner.transform.localScale = Vector3.one * 0.092f;
            sanctuaryLoopInner.transform.localPosition = Vector3.zero;
            sanctuaryLoopInner.transform.localRotation = adaptedAreaRotation;
            OffsetRendererOrders(sanctuaryLoopInner, 10);

            var areaPulse = InstantiateNestedPrefab(regenerationAreaPrefab, root.transform, "AreaPulse");
            areaPulse.transform.localScale = Vector3.one * 0.074f;
            areaPulse.transform.localPosition = Vector3.zero;
            areaPulse.transform.localRotation = adaptedAreaRotation;
            OffsetRendererOrders(areaPulse, 11);

            SavePrefab(root, SunBlessingFieldPrefabPath);
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
