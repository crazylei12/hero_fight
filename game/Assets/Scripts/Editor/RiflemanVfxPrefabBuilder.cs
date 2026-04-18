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
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/RiflemanVfxPrefabBuilder.cs";

        private const string FragGrenadePrefabPath = SkillPrefabsFolder + "/RiflemanFragGrenadeBurst.prefab";
        private const string FragGrenadeSkillAssetPath = "Assets/Data/Stage01Demo/Skills/marksman_002_rifleman/Frag Grenade.asset";

        private const string CrackDustSourcePrefabPath = "Assets/Game VFX -Explosion & Crack/Prefabs/FX_Crack_Dust.prefab";
        private const string RealisticExplosionSourcePrefabPath = "Assets/Game VFX -Explosion & Crack/Prefabs/FX_RealisticEXP_S02.prefab";

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
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);

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
            return AssetDatabase.LoadAssetAtPath<GameObject>(FragGrenadePrefabPath) != null;
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            return GetLatestTimestampUtc(
                    BuilderScriptAssetPath,
                    CrackDustSourcePrefabPath,
                    RealisticExplosionSourcePrefabPath)
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

        private static void BuildFragGrenadeBurstPrefab()
        {
            var crackDustPrefab = LoadRequiredAsset<GameObject>(CrackDustSourcePrefabPath);
            var realisticExplosionPrefab = LoadRequiredAsset<GameObject>(RealisticExplosionSourcePrefabPath);

            var root = new GameObject("RiflemanFragGrenadeBurst");
            root.AddComponent<SortingGroup>();

            // Keep the final skill reference project-owned while swapping source-pack pieces freely.
            var crackDust = InstantiateNestedPrefab(crackDustPrefab, root.transform, "CrackDust");
            crackDust.transform.localScale = Vector3.one * 0.16f;
            crackDust.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            TuneBurstParticleSystems(crackDust, 0.55f, 1.2f);
            OffsetRendererOrders(crackDust, 8);

            var centerExplosion = InstantiateNestedPrefab(realisticExplosionPrefab, root.transform, "CenterExplosion");
            centerExplosion.transform.localScale = Vector3.one * 0.12f;
            centerExplosion.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            TuneBurstParticleSystems(centerExplosion, 0.55f, 1.3f);
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
