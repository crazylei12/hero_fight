using System;
using System.Collections.Generic;
using System.IO;
using Fight.Battle;
using Fight.Data;
using Fight.UI.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fight.Editor
{
    public static class Stage01SampleContentBuilder
    {
        private const string OpenMainMenuMenuPath = "Fight/Play/Open Main Menu";
        private const string OpenBattleMenuPath = "Fight/Dev/Open Battle Scene";
        private const string OverwriteDemoContentMenuPath = "Fight/Dev/Regenerate Demo Content From Defaults (Overwrite Existing Tuning)";
        private const string DemoRoot = "Assets/Data/Stage01Demo";
        private const string SkillsRootFolder = DemoRoot + "/Skills";
        private const string HeroesRootFolder = DemoRoot + "/Heroes";
        private const string BattlesFolder = DemoRoot + "/Battles";
        private const string ResourcesFolder = "Assets/Resources";
        private const string ResourcesDemoFolder = ResourcesFolder + "/Stage01Demo";
        private const string ScenesFolder = "Assets/Scenes";
        private const string BattleScenePath = ScenesFolder + "/Battle.unity";
        private const string BasicAttackOnlyBattleScenePath = ScenesFolder + "/BattleBasicAttackOnly.unity";
        private const string HeroSelectScenePath = ScenesFolder + "/HeroSelect.unity";
        private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        private const string ResultScenePath = ScenesFolder + "/Result.unity";
        private const string DefaultBattleInputAssetPath = ResourcesDemoFolder + "/Stage01DemoBattleInput.asset";
        private const string DefaultHeroCatalogAssetPath = ResourcesDemoFolder + "/Stage01HeroCatalog.asset";
        private const string BladesmanHeroAssetPath = HeroesRootFolder + "/warrior_002_bladesman/Bladesman.asset";
        private const string BladesmanActiveSkillAssetPath = SkillsRootFolder + "/warrior_002_bladesman/Rending Slash.asset";
        private const string BladesmanUltimateSkillAssetPath = SkillsRootFolder + "/warrior_002_bladesman/Flying Swallow Sever.asset";
        private const string WindchimeHeroAssetPath = HeroesRootFolder + "/support_002_windchime/Windchime.asset";
        private const string WindchimeActiveSkillAssetPath = SkillsRootFolder + "/support_002_windchime/Echo Canopy.asset";
        private const string WindchimeUltimateSkillAssetPath = SkillsRootFolder + "/support_002_windchime/Stillwind Domain.asset";
        private const string MonkHeroAssetPath = HeroesRootFolder + "/support_003_monk/Monk.asset";
        private const string MonkActiveSkillAssetPath = SkillsRootFolder + "/support_003_monk/Renewing Pulse.asset";
        private const string MonkUltimateSkillAssetPath = SkillsRootFolder + "/support_003_monk/Guardian Mantra.asset";
        private const string SandemperorHeroAssetPath = HeroesRootFolder + "/mage_003_sandemperor/Sandemperor.asset";
        private const string SandemperorActiveSkillAssetPath = SkillsRootFolder + "/mage_003_sandemperor/Raise Sandguard.asset";
        private const string SandemperorUltimateSkillAssetPath = SkillsRootFolder + "/mage_003_sandemperor/Imperial Encirclement.asset";
        private const string MonkActiveImpactVfxPrefabPath = "Assets/Prefabs/VFX/Skills/MonkRenewingPulseBurst.prefab";
        private const string AssassinPrefabPath = "Assets/Prefabs/Heroes/assassin_001_shadowstep/Shadowstep.prefab";
        private const string TidefinPrefabPath = "Assets/Prefabs/Heroes/assassin_002_tidefin/Tidefin.prefab";
        private const string MarksmanPrefabPath = "Assets/Prefabs/Heroes/marksman_001_longshot/Longshot.prefab";
        private const string RiflemanPrefabPath = "Assets/Prefabs/Heroes/marksman_002_rifleman/Rifleman.prefab";
        private const string SupportPrefabPath = "Assets/Prefabs/Heroes/support_001_sunpriest/Sunpriest.prefab";
        private const string WindchimePrefabPath = "Assets/Prefabs/Heroes/support_002_windchime/Windchime.prefab";
        private const string MonkPrefabPath = "Assets/Prefabs/Heroes/support_003_monk/Monk.prefab";
        private const string WarriorPrefabPath = "Assets/Prefabs/Heroes/warrior_001_skybreaker/Skybreaker.prefab";
        private const string BladesmanPrefabPath = "Assets/Prefabs/Heroes/warrior_002_bladesman/Bladesman.prefab";
        private const string FireMagePrefabPath = "Assets/Prefabs/Heroes/mage_001_firemage/FIREMAGE.prefab";
        private const string FrostMagePrefabPath = "Assets/Prefabs/Heroes/mage_002_frostmage/Frostmage.prefab";
        private const string TankPrefabPath = "Assets/Prefabs/Heroes/tank_001_ironwall/Ironwall.prefab";
        private const string ShieldwardenPrefabPath = "Assets/Prefabs/Heroes/tank_002_shieldwarden/Shieldwarden.prefab";
        private const string HeroEditorControllerPath = "Assets/HeroEditor4D/Common/Animation/Controller.controller";
        private const string FireMageProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/FireMageBasicAttackProjectile.prefab";
        private const string FrostMageProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/FrostMageBasicAttackProjectile.prefab";
        private const string LongshotProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/LongshotBasicAttackProjectile.prefab";
        private const string RiflemanProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/RiflemanBasicAttackProjectile.prefab";
        private const string MageActiveAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/FireMageEmberBurst.prefab";
        private const string FrostMageActiveAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/FrostMageFrostBurst.prefab";
        private const string FrostMageUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/FrostMageBlizzardField.prefab";
        private const string MageUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/FireMageMeteorField.prefab";
        private const string BladesmanActiveImpactVfxPrefabPath = "Assets/Prefabs/VFX/Skills/BladesmanRendingSlash.prefab";
        private const string BladesmanUltimateDashVfxPrefabPath = "Assets/Prefabs/VFX/Skills/BladesmanFlyingSwallowWave.prefab";
        private const string RiflemanActiveTargetIndicatorVfxPrefabPath = "Assets/Prefabs/VFX/Skills/RiflemanBurstFireTargetReticle.prefab";
        private const string RiflemanUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/RiflemanFragGrenadeBurst.prefab";
        private const string RiflemanUltimateProjectileVfxPrefabPath = "Assets/Prefabs/VFX/Projectiles/RiflemanFragGrenadeProjectile.prefab";
        private const string SunpriestProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/SunpriestBasicAttackProjectile.prefab";
        private const string SunpriestUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/SunpriestSunBlessingField.prefab";
        private const string WindchimeUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/WindchimeStillwindDomainField.prefab";
        private const string PoisonStatusThemeKey = "poison";
        private const string VenomshooterPoisonStackGroupKey = "venomshooter_poison_pool";
        private static bool autoEnsureScheduled;

        private static float ScaleRangedHeroDistance(float value)
        {
            return value * Stage01ArenaSpec.ArenaScaleMultiplier;
        }

        [MenuItem(OpenMainMenuMenuPath)]
        public static void OpenMainMenuScene()
        {
            EnsureDemoContent();
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        [MenuItem(OpenBattleMenuPath)]
        public static void OpenBattleScene()
        {
            EnsureDemoContent();
            EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
        }

        [MenuItem(OverwriteDemoContentMenuPath)]
        public static void RegenerateDemoContentFromDefaults()
        {
            if (!EditorUtility.DisplayDialog(
                    "Overwrite Existing Tuning",
                    "This action will overwrite existing hero and skill tuning back to the demo defaults. Continue?",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            GenerateDemoContentInternal(overwriteExistingContent: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Stage 01",
                    "Demo content regenerated from defaults and existing tuning was overwritten.",
                    "OK");
            }
        }

        [InitializeOnLoadMethod]
        private static void ScheduleAutoEnsureDemoContentIfNeeded()
        {
            if (Application.isBatchMode || autoEnsureScheduled)
            {
                return;
            }

            autoEnsureScheduled = true;
            EditorApplication.delayCall += TryAutoEnsureDemoContentIfNeeded;
        }

        public static void GenerateDemoContent()
        {
            GenerateDemoContentInternal(overwriteExistingContent: false);
        }

        private static void GenerateDemoContentInternal(bool overwriteExistingContent)
        {
            EnsureFolders();

            var warriorActive = CreateSkybreakerActiveSkill(overwriteExistingContent);
            var warriorUltimateSkill = CreateSkybreakerUltimateSkill(overwriteExistingContent, out var warriorUltimateExisted);

            var warrior = CreateHero(
                "warrior_001_skybreaker",
                "Skybreaker",
                HeroClass.Warrior,
                420f, 38f, 24f, 1.1f, 4.2f, 0.15f, 1.6f, 1.7f,
                warriorActive,
                ConfigureSkybreakerUltimate(warriorUltimateSkill, overwriteExistingContent, warriorUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Dive, HeroTag.Control);

            var bladesmanActive = CreateBladesmanActiveSkill(overwriteExistingContent);
            var bladesmanUltimate = CreateBladesmanUltimateSkill(overwriteExistingContent, out _);
            var bladesman = CreateHero(
                "warrior_002_bladesman",
                "Bladesman",
                HeroClass.Warrior,
                410f, 46f, 22f, 1f / 1.12f, 4.5f, 0.12f, 1.65f, 1.9f,
                bladesmanActive,
                bladesmanUltimate,
                overwriteExistingContent,
                out var bladesmanHeroExisted,
                HeroTag.Melee, HeroTag.Dive, HeroTag.Burst);
            ConfigureBladesmanBasicAttack(bladesman, overwriteExistingContent, bladesmanHeroExisted);
            EnsureHeroSkillReferences(bladesman, bladesmanActive, bladesmanUltimate);
            EnsureHeroBattlePrefabReference(bladesman, LoadBattlePrefab("warrior_002_bladesman", HeroClass.Warrior));
            EnsureSkillCastImpactVfxPresentation(
                bladesmanActive,
                AssetDatabase.LoadAssetAtPath<GameObject>(BladesmanActiveImpactVfxPrefabPath),
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0f, 0f, -90f),
                new Vector3(0.18f, 0.18f, 1f),
                true);

            var berserkerActive = CreateBerserkerActiveSkill(overwriteExistingContent);
            var berserkerUltimate = CreateBerserkerUltimateSkill(overwriteExistingContent, out var berserkerUltimateExisted);
            var berserker = CreateHero(
                "warrior_003_berserker",
                "Berserker",
                HeroClass.Warrior,
                440f, 42f, 18f, 1f, 4.45f, 0.08f, 1.6f, 1.85f,
                berserkerActive,
                ConfigureBerserkerUltimate(berserkerUltimate, overwriteExistingContent, berserkerUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.SustainedDamage, HeroTag.Buff);
            EnsureHeroSkillReferences(berserker, berserkerActive, berserkerUltimate);
            EnsureHeroBattlePrefabReference(berserker, LoadBattlePrefab("warrior_003_berserker", HeroClass.Warrior));

            var mageUltimateSkill = CreateSkill("skill_mage_ultimate_meteor", "Meteor Fall", SkillSlotType.Ultimate, SkillType.AreaDamage, SkillTargetType.Self, 0f, ScaleRangedHeroDistance(6f), 3.3f, 0f, 3, overwriteExistingContent, out var mageUltimateExisted);

            var mage = CreateHero(
                "mage_001_firemage",
                "FIREMAGE",
                HeroClass.Mage,
                300f, 48f, 10f, 0.8f, 3.8f, 0.08f, 1.5f, ScaleRangedHeroDistance(5.8f),
                CreateMageActiveBurstSkill(overwriteExistingContent),
                ConfigureMageUltimate(mageUltimateSkill, overwriteExistingContent, mageUltimateExisted),
                overwriteExistingContent,
                HeroTag.Ranged, HeroTag.Burst, HeroTag.AreaDamage);

            var frostmageUltimateSkill = CreateFrostmageUltimateSkill(overwriteExistingContent, out var frostmageUltimateExisted);

            var frostmage = CreateHero(
                "mage_002_frostmage",
                "Frostmage",
                HeroClass.Mage,
                310f, 42f, 10f, 1f / 1.30f, 3.7f, 0.08f, 1.5f, ScaleRangedHeroDistance(5.8f),
                CreateFrostmageActiveSkill(overwriteExistingContent),
                ConfigureFrostmageUltimate(frostmageUltimateSkill, overwriteExistingContent, frostmageUltimateExisted),
                overwriteExistingContent,
                HeroTag.Ranged, HeroTag.Control, HeroTag.AreaDamage);

            var sandemperorActive = CreateSandemperorActiveSkill(overwriteExistingContent);
            var sandemperorUltimateSkill = CreateSandemperorUltimateSkill(overwriteExistingContent, out var sandemperorUltimateExisted);

            var sandemperor = CreateHero(
                "mage_003_sandemperor",
                "Sandemperor",
                HeroClass.Mage,
                300f, 38f, 10f, 1f / 1.10f, 3.7f, 0.05f, 1.5f, ScaleRangedHeroDistance(5.8666667f),
                sandemperorActive,
                ConfigureSandemperorUltimate(sandemperorUltimateSkill, overwriteExistingContent, sandemperorUltimateExisted),
                overwriteExistingContent,
                out var sandemperorHeroExisted,
                HeroTag.Ranged, HeroTag.SustainedDamage, HeroTag.AreaDamage);
            ConfigureSandemperorBasicAttack(sandemperor, overwriteExistingContent, sandemperorHeroExisted);
            EnsureHeroSkillReferences(sandemperor, sandemperorActive, sandemperorUltimateSkill);
            EnsureHeroBattlePrefabReference(sandemperor, LoadBattlePrefab("mage_003_sandemperor", HeroClass.Mage));

            CreateArchivedMageFireboltSkill(overwriteExistingContent);

            var assassinActive = CreateShadowstepActiveSkill(overwriteExistingContent);
            var assassinUltimateSkill = CreateShadowstepUltimateSkill(overwriteExistingContent, out var assassinUltimateExisted);

            var assassin = CreateHero(
                "assassin_001_shadowstep",
                "Shadowstep",
                HeroClass.Assassin,
                290f, 52f, 8f, 1.2f, 5.4f, 0.2f, 1.8f, 1.3f,
                assassinActive,
                ConfigureShadowstepUltimate(assassinUltimateSkill, overwriteExistingContent, assassinUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Dive, HeroTag.Burst);
            assassin.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            EditorUtility.SetDirty(assassin);

            var tidefinActive = CreateTidefinActiveSkill(overwriteExistingContent);
            var tidefinUltimateSkill = CreateTidefinUltimateSkill(overwriteExistingContent, out _);

            var tidefin = CreateHero(
                "assassin_002_tidefin",
                "Tidefin",
                HeroClass.Assassin,
                320f, 44f, 12f, 1f / 0.90f, 5f, 0.12f, 1.6f, 1.3f,
                tidefinActive,
                tidefinUltimateSkill,
                overwriteExistingContent,
                out var tidefinHeroExisted,
                HeroTag.Melee, HeroTag.Dive, HeroTag.Control);
            ConfigureTidefinBasicAttack(tidefin, overwriteExistingContent, tidefinHeroExisted);

            var tankActive = CreateIronwallActiveSkill(overwriteExistingContent);
            var tankUltimateSkill = CreateBuffSkill("skill_tank_ultimate_ironoath", "Iron Oath", SkillSlotType.Ultimate, SkillTargetType.AllAllies, 6f, 6f, 1f, 0f, StatusEffectType.DefenseModifier, 8f, 2.5f, overwriteExistingContent, out var tankUltimateExisted);

            var tank = CreateHero(
                "tank_001_ironwall",
                "Ironwall",
                HeroClass.Tank,
                560f, 28f, 40f, 0.9f, 3.6f, 0.05f, 1.5f, 1.8f,
                tankActive,
                ConfigureTankUltimate(tankUltimateSkill, overwriteExistingContent, tankUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Control, HeroTag.Buff);

            var shieldwardenActive = CreateShieldwardenActiveSkill(overwriteExistingContent);
            var shieldwardenUltimateSkill = CreateShieldwardenUltimateSkill(overwriteExistingContent, out var shieldwardenUltimateExisted);

            var shieldwarden = CreateHero(
                "tank_002_shieldwarden",
                "Shieldwarden",
                HeroClass.Tank,
                560f, 24f, 34f, 1f / 1.05f, 3.6f, 0.05f, 1.5f, 1.8f,
                shieldwardenActive,
                ConfigureShieldwardenUltimate(shieldwardenUltimateSkill, overwriteExistingContent, shieldwardenUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Control, HeroTag.Buff);

            var supportActive = CreateSupportActiveSkill(overwriteExistingContent);
            var supportUltimateSkill = CreateSupportUltimateSkill(overwriteExistingContent, out var supportUltimateExisted);

            var support = CreateHero(
                "support_001_sunpriest",
                "Sunpriest",
                HeroClass.Support,
                320f, 22f, 12f, 0.9f, 4.0f, 0.05f, 1.5f, ScaleRangedHeroDistance(5.2f),
                supportActive,
                ConfigureSupportUltimate(supportUltimateSkill, overwriteExistingContent, supportUltimateExisted),
                overwriteExistingContent,
                out var supportHeroExisted,
                HeroTag.Ranged, HeroTag.Heal, HeroTag.Buff);
            ConfigureSupportBasicAttack(support, overwriteExistingContent, supportHeroExisted);

            var windchimeActive = CreateWindchimeActiveSkill(overwriteExistingContent);
            var windchimeUltimateSkill = CreateWindchimeUltimateSkill(overwriteExistingContent, out var windchimeUltimateExisted);

            var windchime = CreateHero(
                "support_002_windchime",
                "Windchime",
                HeroClass.Support,
                305f, 24f, 11f, 1f / 1.05f, 4.1f, 0.06f, 1.5f, ScaleRangedHeroDistance(5.6f),
                windchimeActive,
                ConfigureWindchimeUltimate(windchimeUltimateSkill, overwriteExistingContent, windchimeUltimateExisted),
                overwriteExistingContent,
                out var windchimeHeroExisted,
                HeroTag.Ranged, HeroTag.Control, HeroTag.Buff);
            ConfigureWindchimeBasicAttack(windchime, overwriteExistingContent, windchimeHeroExisted);
            EnsureHeroSkillReferences(windchime, windchimeActive, windchimeUltimateSkill);
            EnsureHeroBattlePrefabReference(windchime, LoadBattlePrefab("support_002_windchime", HeroClass.Support));

            var monkActive = CreateMonkActiveSkill(overwriteExistingContent);
            var monkUltimateSkill = CreateMonkUltimateSkill(overwriteExistingContent, out var monkUltimateExisted);

            var monk = CreateHero(
                "support_003_monk",
                "Monk",
                HeroClass.Support,
                430f, 20f, 24f, 1f / 1.05f, 4f, 0.05f, 1.5f, 1.9f,
                monkActive,
                ConfigureMonkUltimate(monkUltimateSkill, overwriteExistingContent, monkUltimateExisted),
                overwriteExistingContent,
                out var monkHeroExisted,
                HeroTag.Melee, HeroTag.Heal, HeroTag.Buff);
            ConfigureMonkBasicAttack(monk, overwriteExistingContent, monkHeroExisted);
            EnsureHeroSkillReferences(monk, monkActive, monkUltimateSkill);
            EnsureHeroBattlePrefabReference(monk, LoadBattlePrefab("support_003_monk", HeroClass.Support));
            EnsureSkillCastImpactVfxPresentation(
                monkActive,
                AssetDatabase.LoadAssetAtPath<GameObject>(MonkActiveImpactVfxPrefabPath),
                Vector3.zero,
                Vector3.zero,
                Vector3.one,
                false,
                true,
                1f);

            var marksmanActive = CreateLongshotActiveSkill(overwriteExistingContent);
            var marksmanUltimateSkill = CreateLongshotUltimateSkill(overwriteExistingContent, out var marksmanUltimateExisted);

            var marksman = CreateHero(
                "marksman_001_longshot",
                "Longshot",
                HeroClass.Marksman,
                310f, 34f, 12f, 1f / 0.74f, 4.1f, 0.22f, 1.9f, ScaleRangedHeroDistance(6f),
                marksmanActive,
                ConfigureLongshotUltimate(marksmanUltimateSkill, overwriteExistingContent, marksmanUltimateExisted),
                overwriteExistingContent,
                out var marksmanHeroExisted,
                HeroTag.Ranged, HeroTag.SustainedDamage);
            ConfigureLongshotBasicAttack(marksman, overwriteExistingContent, marksmanHeroExisted);

            var riflemanActive = CreateRiflemanActiveSkill(overwriteExistingContent);
            var riflemanUltimateSkill = CreateRiflemanUltimateSkill(overwriteExistingContent, out var riflemanUltimateExisted);

            var rifleman = CreateHero(
                "marksman_002_rifleman",
                "Rifleman",
                HeroClass.Marksman,
                280f, 40f, 6f, 1f / 1.43f, 3.2f, 0.18f, 1.75f, ScaleRangedHeroDistance(6.2f),
                riflemanActive,
                ConfigureRiflemanUltimate(riflemanUltimateSkill, overwriteExistingContent, riflemanUltimateExisted),
                overwriteExistingContent,
                out var riflemanHeroExisted,
                HeroTag.Ranged, HeroTag.SustainedDamage, HeroTag.AreaDamage);
            ConfigureRiflemanBasicAttack(rifleman, overwriteExistingContent, riflemanHeroExisted);

            var venomshooterActive = CreateVenomshooterActiveSkill(overwriteExistingContent);
            var venomshooterUltimateSkill = CreateVenomshooterUltimateSkill(overwriteExistingContent, out var venomshooterUltimateExisted);

            var venomshooter = CreateHero(
                "marksman_003_venomshooter",
                "Venomshooter",
                HeroClass.Marksman,
                295f, 32f, 8f, 1f / 0.9f, 3.8f, 0.14f, 1.75f, ScaleRangedHeroDistance(6f),
                venomshooterActive,
                ConfigureVenomshooterUltimate(venomshooterUltimateSkill, overwriteExistingContent, venomshooterUltimateExisted),
                overwriteExistingContent,
                out var venomshooterHeroExisted,
                HeroTag.Ranged, HeroTag.SustainedDamage, HeroTag.AreaDamage);
            ConfigureVenomshooterBasicAttack(venomshooter, overwriteExistingContent, venomshooterHeroExisted);
            EnsureHeroSkillReferences(venomshooter, venomshooterActive, venomshooterUltimateSkill);
            EnsureHeroBattlePrefabReference(venomshooter, LoadBattlePrefab("marksman_003_venomshooter", HeroClass.Marksman));

            CreateHeroCatalog(warrior, bladesman, berserker, mage, frostmage, sandemperor, assassin, tidefin, tank, shieldwarden, support, windchime, monk, marksman, rifleman, venomshooter);

            var battleInput = CreateBattleInput(
                "Stage01DemoBattleInput",
                includeAssassinOnRedTeam: true,
                enableSkills: true,
                overwriteExistingContent,
                warrior,
                mage,
                assassin,
                tank,
                support,
                marksman);
            var basicAttackOnlyInput = CreateBattleInput(
                "Stage01BasicAttackOnlyBattleInput",
                includeAssassinOnRedTeam: true,
                enableSkills: false,
                overwriteExistingContent,
                warrior,
                mage,
                assassin,
                tank,
                support,
                marksman);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CreateFlowScenes(overwriteExistingContent);
            CreateBattleScene(battleInput, overwriteExistingContent);
            CreateBasicAttackOnlyBattleScene(basicAttackOnlyInput, overwriteExistingContent);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!Application.isBatchMode)
            {
                var message = overwriteExistingContent
                    ? "Demo content regenerated and existing tuning was overwritten.\nUse Fight/Play/Open Main Menu for the formal flow, or Fight/Dev/Open Battle Scene for direct battle scene access."
                    : "Demo content ensured without overwriting existing tuning.\nUse Fight/Play/Open Main Menu for the formal flow, or Fight/Dev/Open Battle Scene for direct battle scene access.";
                EditorUtility.DisplayDialog("Stage 01", message, "OK");
            }
        }

        public static void GenerateDemoContentForBuild()
        {
            GenerateDemoContentInternal(overwriteExistingContent: false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnsureDemoContentValidationPassed(logFailures: true);
        }

        public static void GenerateDemoContentBatch()
        {
            GenerateDemoContentForBuild();
            EditorApplication.Exit(0);
        }

        private static void TryAutoEnsureDemoContentIfNeeded()
        {
            autoEnsureScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoEnsureDemoContentIfNeeded();
                return;
            }

            if (NeedsDemoContentBootstrap())
            {
                GenerateDemoContent();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Stage-01 demo content synchronized.");
            }

            ValidateDemoContentConsistency(logFailures: true);
        }

        private static void EnsureDemoContent()
        {
            if (!NeedsDemoContentBootstrap())
            {
                ValidateDemoContentConsistency(logFailures: true);
                return;
            }

            GenerateDemoContent();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateDemoContentConsistency(logFailures: true);
        }

        private static bool NeedsDemoContentBootstrap()
        {
            var hasMainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
            var hasHeroSelectScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HeroSelectScenePath) != null;
            var hasBattleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath) != null;
            var hasResultScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ResultScenePath) != null;
            var hasBasicAttackOnlyScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BasicAttackOnlyBattleScenePath) != null;
            var hasDefaultBattleInput = AssetDatabase.LoadAssetAtPath<BattleInputConfig>(DefaultBattleInputAssetPath) != null;
            var heroCatalog = AssetDatabase.LoadAssetAtPath<HeroCatalogData>(DefaultHeroCatalogAssetPath);
            var bladesmanHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(BladesmanHeroAssetPath);
            var bladesmanActiveSkill = AssetDatabase.LoadAssetAtPath<SkillData>(BladesmanActiveSkillAssetPath);
            var bladesmanUltimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(BladesmanUltimateSkillAssetPath);
            var bladesmanBattlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BladesmanPrefabPath);
            var bladesmanActiveImpactVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BladesmanActiveImpactVfxPrefabPath);
            var windchimeHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(WindchimeHeroAssetPath);
            var windchimeActiveSkill = AssetDatabase.LoadAssetAtPath<SkillData>(WindchimeActiveSkillAssetPath);
            var windchimeUltimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(WindchimeUltimateSkillAssetPath);
            var windchimeBattlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindchimePrefabPath);
            var monkHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(MonkHeroAssetPath);
            var monkActiveSkill = AssetDatabase.LoadAssetAtPath<SkillData>(MonkActiveSkillAssetPath);
            var monkUltimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(MonkUltimateSkillAssetPath);
            var monkBattlePrefab = LoadBattlePrefab("support_003_monk", HeroClass.Support);
            var sandemperorHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(SandemperorHeroAssetPath);
            var sandemperorActiveSkill = AssetDatabase.LoadAssetAtPath<SkillData>(SandemperorActiveSkillAssetPath);
            var sandemperorUltimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(SandemperorUltimateSkillAssetPath);
            var sandemperorBattlePrefab = LoadBattlePrefab("mage_003_sandemperor", HeroClass.Mage);
            var catalogContainsBladesman = CatalogContainsHero(heroCatalog, "warrior_002_bladesman");
            var catalogContainsWindchime = CatalogContainsHero(heroCatalog, "support_002_windchime");
            var catalogContainsMonk = CatalogContainsHero(heroCatalog, "support_003_monk");
            var catalogContainsSandemperor = CatalogContainsHero(heroCatalog, "mage_003_sandemperor");
            var bladesmanReferencesValid = HeroHasExpectedSkillReferences(bladesmanHero, bladesmanActiveSkill, bladesmanUltimateSkill);
            var bladesmanBattlePrefabValid = HeroHasExpectedBattlePrefab(bladesmanHero, bladesmanBattlePrefab);
            var bladesmanActiveImpactVfxValid = SkillHasExpectedCastImpactVfxPresentation(
                bladesmanActiveSkill,
                bladesmanActiveImpactVfxPrefab,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0f, 0f, -90f),
                new Vector3(0.18f, 0.18f, 1f),
                true);
            var windchimeReferencesValid = HeroHasExpectedSkillReferences(windchimeHero, windchimeActiveSkill, windchimeUltimateSkill);
            var windchimeBattlePrefabValid = HeroHasExpectedBattlePrefab(windchimeHero, windchimeBattlePrefab);
            var monkReferencesValid = HeroHasExpectedSkillReferences(monkHero, monkActiveSkill, monkUltimateSkill);
            var monkBattlePrefabValid = monkBattlePrefab == null || HeroHasExpectedBattlePrefab(monkHero, monkBattlePrefab);
            var sandemperorReferencesValid = HeroHasExpectedSkillReferences(sandemperorHero, sandemperorActiveSkill, sandemperorUltimateSkill);
            var sandemperorBattlePrefabValid = sandemperorBattlePrefab == null || HeroHasExpectedBattlePrefab(sandemperorHero, sandemperorBattlePrefab);
            var monkActiveImpactVfxValid = SkillHasExpectedCastImpactVfxPresentation(
                monkActiveSkill,
                AssetDatabase.LoadAssetAtPath<GameObject>(MonkActiveImpactVfxPrefabPath),
                Vector3.zero,
                Vector3.zero,
                Vector3.one,
                false,
                true,
                1f);

            return !hasMainMenuScene
                || !hasHeroSelectScene
                || !hasBattleScene
                || !hasResultScene
                || !hasBasicAttackOnlyScene
                || !hasDefaultBattleInput
                || heroCatalog == null
                || bladesmanHero == null
                || bladesmanActiveSkill == null
                || bladesmanUltimateSkill == null
                || windchimeHero == null
                || windchimeActiveSkill == null
                || windchimeUltimateSkill == null
                || monkHero == null
                || monkActiveSkill == null
                || monkUltimateSkill == null
                || sandemperorHero == null
                || sandemperorActiveSkill == null
                || sandemperorUltimateSkill == null
                || !catalogContainsBladesman
                || !catalogContainsWindchime
                || !catalogContainsMonk
                || !catalogContainsSandemperor
                || !bladesmanReferencesValid
                || !bladesmanBattlePrefabValid
                || !bladesmanActiveImpactVfxValid
                || !windchimeReferencesValid
                || !windchimeBattlePrefabValid
                || !monkReferencesValid
                || !monkBattlePrefabValid
                || !sandemperorReferencesValid
                || !sandemperorBattlePrefabValid
                || !monkActiveImpactVfxValid;
        }

        private static void EnsureDemoContentValidationPassed(bool logFailures)
        {
            if (ValidateDemoContentConsistency(logFailures))
            {
                return;
            }

            throw new InvalidOperationException("Stage-01 demo content validation failed. Reconcile demo asset drift before batch generation or build export.");
        }

        private static bool ValidateDemoContentConsistency(bool logFailures)
        {
            var issues = new List<string>();
            var monkHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(MonkHeroAssetPath);
            var monkActiveSkill = AssetDatabase.LoadAssetAtPath<SkillData>(MonkActiveSkillAssetPath);
            var monkUltimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(MonkUltimateSkillAssetPath);

            CollectMonkValidationIssues(issues, monkHero, monkActiveSkill, monkUltimateSkill);

            if (issues.Count == 0)
            {
                return true;
            }

            if (logFailures)
            {
                Debug.LogWarning("[Stage01] Demo content validation issues:\n- " + string.Join("\n- ", issues));
            }

            return false;
        }

        private static void CollectMonkValidationIssues(List<string> issues, HeroDefinition monkHero, SkillData monkActiveSkill, SkillData monkUltimateSkill)
        {
            if (issues == null)
            {
                return;
            }

            if (monkHero == null)
            {
                issues.Add("Monk hero asset is missing.");
            }
            else
            {
                if (monkHero.heroClass != HeroClass.Support)
                {
                    issues.Add($"Monk heroClass expected {HeroClass.Support} but found {monkHero.heroClass}.");
                }

                if (monkHero.baseStats == null)
                {
                    issues.Add("Monk baseStats block is missing.");
                }
                else
                {
                    ReportFloatMismatch(issues, "Monk maxHealth", monkHero.baseStats.maxHealth, 430f);
                    ReportFloatMismatch(issues, "Monk attackPower", monkHero.baseStats.attackPower, 20f);
                    ReportFloatMismatch(issues, "Monk defense", monkHero.baseStats.defense, 24f);
                    ReportFloatMismatch(issues, "Monk attackSpeed", monkHero.baseStats.attackSpeed, 1f / 1.05f);
                    ReportFloatMismatch(issues, "Monk moveSpeed", monkHero.baseStats.moveSpeed, 4f);
                    ReportFloatMismatch(issues, "Monk attackRange", monkHero.baseStats.attackRange, 1.9f);
                }

                if (monkHero.basicAttack == null)
                {
                    issues.Add("Monk basicAttack block is missing.");
                }
                else
                {
                    ReportFloatMismatch(issues, "Monk basic attack damageMultiplier", monkHero.basicAttack.damageMultiplier, 0.95f);
                    ReportFloatMismatch(issues, "Monk basic attack attackInterval", monkHero.basicAttack.attackInterval, 1.05f);
                    ReportFloatMismatch(issues, "Monk basic attack rangeOverride", monkHero.basicAttack.rangeOverride, 1.9f);

                    if (monkHero.basicAttack.usesProjectile)
                    {
                        issues.Add("Monk basic attack expected melee instant hit, but usesProjectile is true.");
                    }

                    if (monkHero.basicAttack.effectType != BasicAttackEffectType.Damage)
                    {
                        issues.Add($"Monk basic attack effectType expected {BasicAttackEffectType.Damage} but found {monkHero.basicAttack.effectType}.");
                    }

                    if (monkHero.basicAttack.targetType != BasicAttackTargetType.NearestEnemy)
                    {
                        issues.Add($"Monk basic attack targetType expected {BasicAttackTargetType.NearestEnemy} but found {monkHero.basicAttack.targetType}.");
                    }

                    if (monkHero.basicAttack.onHitStatusEffects != null && monkHero.basicAttack.onHitStatusEffects.Count > 0)
                    {
                        issues.Add($"Monk basic attack expected no on-hit status effects, but found {monkHero.basicAttack.onHitStatusEffects.Count}.");
                    }
                }
            }

            if (monkActiveSkill == null)
            {
                issues.Add("Monk active skill asset is missing.");
            }
            else
            {
                if (monkActiveSkill.targetType != SkillTargetType.Self)
                {
                    issues.Add($"Monk active targetType expected {SkillTargetType.Self} but found {monkActiveSkill.targetType}.");
                }

                ReportFloatMismatch(issues, "Monk active castRange", monkActiveSkill.castRange, 0f);
                ReportFloatMismatch(issues, "Monk active areaRadius", monkActiveSkill.areaRadius, 4.5f);
                ReportFloatMismatch(issues, "Monk active cooldownSeconds", monkActiveSkill.cooldownSeconds, 8f);

                if (!monkActiveSkill.allowsSelfCast)
                {
                    issues.Add("Monk active expected allowsSelfCast = true.");
                }

                if (monkActiveSkill.effects == null || monkActiveSkill.effects.Count != 1)
                {
                    issues.Add($"Monk active expected exactly 1 effect, but found {(monkActiveSkill.effects == null ? 0 : monkActiveSkill.effects.Count)}.");
                }
                else
                {
                    var healEffect = monkActiveSkill.effects[0];
                    if (healEffect.effectType != SkillEffectType.DirectHeal)
                    {
                        issues.Add($"Monk active effectType expected {SkillEffectType.DirectHeal} but found {healEffect.effectType}.");
                    }

                    if (healEffect.targetMode != SkillEffectTargetMode.AlliesInRadiusAroundCaster)
                    {
                        issues.Add($"Monk active targetMode expected {SkillEffectTargetMode.AlliesInRadiusAroundCaster} but found {healEffect.targetMode}.");
                    }

                    ReportFloatMismatch(issues, "Monk active heal powerMultiplier", healEffect.powerMultiplier, 0.9f);
                    ReportFloatMismatch(issues, "Monk active heal radiusOverride", healEffect.radiusOverride, 4.5f);
                }
            }

            if (monkUltimateSkill == null)
            {
                issues.Add("Monk ultimate skill asset is missing.");
                return;
            }

            if (monkUltimateSkill.skillType != SkillType.Buff)
            {
                issues.Add($"Monk ultimate skillType expected {SkillType.Buff} but found {monkUltimateSkill.skillType}.");
            }

            if (monkUltimateSkill.targetType != SkillTargetType.Self)
            {
                issues.Add($"Monk ultimate targetType expected {SkillTargetType.Self} but found {monkUltimateSkill.targetType}.");
            }

            ReportFloatMismatch(issues, "Monk ultimate castRange", monkUltimateSkill.castRange, 0f);
            ReportFloatMismatch(issues, "Monk ultimate areaRadius", monkUltimateSkill.areaRadius, 6.8f);

            if (!monkUltimateSkill.allowsSelfCast)
            {
                issues.Add("Monk ultimate expected allowsSelfCast = true.");
            }

            if (monkUltimateSkill.effects == null || monkUltimateSkill.effects.Count != 1)
            {
                issues.Add($"Monk ultimate expected exactly 1 effect, but found {(monkUltimateSkill.effects == null ? 0 : monkUltimateSkill.effects.Count)}.");
            }
            else
            {
                var shieldEffect = monkUltimateSkill.effects[0];
                if (shieldEffect.effectType != SkillEffectType.ApplyStatusEffects)
                {
                    issues.Add($"Monk ultimate effectType expected {SkillEffectType.ApplyStatusEffects} but found {shieldEffect.effectType}.");
                }

                if (shieldEffect.targetMode != SkillEffectTargetMode.AlliesInRadiusAroundCaster)
                {
                    issues.Add($"Monk ultimate targetMode expected {SkillEffectTargetMode.AlliesInRadiusAroundCaster} but found {shieldEffect.targetMode}.");
                }

                ReportFloatMismatch(issues, "Monk ultimate shield radiusOverride", shieldEffect.radiusOverride, 6.8f);

                if (shieldEffect.statusEffects == null || shieldEffect.statusEffects.Count != 1)
                {
                    issues.Add($"Monk ultimate expected exactly 1 status effect, but found {(shieldEffect.statusEffects == null ? 0 : shieldEffect.statusEffects.Count)}.");
                }
                else
                {
                    var shieldStatus = shieldEffect.statusEffects[0];
                    if (shieldStatus.effectType != StatusEffectType.Shield)
                    {
                        issues.Add($"Monk ultimate status effect expected {StatusEffectType.Shield} but found {shieldStatus.effectType}.");
                    }

                    ReportFloatMismatch(issues, "Monk ultimate shield durationSeconds", shieldStatus.durationSeconds, 5f);
                    ReportFloatMismatch(issues, "Monk ultimate shield magnitude", shieldStatus.magnitude, 130f);

                    if (shieldStatus.maxStacks != 1)
                    {
                        issues.Add($"Monk ultimate shield maxStacks expected 1 but found {shieldStatus.maxStacks}.");
                    }

                    if (!shieldStatus.refreshDurationOnReapply)
                    {
                        issues.Add("Monk ultimate shield expected refreshDurationOnReapply = true.");
                    }
                }
            }

            var decision = monkUltimateSkill.ultimateDecision;
            if (decision == null)
            {
                issues.Add("Monk ultimateDecision block is missing.");
                return;
            }

            if (decision.targetingType != UltimateTargetingType.Self)
            {
                issues.Add($"Monk ultimate targetingType expected {UltimateTargetingType.Self} but found {decision.targetingType}.");
            }

            if (decision.combineMode != UltimateConditionCombineMode.AllMustPass)
            {
                issues.Add($"Monk ultimate combineMode expected {UltimateConditionCombineMode.AllMustPass} but found {decision.combineMode}.");
            }

            var primaryCondition = decision.primaryCondition;
            if (primaryCondition == null)
            {
                issues.Add("Monk ultimate primaryCondition is missing.");
            }
            else
            {
                if (primaryCondition.conditionType != UltimateConditionType.AllyCountInRange)
                {
                    issues.Add($"Monk ultimate primary condition expected {UltimateConditionType.AllyCountInRange} but found {primaryCondition.conditionType}.");
                }

                ReportFloatMismatch(issues, "Monk ultimate primary searchRadius", primaryCondition.searchRadius, 6.8f);

                if (primaryCondition.requiredUnitCount != 2)
                {
                    issues.Add($"Monk ultimate primary requiredUnitCount expected 2 but found {primaryCondition.requiredUnitCount}.");
                }
            }

            var secondaryCondition = decision.secondaryCondition;
            if (secondaryCondition == null)
            {
                issues.Add("Monk ultimate secondaryCondition is missing.");
            }
            else
            {
                if (secondaryCondition.conditionType != UltimateConditionType.AllyLowHealthInRange)
                {
                    issues.Add($"Monk ultimate secondary condition expected {UltimateConditionType.AllyLowHealthInRange} but found {secondaryCondition.conditionType}.");
                }

                ReportFloatMismatch(issues, "Monk ultimate secondary searchRadius", secondaryCondition.searchRadius, 6.8f);

                if (secondaryCondition.requiredUnitCount != 2)
                {
                    issues.Add($"Monk ultimate secondary requiredUnitCount expected 2 but found {secondaryCondition.requiredUnitCount}.");
                }

                ReportFloatMismatch(issues, "Monk ultimate secondary healthPercentThreshold", secondaryCondition.healthPercentThreshold, 0.7f);
            }

            var fallback = decision.fallback;
            if (fallback == null)
            {
                issues.Add("Monk ultimate fallback block is missing.");
                return;
            }

            if (fallback.fallbackType != UltimateFallbackType.LowerPrimaryThreshold)
            {
                issues.Add($"Monk ultimate fallbackType expected {UltimateFallbackType.LowerPrimaryThreshold} but found {fallback.fallbackType}.");
            }

            ReportFloatMismatch(issues, "Monk ultimate fallback triggerAfterSeconds", fallback.triggerAfterSeconds, 45f);
            ReportFloatMismatch(issues, "Monk ultimate fallback overrideHealthPercentThreshold", fallback.overrideHealthPercentThreshold, 0.8f);

            if (fallback.overrideRequiredUnitCount != 0)
            {
                issues.Add($"Monk ultimate fallback overrideRequiredUnitCount expected 0 but found {fallback.overrideRequiredUnitCount}.");
            }
        }

        private static void ReportFloatMismatch(List<string> issues, string label, float actual, float expected, float tolerance = 0.0001f)
        {
            if (Mathf.Abs(actual - expected) <= tolerance)
            {
                return;
            }

            issues.Add($"{label} expected {expected:0.####} but found {actual:0.####}.");
        }

        private static bool CatalogContainsHero(HeroCatalogData catalog, string heroId)
        {
            if (catalog?.heroes == null || string.IsNullOrWhiteSpace(heroId))
            {
                return false;
            }

            for (var i = 0; i < catalog.heroes.Count; i++)
            {
                var hero = catalog.heroes[i];
                if (hero != null && hero.heroId == heroId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HeroHasExpectedSkillReferences(HeroDefinition hero, SkillData expectedActiveSkill, SkillData expectedUltimateSkill)
        {
            return hero != null
                && expectedActiveSkill != null
                && expectedUltimateSkill != null
                && hero.activeSkill == expectedActiveSkill
                && hero.ultimateSkill == expectedUltimateSkill;
        }

        private static void EnsureHeroSkillReferences(HeroDefinition hero, SkillData activeSkill, SkillData ultimateSkill)
        {
            if (hero == null || activeSkill == null || ultimateSkill == null)
            {
                return;
            }

            if (hero.activeSkill == activeSkill && hero.ultimateSkill == ultimateSkill)
            {
                return;
            }

            hero.activeSkill = activeSkill;
            hero.ultimateSkill = ultimateSkill;
            EditorUtility.SetDirty(hero);
        }

        private static bool HeroHasExpectedBattlePrefab(HeroDefinition hero, GameObject expectedBattlePrefab)
        {
            return hero != null
                && expectedBattlePrefab != null
                && hero.visualConfig != null
                && hero.visualConfig.battlePrefab == expectedBattlePrefab;
        }

        private static bool SkillHasExpectedCastImpactVfxPresentation(
            SkillData skill,
            GameObject expectedPrefab,
            Vector3 expectedOffset,
            Vector3 expectedEulerAngles,
            Vector3 expectedScaleMultiplier,
            bool expectedAlignToTargetDirection,
            bool expectedScaleWithSkillArea = false,
            float expectedAreaDiameterScaleMultiplier = 1f)
        {
            return skill != null
                && expectedPrefab != null
                && skill.castImpactVfxPrefab == expectedPrefab
                && skill.castImpactVfxLocalOffset == expectedOffset
                && skill.castImpactVfxEulerAngles == expectedEulerAngles
                && skill.castImpactVfxScaleMultiplier == expectedScaleMultiplier
                && skill.castImpactVfxAlignToTargetDirection == expectedAlignToTargetDirection
                && skill.castImpactVfxScaleWithSkillArea == expectedScaleWithSkillArea
                && Mathf.Approximately(skill.castImpactVfxAreaDiameterScaleMultiplier, expectedAreaDiameterScaleMultiplier);
        }

        private static void EnsureHeroBattlePrefabReference(HeroDefinition hero, GameObject battlePrefab)
        {
            if (hero == null || battlePrefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            if (hero.visualConfig.battlePrefab == battlePrefab)
            {
                return;
            }

            hero.visualConfig.battlePrefab = battlePrefab;
            HeroPortraitSyncUtility.TryAssignPortraitFromPrefabFolder(hero);
            EditorUtility.SetDirty(hero);
        }

        private static void EnsureSkillCastImpactVfxPresentation(
            SkillData skill,
            GameObject prefab,
            Vector3 localOffset,
            Vector3 eulerAngles,
            Vector3 scaleMultiplier,
            bool alignToTargetDirection,
            bool scaleWithSkillArea = false,
            float areaDiameterScaleMultiplier = 1f)
        {
            if (skill == null || prefab == null)
            {
                return;
            }

            if (skill.castImpactVfxPrefab == prefab
                && skill.castImpactVfxLocalOffset == localOffset
                && skill.castImpactVfxEulerAngles == eulerAngles
                && skill.castImpactVfxScaleMultiplier == scaleMultiplier
                && skill.castImpactVfxAlignToTargetDirection == alignToTargetDirection
                && skill.castImpactVfxScaleWithSkillArea == scaleWithSkillArea
                && Mathf.Approximately(skill.castImpactVfxAreaDiameterScaleMultiplier, areaDiameterScaleMultiplier))
            {
                return;
            }

            skill.castImpactVfxPrefab = prefab;
            skill.castImpactVfxLocalOffset = localOffset;
            skill.castImpactVfxEulerAngles = eulerAngles;
            skill.castImpactVfxScaleMultiplier = scaleMultiplier;
            skill.castImpactVfxAlignToTargetDirection = alignToTargetDirection;
            skill.castImpactVfxScaleWithSkillArea = scaleWithSkillArea;
            skill.castImpactVfxAreaDiameterScaleMultiplier = areaDiameterScaleMultiplier;
            EditorUtility.SetDirty(skill);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Scripts");
            EnsureFolder("Assets/Scripts", "Editor");
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesFolder, "Stage01Demo");
            EnsureFolder("Assets/Data", "Stage01Demo");
            EnsureFolder(DemoRoot, "Skills");
            EnsureFolder(DemoRoot, "Heroes");
            EnsureFolder(DemoRoot, "Battles");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var folderPath = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static HeroDefinition CreateHero(
            string heroId,
            string displayName,
            HeroClass heroClass,
            float maxHealth,
            float attackPower,
            float defense,
            float attackSpeed,
            float moveSpeed,
            float critChance,
            float critDamageMultiplier,
            float attackRange,
            SkillData activeSkill,
            SkillData ultimateSkill,
            bool overwriteExistingContent,
            params HeroTag[] tags)
        {
            return CreateHero(
                heroId,
                displayName,
                heroClass,
                maxHealth,
                attackPower,
                defense,
                attackSpeed,
                moveSpeed,
                critChance,
                critDamageMultiplier,
                attackRange,
                activeSkill,
                ultimateSkill,
                overwriteExistingContent,
                out _,
                tags);
        }

        private static HeroDefinition CreateHero(
            string heroId,
            string displayName,
            HeroClass heroClass,
            float maxHealth,
            float attackPower,
            float defense,
            float attackSpeed,
            float moveSpeed,
            float critChance,
            float critDamageMultiplier,
            float attackRange,
            SkillData activeSkill,
            SkillData ultimateSkill,
            bool overwriteExistingContent,
            out bool existedBefore,
            params HeroTag[] tags)
        {
            var assetPath = GetHeroAssetPath(heroId, displayName);
            existedBefore = TryLoadAsset(assetPath, out HeroDefinition existingHero);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return existingHero;
            }

            var hero = LoadOrCreateAsset<HeroDefinition>(assetPath);
            hero.heroId = heroId;
            hero.displayName = displayName;
            hero.heroClass = heroClass;
            hero.tags.Clear();
            hero.tags.AddRange(tags);

            hero.baseStats.maxHealth = maxHealth;
            hero.baseStats.attackPower = attackPower;
            hero.baseStats.defense = defense;
            hero.baseStats.attackSpeed = attackSpeed;
            hero.baseStats.moveSpeed = moveSpeed;
            hero.baseStats.criticalChance = critChance;
            hero.baseStats.criticalDamageMultiplier = critDamageMultiplier;
            hero.baseStats.attackRange = attackRange;

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.rangeOverride = attackRange;
            hero.basicAttack.usesProjectile = tags != null && System.Array.Exists(tags, tag => tag == HeroTag.Ranged);
            hero.basicAttack.projectileSpeed = hero.basicAttack.usesProjectile ? 14f : 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = heroClass == HeroClass.Assassin
                ? BasicAttackTargetType.PreferredEnemy
                : BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();

            hero.activeSkill = activeSkill;
            hero.ultimateSkill = ultimateSkill;
            hero.aiTemplateId = $"{heroClass.ToString().ToLowerInvariant()}_default";
            hero.usesSpecialLogic = false;
            hero.specialLogicNotes = string.Empty;
            hero.debugNotes = $"Stage-01 demo hero for {heroClass}.";
            var battlePrefab = LoadBattlePrefab(heroId, heroClass);
            hero.visualConfig.battlePrefab = battlePrefab;
            hero.visualConfig.animatorController = heroId == "assassin_002_tidefin"
                ? null
                : battlePrefab != null
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroEditorControllerPath)
                    : null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = heroId == "assassin_002_tidefin";
            hero.visualConfig.projectilePrefab = heroId switch
            {
                "mage_001_firemage" => AssetDatabase.LoadAssetAtPath<GameObject>(FireMageProjectilePrefabPath),
                "mage_002_frostmage" => AssetDatabase.LoadAssetAtPath<GameObject>(FrostMageProjectilePrefabPath),
                "mage_003_sandemperor" => AssetDatabase.LoadAssetAtPath<GameObject>(FireMageProjectilePrefabPath),
                "marksman_001_longshot" => AssetDatabase.LoadAssetAtPath<GameObject>(LongshotProjectilePrefabPath),
                "marksman_002_rifleman" => AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanProjectilePrefabPath),
                "marksman_003_venomshooter" => AssetDatabase.LoadAssetAtPath<GameObject>(LongshotProjectilePrefabPath),
                "support_001_sunpriest" => AssetDatabase.LoadAssetAtPath<GameObject>(SunpriestProjectilePrefabPath),
                "support_002_windchime" => AssetDatabase.LoadAssetAtPath<GameObject>(SunpriestProjectilePrefabPath),
                _ => null,
            };
            hero.visualConfig.projectileAlignToMovement =
                heroId == "mage_001_firemage"
                || heroId == "mage_002_frostmage"
                || heroId == "mage_003_sandemperor"
                || heroId == "marksman_001_longshot"
                || heroId == "marksman_002_rifleman"
                || heroId == "marksman_003_venomshooter"
                || heroId == "support_001_sunpriest"
                || heroId == "support_002_windchime";
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            hero.visualConfig.hitVfxPrefab = null;
            HeroPortraitSyncUtility.TryAssignPortraitFromPrefabFolder(hero);
            EditorUtility.SetDirty(hero);
            return hero;
        }

        private static GameObject LoadBattlePrefab(string heroId, HeroClass heroClass)
        {
            var prefabPath = heroId switch
            {
                "assassin_001_shadowstep" => AssassinPrefabPath,
                "assassin_002_tidefin" => TidefinPrefabPath,
                "mage_001_firemage" => FireMagePrefabPath,
                "mage_002_frostmage" => FrostMagePrefabPath,
                "mage_003_sandemperor" => FireMagePrefabPath,
                "marksman_001_longshot" => MarksmanPrefabPath,
                "marksman_002_rifleman" => RiflemanPrefabPath,
                "marksman_003_venomshooter" => MarksmanPrefabPath,
                "support_002_windchime" => WindchimePrefabPath,
                "support_003_monk" => MonkPrefabPath,
                "warrior_002_bladesman" => BladesmanPrefabPath,
                "tank_002_shieldwarden" => ShieldwardenPrefabPath,
                _ => heroClass switch
                {
                    HeroClass.Marksman => MarksmanPrefabPath,
                    HeroClass.Support => SupportPrefabPath,
                    HeroClass.Warrior => WarriorPrefabPath,
                    HeroClass.Mage => FireMagePrefabPath,
                    HeroClass.Tank => TankPrefabPath,
                    _ => null,
                },
            };

            return string.IsNullOrWhiteSpace(prefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static SkillData CreateSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            SkillType skillType,
            SkillTargetType targetType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            int minTargetsToCast,
            bool overwriteExistingContent)
        {
            return CreateSkill(
                skillId,
                displayName,
                slotType,
                skillType,
                targetType,
                castRange,
                areaRadius,
                powerMultiplier,
                cooldownSeconds,
                minTargetsToCast,
                overwriteExistingContent,
                out _);
        }

        private static SkillData CreateSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            SkillType skillType,
            SkillTargetType targetType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            int minTargetsToCast,
            bool overwriteExistingContent,
            out bool existedBefore)
        {
            var assetPath = GetSkillAssetPath(skillId, displayName);
            existedBefore = TryLoadAsset(assetPath, out SkillData existingSkill);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return existingSkill;
            }

            var skill = LoadOrCreateAsset<SkillData>(assetPath);
            skill.skillId = skillId;
            skill.displayName = displayName;
            skill.description = $"Stage-01 demo skill: {displayName}";
            skill.slotType = slotType;
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = skillType;
            skill.targetType = targetType;
            skill.preferredEnemyHeroClass = HeroClass.Assassin;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.targetPrioritySearchRadius = 0f;
            skill.targetPriorityRequiredUnitCount = 1;
            skill.castRange = castRange;
            skill.areaRadius = areaRadius;
            skill.cooldownSeconds = slotType == SkillSlotType.Ultimate ? 0f : cooldownSeconds;
            skill.minTargetsToCast = minTargetsToCast;
            skill.effects.Clear();
            skill.allowsSelfCast = targetType == SkillTargetType.Self || targetType == SkillTargetType.AllAllies;
            ResetReactiveGuard(skill);
            ResetActionSequence(skill);
            ResetPassiveSkillData(skill);
            ResetTemporaryOverride(skill);
            skill.castImpactVfxPrefab = null;
            skill.castImpactVfxLocalOffset = Vector3.zero;
            skill.castImpactVfxEulerAngles = Vector3.zero;
            skill.castImpactVfxScaleMultiplier = Vector3.one;
            skill.castImpactVfxAlignToTargetDirection = false;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            ResetUltimateDecision(skill);

            AddDefaultEffectsForSkill(skill, powerMultiplier);

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateLongshotActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_marksman_active_focusshot",
                "Heavy Shot",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetDamage,
                SkillTargetType.NearestEnemy,
                ScaleRangedHeroDistance(6f),
                0f,
                1.7f,
                6f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            AddDamageEffect(skill, 1.7f);
            AddForcedMovementEffect(skill, 2.4f, 0.25f, 0f);
            skill.description = "Stage-01 demo skill: Heavy Shot";
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateShadowstepActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_assassin_active_shadowblink",
                "Shadow Blink",
                SkillSlotType.ActiveSkill,
                SkillType.Dash,
                SkillTargetType.NearestEnemy,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0f,
                0f,
                6f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Dash;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 6f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddRepositionEffect(skill);
            AddDamageEffect(skill, 1.25f);
            skill.description = "Stage-01 demo skill: Shadow Blink";
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateRiflemanActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_rifleman_active_burstfire",
                "Burst Fire",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetDamage,
                SkillTargetType.NearestEnemy,
                ScaleRangedHeroDistance(6.2f),
                0f,
                0.55f,
                5f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.castRange = ScaleRangedHeroDistance(6.2f);
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddDamageEffect(skill, 0.55f);
            ResetActionSequence(skill);
            skill.actionSequence.enabled = true;
            skill.actionSequence.payloadType = CombatActionSequencePayloadType.SourceSkill;
            skill.actionSequence.repeatMode = CombatActionSequenceRepeatMode.FixedCount;
            skill.actionSequence.repeatCount = 3;
            skill.actionSequence.durationSeconds = 0.54f;
            skill.actionSequence.intervalSeconds = 0.18f;
            skill.actionSequence.windupSeconds = 0f;
            skill.actionSequence.recoverySeconds = 0f;
            skill.actionSequence.temporaryBasicAttackRangeOverride = 0f;
            skill.actionSequence.temporarySkillCastRangeOverride = 0f;
            skill.actionSequence.targetRefreshMode = CombatActionSequenceTargetRefreshMode.KeepCurrentTarget;
            skill.actionSequence.interruptFlags =
                CombatActionSequenceInterruptFlags.HardControl | CombatActionSequenceInterruptFlags.ForcedMovement;
            skill.targetIndicatorVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanActiveTargetIndicatorVfxPrefabPath);
            skill.description = "Stage-01 demo skill: Burst Fire";
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateTidefinActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_tidefin_active_tidalpounce",
                "Tidal Pounce",
                SkillSlotType.ActiveSkill,
                SkillType.Dash,
                SkillTargetType.HighestDamageEnemyInRange,
                4f,
                0f,
                0f,
                5.5f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Dash;
            skill.targetType = SkillTargetType.HighestDamageEnemyInRange;
            skill.castRange = 4f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 5.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddRepositionEffect(skill, 0.28f, 0.85f);
            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 5f,
                magnitude = -0.2f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            skill.description = "Stage-01 demo skill: Tidal Pounce";
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateLongshotUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_marksman_ultimate_arrowrain",
                "Rapid Barrage",
                SkillSlotType.Ultimate,
                SkillType.SingleTargetDamage,
                SkillTargetType.LowestHealthEnemy,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            AddDamageEffect(skill, 0f);
            skill.description = "Stage-01 demo skill: Rapid Barrage";
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateShadowstepUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_assassin_ultimate_smokeveil",
                "Smoke Veil",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.NearestEnemy,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            // Smoke Veil is a self-buff, but we still require a valid enemy target before
            // casting it. NearestEnemy is therefore used as cast gating, while the actual
            // status payload still lands on the caster.
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            var effect = AddApplyStatusEffectsEffect(skill);
            effect.targetMode = SkillEffectTargetMode.Caster;
            effect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Untargetable,
                durationSeconds = 6f,
                magnitude = 0f,
                activeSkillCooldownCapSeconds = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.description = "Stage-01 demo skill: Smoke Veil";
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateRiflemanUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_rifleman_ultimate_fraggrenade",
                "Frag Grenade",
                SkillSlotType.Ultimate,
                SkillType.AreaDamage,
                SkillTargetType.DensestEnemyArea,
                ScaleRangedHeroDistance(7f),
                ScaleRangedHeroDistance(4f),
                4f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.DensestEnemyArea;
            skill.castRange = ScaleRangedHeroDistance(7f);
            skill.areaRadius = ScaleRangedHeroDistance(4f);
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectDamage, PersistentAreaTargetType.Enemies, 7f, skill.areaRadius, 0.45f, 0.45f, false);
            skill.description = "Stage-01 demo skill: Frag Grenade";
            ResetActionSequence(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateVenomshooterActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_venomshooter_active_poisonmist",
                "Poison Mist",
                SkillSlotType.ActiveSkill,
                SkillType.AreaDamage,
                SkillTargetType.DensestEnemyArea,
                ScaleRangedHeroDistance(6.133333f),
                ScaleRangedHeroDistance(2.4f),
                0f,
                6f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.DensestEnemyArea;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = ScaleRangedHeroDistance(6.133333f);
            skill.areaRadius = ScaleRangedHeroDistance(2.4f);
            skill.cooldownSeconds = 6f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var poisonEffect = AddApplyStatusEffectsEffect(skill);
            poisonEffect.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundPrimaryTarget;
            poisonEffect.radiusOverride = skill.areaRadius;
            AddVenomshooterPoisonStacks(poisonEffect.statusEffects, 3);

            skill.description = "Stage-01 demo skill: ranged poison field that rapidly seeds 3 poison stacks across a clustered enemy group.";
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateVenomshooterUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_venomshooter_ultimate_venomdetonation",
                "Venom Detonation",
                SkillSlotType.Ultimate,
                SkillType.AreaDamage,
                SkillTargetType.AllEnemies,
                0f,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 2.6f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var detonation = AddDamageEffect(skill, 0f);
            detonation.targetMode = SkillEffectTargetMode.SkillTargets;
            detonation.statusStackQueryEffectType = StatusEffectType.DamageOverTime;
            detonation.statusStackQueryThemeKey = PoisonStatusThemeKey;
            detonation.minimumRequiredStatusStacks = 1;
            detonation.bonusPowerMultiplierPerStatusStack = 1.8f;
            detonation.triggerFollowUpAreaOnTargetDeath = true;
            detonation.followUpAreaRadius = 3.4f;
            detonation.followUpAreaPowerMultiplier = 1.8f;
            detonation.followUpAreaCanChain = true;
            detonation.followUpAreaLimitTriggerOncePerUnitPerExecution = true;
            AddVenomshooterPoisonStacks(detonation.followUpAreaStatusEffects, 2);

            skill.description = "Stage-01 demo skill: detonates existing poison on all enemies, then chains corpse explosions that re-spread poison.";
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateTidefinUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_tidefin_ultimate_ruintide",
                "Ruin Tide",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.Self,
                0f,
                4f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.castRange = 0f;
            skill.areaRadius = 4f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();
            var effect = AddApplyStatusEffectsEffect(skill);
            effect.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundCaster;
            effect.radiusOverride = skill.areaRadius;
            effect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 5f,
                magnitude = -0.3f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            effect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 5f,
                magnitude = -0.3f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.description = "Stage-01 demo skill: Ruin Tide";
            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyLowHealthInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.secondaryCondition.healthPercentThreshold = 0.5f;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AnyPass;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 40f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 1;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateMageActiveBurstSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_mage_active_emberburst",
                "Ember Burst",
                SkillSlotType.ActiveSkill,
                SkillType.AreaDamage,
                SkillTargetType.DensestEnemyArea,
                ScaleRangedHeroDistance(6f),
                ScaleRangedHeroDistance(2f),
                1.2f,
                6f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            skill.targetType = SkillTargetType.DensestEnemyArea;
            skill.allowsSelfCast = false;
            skill.castRange = ScaleRangedHeroDistance(6f);
            skill.areaRadius = ScaleRangedHeroDistance(2f);
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MageActiveAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectDamage, PersistentAreaTargetType.Enemies, 1.2f, skill.areaRadius, 0.4f, 1f, false);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateFrostmageActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_frostmage_active_frostburst",
                "Frost Burst",
                SkillSlotType.ActiveSkill,
                SkillType.AreaDamage,
                SkillTargetType.DensestEnemyArea,
                ScaleRangedHeroDistance(6.2f),
                ScaleRangedHeroDistance(2f),
                0.9f,
                6.5f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.DensestEnemyArea;
            skill.castRange = ScaleRangedHeroDistance(6.2f);
            skill.areaRadius = ScaleRangedHeroDistance(2f);
            skill.cooldownSeconds = 6.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrostMageActiveAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;

            AddDamageEffect(skill, 0.9f);
            var slowEffect = AddApplyStatusEffectsEffect(skill);
            slowEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.MoveSpeedModifier,
                durationSeconds = 2.5f,
                magnitude = -0.3f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.None, PersistentAreaTargetType.Enemies, 0f, skill.areaRadius, 0.45f, 1f, false);

            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateFrostmageUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_frostmage_ultimate_blizzard",
                "Blizzard",
                SkillSlotType.Ultimate,
                SkillType.AreaDamage,
                SkillTargetType.DensestEnemyArea,
                ScaleRangedHeroDistance(7f),
                ScaleRangedHeroDistance(5.5f),
                2.7f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.DensestEnemyArea;
            skill.castRange = ScaleRangedHeroDistance(7f);
            skill.areaRadius = ScaleRangedHeroDistance(5.5f);
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrostMageUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;

            var areaEffect = AddPersistentAreaEffect(
                skill,
                PersistentAreaPulseEffectType.DirectDamage,
                PersistentAreaTargetType.Enemies,
                2.7f,
                skill.areaRadius,
                5f,
                1f,
                false);
            areaEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.MoveSpeedModifier,
                durationSeconds = 1.2f,
                magnitude = -0.6f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSandemperorActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_sandemperor_active_raisesandguard",
                "Raise Sandguard",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.CurrentEnemyTarget,
                ScaleRangedHeroDistance(6f),
                0f,
                0f,
                4f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: deploys a sandguard near the current basic-attack target. Sandguards only strike when Sandemperor basic-attacks.";
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.CurrentEnemyTarget;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = ScaleRangedHeroDistance(6f);
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 4f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddDeployableProxyEffect(
                skill,
                0.55f,
                ScaleRangedHeroDistance(1.7333333f),
                12f,
                5,
                DeployableProxySpawnMode.AroundTarget,
                1.2f,
                DeployableProxyTriggerMode.OnOwnerBasicAttack,
                true,
                false);
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSandemperorUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_sandemperor_ultimate_imperialencirclement",
                "Imperial Encirclement",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.AllEnemies,
                0f,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: instantly rebuilds the sandguard line around every living enemy and triggers one opening strike.";
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 2.6f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddDeployableProxyEffect(
                skill,
                0.55f,
                ScaleRangedHeroDistance(1.7333333f),
                5f,
                5,
                DeployableProxySpawnMode.AroundTarget,
                1.2f,
                DeployableProxyTriggerMode.OnOwnerBasicAttack,
                true,
                true);
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateArchivedMageFireboltSkill(bool overwriteExistingContent)
        {
            var assetPath = $"{SkillsRootFolder}/Archive/Firebolt Burst.asset";
            if (!overwriteExistingContent && TryLoadAsset(assetPath, out SkillData existingSkill))
            {
                return existingSkill;
            }

            var skill = LoadOrCreateAsset<SkillData>(assetPath);
            skill.skillId = "skill_mage_active_firebolt";
            skill.displayName = "Firebolt Burst";
            skill.description = "Stage-01 archived mage single-target skill.";
            skill.slotType = SkillSlotType.ActiveSkill;
            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.LowestHealthEnemy;
            skill.castRange = 6f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 6f;
            skill.minTargetsToCast = 1;
            skill.effects.Clear();
            skill.allowsSelfCast = false;
            skill.persistentAreaVfxPrefab = null;
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            ResetUltimateDecision(skill);
            AddDamageEffect(skill, 1.5f);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSkybreakerActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_warrior_active_breakerrush",
                "Breaker Rush",
                SkillSlotType.ActiveSkill,
                SkillType.Dash,
                SkillTargetType.NearestEnemy,
                3f,
                0f,
                0f,
                8f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.castRange = 3f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 8f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;

            var shieldEffect = AddApplyStatusEffectsEffect(skill);
            shieldEffect.targetMode = SkillEffectTargetMode.Caster;
            shieldEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Shield,
                durationSeconds = 4f,
                magnitude = 60f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            AddRepositionEffect(skill, 0.25f);

            var knockUpMovementEffect = AddForcedMovementEffect(skill, 0f, 1f, 1.8f);
            knockUpMovementEffect.targetMode = SkillEffectTargetMode.DashPathEnemies;
            knockUpMovementEffect.radiusOverride = 0.8f;

            var knockUpStatusEffect = AddApplyStatusEffectsEffect(skill);
            knockUpStatusEffect.targetMode = SkillEffectTargetMode.DashPathEnemies;
            knockUpStatusEffect.radiusOverride = 0.8f;
            knockUpStatusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = 1f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSkybreakerUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_warrior_ultimate_skyquake",
                "Skyquake",
                SkillSlotType.Ultimate,
                SkillType.KnockUp,
                SkillTargetType.Self,
                0f,
                5f,
                8f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            skill.targetType = SkillTargetType.Self;
            skill.castRange = 0f;
            skill.areaRadius = 5f;
            skill.allowsSelfCast = true;

            var damageEffect = AddDamageEffect(skill, 8f);
            damageEffect.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundCaster;
            damageEffect.radiusOverride = skill.areaRadius;

            var knockUpMovementEffect = AddForcedMovementEffect(skill, 0f, 1f, 1.8f);
            knockUpMovementEffect.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundCaster;
            knockUpMovementEffect.radiusOverride = skill.areaRadius;

            var knockUpStatusEffect = AddApplyStatusEffectsEffect(skill);
            knockUpStatusEffect.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundCaster;
            knockUpStatusEffect.radiusOverride = skill.areaRadius;
            knockUpStatusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = 1f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBladesmanActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_bladesman_active_rendingslash",
                "裂甲斩",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetDamage,
                SkillTargetType.NearestEnemy,
                2.4f,
                0f,
                1.35f,
                6.5f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.castRange = 2.4f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 6.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var defenseDownEffect = AddApplyStatusEffectsEffect(skill);
            defenseDownEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 4f,
                magnitude = -0.25f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            AddDamageEffect(skill, 1.35f);
            skill.description = "Stage-01 demo skill: apply defense down before dealing a heavy single-target strike.";
            skill.castImpactVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BladesmanActiveImpactVfxPrefabPath);
            skill.castImpactVfxLocalOffset = new Vector3(0f, 0.12f, 0f);
            skill.castImpactVfxEulerAngles = new Vector3(0f, 0f, -90f);
            skill.castImpactVfxScaleMultiplier = new Vector3(0.18f, 0.18f, 1f);
            skill.castImpactVfxAlignToTargetDirection = true;
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBladesmanUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_bladesman_ultimate_flyingswallowsever",
                "飞燕断空",
                SkillSlotType.Ultimate,
                SkillType.Dash,
                SkillTargetType.NearestEnemy,
                6.5f,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Dash;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.castRange = 6.5f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            AddRepositionEffect(skill, 0.22f, 0f, 16f);

            var pathDamageEffect = AddDamageEffect(skill, 8f);
            pathDamageEffect.targetMode = SkillEffectTargetMode.DashPathEnemies;
            pathDamageEffect.radiusOverride = 4f;

            skill.description = "Stage-01 demo skill: lock a straight dash line and damage every enemy cut through once.";
            skill.dashTravelVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BladesmanUltimateDashVfxPrefabPath);
            skill.dashTravelVfxLocalOffset = Vector3.zero;
            skill.dashTravelVfxForwardOffset = 0.9f;
            skill.dashTravelVfxEulerAngles = Vector3.zero;
            skill.dashTravelVfxScaleMultiplier = Vector3.one;
            skill.dashTravelVfxPathWidthScaleMultiplier = 0.18f;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.CurrentTarget;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInDashPath;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            ApplyCountFallback(skill, 40f, 1, 0f, 0);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBerserkerActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_berserker_active_bloodfury",
                "Bloodfury",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.Self,
                0f,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo passive skill: missing health increases attack power.";
            skill.activationMode = SkillActivationMode.Passive;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();
            skill.passiveSkill.missingHealthAttackPowerRatio = 0.6f;
            skill.passiveSkill.maxAttackPowerBonus = 0.6f;
            ResetTemporaryOverride(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBerserkerUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_berserker_ultimate_titanrage",
                "Titan Rage",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.Self,
                0f,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyBerserkerUltimateBaseConfiguration(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureBerserkerUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyBerserkerUltimateBaseConfiguration(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.CurrentTargetOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.SelfLowHealth;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.65f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = 3.2f;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.AlternatePrimaryCondition;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.alternatePrimaryCondition.conditionType = UltimateConditionType.TargetIsHighValue;
            skill.ultimateDecision.fallback.alternatePrimaryCondition.highValueTargetType = HighValueTargetType.None;
            skill.ultimateDecision.fallback.alternatePrimaryCondition.requireTargetInCastRange = true;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyBerserkerUltimateBaseConfiguration(SkillData skill)
        {
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();
            ResetPassiveSkillData(skill);

            var selfBuffEffect = AddApplyStatusEffectsEffect(skill);
            selfBuffEffect.targetMode = SkillEffectTargetMode.Caster;
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 6f,
                magnitude = 0.25f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackSpeedModifier,
                durationSeconds = 6f,
                magnitude = 0.4f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.temporaryOverride.durationSeconds = 6f;
            skill.temporaryOverride.lifestealRatio = 0.35f;
            skill.temporaryOverride.visualScaleMultiplier = 1.25f;
            skill.description = "Stage-01 demo skill: enter a short frenzy with bonus damage, attack speed, lifesteal, and visual growth.";
        }

        private static void AddDefaultEffectsForSkill(SkillData skill, float powerMultiplier)
        {
            switch (skill.skillType)
            {
                case SkillType.SingleTargetDamage:
                case SkillType.AreaDamage:
                    AddDamageEffect(skill, powerMultiplier);
                    break;
                case SkillType.SingleTargetHeal:
                case SkillType.AreaHeal:
                    AddHealEffect(skill, powerMultiplier);
                    break;
                case SkillType.Dash:
                    AddRepositionEffect(skill);
                    AddDamageEffect(skill, powerMultiplier);
                    break;
                case SkillType.Taunt:
                    AddDamageEffect(skill, powerMultiplier);
                    break;
            }
        }

        private static SkillEffectData AddDamageEffect(SkillData skill, float powerMultiplier)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.DirectDamage,
                powerMultiplier = powerMultiplier,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddHealEffect(SkillData skill, float powerMultiplier)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.DirectHeal,
                powerMultiplier = powerMultiplier,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddApplyStatusEffectsEffect(SkillData skill)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.ApplyStatusEffects,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddForcedMovementEffect(
            SkillData skill,
            float distance,
            float durationSeconds,
            float peakHeight,
            ForcedMovementDirectionMode directionMode = ForcedMovementDirectionMode.AwayFromSource)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.ApplyForcedMovement,
                forcedMovementDirection = directionMode,
                forcedMovementDistance = distance,
                forcedMovementDurationSeconds = durationSeconds,
                forcedMovementPeakHeight = peakHeight,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddRepositionEffect(
            SkillData skill,
            float durationSeconds = 0f,
            float peakHeight = 0f,
            float dashDistance = 0f)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.RepositionNearPrimaryTarget,
                durationSeconds = durationSeconds,
                forcedMovementDurationSeconds = durationSeconds,
                forcedMovementPeakHeight = peakHeight,
                forcedMovementDistance = dashDistance,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddPersistentAreaEffect(
            SkillData skill,
            PersistentAreaPulseEffectType pulseEffectType,
            PersistentAreaTargetType targetType,
            float powerMultiplier,
            float radiusOverride,
            float durationSeconds,
            float tickIntervalSeconds,
            bool followCaster)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.CreatePersistentArea,
                powerMultiplier = powerMultiplier,
                radiusOverride = radiusOverride,
                durationSeconds = durationSeconds,
                tickIntervalSeconds = tickIntervalSeconds,
                followCaster = followCaster,
                persistentAreaPulseEffectType = pulseEffectType,
                persistentAreaTargetType = targetType,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddDeployableProxyEffect(
            SkillData skill,
            float powerMultiplier,
            float strikeRadius,
            float durationSeconds,
            int maxCount,
            DeployableProxySpawnMode spawnMode,
            float spawnOffsetDistance,
            DeployableProxyTriggerMode triggerMode,
            bool replaceOldestWhenLimitReached,
            bool immediateStrikeOnSpawn)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.CreateDeployableProxy,
                powerMultiplier = powerMultiplier,
                durationSeconds = durationSeconds,
                deployableProxyStrikeRadius = strikeRadius,
                deployableProxySpawnMode = spawnMode,
                deployableProxySpawnOffsetDistance = spawnOffsetDistance,
                deployableProxyTriggerMode = triggerMode,
                deployableProxyMaxCount = maxCount,
                deployableProxyReplaceOldestWhenLimitReached = replaceOldestWhenLimitReached,
                deployableProxyImmediateStrikeOnSpawn = immediateStrikeOnSpawn,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static StatusEffectData CreateVenomshooterPoisonStatus()
        {
            return new StatusEffectData
            {
                effectType = StatusEffectType.DamageOverTime,
                durationSeconds = 4f,
                magnitude = 0f,
                sourceAttackPowerMultiplier = 0.3f,
                tickIntervalSeconds = 1f,
                maxStacks = 5,
                stackGroupKey = VenomshooterPoisonStackGroupKey,
                statusThemeKey = PoisonStatusThemeKey,
                refreshDurationOnReapply = true,
            };
        }

        private static void AddVenomshooterPoisonStacks(ICollection<StatusEffectData> statuses, int stackCount)
        {
            if (statuses == null)
            {
                return;
            }

            for (var i = 0; i < Mathf.Max(0, stackCount); i++)
            {
                statuses.Add(CreateVenomshooterPoisonStatus());
            }
        }

        private static void ConfigureSupportBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.effectType = BasicAttackEffectType.Heal;
            hero.basicAttack.targetType = BasicAttackTargetType.LowestHealthAlly;
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 14f;
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureWindchimeBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 1.05f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(5.6f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 14f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.ThreateningEnemyNearRangedAlly;
            hero.basicAttack.targetPrioritySearchRadius = ScaleRangedHeroDistance(3f);
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.onHitStatusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 3f,
                magnitude = -0.06f,
                maxStacks = 3,
                refreshDurationOnReapply = true,
            });
            hero.debugNotes = "Stage-01 demo hero for Support. Windchime validates reactive backline protection, threatened-ranged target selection, and anti-dive area denial.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureMonkBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.95f;
            hero.basicAttack.attackInterval = 1.05f;
            hero.basicAttack.rangeOverride = 1.9f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Support. Monk validates melee frontline sustain with self-centered burst healing and low-threshold team shielding.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureTidefinBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 0.90f;
            hero.basicAttack.rangeOverride = 1.3f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.PreferredEnemy;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.onHitStatusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 3f,
                magnitude = -0.05f,
                maxStacks = 5,
                refreshDurationOnReapply = true,
            });
            hero.debugNotes = "Stage-01 demo hero for Assassin. Tidefin validates shared basic-attack on-hit debuffs and high-output target suppression.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureBladesmanBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1.2f;
            hero.basicAttack.attackInterval = 1.12f;
            hero.basicAttack.rangeOverride = 1.9f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Warrior. Bladesman validates pre-damage defense shred and fixed-distance line-breaking dash damage.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureLongshotBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 0.74f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(6f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 16f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.debugNotes = "Stage-01 demo hero for Marksman. Longshot replaces the placeholder Focus Shot / Arrow Rain kit.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureRiflemanBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1.15f;
            hero.basicAttack.attackInterval = 1.43f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(6.2f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 18f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.debugNotes = "Stage-01 demo hero for Marksman. Rifleman validates heavy single-shot pacing plus burst-fire sequence and delayed grenade payoff.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureVenomshooterBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.6f;
            hero.basicAttack.attackInterval = 0.9f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(6f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 15f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.onHitStatusEffects.Add(CreateVenomshooterPoisonStatus());
            hero.debugNotes = "Stage-01 demo hero for Marksman. Venomshooter validates same-source poison stack pooling, cross-source poison-theme reading, and chained on-kill area follow-up effects.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureSandemperorBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.8f;
            hero.basicAttack.attackInterval = 1.10f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(5.8666667f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 14f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Mage. Sandemperor validates fixed deployable proxies that only strike off the owner's basic-attack cadence.";
            EditorUtility.SetDirty(hero);
        }

        private static SkillData CreateSupportActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_support_active_heal",
                "Radiant Heal",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetHeal,
                SkillTargetType.LowestHealthAlly,
                ScaleRangedHeroDistance(6f),
                0f,
                1.35f,
                7f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            AddHealEffect(skill, 1.35f);
            var shieldEffect = AddApplyStatusEffectsEffect(skill);
            shieldEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Shield,
                durationSeconds = 4f,
                magnitude = 45f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSupportUltimateSkill(bool overwriteExistingContent)
        {
            return CreateSupportUltimateSkill(overwriteExistingContent, out _);
        }

        private static SkillData CreateSupportUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_support_ultimate_blessing",
                "Sun Blessing",
                SkillSlotType.Ultimate,
                SkillType.AreaHeal,
                SkillTargetType.LowestHealthAlly,
                ScaleRangedHeroDistance(6f),
                ScaleRangedHeroDistance(5f),
                4.8f,
                0f,
                2,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            skill.targetType = SkillTargetType.LowestHealthAlly;
            skill.castRange = ScaleRangedHeroDistance(6f);
            skill.areaRadius = ScaleRangedHeroDistance(5f);
            skill.allowsSelfCast = false;
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SunpriestUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectHeal, PersistentAreaTargetType.Allies, 4.8f, skill.areaRadius, 5f, 1f, false);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ResetUltimateDecision(SkillData skill)
        {
            if (skill.ultimateDecision == null)
            {
                skill.ultimateDecision = new UltimateDecisionData();
            }

            if (skill.ultimateDecision.primaryCondition == null)
            {
                skill.ultimateDecision.primaryCondition = new UltimateConditionData();
            }

            if (skill.ultimateDecision.secondaryCondition == null)
            {
                skill.ultimateDecision.secondaryCondition = new UltimateConditionData();
            }

            if (skill.ultimateDecision.fallback == null)
            {
                skill.ultimateDecision.fallback = new UltimateFallbackData();
            }

            if (skill.ultimateDecision.fallback.alternatePrimaryCondition == null)
            {
                skill.ultimateDecision.fallback.alternatePrimaryCondition = new UltimateConditionData();
            }

            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;

            ResetUltimateCondition(skill.ultimateDecision.primaryCondition);
            ResetUltimateCondition(skill.ultimateDecision.secondaryCondition);
            ResetUltimateCondition(skill.ultimateDecision.fallback.alternatePrimaryCondition);

            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.None;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = -1f;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.secondaryOverrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.secondaryOverrideHealthPercentThreshold = -1f;
        }

        private static void ResetActionSequence(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.actionSequence == null)
            {
                skill.actionSequence = new CombatActionSequenceData();
            }

            skill.actionSequence.enabled = false;
            skill.actionSequence.payloadType = CombatActionSequencePayloadType.BasicAttack;
            skill.actionSequence.repeatMode = CombatActionSequenceRepeatMode.FixedCount;
            skill.actionSequence.repeatCount = 1;
            skill.actionSequence.durationSeconds = 1f;
            skill.actionSequence.intervalSeconds = 0.25f;
            skill.actionSequence.windupSeconds = 0f;
            skill.actionSequence.recoverySeconds = 0f;
            skill.actionSequence.temporaryBasicAttackRangeOverride = 0f;
            skill.actionSequence.temporarySkillCastRangeOverride = 0f;
            skill.actionSequence.targetRefreshMode = CombatActionSequenceTargetRefreshMode.RefreshOnInvalid;
            skill.actionSequence.interruptFlags =
                CombatActionSequenceInterruptFlags.HardControl | CombatActionSequenceInterruptFlags.ForcedMovement;
        }

        private static void ResetPassiveSkillData(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.passiveSkill == null)
            {
                skill.passiveSkill = new PassiveSkillData();
            }

            skill.passiveSkill.missingHealthAttackPowerRatio = 0f;
            skill.passiveSkill.maxAttackPowerBonus = 0f;
        }

        private static void ResetTemporaryOverride(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.temporaryOverride == null)
            {
                skill.temporaryOverride = new SkillTemporaryOverrideData();
            }

            skill.temporaryOverride.durationSeconds = 0f;
            skill.temporaryOverride.lifestealRatio = 0f;
            skill.temporaryOverride.visualScaleMultiplier = 1f;
        }

        private static void ResetReactiveGuard(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.reactiveGuard == null)
            {
                skill.reactiveGuard = new ReactiveGuardData();
            }

            skill.reactiveGuard.enabled = false;
            skill.reactiveGuard.durationSeconds = 0f;
            skill.reactiveGuard.triggerRadius = 0f;
            skill.reactiveGuard.effectRadius = 0f;
            skill.reactiveGuard.maxTriggerCount = 1;
            skill.reactiveGuard.forcedMovementDistance = 0f;
            skill.reactiveGuard.forcedMovementDurationSeconds = 0f;
            skill.reactiveGuard.forcedMovementPeakHeight = 0f;
            skill.reactiveGuard.healProtectedHeroPerSuccessfulKnockUp = 0f;
            skill.reactiveGuard.onTriggerStatusEffects.Clear();
        }

        private static void ResetUltimateCondition(UltimateConditionData condition)
        {
            condition.conditionType = UltimateConditionType.None;
            condition.searchRadius = 0f;
            condition.requiredUnitCount = 1;
            condition.healthPercentThreshold = 1f;
            condition.durationSeconds = 0f;
            condition.highValueTargetType = HighValueTargetType.None;
            condition.heroClassFilter = HeroClass.Assassin;
            condition.statusEffectTypeFilter = StatusEffectType.None;
            condition.statusThemeKey = string.Empty;
            condition.minimumStatusStacks = 1;
            condition.requireTargetInCastRange = true;
        }

        private static bool ShouldPreserveExistingAsset(bool overwriteExistingContent, bool existedBefore)
        {
            return !overwriteExistingContent && existedBefore;
        }

        private static SkillData ConfigureSkybreakerUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 5f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            ApplyCountFallback(skill, 35f, 2, 50f, 1);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureMageUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.targetType = SkillTargetType.Self;
            skill.castRange = 0f;
            skill.allowsSelfCast = true;
            skill.areaRadius = ScaleRangedHeroDistance(6f);
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MageUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            skill.effects.Clear();
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectDamage, PersistentAreaTargetType.Enemies, 3.3f, skill.areaRadius, 5f, 1f, false);

            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 30f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 2;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.secondaryOverrideRequiredUnitCount = 1;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureFrostmageUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.EnemyDensestPosition;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            ApplyCountFallback(skill, 30f, 2, 45f, 1);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureSandemperorUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.None;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.secondaryOverrideRequiredUnitCount = 0;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureShadowstepUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AnyPass;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.SelfLowHealth;
            skill.ultimateDecision.primaryCondition.searchRadius = 0f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.6f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyLowHealthInRange;
            // Stage-01 has no dedicated "primary target HP <= X%" ultimate condition. We use
            // a tiny search radius so EnemyLowHealthInRange effectively checks the selected
            // nearest target itself instead of nearby units.
            skill.ultimateDecision.secondaryCondition.searchRadius = 0.25f;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.secondaryCondition.healthPercentThreshold = 0.7f;
            skill.ultimateDecision.secondaryCondition.requireTargetInCastRange = true;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 35f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = 1f;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.secondaryOverrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.secondaryOverrideHealthPercentThreshold = -1f;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateMonkActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_monk_active_renewingpulse",
                "Renewing Pulse",
                SkillSlotType.ActiveSkill,
                SkillType.AreaHeal,
                SkillTargetType.Self,
                0f,
                4.5f,
                0.9f,
                8f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: self-centered burst heal for nearby allies.";
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 4.5f;
            skill.cooldownSeconds = 8f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();

            var healEffect = AddHealEffect(skill, 0.9f);
            healEffect.targetMode = SkillEffectTargetMode.AlliesInRadiusAroundCaster;
            healEffect.radiusOverride = skill.areaRadius;

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateMonkUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_monk_ultimate_guardianmantra",
                "Guardian Mantra",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.Self,
                0f,
                6.8f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: self-centered group shield for nearby allies.";
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 6.8f;
            skill.cooldownSeconds = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();

            var shieldEffect = AddApplyStatusEffectsEffect(skill);
            shieldEffect.targetMode = SkillEffectTargetMode.AlliesInRadiusAroundCaster;
            shieldEffect.radiusOverride = skill.areaRadius;
            shieldEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Shield,
                durationSeconds = 5f,
                magnitude = 130f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureTankUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.AllAllies;
            skill.castRange = 6f;
            skill.areaRadius = 6f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();
            var effect = AddApplyStatusEffectsEffect(skill);
            effect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 8f,
                magnitude = 2.5f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.EnemyDensestPosition;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.AllyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 6f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 2;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureSupportUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.ultimateDecision.targetingType = UltimateTargetingType.LowestHealthAllyInRange;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AllMustPass;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.AllyLowHealthInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Mathf.Max(skill.castRange, 5f);
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.55f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.AllyCountInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = Mathf.Max(skill.areaRadius, 5f);
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            ApplyHealthFallback(skill, 30f, 0.7f, 45f, 0.85f);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureMonkUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AllMustPass;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.AllyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.AllyLowHealthInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.healthPercentThreshold = 0.7f;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = 0.8f;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureWindchimeUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 2;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureLongshotUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.LowestHealthEnemy;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;

            ResetActionSequence(skill);
            skill.actionSequence.enabled = true;
            skill.actionSequence.payloadType = CombatActionSequencePayloadType.BasicAttack;
            skill.actionSequence.repeatMode = CombatActionSequenceRepeatMode.FixedCount;
            skill.actionSequence.repeatCount = 20;
            skill.actionSequence.durationSeconds = 5f;
            skill.actionSequence.intervalSeconds = 0.25f;
            skill.actionSequence.windupSeconds = 0f;
            skill.actionSequence.recoverySeconds = 0f;
            skill.actionSequence.temporaryBasicAttackRangeOverride = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.actionSequence.temporarySkillCastRangeOverride = 0f;
            skill.actionSequence.targetRefreshMode = CombatActionSequenceTargetRefreshMode.RefreshOnInvalid;
            skill.actionSequence.interruptFlags =
                CombatActionSequenceInterruptFlags.HardControl | CombatActionSequenceInterruptFlags.ForcedMovement;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.CurrentTarget;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyLowHealthInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 0.1f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.65f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = ScaleRangedHeroDistance(6f);
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AnyPass;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 40f;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = 1f;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureVenomshooterUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AnyPass;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyWithStatusInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.primaryCondition.statusEffectTypeFilter = StatusEffectType.DamageOverTime;
            skill.ultimateDecision.primaryCondition.statusThemeKey = PoisonStatusThemeKey;
            skill.ultimateDecision.primaryCondition.minimumStatusStacks = 1;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyLowHealthWithStatusInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.healthPercentThreshold = 0.5f;
            skill.ultimateDecision.secondaryCondition.statusEffectTypeFilter = StatusEffectType.DamageOverTime;
            skill.ultimateDecision.secondaryCondition.statusThemeKey = PoisonStatusThemeKey;
            skill.ultimateDecision.secondaryCondition.minimumStatusStacks = 1;
            ApplyCountFallback(skill, 30f, 2, 45f, 1);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateWindchimeUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_windchime_ultimate_stillwinddomain",
                "Stillwind Domain",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.ThreatenedRangedAllyOrEnemyDensestAnchor,
                ScaleRangedHeroDistance(7f),
                ScaleRangedHeroDistance(4.3333335f),
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: anti-engage field that knocks enemies up, shields allies, and pulses attack-power and move-speed debuffs.";
            skill.targetType = SkillTargetType.ThreatenedRangedAllyOrEnemyDensestAnchor;
            skill.fallbackTargetType = SkillTargetType.DensestEnemyArea;
            skill.targetPrioritySearchRadius = ScaleRangedHeroDistance(3f);
            skill.targetPriorityRequiredUnitCount = 2;
            skill.castRange = ScaleRangedHeroDistance(7f);
            skill.areaRadius = ScaleRangedHeroDistance(4.3333335f);
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindchimeUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;

            var enemyKnockUp = AddApplyStatusEffectsEffect(skill);
            enemyKnockUp.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundPrimaryTarget;
            enemyKnockUp.radiusOverride = skill.areaRadius;
            enemyKnockUp.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = 0.75f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            var allyShield = AddApplyStatusEffectsEffect(skill);
            allyShield.targetMode = SkillEffectTargetMode.AlliesInRadiusAroundPrimaryTarget;
            allyShield.radiusOverride = skill.areaRadius;
            allyShield.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Shield,
                durationSeconds = 4f,
                magnitude = 90f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            var areaEffect = AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.None, PersistentAreaTargetType.Enemies, 0f, skill.areaRadius, 5f, 1f, false);
            areaEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.MoveSpeedModifier,
                durationSeconds = 1.2f,
                magnitude = -0.3f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            areaEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 1.2f,
                magnitude = -0.2f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateWindchimeActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_windchime_active_echocanopy",
                "Echo Canopy",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.ThreatenedRangedAlly,
                ScaleRangedHeroDistance(6.133333f),
                0f,
                0f,
                8f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: single-target anti-dive protection with a one-shot reactive knock-up, knockback, and per-knock-up healing.";
            skill.targetType = SkillTargetType.ThreatenedRangedAlly;
            skill.fallbackTargetType = SkillTargetType.LowestHealthRangedAlly;
            skill.targetPrioritySearchRadius = ScaleRangedHeroDistance(2.333333f);
            skill.targetPriorityRequiredUnitCount = 1;
            skill.castRange = ScaleRangedHeroDistance(6.133333f);
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var shieldEffect = AddApplyStatusEffectsEffect(skill);
            shieldEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Shield,
                durationSeconds = 3f,
                magnitude = 55f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            var moveSpeedEffect = AddApplyStatusEffectsEffect(skill);
            moveSpeedEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.MoveSpeedModifier,
                durationSeconds = 2.5f,
                magnitude = 0.35f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetReactiveGuard(skill);
            skill.reactiveGuard.enabled = true;
            skill.reactiveGuard.durationSeconds = 3f;
            skill.reactiveGuard.triggerRadius = 2.4f;
            skill.reactiveGuard.effectRadius = 2.4f;
            skill.reactiveGuard.maxTriggerCount = 1;
            skill.reactiveGuard.forcedMovementDistance = 1.8f;
            skill.reactiveGuard.forcedMovementDurationSeconds = 0.25f;
            skill.reactiveGuard.forcedMovementPeakHeight = 0.7f;
            skill.reactiveGuard.healProtectedHeroPerSuccessfulKnockUp = 15f;
            skill.reactiveGuard.onTriggerStatusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = 0.6f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateShieldwardenActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_shieldwarden_active_wardenscall",
                "Warden's Call",
                SkillSlotType.ActiveSkill,
                SkillType.Taunt,
                SkillTargetType.PriorityEnemyHeroClass,
                8f,
                0f,
                0f,
                8f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.targetType = SkillTargetType.PriorityEnemyHeroClass;
            skill.preferredEnemyHeroClass = HeroClass.Assassin;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = 8f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Taunt,
                durationSeconds = 1.5f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateShieldwardenUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_shieldwarden_ultimate_lastbastion",
                "Last Bastion",
                SkillSlotType.Ultimate,
                SkillType.Taunt,
                SkillTargetType.AllEnemies,
                0f,
                0f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyShieldwardenUltimateBaseConfiguration(skill);

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureShieldwardenUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyShieldwardenUltimateBaseConfiguration(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 6f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyHeroClassInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = 6f;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.secondaryCondition.heroClassFilter = HeroClass.Assassin;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 2;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyShieldwardenUltimateBaseConfiguration(SkillData skill)
        {
            skill.skillType = SkillType.Taunt;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var tauntEffect = AddApplyStatusEffectsEffect(skill);
            tauntEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Taunt,
                durationSeconds = 3f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            var selfBuffEffect = AddApplyStatusEffectsEffect(skill);
            selfBuffEffect.targetMode = SkillEffectTargetMode.Caster;
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 4f,
                magnitude = 2.5f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
        }

        private static SkillData ConfigureRiflemanUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.DensestEnemyArea;
            skill.castRange = ScaleRangedHeroDistance(7f);
            skill.areaRadius = ScaleRangedHeroDistance(4f);
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanUltimateProjectileVfxPrefabPath);
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.ThrownProjectile;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.EnemyDensestPosition;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            ApplyCountFallback(skill, 40f, 2, 52f, 1);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyCountFallback(SkillData skill, float firstTriggerSeconds, int firstRequiredUnitCount, float secondTriggerSeconds, int secondRequiredUnitCount)
        {
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = firstTriggerSeconds;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = firstRequiredUnitCount;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = secondTriggerSeconds;
            skill.ultimateDecision.fallback.secondaryOverrideRequiredUnitCount = secondRequiredUnitCount;
        }

        private static void ApplyHealthFallback(SkillData skill, float firstTriggerSeconds, float firstHealthThreshold, float secondTriggerSeconds, float secondHealthThreshold)
        {
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = firstTriggerSeconds;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = firstHealthThreshold;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = secondTriggerSeconds;
            skill.ultimateDecision.fallback.secondaryOverrideHealthPercentThreshold = secondHealthThreshold;
        }

        private static SkillData CreateStunSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            float stunDuration,
            bool overwriteExistingContent)
        {
            var skill = CreateSkill(skillId, displayName, slotType, SkillType.Stun, areaRadius > 0f ? SkillTargetType.DensestEnemyArea : SkillTargetType.NearestEnemy, castRange, areaRadius, powerMultiplier, cooldownSeconds, areaRadius > 0f ? 2 : 1, overwriteExistingContent, out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            AddDamageEffect(skill, powerMultiplier);
            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Stun,
                durationSeconds = stunDuration,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateKnockUpSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            float knockUpDuration,
            float forcedMovementDistance,
            float forcedMovementDurationSeconds,
            float forcedMovementPeakHeight,
            bool overwriteExistingContent)
        {
            return CreateKnockUpSkill(
                skillId,
                displayName,
                slotType,
                castRange,
                areaRadius,
                powerMultiplier,
                cooldownSeconds,
                knockUpDuration,
                forcedMovementDistance,
                forcedMovementDurationSeconds,
                forcedMovementPeakHeight,
                overwriteExistingContent,
                out _);
        }

        private static SkillData CreateKnockUpSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            float knockUpDuration,
            float forcedMovementDistance,
            float forcedMovementDurationSeconds,
            float forcedMovementPeakHeight,
            bool overwriteExistingContent,
            out bool existedBefore)
        {
            var skill = CreateSkill(skillId, displayName, slotType, SkillType.KnockUp, areaRadius > 0f ? SkillTargetType.DensestEnemyArea : SkillTargetType.NearestEnemy, castRange, areaRadius, powerMultiplier, cooldownSeconds, areaRadius > 0f ? 2 : 1, overwriteExistingContent, out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            AddDamageEffect(skill, powerMultiplier);
            AddForcedMovementEffect(skill, forcedMovementDistance, forcedMovementDurationSeconds, forcedMovementPeakHeight);
            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = knockUpDuration,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBuffSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            SkillTargetType targetType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            StatusEffectType effectType,
            float durationSeconds,
            float magnitude,
            bool overwriteExistingContent)
        {
            return CreateBuffSkill(
                skillId,
                displayName,
                slotType,
                targetType,
                castRange,
                areaRadius,
                powerMultiplier,
                cooldownSeconds,
                effectType,
                durationSeconds,
                magnitude,
                overwriteExistingContent,
                out _);
        }

        private static SkillData CreateBuffSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            SkillTargetType targetType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            StatusEffectType effectType,
            float durationSeconds,
            float magnitude,
            bool overwriteExistingContent,
            out bool existedBefore)
        {
            var skill = CreateSkill(skillId, displayName, slotType, SkillType.Buff, targetType, castRange, areaRadius, powerMultiplier, cooldownSeconds, 1, overwriteExistingContent, out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            var effect = AddApplyStatusEffectsEffect(skill);
            effect.statusEffects.Add(new StatusEffectData
            {
                effectType = effectType,
                durationSeconds = durationSeconds,
                magnitude = magnitude,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateTauntSkill(
            string skillId,
            string displayName,
            SkillSlotType slotType,
            float castRange,
            float areaRadius,
            float powerMultiplier,
            float cooldownSeconds,
            float tauntDuration,
            bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                skillId,
                displayName,
                slotType,
                SkillType.Taunt,
                areaRadius > 0f ? SkillTargetType.DensestEnemyArea : SkillTargetType.NearestEnemy,
                castRange,
                areaRadius,
                powerMultiplier,
                cooldownSeconds,
                areaRadius > 0f ? 2 : 1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.effects.Clear();
            AddDamageEffect(skill, powerMultiplier);
            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Taunt,
                durationSeconds = tauntDuration,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateIronwallActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_tank_active_shieldbash",
                "Bulwark Bond",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.ThreatenedAlly,
                4.5f,
                0f,
                0f,
                8f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: short-area damage share protection for nearby allies.";
            skill.targetType = SkillTargetType.ThreatenedAlly;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.targetPrioritySearchRadius = 6f;
            skill.targetPriorityRequiredUnitCount = 1;
            skill.castRange = 4.5f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 4f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.targetMode = SkillEffectTargetMode.OtherAlliesInRadiusAroundCaster;
            statusEffect.radiusOverride = 4.5f;
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DamageShare,
                durationSeconds = 4f,
                magnitude = 0.35f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void EnsureBasicAttackStatusList(BasicAttackData basicAttack)
        {
            if (basicAttack != null && basicAttack.onHitStatusEffects == null)
            {
                basicAttack.onHitStatusEffects = new System.Collections.Generic.List<StatusEffectData>();
            }
        }

        private static BattleInputConfig CreateBattleInput(
            string assetName,
            bool includeAssassinOnRedTeam,
            bool enableSkills,
            bool overwriteExistingContent,
            HeroDefinition warrior,
            HeroDefinition mage,
            HeroDefinition assassin,
            HeroDefinition tank,
            HeroDefinition support,
            HeroDefinition marksman)
        {
            var assetPath = $"{BattlesFolder}/{assetName}.asset";
            if (!overwriteExistingContent && TryLoadAsset(assetPath, out BattleInputConfig existingInput))
            {
                return existingInput;
            }

            var input = LoadOrCreateAsset<BattleInputConfig>(assetPath);
            input.regulationDurationSeconds = 60f;
            input.respawnDelaySeconds = 5f;
            input.enableBattleEventLogs = true;
            input.enableSkills = enableSkills;
            input.arenaId = Stage01ArenaSpec.ArenaId;

            input.blueTeam.side = TeamSide.Blue;
            input.blueTeam.heroes.Clear();
            input.blueTeam.heroes.AddRange(new[] { tank, warrior, mage, support, marksman });

            input.redTeam.side = TeamSide.Red;
            input.redTeam.heroes.Clear();
            input.redTeam.heroes.AddRange(includeAssassinOnRedTeam
                ? new[] { tank, assassin, mage, support, marksman }
                : new[] { tank, warrior, mage, support, marksman });

            EditorUtility.SetDirty(input);

            var resourcesAssetPath = $"{ResourcesDemoFolder}/{assetName}.asset";
            var resourcesInput = AssetDatabase.LoadAssetAtPath<BattleInputConfig>(resourcesAssetPath);
            if (resourcesInput == null)
            {
                AssetDatabase.CopyAsset(assetPath, resourcesAssetPath);
                resourcesInput = AssetDatabase.LoadAssetAtPath<BattleInputConfig>(resourcesAssetPath);
            }

            if (resourcesInput != null)
            {
                resourcesInput.regulationDurationSeconds = input.regulationDurationSeconds;
                resourcesInput.respawnDelaySeconds = input.respawnDelaySeconds;
                resourcesInput.enableBattleEventLogs = input.enableBattleEventLogs;
                resourcesInput.enableSkills = input.enableSkills;
                resourcesInput.arenaId = input.arenaId;

                resourcesInput.blueTeam.side = input.blueTeam.side;
                resourcesInput.blueTeam.heroes.Clear();
                resourcesInput.blueTeam.heroes.AddRange(input.blueTeam.heroes);

                resourcesInput.redTeam.side = input.redTeam.side;
                resourcesInput.redTeam.heroes.Clear();
                resourcesInput.redTeam.heroes.AddRange(input.redTeam.heroes);

                EditorUtility.SetDirty(resourcesInput);
            }

            return input;
        }

        private static HeroCatalogData CreateHeroCatalog(params HeroDefinition[] heroes)
        {
            var catalog = LoadOrCreateAsset<HeroCatalogData>(DefaultHeroCatalogAssetPath);
            catalog.heroes.Clear();
            if (heroes != null)
            {
                for (var i = 0; i < heroes.Length; i++)
                {
                    var hero = heroes[i];
                    if (hero == null || catalog.heroes.Contains(hero))
                    {
                        continue;
                    }

                    catalog.heroes.Add(hero);
                }
            }

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static string GetHeroAssetPath(string heroId, string displayName)
        {
            return $"{HeroesRootFolder}/{heroId}/{displayName}.asset";
        }

        private static string GetSkillAssetPath(string skillId, string displayName)
        {
            var ownerId = GetSkillOwnerId(skillId);
            var assetName = GetLegacySkillAssetNameOverride(skillId) ?? displayName;
            return $"{SkillsRootFolder}/{ownerId}/{assetName}.asset";
        }

        private static string GetLegacySkillAssetNameOverride(string skillId)
        {
            return skillId switch
            {
                "skill_bladesman_active_rendingslash" => "Rending Slash",
                "skill_bladesman_ultimate_flyingswallowsever" => "Flying Swallow Sever",
                "skill_tank_active_shieldbash" => "Shield Bash",
                "skill_tank_ultimate_ironoath" => "Ground Lock",
                _ => null,
            };
        }

        private static string GetSkillOwnerId(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return "shared";
            }

            var explicitOwnerId = GetExplicitSkillOwnerIdOverride(skillId);
            if (!string.IsNullOrWhiteSpace(explicitOwnerId))
            {
                return explicitOwnerId;
            }

            var parts = skillId.Split('_');
            return parts.Length >= 2
                ? $"{parts[1]}_001_{GetDefaultHeroName(parts[1])}"
                : "shared";
        }

        private static string GetExplicitSkillOwnerIdOverride(string skillId)
        {
            return skillId switch
            {
                "skill_bladesman_active_rendingslash" => "warrior_002_bladesman",
                "skill_bladesman_ultimate_flyingswallowsever" => "warrior_002_bladesman",
                "skill_frostmage_active_frostburst" => "mage_002_frostmage",
                "skill_frostmage_ultimate_blizzard" => "mage_002_frostmage",
                "skill_tidefin_active_tidalpounce" => "assassin_002_tidefin",
                "skill_tidefin_ultimate_ruintide" => "assassin_002_tidefin",
                "skill_shieldwarden_active_wardenscall" => "tank_002_shieldwarden",
                "skill_shieldwarden_ultimate_lastbastion" => "tank_002_shieldwarden",
                "skill_windchime_active_echocanopy" => "support_002_windchime",
                "skill_windchime_ultimate_stillwinddomain" => "support_002_windchime",
                "skill_monk_active_renewingpulse" => "support_003_monk",
                "skill_monk_ultimate_guardianmantra" => "support_003_monk",
                "skill_rifleman_active_burstfire" => "marksman_002_rifleman",
                "skill_rifleman_ultimate_fraggrenade" => "marksman_002_rifleman",
                "skill_venomshooter_active_poisonmist" => "marksman_003_venomshooter",
                "skill_venomshooter_ultimate_venomdetonation" => "marksman_003_venomshooter",
                "skill_sandemperor_active_raisesandguard" => "mage_003_sandemperor",
                "skill_sandemperor_ultimate_imperialencirclement" => "mage_003_sandemperor",
                "skill_berserker_active_bloodfury" => "warrior_003_berserker",
                "skill_berserker_ultimate_titanrage" => "warrior_003_berserker",
                _ => null,
            };
        }

        private static string GetDefaultHeroName(string heroClassKey)
        {
            switch (heroClassKey)
            {
                case "warrior":
                    return "skybreaker";
                case "mage":
                    return "firemage";
                case "assassin":
                    return "shadowstep";
                case "tank":
                    return "ironwall";
                case "support":
                    return "sunpriest";
                case "marksman":
                    return "longshot";
                default:
                    return "shared";
            }
        }

        private static void CreateFlowScenes(bool overwriteExistingContent)
        {
            CreateSceneWithController<MainMenuSceneController>(MainMenuScenePath, "MainMenuRoot", overwriteExistingContent);
            CreateSceneWithController<HeroSelectSceneController>(HeroSelectScenePath, "HeroSelectRoot", overwriteExistingContent);
            CreateSceneWithController<ResultSceneController>(ResultScenePath, "ResultRoot", overwriteExistingContent);
        }

        private static void CreateSceneWithController<T>(string scenePath, string rootName, bool overwriteExistingContent) where T : Component
        {
            if (!overwriteExistingContent && File.Exists(scenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(rootName);
            root.AddComponent<T>();
            root.transform.position = Vector3.zero;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateBattleScene(BattleInputConfig battleInput, bool overwriteExistingContent)
        {
            CreateBattleSceneWithInput(
                BattleScenePath,
                battleInput,
                overwriteExistingContent,
                useDevelopmentBootstrap: false,
                fallbackResourcesPath: "Stage01Demo/Stage01DemoBattleInput");
            EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            AddScenesToBuildSettings();
        }

        private static void CreateBasicAttackOnlyBattleScene(BattleInputConfig battleInput, bool overwriteExistingContent)
        {
            CreateBattleSceneWithInput(
                BasicAttackOnlyBattleScenePath,
                battleInput,
                overwriteExistingContent,
                useDevelopmentBootstrap: true,
                fallbackResourcesPath: "Stage01Demo/Stage01BasicAttackOnlyBattleInput");
        }

        private static void CreateBattleSceneWithInput(
            string scenePath,
            BattleInputConfig battleInput,
            bool overwriteExistingContent,
            bool useDevelopmentBootstrap,
            string fallbackResourcesPath)
        {
            if (!overwriteExistingContent && File.Exists(scenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("BattleRoot");
            if (useDevelopmentBootstrap)
            {
                var debugBootstrap = root.AddComponent<BattleDebugSceneBootstrap>();
                SetPrivateObjectReference(debugBootstrap, "defaultInputConfig", battleInput);
                SetPrivateBool(debugBootstrap, "startBattleOnPlay", true);
                SetPrivateBool(debugBootstrap, "addBattleHud", true);
                SetPrivateBool(debugBootstrap, "addBattleView", true);
                SetPrivateBool(debugBootstrap, "addDebugHud", true);
                SetPrivateBool(debugBootstrap, "addDebugLogForwarder", true);
                SetPrivateString(debugBootstrap, "fallbackResourcesPath", fallbackResourcesPath);
            }
            else
            {
                var battleBootstrap = root.AddComponent<BattleSceneBootstrap>();
                SetPrivateObjectReference(battleBootstrap, "defaultInputConfig", battleInput);
                SetPrivateBool(battleBootstrap, "startBattleOnPlay", true);
                SetPrivateBool(battleBootstrap, "addBattleHud", true);
                SetPrivateBool(battleBootstrap, "addBattleView", true);
                SetPrivateBool(battleBootstrap, "addBattleEventLogRecorder", true);
                SetPrivateString(battleBootstrap, "fallbackResourcesPath", fallbackResourcesPath);
                SetPrivateString(battleBootstrap, "resultSceneName", "Result");
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            AddScenesToBuildSettings();
        }

        private static void AddScenesToBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(HeroSelectScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true),
                new EditorBuildSettingsScene(ResultScenePath, true),
                new EditorBuildSettingsScene(BasicAttackOnlyBattleScenePath, true),
            };
        }

        private static void SetPrivateObjectReference(UnityEngine.Object targetObject, string propertyName, UnityEngine.Object value)
        {
            var so = new SerializedObject(targetObject);
            var property = so.FindProperty(propertyName);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        private static void SetPrivateBool(UnityEngine.Object targetObject, string propertyName, bool value)
        {
            var so = new SerializedObject(targetObject);
            var property = so.FindProperty(propertyName);
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        private static void SetPrivateString(UnityEngine.Object targetObject, string propertyName, string value)
        {
            var so = new SerializedObject(targetObject);
            var property = so.FindProperty(propertyName);
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            var directory = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                var parts = directory.Split('/');
                var current = parts[0];
                for (var i = 1; i < parts.Length; i++)
                {
                    EnsureFolder(current, parts[i]);
                    current += "/" + parts[i];
                }
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static bool TryLoadAsset<T>(string assetPath, out T asset) where T : ScriptableObject
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return asset != null;
        }
    }
}
