using System;
using System.Collections.Generic;
using System.IO;
using Fight.Data;
using Fight.UI.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class PythonessVfxPrefabBuilder
    {
        private const string MainSourceSheetPath = "Assets/Art/Heroes/mage_004_pythoness/pythoness_source_sheet.png";
        private const string EffectSourceSheetPath = "Assets/Art/Heroes/mage_004_pythoness/pythoness_effect_sheet.png";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/mage_004_pythoness";
        private const string ResourcesPrefix = "HeroPreview/mage_004_pythoness";
        private const string ShrinemaidenHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_004_shrinemaiden/Shrinemaiden.asset";
        private const string ShrinemaidenActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_004_shrinemaiden/Prayer Bloom.asset";
        private const string ShrinemaidenUltimateSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_004_shrinemaiden/Twin Rite Totem.asset";
        private const string ShrinemaidenPrefabPath = "Assets/Prefabs/Heroes/support_004_shrinemaiden/Shrinemaiden.prefab";
        private const string HeroEditorControllerPath = "Assets/HeroEditor4D/Common/Animation/Controller.controller";
        private const string DamageProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ShrinemaidenDamageProjectile.prefab";
        private const string HealProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ShrinemaidenHealProjectile.prefab";
        private const string DamageImpactPrefabPath = "Assets/Prefabs/VFX/Shared/ShrinemaidenDamageImpact.prefab";
        private const string HealImpactPrefabPath = "Assets/Prefabs/VFX/Shared/ShrinemaidenHealImpact.prefab";
        private const string PrayerBloomImpactPrefabPath = "Assets/Prefabs/VFX/Skills/ShrinemaidenPrayerBloomImpact.prefab";
        private const string TotemSpawnPrefabPath = "Assets/Prefabs/VFX/Skills/ShrinemaidenTotemSpawn.prefab";
        private const string TotemLoopPrefabPath = "Assets/Prefabs/VFX/Skills/ShrinemaidenTotemLoop.prefab";
        private const string TotemDisappearPrefabPath = "Assets/Prefabs/VFX/Skills/ShrinemaidenTotemDisappear.prefab";

        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

        private static readonly Dictionary<int, RectInt> MainFrameRects = new Dictionary<int, RectInt>
        {
            [21] = new RectInt(864, 624, 16, 16),
            [22] = new RectInt(880, 672, 16, 16),
            [23] = new RectInt(880, 688, 16, 16),
            [24] = new RectInt(880, 656, 16, 16),
            [25] = new RectInt(880, 640, 16, 16),
            [26] = new RectInt(832, 688, 16, 16),
            [27] = new RectInt(832, 576, 16, 16),
            [28] = new RectInt(880, 624, 16, 16),
            [29] = new RectInt(864, 608, 16, 16),
            [30] = new RectInt(832, 592, 16, 16),
            [31] = new RectInt(848, 576, 16, 16),
            [32] = new RectInt(848, 592, 16, 16),
            [33] = new RectInt(864, 648, 8, 8),
            [34] = new RectInt(880, 608, 16, 16),
            [35] = new RectInt(864, 448, 32, 32),
            [36] = new RectInt(832, 128, 64, 64),
            [37] = new RectInt(0, 128, 64, 64),
            [38] = new RectInt(64, 128, 64, 64),
            [39] = new RectInt(864, 416, 32, 32),
            [40] = new RectInt(192, 128, 64, 64),
            [41] = new RectInt(832, 416, 32, 32),
            [42] = new RectInt(832, 320, 32, 32),
            [43] = new RectInt(576, 128, 64, 64),
            [44] = new RectInt(640, 128, 64, 64),
            [45] = new RectInt(704, 128, 64, 64),
            [46] = new RectInt(832, 192, 64, 64),
            [47] = new RectInt(832, 256, 64, 64),
            [48] = new RectInt(512, 128, 64, 64),
            [49] = new RectInt(320, 128, 64, 64),
            [50] = new RectInt(832, 352, 32, 32),
            [51] = new RectInt(128, 128, 64, 64),
            [52] = new RectInt(864, 320, 32, 32),
            [53] = new RectInt(832, 384, 32, 32),
            [54] = new RectInt(640, 640, 128, 128),
            [55] = new RectInt(512, 896, 128, 128),
            [56] = new RectInt(0, 896, 128, 128),
            [57] = new RectInt(128, 768, 128, 128),
            [58] = new RectInt(128, 896, 128, 128),
            [59] = new RectInt(128, 640, 128, 128),
            [60] = new RectInt(448, 128, 64, 64),
            [84] = new RectInt(0, 640, 128, 128),
            [85] = new RectInt(256, 640, 128, 128),
            [86] = new RectInt(256, 768, 128, 128),
            [87] = new RectInt(256, 896, 128, 128),
            [88] = new RectInt(256, 512, 128, 128),
            [89] = new RectInt(128, 512, 128, 128),
            [90] = new RectInt(0, 512, 128, 128),
            [91] = new RectInt(384, 512, 128, 128),
            [92] = new RectInt(384, 640, 128, 128),
            [93] = new RectInt(384, 768, 128, 128),
            [94] = new RectInt(384, 896, 128, 128),
            [95] = new RectInt(384, 384, 128, 128),
            [96] = new RectInt(256, 384, 128, 128),
            [97] = new RectInt(640, 384, 128, 128),
            [98] = new RectInt(640, 256, 128, 128),
            [99] = new RectInt(0, 256, 128, 128),
            [100] = new RectInt(128, 256, 128, 128),
            [101] = new RectInt(256, 256, 128, 128),
            [102] = new RectInt(384, 256, 128, 128),
            [103] = new RectInt(640, 512, 128, 128),
            [104] = new RectInt(512, 256, 128, 128),
            [105] = new RectInt(512, 768, 128, 128),
            [106] = new RectInt(512, 640, 128, 128),
            [107] = new RectInt(512, 512, 128, 128),
            [108] = new RectInt(512, 384, 128, 128),
            [109] = new RectInt(0, 384, 128, 128),
            [110] = new RectInt(128, 384, 128, 128),
            [111] = new RectInt(0, 768, 128, 128),
            [112] = new RectInt(640, 768, 128, 128),
        };

        private static readonly Dictionary<int, RectInt> EffectFrameRects = new Dictionary<int, RectInt>
        {
            [0] = new RectInt(0, 896, 128, 128),
            [1] = new RectInt(128, 896, 128, 128),
            [2] = new RectInt(256, 896, 128, 128),
            [3] = new RectInt(384, 896, 128, 128),
            [4] = new RectInt(512, 896, 128, 128),
            [5] = new RectInt(0, 768, 128, 128),
            [6] = new RectInt(128, 768, 128, 128),
            [7] = new RectInt(256, 768, 128, 128),
            [8] = new RectInt(384, 768, 128, 128),
            [9] = new RectInt(512, 768, 128, 128),
            [10] = new RectInt(0, 640, 128, 128),
            [11] = new RectInt(128, 640, 128, 128),
            [12] = new RectInt(256, 640, 128, 128),
            [13] = new RectInt(384, 640, 128, 128),
            [14] = new RectInt(512, 640, 128, 128),
            [15] = new RectInt(0, 512, 128, 128),
            [16] = new RectInt(128, 512, 128, 128),
            [17] = new RectInt(256, 512, 128, 128),
            [18] = new RectInt(384, 512, 128, 128),
            [19] = new RectInt(512, 512, 128, 128),
        };

        [MenuItem("Fight/Stage 01/Build Pythoness Shrinemaiden VFX")]
        public static void BuildPythonessVfxPrefabs()
        {
            BuildAll();
        }

        public static void BuildPythonessVfxPrefabsBatch()
        {
            BuildAll();
        }

        private static void BuildAll()
        {
            GenerateFrameFolders();
            BuildPrefabs();
            ApplyShrinemaidenDataReferences();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built Pythoness sprite-sheet VFX prefabs for Shrinemaiden.");
        }

        private static void GenerateFrameFolders()
        {
            EnsureFolder(ResourcesRoot);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "Atk1Projectile", "atk1projectile", 21, 22, 23, 24, 24);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "Atk1Effect", "atk1effect", 33, 34, 35, 36, 37, 38, 38);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "Atk2Projectile", "atk2projectile", 25, 26, 27, 28, 28);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "Atk2Effect", "atk2effect", 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 49);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "SkillProjectile", "skillprojectile", 29, 30, 31, 32, 32);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "SkillEffect", "skilleffect", 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 60);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "DoorSpawn", "doorspawn", 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 94);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "DoorLoop", "doorloop", 95, 96, 97, 98, 98);
            GenerateSequence(MainSourceSheetPath, MainFrameRects, "DoorDisappear", "doordisappear", 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 112);
            GenerateSequence(EffectSourceSheetPath, EffectFrameRects, "EffectAttack1", "effectattack1", 0, 1, 2, 3, 3);
            GenerateSequence(EffectSourceSheetPath, EffectFrameRects, "EffectAttack2", "effectattack2", 4, 5, 6, 7, 7);
            GenerateSequence(EffectSourceSheetPath, EffectFrameRects, "EffectSkill", "effectskill", 8, 9, 10, 11, 12, 13, 13);
            GenerateSequence(EffectSourceSheetPath, EffectFrameRects, "EffectUlt", "effectult", 14, 15, 16, 17, 18, 19, 19);
        }

        private static void BuildPrefabs()
        {
            EnsureFolder("Assets/Prefabs/VFX/Projectiles");
            EnsureFolder("Assets/Prefabs/VFX/Shared");
            EnsureFolder("Assets/Prefabs/VFX/Skills");

            SaveAnimatedPrefab(
                "ShrinemaidenDamageProjectile",
                DamageProjectilePrefabPath,
                new Vector3(4.25f, 4.25f, 1f),
                Layer("Core", "Atk1Projectile", 20f, 28f, Vector3.one, Color.white, 2, true),
                Layer("Glow", "EffectAttack1", 20f, 128f, new Vector3(0.62f, 0.62f, 1f), new Color(1f, 1f, 1f, 0.9f), 1, true));

            SaveAnimatedPrefab(
                "ShrinemaidenHealProjectile",
                HealProjectilePrefabPath,
                new Vector3(4.25f, 4.25f, 1f),
                Layer("Core", "Atk2Projectile", 20f, 28f, Vector3.one, Color.white, 2, true),
                Layer("Glow", "EffectAttack2", 20f, 128f, new Vector3(0.64f, 0.64f, 1f), new Color(1f, 1f, 1f, 0.9f), 1, true));

            SaveAnimatedPrefab(
                "ShrinemaidenDamageImpact",
                DamageImpactPrefabPath,
                new Vector3(1.05f, 1.05f, 1f),
                Layer("Burst", "Atk1Effect", 18f, 56f, Vector3.one, Color.white, 1, false),
                Layer("Spark", "EffectAttack1", 18f, 128f, new Vector3(0.9f, 0.9f, 1f), new Color(1f, 1f, 1f, 0.95f), 2, false));

            SaveAnimatedPrefab(
                "ShrinemaidenHealImpact",
                HealImpactPrefabPath,
                new Vector3(1.05f, 1.05f, 1f),
                Layer("Bloom", "Atk2Effect", 18f, 60f, Vector3.one, Color.white, 1, false),
                Layer("Spark", "EffectAttack2", 18f, 128f, new Vector3(0.92f, 0.92f, 1f), new Color(1f, 1f, 1f, 0.95f), 2, false));

            SaveAnimatedPrefab(
                "ShrinemaidenPrayerBloomImpact",
                PrayerBloomImpactPrefabPath,
                Vector3.one,
                Layer("Bloom", "SkillEffect", 18f, 128f, Vector3.one, Color.white, 1, false),
                Layer("PrayerGlyph", "EffectSkill", 18f, 128f, new Vector3(0.92f, 0.92f, 1f), new Color(1f, 1f, 1f, 0.9f), 2, false));

            SaveAnimatedPrefab(
                "ShrinemaidenTotemSpawn",
                TotemSpawnPrefabPath,
                new Vector3(4.75f, 4.75f, 1f),
                Layer("Door", "DoorSpawn", 12f, 88f, Vector3.one, Color.white, 1, false),
                Layer("UltFlare", "EffectUlt", 12f, 128f, new Vector3(0.9f, 0.9f, 1f), new Color(1f, 1f, 1f, 0.85f), 2, false));

            SaveAnimatedPrefab(
                "ShrinemaidenTotemLoop",
                TotemLoopPrefabPath,
                new Vector3(4.75f, 4.75f, 1f),
                Layer("Door", "DoorLoop", 10f, 88f, Vector3.one, Color.white, 1, true),
                Layer("Pulse", "EffectUlt", 10f, 128f, new Vector3(0.74f, 0.74f, 1f), new Color(1f, 1f, 1f, 0.6f), 2, true));

            SaveAnimatedPrefab(
                "ShrinemaidenTotemDisappear",
                TotemDisappearPrefabPath,
                new Vector3(4.75f, 4.75f, 1f),
                Layer("Door", "DoorDisappear", 12f, 88f, Vector3.one, Color.white, 1, false),
                Layer("UltFlare", "EffectUlt", 12f, 128f, new Vector3(0.86f, 0.86f, 1f), new Color(1f, 1f, 1f, 0.78f), 2, false));
        }

        private static void ApplyShrinemaidenDataReferences()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ShrinemaidenHeroAssetPath);
            if (hero != null)
            {
                hero.visualConfig ??= new HeroVisualConfig();
                hero.visualConfig.battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShrinemaidenPrefabPath);
                hero.visualConfig.animatorController = hero.visualConfig.battlePrefab != null
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroEditorControllerPath)
                    : null;
                hero.visualConfig.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DamageProjectilePrefabPath);
                hero.visualConfig.projectileAlignToMovement = true;
                hero.visualConfig.projectileEulerAngles = Vector3.zero;
                hero.visualConfig.basicAttackVariantVisuals = new[]
                {
                    new BasicAttackVariantVisualConfig
                    {
                        variantKey = "attack_damage",
                        projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DamageProjectilePrefabPath),
                        hitVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DamageImpactPrefabPath),
                    },
                    new BasicAttackVariantVisualConfig
                    {
                        variantKey = "attack_heal",
                        projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealProjectilePrefabPath),
                        hitVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealImpactPrefabPath),
                    },
                };
                EditorUtility.SetDirty(hero);
            }

            var activeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ShrinemaidenActiveSkillAssetPath);
            if (activeSkill != null)
            {
                activeSkill.castImpactVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrayerBloomImpactPrefabPath);
                activeSkill.castImpactVfxLocalOffset = Vector3.zero;
                activeSkill.castImpactVfxEulerAngles = Vector3.zero;
                activeSkill.castImpactVfxScaleMultiplier = Vector3.one;
                activeSkill.castImpactVfxAlignToTargetDirection = false;
                activeSkill.castImpactVfxScaleWithSkillArea = true;
                activeSkill.castImpactVfxAreaDiameterScaleMultiplier = 1f;
                EditorUtility.SetDirty(activeSkill);
            }

            var ultimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ShrinemaidenUltimateSkillAssetPath);
            if (ultimateSkill?.effects != null)
            {
                for (var i = 0; i < ultimateSkill.effects.Count; i++)
                {
                    var effect = ultimateSkill.effects[i];
                    if (effect == null || effect.effectType != SkillEffectType.CreateDeployableProxy)
                    {
                        continue;
                    }

                    effect.deployableProxySpawnVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TotemSpawnPrefabPath);
                    effect.deployableProxyLoopVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TotemLoopPrefabPath);
                    effect.deployableProxyRemovalVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TotemDisappearPrefabPath);
                    effect.deployableProxyVfxLocalOffset = new Vector3(0f, 0.28f, 0f);
                    effect.deployableProxyVfxEulerAngles = Vector3.zero;
                    effect.deployableProxyVfxScaleMultiplier = Vector3.one;
                }

                EditorUtility.SetDirty(ultimateSkill);
            }
        }

        private static void GenerateSequence(
            string sourceAssetPath,
            IReadOnlyDictionary<int, RectInt> frameRects,
            string folderName,
            string filePrefix,
            params int[] frameIndices)
        {
            if (!TryLoadTexture(sourceAssetPath, out var sourceTexture))
            {
                Debug.LogWarning($"Skipping {folderName}; source sheet is missing: {sourceAssetPath}");
                return;
            }

            var folderPath = $"{ResourcesRoot}/{folderName}";
            EnsureFolder(folderPath);
            for (var i = 0; i < frameIndices.Length; i++)
            {
                var frameIndex = frameIndices[i];
                if (!frameRects.TryGetValue(frameIndex, out var pngRect))
                {
                    throw new InvalidOperationException($"Missing frame rect for {sourceAssetPath} frame {frameIndex}.");
                }

                var frameTexture = CropPngRect(sourceTexture, pngRect);
                var assetPath = $"{folderPath}/{filePrefix}_{i:00}.png";
                File.WriteAllBytes(ToAbsolutePath(assetPath), frameTexture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(frameTexture);
                ApplyFrameTextureImporter(assetPath);
            }

            UnityEngine.Object.DestroyImmediate(sourceTexture);
        }

        private static Texture2D CropPngRect(Texture2D sourceTexture, RectInt pngRect)
        {
            var unityY = sourceTexture.height - pngRect.y - pngRect.height;
            var frameTexture = new Texture2D(pngRect.width, pngRect.height, TextureFormat.RGBA32, false);
            frameTexture.SetPixels(sourceTexture.GetPixels(pngRect.x, unityY, pngRect.width, pngRect.height));
            frameTexture.filterMode = FilterMode.Point;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.Apply();
            return frameTexture;
        }

        private static bool TryLoadTexture(string assetPath, out Texture2D texture)
        {
            texture = null;
            var absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return false;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath), markNonReadable: false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                texture = null;
                return false;
            }

            return true;
        }

        private static void ApplyFrameTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static AnimatedLayerSpec Layer(
            string name,
            string folderName,
            float framesPerSecond,
            float pixelsPerUnit,
            Vector3 localScale,
            Color color,
            int sortingOrder,
            bool loop)
        {
            return new AnimatedLayerSpec(
                name,
                $"{ResourcesPrefix}/{folderName}",
                framesPerSecond,
                pixelsPerUnit,
                localScale,
                color,
                sortingOrder,
                loop);
        }

        private static void SaveAnimatedPrefab(
            string prefabName,
            string prefabPath,
            Vector3 rootScale,
            params AnimatedLayerSpec[] layers)
        {
            var root = new GameObject(prefabName);
            root.transform.localScale = rootScale;
            root.AddComponent<SortingGroup>();

            for (var i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                var child = new GameObject(layer.Name);
                child.transform.SetParent(root.transform, false);
                child.transform.localScale = layer.LocalScale;

                var spriteRenderer = child.AddComponent<SpriteRenderer>();
                spriteRenderer.color = layer.Color;
                spriteRenderer.sortingOrder = layer.SortingOrder;

                var animator = child.AddComponent<SpriteTextureFrameAnimator>();
                animator.Configure(
                    layer.ResourcesFolder,
                    layer.FramesPerSecond,
                    layer.PixelsPerUnit,
                    CenterPivot,
                    layer.Loop);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/');
            if (assetFolder == "Assets" || AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parent))
            {
                parent = "Assets";
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath);
        }

        private sealed class AnimatedLayerSpec
        {
            public AnimatedLayerSpec(
                string name,
                string resourcesFolder,
                float framesPerSecond,
                float pixelsPerUnit,
                Vector3 localScale,
                Color color,
                int sortingOrder,
                bool loop)
            {
                Name = name;
                ResourcesFolder = resourcesFolder;
                FramesPerSecond = framesPerSecond;
                PixelsPerUnit = pixelsPerUnit;
                LocalScale = localScale;
                Color = color;
                SortingOrder = sortingOrder;
                Loop = loop;
            }

            public string Name { get; }
            public string ResourcesFolder { get; }
            public float FramesPerSecond { get; }
            public float PixelsPerUnit { get; }
            public Vector3 LocalScale { get; }
            public Color Color { get; }
            public int SortingOrder { get; }
            public bool Loop { get; }
        }
    }
}
