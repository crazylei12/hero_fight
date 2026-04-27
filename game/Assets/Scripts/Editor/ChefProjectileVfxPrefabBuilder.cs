using System.IO;
using Fight.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class ChefProjectileVfxPrefabBuilder
    {
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated/ChefProjectiles";
        private const string ProjectilePrefabsFolder = "Assets/Prefabs/VFX/Projectiles";
        private const string ChefHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_005_chef/Chef.asset";
        private const string ChefActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_005_chef/Daily Special.asset";
        private const string ChefUltimateSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_005_chef/Grand Feast.asset";

        private const string PizzaSpritePath = GeneratedArtFolder + "/chef_projectile_pizza.png";
        private const string BurgerSpritePath = GeneratedArtFolder + "/chef_projectile_burger.png";
        private const string HotdogSpritePath = GeneratedArtFolder + "/chef_projectile_hotdog.png";
        private const string FriesSpritePath = GeneratedArtFolder + "/chef_projectile_fries.png";
        private const string BigmacSpritePath = GeneratedArtFolder + "/chef_projectile_bigmac.png";

        private const string PizzaPrefabPath = ProjectilePrefabsFolder + "/ChefPizzaProjectile.prefab";
        private const string BurgerPrefabPath = ProjectilePrefabsFolder + "/ChefBurgerProjectile.prefab";
        private const string HotdogPrefabPath = ProjectilePrefabsFolder + "/ChefHotdogProjectile.prefab";
        private const string FriesPrefabPath = ProjectilePrefabsFolder + "/ChefFriesProjectile.prefab";
        private const string BigmacPrefabPath = ProjectilePrefabsFolder + "/ChefBigmacProjectile.prefab";

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

        [MenuItem("Fight/Stage 01/Build Chef Projectile VFX")]
        public static void BuildChefProjectileVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(ProjectilePrefabsFolder);

            ApplySpriteImporter(PizzaSpritePath);
            ApplySpriteImporter(BurgerSpritePath);
            ApplySpriteImporter(HotdogSpritePath);
            ApplySpriteImporter(FriesSpritePath);
            ApplySpriteImporter(BigmacSpritePath);

            AssetDatabase.ImportAsset(PizzaSpritePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(BurgerSpritePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(HotdogSpritePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(FriesSpritePath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(BigmacSpritePath, ImportAssetOptions.ForceSynchronousImport);

            BuildFoodProjectilePrefab(
                "ChefPizzaProjectile",
                PizzaPrefabPath,
                LoadRequiredAsset<Sprite>(PizzaSpritePath),
                new Vector3(0.46f, 0.46f, 1f));
            BuildFoodProjectilePrefab(
                "ChefBurgerProjectile",
                BurgerPrefabPath,
                LoadRequiredAsset<Sprite>(BurgerSpritePath),
                new Vector3(0.52f, 0.52f, 1f));
            BuildFoodProjectilePrefab(
                "ChefHotdogProjectile",
                HotdogPrefabPath,
                LoadRequiredAsset<Sprite>(HotdogSpritePath),
                new Vector3(0.52f, 0.52f, 1f));
            BuildFoodProjectilePrefab(
                "ChefFriesProjectile",
                FriesPrefabPath,
                LoadRequiredAsset<Sprite>(FriesSpritePath),
                new Vector3(0.5f, 0.5f, 1f));
            BuildFoodProjectilePrefab(
                "ChefBigmacProjectile",
                BigmacPrefabPath,
                LoadRequiredAsset<Sprite>(BigmacSpritePath),
                new Vector3(0.56f, 0.56f, 1f));

            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Chef projectile VFX prefabs rebuilt.");
        }

        public static void BuildChefProjectileVfxPrefabsBatch()
        {
            BuildChefProjectileVfxPrefabs();
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

            if (!File.Exists(GetAbsoluteProjectPath(PizzaSpritePath))
                || !File.Exists(GetAbsoluteProjectPath(BurgerSpritePath))
                || !File.Exists(GetAbsoluteProjectPath(HotdogSpritePath))
                || !File.Exists(GetAbsoluteProjectPath(FriesSpritePath))
                || !File.Exists(GetAbsoluteProjectPath(BigmacSpritePath)))
            {
                return;
            }

            BuildChefProjectileVfxPrefabs();
        }

        private static void SyncStage01DemoAssets()
        {
            var pizza = AssetDatabase.LoadAssetAtPath<GameObject>(PizzaPrefabPath);
            var burger = AssetDatabase.LoadAssetAtPath<GameObject>(BurgerPrefabPath);
            var hotdog = AssetDatabase.LoadAssetAtPath<GameObject>(HotdogPrefabPath);
            var fries = AssetDatabase.LoadAssetAtPath<GameObject>(FriesPrefabPath);
            var bigmac = AssetDatabase.LoadAssetAtPath<GameObject>(BigmacPrefabPath);

            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ChefHeroAssetPath);
            if (hero != null)
            {
                hero.visualConfig ??= new HeroVisualConfig();
                hero.visualConfig.projectilePrefab = pizza;
                hero.visualConfig.projectileAlignToMovement = pizza != null;
                hero.visualConfig.projectileEulerAngles = Vector3.zero;
                EditorUtility.SetDirty(hero);
            }

            var activeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ChefActiveSkillAssetPath);
            if (activeSkill != null)
            {
                activeSkill.playCastProjectileOnSkillCast = true;
                activeSkill.castProjectileVfxPrefab = burger;
                activeSkill.castProjectileVfxFlightDurationSeconds = 0.38f;
                activeSkill.castProjectileVfxScaleMultiplier = Vector3.one;
                activeSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                SetVariantProjectile(activeSkill, "burger", burger);
                SetVariantProjectile(activeSkill, "hotdog", hotdog);
                SetVariantProjectile(activeSkill, "fries", fries);
                EditorUtility.SetDirty(activeSkill);
            }

            var ultimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ChefUltimateSkillAssetPath);
            if (ultimateSkill != null)
            {
                ultimateSkill.playCastProjectileOnSkillCast = true;
                ultimateSkill.castProjectileVfxPrefab = bigmac;
                ultimateSkill.castProjectileVfxFlightDurationSeconds = 0.44f;
                ultimateSkill.castProjectileVfxScaleMultiplier = Vector3.one;
                ultimateSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(ultimateSkill);
            }
        }

        private static void SetVariantProjectile(SkillData skill, string variantKey, GameObject prefab)
        {
            var variant = skill != null ? skill.FindVariant(variantKey) : null;
            if (variant == null)
            {
                return;
            }

            variant.castProjectileVfxPrefab = prefab;
            variant.castProjectileVfxScaleMultiplier = Vector3.one;
        }

        private static void BuildFoodProjectilePrefab(string prefabName, string prefabPath, Sprite sprite, Vector3 bodyScale)
        {
            var root = new GameObject(prefabName);
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "FoodShadow",
                sprite,
                new Color(0f, 0f, 0f, 0.18f),
                1,
                new Vector3(0.04f, -0.05f, 0f),
                Vector3.Scale(bodyScale, new Vector3(1.06f, 0.78f, 1f)));
            CreateSprite(
                root.transform,
                "FoodSprite",
                sprite,
                Color.white,
                10,
                Vector3.zero,
                bodyScale);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
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

        private static void ApplySpriteImporter(string spritePath)
        {
            if (!File.Exists(GetAbsoluteProjectPath(spritePath)))
            {
                throw new FileNotFoundException($"Missing Chef projectile sprite at path: {spritePath}");
            }

            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(spritePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;
                importer.spritePixelsPerUnit = 96f;
                importer.SaveAndReimport();
            }
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
