using System.Collections.Generic;
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
        private const string SourceArtFolder = "Assets/Art/VFX/Source";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string ProjectilePrefabsFolder = PrefabsRootFolder + "/Projectiles";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/RiflemanVfxPrefabBuilder.cs";

        private const string RiflemanBasicAttackProjectilePrefabPath = ProjectilePrefabsFolder + "/RiflemanBasicAttackProjectile.prefab";
        private const string FragGrenadeProjectilePrefabPath = ProjectilePrefabsFolder + "/RiflemanFragGrenadeProjectile.prefab";
        private const string FragGrenadeBurstPrefabPath = SkillPrefabsFolder + "/RiflemanFragGrenadeBurst.prefab";
        private const string RiflemanBasicAttackSpritePath = GeneratedArtFolder + "/RiflemanBasicAttackBullet.png";
        private const string FragGrenadeSourceTexturePath = SourceArtFolder + "/RiflemanFragGrenadeSource.png";
        private const string FragGrenadeSpritePath = GeneratedArtFolder + "/RiflemanFragGrenadeSprite.png";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string RiflemanHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/marksman_002_rifleman/Rifleman.asset";
        private const string FragGrenadeSkillAssetPath = "Assets/Data/Stage01Demo/Skills/marksman_002_rifleman/Frag Grenade.asset";
        private const string RiflemanBasicAttackSourceFileName = "ChatGPT Image 2026年4月18日 19_52_36.png";

        private const string CrackDustSourcePrefabPath = "Assets/Game VFX -Explosion & Crack/Prefabs/FX_Crack_Dust.prefab";
        private const string RealisticExplosionSourcePrefabPath = "Assets/Game VFX -Explosion & Crack/Prefabs/FX_RealisticEXP_S02.prefab";

        private const float FragGrenadeBurstBaseVisualDurationSeconds = 0.55f;
        private const float FragGrenadeBurstDurationExtensionSeconds = 0.5f;
        private const float RiflemanBasicAttackPixelsPerUnit = 1024f;
        private const float RiflemanBasicAttackScale = 0.24f;
        private const int WhiteBackgroundThreshold = 242;
        private const byte MinimumVisibleAlpha = 8;
        private static bool autoBuildScheduled;
        private const float RiflemanBasicAttackWidthMultiplier = 3f;

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
            EnsureFolder(SourceArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(ProjectilePrefabsFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            var riflemanBulletSprite = EnsureRiflemanBasicAttackSprite();
            var grenadeSprite = EnsureFragGrenadeSprite();
            BuildRiflemanBasicAttackProjectilePrefab(riflemanBulletSprite, softCircleSprite);
            BuildFragGrenadeProjectilePrefab(grenadeSprite);
            BuildFragGrenadeBurstPrefab();
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
            return AssetDatabase.LoadAssetAtPath<Sprite>(SoftCircleSpritePath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanBasicAttackProjectilePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<Sprite>(RiflemanBasicAttackSpritePath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadeProjectilePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadeBurstPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<Sprite>(FragGrenadeSpritePath) != null;
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            var latestInputTimestamp = GetLatestTimestampUtc(
                BuilderScriptAssetPath,
                SoftCircleSpritePath,
                FragGrenadeSourceTexturePath,
                CrackDustSourcePrefabPath,
                RealisticExplosionSourcePrefabPath);
            var riflemanBulletSourceTimestamp = GetFileTimestampUtc(GetExternalRepoPath(RiflemanBasicAttackSourceFileName));
            if (riflemanBulletSourceTimestamp > latestInputTimestamp)
            {
                latestInputTimestamp = riflemanBulletSourceTimestamp;
            }

            return latestInputTimestamp
                > GetOldestTimestampUtc(
                    RiflemanBasicAttackProjectilePrefabPath,
                    RiflemanBasicAttackSpritePath,
                    SoftCircleSpritePath,
                    FragGrenadeProjectilePrefabPath,
                    FragGrenadeBurstPrefabPath,
                    FragGrenadeSpritePath);
        }

        private static void SyncStage01DemoAssets()
        {
            var riflemanProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanBasicAttackProjectilePrefabPath);
            var riflemanHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(RiflemanHeroAssetPath);
            if (riflemanHero != null)
            {
                riflemanHero.visualConfig ??= new HeroVisualConfig();
                riflemanHero.visualConfig.projectilePrefab = riflemanProjectilePrefab;
                riflemanHero.visualConfig.projectileAlignToMovement = riflemanProjectilePrefab != null;
                riflemanHero.visualConfig.projectileEulerAngles = Vector3.zero;
                riflemanHero.visualConfig.hitVfxPrefab = null;
                EditorUtility.SetDirty(riflemanHero);
            }

            var fragGrenadeProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadeProjectilePrefabPath);
            var fragGrenadeBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadeBurstPrefabPath);
            var fragGrenadeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(FragGrenadeSkillAssetPath);
            if (fragGrenadeSkill == null)
            {
                return;
            }

            fragGrenadeSkill.castProjectileVfxPrefab = fragGrenadeProjectilePrefab;
            fragGrenadeSkill.persistentAreaVfxPrefab = fragGrenadeBurstPrefab;
            fragGrenadeSkill.persistentAreaVfxScaleMultiplier = 1f;
            fragGrenadeSkill.persistentAreaVfxEulerAngles = Vector3.zero;
            fragGrenadeSkill.skillAreaPresentationType = SkillAreaPresentationType.ThrownProjectile;
            EditorUtility.SetDirty(fragGrenadeSkill);
        }

        private static void BuildRiflemanBasicAttackProjectilePrefab(Sprite riflemanBulletSprite, Sprite softCircleSprite)
        {
            var root = new GameObject("RiflemanBasicAttackProjectile");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "TrailBloom",
                softCircleSprite,
                new Color(1f, 0.82f, 0.38f, 0.14f),
                1,
                new Vector3(-0.14f, 0f, 0f),
                new Vector3(0.34f, 0.1f, 1f));
            CreateSprite(
                root.transform,
                "SpeedStreak",
                softCircleSprite,
                new Color(1f, 0.95f, 0.78f, 0.18f),
                2,
                new Vector3(-0.04f, 0f, 0f),
                new Vector3(0.22f, 0.06f, 1f));
            CreateSprite(
                root.transform,
                "BulletHeat",
                softCircleSprite,
                new Color(1f, 0.76f, 0.34f, 0.14f),
                4,
                new Vector3(0.02f, 0f, 0f),
                new Vector3(0.18f, 0.08f, 1f));

            var bullet = new GameObject("BulletCore");
            bullet.transform.SetParent(root.transform, false);
            bullet.transform.localPosition = new Vector3(0.04f, 0f, 0f);
            bullet.transform.localScale = new Vector3(
                RiflemanBasicAttackScale * RiflemanBasicAttackWidthMultiplier,
                RiflemanBasicAttackScale,
                RiflemanBasicAttackScale);

            var renderer = bullet.AddComponent<SpriteRenderer>();
            renderer.sprite = riflemanBulletSprite;
            renderer.sortingOrder = 10;

            SavePrefab(root, RiflemanBasicAttackProjectilePrefabPath);
        }

        private static void BuildFragGrenadeProjectilePrefab(Sprite grenadeSprite)
        {
            var root = new GameObject("RiflemanFragGrenadeProjectile");
            root.AddComponent<SortingGroup>();

            var grenadeBody = new GameObject("GrenadeBody");
            grenadeBody.transform.SetParent(root.transform, false);
            grenadeBody.transform.localScale = Vector3.one * 0.22f;
            grenadeBody.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);

            var renderer = grenadeBody.AddComponent<SpriteRenderer>();
            renderer.sprite = grenadeSprite;
            renderer.sortingOrder = 10;

            SavePrefab(root, FragGrenadeProjectilePrefabPath);
        }

        private static void BuildFragGrenadeBurstPrefab()
        {
            var crackDustPrefab = LoadRequiredAsset<GameObject>(CrackDustSourcePrefabPath);
            var realisticExplosionPrefab = LoadRequiredAsset<GameObject>(RealisticExplosionSourcePrefabPath);

            var root = new GameObject("RiflemanFragGrenadeBurst");
            root.AddComponent<SortingGroup>();

            // Keep the final impact project-owned while allowing source-pack swaps later.
            var crackDust = InstantiateNestedPrefab(crackDustPrefab, root.transform, "CrackDust");
            crackDust.transform.localScale = Vector3.one * 0.16f;
            crackDust.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            TuneBurstParticleSystems(
                crackDust,
                FragGrenadeBurstBaseVisualDurationSeconds,
                FragGrenadeBurstDurationExtensionSeconds,
                1.2f);
            OffsetRendererOrders(crackDust, 8);

            var centerExplosion = InstantiateNestedPrefab(realisticExplosionPrefab, root.transform, "CenterExplosion");
            centerExplosion.transform.localScale = Vector3.one * 0.12f;
            centerExplosion.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            TuneBurstParticleSystems(
                centerExplosion,
                FragGrenadeBurstBaseVisualDurationSeconds,
                FragGrenadeBurstDurationExtensionSeconds,
                1.3f);
            OffsetRendererOrders(centerExplosion, 12);

            SavePrefab(root, FragGrenadeBurstPrefabPath);
        }

        private static Sprite EnsureRiflemanBasicAttackSprite()
        {
            var sourceAbsolutePath = GetExternalRepoPath(RiflemanBasicAttackSourceFileName);
            if (!File.Exists(sourceAbsolutePath))
            {
                throw new FileNotFoundException($"Missing Rifleman basic attack source image at path: {sourceAbsolutePath}");
            }

            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            if (!sourceTexture.LoadImage(File.ReadAllBytes(sourceAbsolutePath), false))
            {
                Object.DestroyImmediate(sourceTexture);
                throw new FileNotFoundException($"Could not decode Rifleman basic attack source image: {sourceAbsolutePath}");
            }

            var sourcePixels = sourceTexture.GetPixels32();
            var bounds = FindOpaqueBounds(sourcePixels, sourceTexture.width, sourceTexture.height);
            if (!bounds.HasValue)
            {
                Object.DestroyImmediate(sourceTexture);
                throw new FileNotFoundException("Could not isolate opaque bullet pixels from the Rifleman basic attack source image.");
            }

            var paddedBounds = bounds.Value;
            paddedBounds.xMin = Mathf.Max(0, paddedBounds.xMin - 20);
            paddedBounds.yMin = Mathf.Max(0, paddedBounds.yMin - 20);
            paddedBounds.xMax = Mathf.Min(sourceTexture.width, paddedBounds.xMax + 20);
            paddedBounds.yMax = Mathf.Min(sourceTexture.height, paddedBounds.yMax + 20);

            var result = new Texture2D(paddedBounds.width, paddedBounds.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var croppedPixels = new Color32[paddedBounds.width * paddedBounds.height];
            for (var y = 0; y < paddedBounds.height; y++)
            {
                var sourceY = paddedBounds.yMin + y;
                for (var x = 0; x < paddedBounds.width; x++)
                {
                    var sourceX = paddedBounds.xMin + x;
                    croppedPixels[(y * paddedBounds.width) + x] = sourcePixels[(sourceY * sourceTexture.width) + sourceX];
                }
            }

            result.SetPixels32(croppedPixels);
            result.Apply();

            try
            {
                File.WriteAllBytes(GetAbsoluteProjectPath(RiflemanBasicAttackSpritePath), result.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(result);
                Object.DestroyImmediate(sourceTexture);
            }

            AssetDatabase.ImportAsset(RiflemanBasicAttackSpritePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureGeneratedRiflemanBulletImporter();
            return LoadRequiredAsset<Sprite>(RiflemanBasicAttackSpritePath);
        }

        private static Sprite EnsureFragGrenadeSprite()
        {
            AssetDatabase.ImportAsset(FragGrenadeSourceTexturePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureSourceTextureImporter();

            var sourceTexture = LoadRequiredAsset<Texture2D>(FragGrenadeSourceTexturePath);
            var processedTexture = BuildTransparentGrenadeTexture(sourceTexture);
            try
            {
                File.WriteAllBytes(GetAbsoluteProjectPath(FragGrenadeSpritePath), processedTexture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(processedTexture);
            }

            AssetDatabase.ImportAsset(FragGrenadeSpritePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureGeneratedSpriteImporter();
            return LoadRequiredAsset<Sprite>(FragGrenadeSpritePath);
        }

        private static void ConfigureSourceTextureImporter()
        {
            if (AssetImporter.GetAtPath(FragGrenadeSourceTexturePath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureGeneratedRiflemanBulletImporter()
        {
            if (AssetImporter.GetAtPath(RiflemanBasicAttackSpritePath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = RiflemanBasicAttackPixelsPerUnit;
            importer.SaveAndReimport();
        }

        private static void ConfigureGeneratedSpriteImporter()
        {
            if (AssetImporter.GetAtPath(FragGrenadeSpritePath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 512f;
            importer.SaveAndReimport();
        }

        private static Texture2D BuildTransparentGrenadeTexture(Texture2D sourceTexture)
        {
            var width = sourceTexture.width;
            var height = sourceTexture.height;
            var sourcePixels = sourceTexture.GetPixels32();
            var maskedBackground = FindEdgeConnectedWhiteBackground(sourcePixels, width, height);
            var transparentPixels = new Color32[sourcePixels.Length];

            for (var i = 0; i < sourcePixels.Length; i++)
            {
                var pixel = sourcePixels[i];
                if (maskedBackground[i])
                {
                    pixel.a = 0;
                }

                transparentPixels[i] = pixel;
            }

            var bounds = FindOpaqueBounds(transparentPixels, width, height);
            if (!bounds.HasValue)
            {
                throw new FileNotFoundException("Could not isolate opaque grenade pixels from RiflemanFragGrenadeSource.png.");
            }

            var paddedBounds = bounds.Value;
            paddedBounds.xMin = Mathf.Max(0, paddedBounds.xMin - 4);
            paddedBounds.yMin = Mathf.Max(0, paddedBounds.yMin - 4);
            paddedBounds.xMax = Mathf.Min(width, paddedBounds.xMax + 4);
            paddedBounds.yMax = Mathf.Min(height, paddedBounds.yMax + 4);

            var result = new Texture2D(paddedBounds.width, paddedBounds.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var croppedPixels = new Color32[paddedBounds.width * paddedBounds.height];
            for (var y = 0; y < paddedBounds.height; y++)
            {
                var sourceY = paddedBounds.yMin + y;
                for (var x = 0; x < paddedBounds.width; x++)
                {
                    var sourceX = paddedBounds.xMin + x;
                    croppedPixels[(y * paddedBounds.width) + x] = transparentPixels[(sourceY * width) + sourceX];
                }
            }

            result.SetPixels32(croppedPixels);
            result.Apply();
            return result;
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                return LoadRequiredAsset<Sprite>(SoftCircleSpritePath);
            }

            var texture = BuildSoftCircleTexture(128);
            try
            {
                File.WriteAllBytes(GetAbsoluteProjectPath(SoftCircleSpritePath), texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(SoftCircleSpritePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(SoftCircleSpritePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return LoadRequiredAsset<Sprite>(SoftCircleSpritePath);
        }

        private static bool[] FindEdgeConnectedWhiteBackground(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    return;
                }

                var index = (y * width) + x;
                if (visited[index] || !IsWhiteBackgroundPixel(pixels[index]))
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % width;
                var y = index / width;
                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            return visited;
        }

        private static RectInt? FindOpaqueBounds(Color32[] pixels, int width, int height)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[(y * width) + x];
                    if (pixel.a <= MinimumVisibleAlpha)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            return maxX < minX || maxY < minY
                ? null
                : new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static bool IsWhiteBackgroundPixel(Color32 pixel)
        {
            return pixel.a > MinimumVisibleAlpha
                && pixel.r >= WhiteBackgroundThreshold
                && pixel.g >= WhiteBackgroundThreshold
                && pixel.b >= WhiteBackgroundThreshold;
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

        private static void TuneBurstParticleSystems(
            GameObject root,
            float baseDurationSeconds,
            float extensionSeconds,
            float minSimulationSpeed)
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
                main.duration = Mathf.Min(main.duration, baseDurationSeconds) + extensionSeconds;
                main.simulationSpeed = Mathf.Max(main.simulationSpeed, minSimulationSpeed);
            }
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

        private static string GetExternalRepoPath(string fileName)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var repoRoot = Directory.GetParent(projectRoot ?? string.Empty)?.FullName;
            return Path.Combine(repoRoot ?? string.Empty, fileName);
        }

        private static System.DateTime GetFileTimestampUtc(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                return System.DateTime.MinValue;
            }

            return File.GetLastWriteTimeUtc(absolutePath);
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

        private static System.DateTime GetOldestTimestampUtc(params string[] assetPaths)
        {
            var oldest = System.DateTime.MaxValue;
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
                    return System.DateTime.MinValue;
                }

                var timestamp = File.GetLastWriteTimeUtc(absolutePath);
                if (timestamp < oldest)
                {
                    oldest = timestamp;
                }
            }

            return oldest == System.DateTime.MaxValue ? System.DateTime.MinValue : oldest;
        }
    }
}
