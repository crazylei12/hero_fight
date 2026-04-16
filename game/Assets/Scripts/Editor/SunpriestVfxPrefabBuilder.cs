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
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/SunpriestVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string HealPlusSpritePath = GeneratedArtFolder + "/vfx_heal_plus.png";

        private const string ProjectilePrefabPath = ProjectilePrefabsFolder + "/SunpriestBasicAttackProjectile.prefab";
        private const string HealImpactPrefabPath = SharedPrefabsFolder + "/SunpriestHealImpact.prefab";
        private const string SunBlessingFieldPrefabPath = SkillPrefabsFolder + "/SunpriestSunBlessingField.prefab";

        private const string SunpriestHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_001_sunpriest/Sunpriest.asset";
        private const string SunBlessingSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_001_sunpriest/Sun Blessing.asset";

        private const string LightProjectileSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/Projectiles_light.prefab";
        private const string FlashCircleSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Flash_circle.prefab";
        private const string BurstRingsSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_rings.prefab";
        private const string RegenerationHealthSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health.prefab";
        private const string RegenerationHealthLoopSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_loop.prefab";
        private const string RegenerationHealthAreaSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_area.prefab";
        private const string RegenerationHealthAreaLoopSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_area_loop.prefab";
        private const string OrbsGoldSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Orbs/Orbs_gold.prefab";
        private const string LightMissileSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_magic_missile_small_yellow.prefab";
        private const string LightSparkSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_light_spark_small_yellow.prefab";

        private static readonly Vector2[] SunBlessingAnchors =
        {
            new Vector2(0f, 0.34f),
            new Vector2(0.24f, 0.24f),
            new Vector2(0.34f, 0f),
            new Vector2(0.24f, -0.24f),
            new Vector2(0f, -0.34f),
            new Vector2(-0.24f, -0.24f),
            new Vector2(-0.34f, 0f),
            new Vector2(-0.24f, 0.24f),
        };

        private static readonly float[] SunBlessingAnchorScales =
        {
            0.08f,
            0.075f,
            0.072f,
            0.076f,
            0.08f,
            0.074f,
            0.072f,
            0.076f,
        };

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

        [MenuItem("Fight/Stage 01/Build Sunpriest VFX Prefabs")]
        public static void BuildSunpriestVfxPrefabsMenu()
        {
            BuildSunpriestVfxPrefabs();
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

            var softCircleSprite = EnsureSoftCircleSprite();
            var healPlusSprite = EnsureHealPlusSprite();
            BuildProjectilePrefab(softCircleSprite);
            BuildHealImpactPrefab(softCircleSprite, healPlusSprite);
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
                && AssetDatabase.LoadAssetAtPath<GameObject>(SunBlessingFieldPrefabPath) != null;
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SoftCircleSpritePath, HealPlusSpritePath)
                > GetLatestTimestampUtc(ProjectilePrefabPath, HealImpactPrefabPath, SunBlessingFieldPrefabPath);
        }

        private static void SyncStage01DemoAssets()
        {
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var healImpactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealImpactPrefabPath);
            var sunBlessingFieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunBlessingFieldPrefabPath);

            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(SunpriestHeroAssetPath);
            if (hero != null)
            {
                hero.visualConfig ??= new HeroVisualConfig();
                hero.visualConfig.projectilePrefab = projectilePrefab;
                hero.visualConfig.projectileAlignToMovement = projectilePrefab != null;
                hero.visualConfig.projectileEulerAngles = Vector3.zero;
                hero.visualConfig.hitVfxPrefab = healImpactPrefab;
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

        private static void BuildHealImpactPrefab(Sprite softCircleSprite, Sprite healPlusSprite)
        {
            var flashCirclePrefab = LoadRequiredAsset<GameObject>(FlashCircleSourcePrefabPath);
            var regenerationHealthPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthSourcePrefabPath);
            var regenerationHealthLoopPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthLoopSourcePrefabPath);

            var root = new GameObject("SunpriestHealImpact");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "HealRing",
                softCircleSprite,
                new Color(0.38f, 0.98f, 0.48f, 0.17f),
                2,
                Vector3.zero,
                new Vector3(1.06f, 1.06f, 1f));
            CreateSprite(
                root.transform,
                "HealCore",
                softCircleSprite,
                new Color(0.84f, 1f, 0.86f, 0.1f),
                3,
                Vector3.zero,
                new Vector3(0.22f, 0.22f, 1f));
            CreateSprite(
                root.transform,
                "HealMist",
                softCircleSprite,
                new Color(0.62f, 1f, 0.68f, 0.13f),
                4,
                new Vector3(0f, 0.01f, 0f),
                new Vector3(0.76f, 0.76f, 1f));

            var flashCircle = InstantiateNestedPrefab(flashCirclePrefab, root.transform, "FlashCircle");
            flashCircle.transform.localScale = Vector3.one * 0.18f;
            flashCircle.transform.localPosition = Vector3.zero;
            OffsetRendererOrders(flashCircle, 8);

            var healPulse = InstantiateNestedPrefab(regenerationHealthPrefab, root.transform, "HealPulse");
            healPulse.transform.localScale = Vector3.one * 0.13f;
            healPulse.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            OffsetRendererOrders(healPulse, 10);

            var healPulseBottom = InstantiateNestedPrefab(regenerationHealthPrefab, root.transform, "HealPulseBottom");
            healPulseBottom.transform.localScale = Vector3.one * 0.13f;
            healPulseBottom.transform.localPosition = new Vector3(0f, -0.34f, 0f);
            OffsetRendererOrders(healPulseBottom, 10);

            var healPulseLeft = InstantiateNestedPrefab(regenerationHealthPrefab, root.transform, "HealPulseLeft");
            healPulseLeft.transform.localScale = Vector3.one * 0.125f;
            healPulseLeft.transform.localPosition = new Vector3(-0.36f, 0f, 0f);
            OffsetRendererOrders(healPulseLeft, 10);

            var healPulseRight = InstantiateNestedPrefab(regenerationHealthPrefab, root.transform, "HealPulseRight");
            healPulseRight.transform.localScale = Vector3.one * 0.125f;
            healPulseRight.transform.localPosition = new Vector3(0.36f, 0f, 0f);
            OffsetRendererOrders(healPulseRight, 10);

            var healLoopTop = InstantiateNestedPrefab(regenerationHealthLoopPrefab, root.transform, "HealLoopTop");
            healLoopTop.transform.localScale = Vector3.one * 0.12f;
            healLoopTop.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            OffsetRendererOrders(healLoopTop, 12);

            var healLoopBottom = InstantiateNestedPrefab(regenerationHealthLoopPrefab, root.transform, "HealLoopBottom");
            healLoopBottom.transform.localScale = Vector3.one * 0.12f;
            healLoopBottom.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            OffsetRendererOrders(healLoopBottom, 12);

            var healLoopLeft = InstantiateNestedPrefab(regenerationHealthLoopPrefab, root.transform, "HealLoopLeft");
            healLoopLeft.transform.localScale = Vector3.one * 0.11f;
            healLoopLeft.transform.localPosition = new Vector3(-0.32f, 0f, 0f);
            OffsetRendererOrders(healLoopLeft, 12);

            var healLoopRight = InstantiateNestedPrefab(regenerationHealthLoopPrefab, root.transform, "HealLoopRight");
            healLoopRight.transform.localScale = Vector3.one * 0.11f;
            healLoopRight.transform.localPosition = new Vector3(0.32f, 0f, 0f);
            OffsetRendererOrders(healLoopRight, 12);

            var plusTop = CreateSprite(
                root.transform,
                "HealPlusTop",
                healPlusSprite,
                new Color(0.52f, 1f, 0.52f, 0.92f),
                14,
                new Vector3(0f, 0.42f, 0f),
                new Vector3(0.13f, 0.13f, 1f));
            plusTop.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);

            var plusBottom = CreateSprite(
                root.transform,
                "HealPlusBottom",
                healPlusSprite,
                new Color(0.74f, 1f, 0.74f, 0.86f),
                15,
                new Vector3(0f, -0.42f, 0f),
                new Vector3(0.12f, 0.12f, 1f));
            plusBottom.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);

            var plusLeft = CreateSprite(
                root.transform,
                "HealPlusLeft",
                healPlusSprite,
                new Color(0.78f, 1f, 0.78f, 0.86f),
                15,
                new Vector3(-0.42f, 0.02f, 0f),
                new Vector3(0.12f, 0.12f, 1f));
            plusLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

            var plusRight = CreateSprite(
                root.transform,
                "HealPlusRight",
                healPlusSprite,
                new Color(0.7f, 1f, 0.72f, 0.78f),
                15,
                new Vector3(0.42f, -0.01f, 0f),
                new Vector3(0.12f, 0.12f, 1f));
            plusRight.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            SavePrefab(root, HealImpactPrefabPath);
        }

        private static void BuildSunBlessingFieldPrefab(Sprite softCircleSprite)
        {
            var regenerationAreaLoopPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthAreaLoopSourcePrefabPath);
            var regenerationAreaPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthAreaSourcePrefabPath);
            var regenerationLoopPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthLoopSourcePrefabPath);
            var burstRingsPrefab = LoadRequiredAsset<GameObject>(BurstRingsSourcePrefabPath);
            var orbsGoldPrefab = LoadRequiredAsset<GameObject>(OrbsGoldSourcePrefabPath);

            var root = new GameObject("SunpriestSunBlessingField");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "OuterSanctuary",
                softCircleSprite,
                new Color(1f, 0.86f, 0.34f, 0.16f),
                0,
                Vector3.zero,
                Vector3.one);
            CreateSprite(
                root.transform,
                "InnerGlow",
                softCircleSprite,
                new Color(1f, 0.98f, 0.82f, 0.14f),
                1,
                Vector3.zero,
                new Vector3(0.78f, 0.78f, 1f));
            CreateSprite(
                root.transform,
                "BlessingCore",
                softCircleSprite,
                new Color(0.84f, 1f, 0.72f, 0.12f),
                2,
                Vector3.zero,
                new Vector3(0.52f, 0.52f, 1f));

            var centerRing = InstantiateNestedPrefab(burstRingsPrefab, root.transform, "CenterRing");
            centerRing.transform.localScale = Vector3.one * 0.14f;
            centerRing.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            OffsetRendererOrders(centerRing, 8);

            var areaLoop = InstantiateNestedPrefab(regenerationAreaLoopPrefab, root.transform, "AreaLoop");
            areaLoop.transform.localScale = Vector3.one * 0.14f;
            areaLoop.transform.localPosition = new Vector3(0f, 0f, 0f);
            OffsetRendererOrders(areaLoop, 10);

            var areaPulse = InstantiateNestedPrefab(regenerationAreaPrefab, root.transform, "AreaPulse");
            areaPulse.transform.localScale = Vector3.one * 0.12f;
            areaPulse.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            OffsetRendererOrders(areaPulse, 12);

            for (var i = 0; i < SunBlessingAnchors.Length; i++)
            {
                var anchor = SunBlessingAnchors[i];
                var scale = SunBlessingAnchorScales[i];

                var healLoop = InstantiateNestedPrefab(regenerationLoopPrefab, root.transform, $"HealLoop_{i:D2}");
                healLoop.transform.localPosition = new Vector3(anchor.x, anchor.y, 0f);
                healLoop.transform.localScale = Vector3.one * scale;
                OffsetRendererOrders(healLoop, 11 + (i % 2));

                var orb = InstantiateNestedPrefab(orbsGoldPrefab, root.transform, $"Orb_{i:D2}");
                orb.transform.localPosition = new Vector3(anchor.x * 0.82f, anchor.y * 0.82f, 0f);
                orb.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale - 0.02f);
                OffsetRendererOrders(orb, 14 + (i % 3));
            }

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

        private static Sprite EnsureHealPlusSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(HealPlusSpritePath)))
            {
                var texture = BuildHealPlusTexture(128);
                File.WriteAllBytes(GetAbsoluteProjectPath(HealPlusSpritePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(HealPlusSpritePath, ImportAssetOptions.ForceSynchronousImport);

                if (AssetImporter.GetAtPath(HealPlusSpritePath) is TextureImporter importer)
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

            return LoadRequiredAsset<Sprite>(HealPlusSpritePath);
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

        private static Texture2D BuildHealPlusTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var armHalfThickness = size * 0.12f;
            var armHalfLength = size * 0.34f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x - center);
                    var dy = Mathf.Abs(y - center);
                    var insideVertical = dx <= armHalfThickness && dy <= armHalfLength;
                    var insideHorizontal = dy <= armHalfThickness && dx <= armHalfLength;
                    var alpha = insideVertical || insideHorizontal ? 1f : 0f;
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
