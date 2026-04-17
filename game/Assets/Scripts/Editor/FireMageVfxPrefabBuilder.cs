using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class FireMageVfxPrefabBuilder
    {
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string ProjectilePrefabsFolder = PrefabsRootFolder + "/Projectiles";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string ProjectilePrefabPath = ProjectilePrefabsFolder + "/FireMageBasicAttackProjectile.prefab";
        private const string EmberBurstPrefabPath = SkillPrefabsFolder + "/FireMageEmberBurst.prefab";
        private const string MeteorFieldPrefabPath = SkillPrefabsFolder + "/FireMageMeteorField.prefab";
        private const string MissileSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_magic_missile_small_orange.prefab";
        private const string SparkSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_light_spark_small_orange.prefab";
        private const string FireTrailSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Fire/Fire_trail.prefab";
        private const string FireSmallSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Fire/Fire_small.prefab";
        private const string FireBurstSmallSourcePrefabPath = "Assets/Super Pixel Effects Pack 2/Prefabs/fx2_fire_burst_small_orange.prefab";
        private const string FireBurstLargeSourcePrefabPath = "Assets/Super Pixel Effects Pack 2/Prefabs/fx2_fire_burst_large_orange.prefab";
        private const string ExplosionSmallSourcePrefabPath = "Assets/Super Pixel Effects Pack 2/Prefabs/fx2_explosion_small_orange.prefab";
        private const string TopDownRocketCircleRedSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Top_down_attack/top_down_rocket_circle_red.prefab";

        public static void BuildFireMageVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(ProjectilePrefabsFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildProjectilePrefab(softCircleSprite);
            BuildEmberBurstPrefab(softCircleSprite);
            BuildMeteorFieldPrefab(softCircleSprite);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FireMage VFX prefabs rebuilt.");
        }

        public static void BuildFireMageVfxPrefabsBatch()
        {
            BuildFireMageVfxPrefabs();
        }

        private static void BuildProjectilePrefab(Sprite softCircleSprite)
        {
            var missilePrefab = LoadRequiredAsset<GameObject>(MissileSourcePrefabPath);
            var sparkPrefab = LoadRequiredAsset<GameObject>(SparkSourcePrefabPath);
            var fireTrailPrefab = LoadRequiredAsset<GameObject>(FireTrailSourcePrefabPath);

            var root = new GameObject("FireMageBasicAttackProjectile");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "OuterGlow",
                softCircleSprite,
                new Color(1f, 0.44f, 0.08f, 0.3f),
                2,
                Vector3.zero,
                Vector3.one * 0.34f);
            CreateSprite(
                root.transform,
                "InnerGlow",
                softCircleSprite,
                new Color(1f, 0.82f, 0.36f, 0.76f),
                4,
                Vector3.zero,
                Vector3.one * 0.21f);

            var trail = InstantiateNestedPrefab(fireTrailPrefab, root.transform, "FireTrail");
            trail.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
            trail.transform.localScale = Vector3.one * 0.14f;
            OffsetRendererOrders(trail, 0);

            var spark = InstantiateNestedPrefab(sparkPrefab, root.transform, "CoreSpark");
            spark.transform.localScale = Vector3.one * 0.3f;
            spark.transform.localPosition = new Vector3(-0.01f, 0f, 0f);
            OffsetRendererOrders(spark, 6);

            var missile = InstantiateNestedPrefab(missilePrefab, root.transform, "MissileHead");
            missile.transform.localScale = Vector3.one * 0.42f;
            missile.transform.localPosition = new Vector3(0.01f, 0f, 0f);
            OffsetRendererOrders(missile, 10);

            SavePrefab(root, ProjectilePrefabPath);
        }

        private static void BuildMeteorFieldPrefab(Sprite softCircleSprite)
        {
            var topDownRocketCircleRedPrefab = LoadRequiredAsset<GameObject>(TopDownRocketCircleRedSourcePrefabPath);

            var root = new GameObject("FireMageMeteorField");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "OuterWarning",
                softCircleSprite,
                new Color(1f, 0.34f, 0.1f, 0.08f),
                0,
                Vector3.zero,
                new Vector3(1.02f, 1.02f, 1f));
            CreateSprite(
                root.transform,
                "WarningRing",
                softCircleSprite,
                new Color(0.96f, 0.16f, 0.05f, 0.12f),
                1,
                Vector3.zero,
                new Vector3(0.84f, 0.84f, 1f));
            CreateSprite(
                root.transform,
                "HeatField",
                softCircleSprite,
                new Color(0.72f, 0.08f, 0.03f, 0.2f),
                2,
                Vector3.zero,
                new Vector3(0.62f, 0.62f, 1f));
            CreateSprite(
                root.transform,
                "CoreHeat",
                softCircleSprite,
                new Color(1f, 0.56f, 0.16f, 0.06f),
                3,
                Vector3.zero,
                new Vector3(0.34f, 0.34f, 1f));

            var meteorPulse = InstantiateNestedPrefab(topDownRocketCircleRedPrefab, root.transform, "MeteorPulse");
            meteorPulse.transform.localScale = Vector3.one * 0.116f;
            meteorPulse.transform.localPosition = Vector3.zero;
            KeepOnlyDirectChildren(meteorPulse.transform, "markers", "hit_controller");
            KeepOnlyDirectChildren(FindDirectChild(meteorPulse.transform, "markers"), "ring01", "ring02");
            KeepOnlyDirectChildren(FindDirectChild(meteorPulse.transform, "hit_controller"), "circle");
            OffsetRendererOrders(meteorPulse, 8);

            SavePrefab(root, MeteorFieldPrefabPath);
        }

        private static void BuildEmberBurstPrefab(Sprite softCircleSprite)
        {
            var fireBurstSmallPrefab = LoadRequiredAsset<GameObject>(FireBurstSmallSourcePrefabPath);
            var fireBurstLargePrefab = LoadRequiredAsset<GameObject>(FireBurstLargeSourcePrefabPath);
            var explosionSmallPrefab = LoadRequiredAsset<GameObject>(ExplosionSmallSourcePrefabPath);
            var fireSmallPrefab = LoadRequiredAsset<GameObject>(FireSmallSourcePrefabPath);
            var fireTrailPrefab = LoadRequiredAsset<GameObject>(FireTrailSourcePrefabPath);

            var root = new GameObject("FireMageEmberBurst");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "FloorWarmth",
                softCircleSprite,
                new Color(1f, 0.38f, 0.12f, 0.24f),
                0,
                Vector3.zero,
                new Vector3(0.92f, 0.84f, 1f));
            CreateSprite(
                root.transform,
                "CoreFlash",
                softCircleSprite,
                new Color(1f, 0.76f, 0.28f, 0.28f),
                1,
                Vector3.zero,
                new Vector3(0.54f, 0.48f, 1f));
            CreateSprite(
                root.transform,
                "ScorchTint",
                softCircleSprite,
                new Color(0.96f, 0.22f, 0.06f, 0.14f),
                2,
                Vector3.zero,
                new Vector3(0.68f, 0.62f, 1f));

            var centerBurst = InstantiateNestedPrefab(fireBurstLargePrefab, root.transform, "CenterBurst");
            centerBurst.transform.localScale = Vector3.one * 0.22f;
            centerBurst.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            OffsetRendererOrders(centerBurst, 10);

            var centerExplosion = InstantiateNestedPrefab(explosionSmallPrefab, root.transform, "CenterExplosion");
            centerExplosion.transform.localScale = Vector3.one * 0.18f;
            centerExplosion.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            OffsetRendererOrders(centerExplosion, 13);

            var centerFire = InstantiateNestedPrefab(fireSmallPrefab, root.transform, "CenterFire");
            centerFire.transform.localScale = Vector3.one * 0.09f;
            centerFire.transform.localPosition = new Vector3(0f, 0f, 0f);
            OffsetRendererOrders(centerFire, 12);

            CreateBurstShard(root.transform, fireBurstSmallPrefab, "ShardNorth", new Vector3(0f, 0.24f, 0f), 0.13f, 11, 0f);
            CreateBurstShard(root.transform, fireBurstSmallPrefab, "ShardSouth", new Vector3(0f, -0.22f, 0f), 0.12f, 11, 180f);
            CreateBurstShard(root.transform, fireBurstSmallPrefab, "ShardWest", new Vector3(-0.28f, 0.02f, 0f), 0.12f, 10, 92f);
            CreateBurstShard(root.transform, fireBurstSmallPrefab, "ShardEast", new Vector3(0.28f, 0.02f, 0f), 0.12f, 10, -88f);

            CreateBurstShard(root.transform, fireTrailPrefab, "TrailNorthWest", new Vector3(-0.18f, 0.16f, 0f), 0.06f, 9, 34f);
            CreateBurstShard(root.transform, fireTrailPrefab, "TrailNorthEast", new Vector3(0.18f, 0.16f, 0f), 0.06f, 9, -34f);
            CreateBurstShard(root.transform, fireTrailPrefab, "TrailSouthWest", new Vector3(-0.18f, -0.14f, 0f), 0.055f, 8, 146f);
            CreateBurstShard(root.transform, fireTrailPrefab, "TrailSouthEast", new Vector3(0.18f, -0.14f, 0f), 0.055f, 8, -146f);

            SavePrefab(root, EmberBurstPrefabPath);
        }

        private static void CreateBurstShard(Transform parent, GameObject sourcePrefab, string name, Vector3 localPosition, float scale, int orderOffset, float localRotationZ)
        {
            var shard = InstantiateNestedPrefab(sourcePrefab, parent, name);
            shard.transform.localPosition = localPosition;
            shard.transform.localScale = Vector3.one * scale;
            shard.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
            OffsetRendererOrders(shard, orderOffset);
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

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void KeepOnlyDirectChildren(Transform parent, params string[] childNames)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                var keep = false;
                for (var nameIndex = 0; nameIndex < childNames.Length; nameIndex++)
                {
                    if (child.name != childNames[nameIndex])
                    {
                        continue;
                    }

                    keep = true;
                    break;
                }

                if (!keep)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
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
