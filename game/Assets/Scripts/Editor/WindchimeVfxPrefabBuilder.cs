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
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/WindchimeVfxPrefabBuilder.cs";
        private const string EchoCanopyGuardPrefabPath = SharedPrefabsFolder + "/WindchimeEchoCanopyGuard.prefab";
        private const string EchoCanopyBurstPrefabPath = SkillPrefabsFolder + "/WindchimeEchoCanopyBurst.prefab";
        private const string WindchimeActiveSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_002_windchime/Echo Canopy.asset";
        private const string ShieldWindSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Shields/Shield_wind.prefab";
        private const string HitWindSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/Hit_wind.prefab";
        private const float EchoCanopyGuardSourceScale = 0.13f;
        private const float EchoCanopyBurstSourceScale = 0.24f;
        private static readonly Quaternion TopDownRotation = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Vector3 ReactiveGuardLocalOffset = new Vector3(0f, 0.7f, 0f);
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
                    ShieldWindSourcePrefabPath,
                    HitWindSourcePrefabPath)
                > GetLatestTimestampUtc(
                    EchoCanopyGuardPrefabPath,
                    EchoCanopyBurstPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(EchoCanopyGuardPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(EchoCanopyBurstPrefabPath) != null;
        }

        private static void BuildEchoCanopyGuardPrefab()
        {
            SavePrefab(CreateEchoCanopyGuardRoot(), EchoCanopyGuardPrefabPath);
        }

        private static void BuildEchoCanopyBurstPrefab()
        {
            SavePrefab(CreateEchoCanopyBurstRoot(), EchoCanopyBurstPrefabPath);
        }

        private static GameObject CreateEchoCanopyGuardRoot()
        {
            var shieldWindPrefab = LoadRequiredAsset<GameObject>(ShieldWindSourcePrefabPath);

            var root = new GameObject("WindchimeEchoCanopyGuard");
            root.AddComponent<SortingGroup>();

            var shieldWind = InstantiateNestedPrefab(shieldWindPrefab, root.transform, "ShieldWind");
            shieldWind.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            shieldWind.transform.localScale = Vector3.one * EchoCanopyGuardSourceScale;
            shieldWind.transform.localRotation = TopDownRotation;
            ConfigureParticleSystems(shieldWind, loop: true, prewarm: true);
            OffsetRendererOrders(shieldWind, 12);

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
            OffsetRendererOrders(windBurst, 18);

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
