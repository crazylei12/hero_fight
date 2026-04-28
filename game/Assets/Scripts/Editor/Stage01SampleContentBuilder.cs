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
        private const string DefaultAthleteRosterAssetPath = ResourcesDemoFolder + "/Stage02AthleteRoster.asset";
        private const string BladesmanHeroAssetPath = HeroesRootFolder + "/warrior_002_bladesman/Bladesman.asset";
        private const string BladesmanActiveSkillAssetPath = SkillsRootFolder + "/warrior_002_bladesman/Rending Slash.asset";
        private const string BladesmanUltimateSkillAssetPath = SkillsRootFolder + "/warrior_002_bladesman/Flying Swallow Sever.asset";
        private const string WindchimeHeroAssetPath = HeroesRootFolder + "/support_002_windchime/Windchime.asset";
        private const string WindchimeActiveSkillAssetPath = SkillsRootFolder + "/support_002_windchime/Echo Canopy.asset";
        private const string WindchimeUltimateSkillAssetPath = SkillsRootFolder + "/support_002_windchime/Stillwind Domain.asset";
        private const string MonkHeroAssetPath = HeroesRootFolder + "/support_003_monk/Monk.asset";
        private const string MonkActiveSkillAssetPath = SkillsRootFolder + "/support_003_monk/Renewing Pulse.asset";
        private const string MonkUltimateSkillAssetPath = SkillsRootFolder + "/support_003_monk/Guardian Mantra.asset";
        private const string ShrinemaidenHeroAssetPath = HeroesRootFolder + "/support_004_shrinemaiden/Shrinemaiden.asset";
        private const string ShrinemaidenActiveSkillAssetPath = SkillsRootFolder + "/support_004_shrinemaiden/Prayer Bloom.asset";
        private const string ShrinemaidenUltimateSkillAssetPath = SkillsRootFolder + "/support_004_shrinemaiden/Twin Rite Totem.asset";
        private const string ChefHeroAssetPath = HeroesRootFolder + "/support_005_chef/Chef.asset";
        private const string ChefActiveSkillAssetPath = SkillsRootFolder + "/support_005_chef/Daily Special.asset";
        private const string ChefUltimateSkillAssetPath = SkillsRootFolder + "/support_005_chef/Grand Feast.asset";
        private const string CommanderHeroAssetPath = HeroesRootFolder + "/support_006_commander/Commander.asset";
        private const string CommanderActiveSkillAssetPath = SkillsRootFolder + "/support_006_commander/Battle Orders.asset";
        private const string CommanderUltimateSkillAssetPath = SkillsRootFolder + "/support_006_commander/Focus Fire Command.asset";
        private const string SandemperorHeroAssetPath = HeroesRootFolder + "/mage_003_sandemperor/Sandemperor.asset";
        private const string SandemperorActiveSkillAssetPath = SkillsRootFolder + "/mage_003_sandemperor/Raise Sandguard.asset";
        private const string SandemperorUltimateSkillAssetPath = SkillsRootFolder + "/mage_003_sandemperor/Imperial Encirclement.asset";
        private const string MonkActiveImpactVfxPrefabPath = "Assets/Prefabs/VFX/Skills/MonkRenewingPulseBurst.prefab";
        private const string MonkUltimateImpactVfxPrefabPath = "Assets/Prefabs/VFX/Skills/MonkGuardianMantraBubbleImpact.prefab";
        private const string AssassinPrefabPath = "Assets/Prefabs/Heroes/assassin_001_shadowstep/Shadowstep.prefab";
        private const string TidefinPrefabPath = "Assets/Prefabs/Heroes/assassin_002_tidefin/Tidefin.prefab";
        private const string ButcherPrefabPath = "Assets/Prefabs/Heroes/assassin_003_butcher/Butcher.prefab";
        private const string LonerPrefabPath = "Assets/Prefabs/Heroes/assassin_004_loner/Loner.prefab";
        private const string MarksmanPrefabPath = "Assets/Prefabs/Heroes/marksman_001_longshot/Longshot.prefab";
        private const string RiflemanPrefabPath = "Assets/Prefabs/Heroes/marksman_002_rifleman/Rifleman.prefab";
        private const string VenomshooterPrefabPath = "Assets/Prefabs/Heroes/marksman_003_venomshooter/Venomshooter.prefab";
        private const string BoomerangerPrefabPath = "Assets/Prefabs/Heroes/marksman_004_boomeranger/Boomeranger.prefab";
        private const string SniperPrefabPath = "Assets/Prefabs/Heroes/marksman_002_rifleman/Rifleman.prefab";
        private const string SupportPrefabPath = "Assets/Prefabs/Heroes/support_001_sunpriest/Sunpriest.prefab";
        private const string WindchimePrefabPath = "Assets/Prefabs/Heroes/support_002_windchime/Windchime.prefab";
        private const string MonkPrefabPath = "Assets/Prefabs/Heroes/support_003_monk/Monk.prefab";
        private const string ShrinemaidenPrefabPath = "Assets/Prefabs/Heroes/support_004_shrinemaiden/ShrinemaidenWunv.prefab";
        private const string ChefPrefabPath = "Assets/Prefabs/Heroes/support_005_chef/Chef.prefab";
        private const string WarriorPrefabPath = "Assets/Prefabs/Heroes/warrior_001_skybreaker/Skybreaker.prefab";
        private const string BladesmanPrefabPath = "Assets/Prefabs/Heroes/warrior_002_bladesman/Bladesman.prefab";
        private const string BerserkerPrefabPath = "Assets/Prefabs/Heroes/warrior_003_berserker/Berserker.prefab";
        private const string SpellbladePrefabPath = "Assets/Prefabs/Heroes/warrior_004_spellblade/Spellblade.prefab";
        private const string FireMagePrefabPath = "Assets/Prefabs/Heroes/mage_001_firemage/FIREMAGE.prefab";
        private const string FireMageSpritePrefabPath = "Assets/Prefabs/Heroes/mage_001_firemage/FireMageSprite.prefab";
        private const string FrostMagePrefabPath = "Assets/Prefabs/Heroes/mage_002_frostmage/Frostmage.prefab";
        private const string LightningmagePrefabPath = "Assets/Prefabs/Heroes/mage_004_lightningmage/Lightningmage.prefab";
        private const string TankPrefabPath = "Assets/Prefabs/Heroes/tank_001_ironwall/Ironwall.prefab";
        private const string ShieldwardenPrefabPath = "Assets/Prefabs/Heroes/tank_002_shieldwarden/Shieldwarden.prefab";
        private const string TidehunterPrefabPath = "Assets/Prefabs/Heroes/tank_003_tidehunter/Tidehunter.prefab";
        private const string MundoPrefabPath = "Assets/Prefabs/Heroes/tank_004_mundo/Mundo.prefab";
        private const string HeroEditorControllerPath = "Assets/HeroEditor4D/Common/Animation/Controller.controller";
        private const string FireMageProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/FireMageBasicAttackProjectile.prefab";
        private const string FrostMageProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/FrostMageBasicAttackProjectile.prefab";
        private const string LongshotProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/LongshotBasicAttackProjectile.prefab";
        private const string BoomerangerBasicProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/BoomerangerWheelProjectile.prefab";
        private const string BoomerangerBounceProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/BoomerangerWheelBounceProjectile.prefab";
        private const string BoomerangerActiveProjectileVfxPrefabPath = "Assets/Prefabs/VFX/Projectiles/BoomerangerReturningWheelProjectile.prefab";
        private const string BoomerangerUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/BoomerangerWheelstormOrbit.prefab";
        private const string ButcherHookChainProjectileVfxPrefabPath = "Assets/Prefabs/VFX/Projectiles/ButcherHookChainProjectile.prefab";
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
        private const string ChefPizzaProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ChefPizzaProjectile.prefab";
        private const string ChefBurgerProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ChefBurgerProjectile.prefab";
        private const string ChefHotdogProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ChefHotdogProjectile.prefab";
        private const string ChefFriesProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ChefFriesProjectile.prefab";
        private const string ChefBigmacProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/ChefBigmacProjectile.prefab";
        private const string SunpriestUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/SunpriestSunBlessingField.prefab";
        private const string WindchimeUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/WindchimeStillwindDomainField.prefab";
        private const string PoisonStatusThemeKey = "poison";
        private const string VenomshooterPoisonStackGroupKey = "venomshooter_poison_pool";
        private const string ShockStatusThemeKey = "shock";
        private const string LightningmageShockStackGroupKey = "lightningmage_shock_pool";
        private const string DemonGreaterFormKey = "greater_demon";
        private static bool autoEnsureScheduled;

        private static float ScaleRangedHeroDistance(float value)
        {
            return value * Stage01ArenaSpec.ArenaScaleMultiplier;
        }

        [MenuItem(OpenMainMenuMenuPath)]
        public static void OpenMainMenuScene()
        {
            EnsureDemoContent();
            OpenSceneForCurrentEditorState(MainMenuScenePath);
        }

        [MenuItem(OpenBattleMenuPath)]
        public static void OpenBattleScene()
        {
            EnsureDemoContent();
            OpenSceneForCurrentEditorState(BattleScenePath);
        }

        [MenuItem(OverwriteDemoContentMenuPath)]
        public static void RegenerateDemoContentFromDefaults()
        {
            if (!CanMutateDemoContent(showDialog: true))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Overwrite Existing Tuning",
                    "This action will overwrite existing hero and skill tuning back to the demo defaults. Continue?",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            GenerateDemoContentInternal(overwriteExistingContent: true, showCompletionDialog: true);
        }

        [InitializeOnLoadMethod]
        private static void ScheduleAutoEnsureDemoContentIfNeeded()
        {
            if (Application.isBatchMode || autoEnsureScheduled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            autoEnsureScheduled = true;
            EditorApplication.delayCall += TryAutoEnsureDemoContentIfNeeded;
        }

        public static void GenerateDemoContent()
        {
            if (!CanMutateDemoContent(showDialog: false))
            {
                return;
            }

            GenerateDemoContentInternal(overwriteExistingContent: false, showCompletionDialog: false);
        }

        private static void GenerateDemoContentInternal(bool overwriteExistingContent, bool showCompletionDialog)
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

            var spellbladeActive = CreateSpellbladeActiveSkill(overwriteExistingContent);
            var spellbladeUltimate = CreateSpellbladeUltimateSkill(overwriteExistingContent, out var spellbladeUltimateExisted);
            var spellblade = CreateHero(
                "warrior_004_spellblade",
                "Spellblade",
                HeroClass.Warrior,
                425f, 43f, 21f, 1f / 1.04f, 4.4f, 0.10f, 1.6f, 1.8f,
                spellbladeActive,
                ConfigureSpellbladeUltimate(spellbladeUltimate, overwriteExistingContent, spellbladeUltimateExisted),
                overwriteExistingContent,
                out var spellbladeHeroExisted,
                HeroTag.Melee, HeroTag.AreaDamage, HeroTag.SustainedDamage);
            ConfigureSpellbladeBasicAttack(spellblade, overwriteExistingContent, spellbladeHeroExisted);
            EnsureHeroSkillReferences(spellblade, spellbladeActive, spellbladeUltimate);
            EnsureHeroBattlePrefabReference(spellblade, LoadBattlePrefab("warrior_004_spellblade", HeroClass.Warrior));

            var trollwarlordActive = CreateTrollWarlordActiveSkill(overwriteExistingContent);
            var trollwarlordUltimate = CreateTrollWarlordUltimateSkill(overwriteExistingContent, out var trollwarlordUltimateExisted);
            var trollwarlord = CreateHero(
                "warrior_005_trollwarlord",
                "TrollWarlord",
                HeroClass.Warrior,
                435f, 39f, 24f, 0.833333f, 4.25f, 0.08f, 1.55f, 2.0f,
                trollwarlordActive,
                ConfigureTrollWarlordUltimate(trollwarlordUltimate, overwriteExistingContent, trollwarlordUltimateExisted),
                overwriteExistingContent,
                out var trollwarlordHeroExisted,
                HeroTag.Melee, HeroTag.SustainedDamage, HeroTag.Buff);
            ConfigureTrollWarlordBasicAttack(trollwarlord, overwriteExistingContent, trollwarlordHeroExisted);
            EnsureHeroSkillReferences(trollwarlord, trollwarlordActive, trollwarlordUltimate);
            EnsureHeroBattlePrefabReference(trollwarlord, LoadBattlePrefab("warrior_005_trollwarlord", HeroClass.Warrior));

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

            var lightningmageActive = CreateLightningmageActiveSkill(overwriteExistingContent);
            var lightningmageUltimateSkill = CreateLightningmageUltimateSkill(overwriteExistingContent, out var lightningmageUltimateExisted);

            var lightningmage = CreateHero(
                "mage_004_lightningmage",
                "Lightningmage",
                HeroClass.Mage,
                290f, 30f, 8f, 1f / 1.05f, 3.75f, 0.10f, 1.7f, ScaleRangedHeroDistance(5.9333333f),
                lightningmageActive,
                ConfigureLightningmageUltimate(lightningmageUltimateSkill, overwriteExistingContent, lightningmageUltimateExisted),
                overwriteExistingContent,
                out var lightningmageHeroExisted,
                HeroTag.Ranged, HeroTag.Control, HeroTag.SustainedDamage);
            ConfigureLightningmageBasicAttack(lightningmage, overwriteExistingContent, lightningmageHeroExisted);
            EnsureHeroSkillReferences(lightningmage, lightningmageActive, lightningmageUltimateSkill);
            EnsureHeroBattlePrefabReference(lightningmage, LoadBattlePrefab("mage_004_lightningmage", HeroClass.Mage));

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
            EnsureHeroSkillReferences(tidefin, tidefinActive, tidefinUltimateSkill);
            EnsureHeroBattlePrefabReference(tidefin, LoadBattlePrefab("assassin_002_tidefin", HeroClass.Assassin));
            if (tidefin?.visualConfig != null)
            {
                tidefin.visualConfig.animatorController = null;
                tidefin.visualConfig.battlePrefabFacesLeftByDefault = false;
                EditorUtility.SetDirty(tidefin);
            }

            var butcherActive = CreateButcherActiveSkill(overwriteExistingContent);
            var butcherUltimateSkill = CreateButcherUltimateSkill(overwriteExistingContent, out var butcherUltimateExisted);

            var butcher = CreateHero(
                "assassin_003_butcher",
                "Butcher",
                HeroClass.Assassin,
                360f, 36f, 16f, 1f / 0.91f, 4.6f, 0.1f, 1.6f, 1.4f,
                butcherActive,
                ConfigureButcherUltimate(butcherUltimateSkill, overwriteExistingContent, butcherUltimateExisted),
                overwriteExistingContent,
                out var butcherHeroExisted,
                HeroTag.Melee, HeroTag.Control, HeroTag.Dive);
            ConfigureButcherBasicAttack(butcher, overwriteExistingContent, butcherHeroExisted);

            var lonerActive = CreateLonerActiveSkill(overwriteExistingContent);
            var lonerUltimateSkill = CreateLonerUltimateSkill(overwriteExistingContent, out _);

            var loner = CreateHero(
                "assassin_004_loner",
                "Loner",
                HeroClass.Assassin,
                305f, 40f, 10f, 1f / 0.83f, 5.3f, 0.16f, 1.7f, 1.35f,
                lonerActive,
                lonerUltimateSkill,
                overwriteExistingContent,
                out var lonerHeroExisted,
                HeroTag.Melee, HeroTag.Dive, HeroTag.SustainedDamage);
            ConfigureLonerBasicAttack(loner, overwriteExistingContent, lonerHeroExisted);
            EnsureHeroSkillReferences(loner, lonerActive, lonerUltimateSkill);
            EnsureHeroBattlePrefabReference(loner, LoadBattlePrefab("assassin_004_loner", HeroClass.Assassin));

            var demonActive = CreateDemonActiveSkill(overwriteExistingContent);
            var demonUltimateSkill = CreateDemonUltimateSkill(overwriteExistingContent, out var demonUltimateExisted);

            var demon = CreateHero(
                "assassin_005_demon",
                "Demon",
                HeroClass.Assassin,
                315f, 42f, 10f, 1f / 0.88f, 5.1f, 0.14f, 1.7f, 1.35f,
                demonActive,
                ConfigureDemonUltimate(demonUltimateSkill, overwriteExistingContent, demonUltimateExisted),
                overwriteExistingContent,
                out var demonHeroExisted,
                HeroTag.Melee, HeroTag.Dive, HeroTag.SustainedDamage);
            ConfigureDemonBasicAttack(demon, overwriteExistingContent, demonHeroExisted);
            ConfigureDemonVisualForms(demon, overwriteExistingContent, demonHeroExisted);
            EnsureHeroSkillReferences(demon, demonActive, demonUltimateSkill);
            EnsureHeroBattlePrefabReference(demon, LoadBattlePrefab("assassin_005_demon", HeroClass.Assassin));

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
            EnsureHeroSkillReferences(tank, tankActive, tankUltimateSkill);
            EnsureHeroBattlePrefabReference(tank, LoadBattlePrefab("tank_001_ironwall", HeroClass.Tank));

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
            EnsureHeroSkillReferences(shieldwarden, shieldwardenActive, shieldwardenUltimateSkill);
            EnsureHeroBattlePrefabReference(shieldwarden, LoadBattlePrefab("tank_002_shieldwarden", HeroClass.Tank));

            var tidehunterActive = CreateTidehunterActiveSkill(overwriteExistingContent);
            var tidehunterUltimateSkill = CreateTidehunterUltimateSkill(overwriteExistingContent, out var tidehunterUltimateExisted);

            var tidehunter = CreateHero(
                "tank_003_tidehunter",
                "Tidehunter",
                HeroClass.Tank,
                580f, 26f, 36f, 0.9f, 3.5f, 0.05f, 1.5f, 1.9f,
                tidehunterActive,
                ConfigureTidehunterUltimate(tidehunterUltimateSkill, overwriteExistingContent, tidehunterUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Control, HeroTag.Buff);
            EnsureHeroSkillReferences(tidehunter, tidehunterActive, tidehunterUltimateSkill);
            EnsureHeroBattlePrefabReference(tidehunter, LoadBattlePrefab("tank_003_tidehunter", HeroClass.Tank));

            var mundoActive = CreateMundoActiveSkill(overwriteExistingContent);
            var mundoUltimateSkill = CreateMundoUltimateSkill(overwriteExistingContent, out var mundoUltimateExisted);

            var mundo = CreateHero(
                "tank_004_mundo",
                "Mundo",
                HeroClass.Tank,
                620f, 22f, 38f, 0.9f, 3.45f, 0.05f, 1.5f, 1.9f,
                mundoActive,
                ConfigureMundoUltimate(mundoUltimateSkill, overwriteExistingContent, mundoUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Heal);
            EnsureHeroSkillReferences(mundo, mundoActive, mundoUltimateSkill);
            EnsureHeroBattlePrefabReference(mundo, LoadBattlePrefab("tank_004_mundo", HeroClass.Tank));

            var blastshieldActive = CreateBlastshieldActiveSkill(overwriteExistingContent);
            var blastshieldUltimateSkill = CreateBlastshieldUltimateSkill(overwriteExistingContent, out var blastshieldUltimateExisted);

            var blastshield = CreateHero(
                "tank_005_blastshield",
                "Blastshield",
                HeroClass.Tank,
                550f, 34f, 38f, 0.9f, 4.0f, 0.05f, 1.5f, 1.8f,
                blastshieldActive,
                ConfigureBlastshieldUltimate(blastshieldUltimateSkill, overwriteExistingContent, blastshieldUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Control, HeroTag.AreaDamage);
            EnsureHeroSkillReferences(blastshield, blastshieldActive, blastshieldUltimateSkill);
            EnsureHeroBattlePrefabReference(blastshield, LoadBattlePrefab("tank_005_blastshield", HeroClass.Tank));

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
            EnsureSkillCastImpactVfxPresentation(
                monkUltimateSkill,
                AssetDatabase.LoadAssetAtPath<GameObject>(MonkUltimateImpactVfxPrefabPath),
                new Vector3(0f, 0.02f, 0f),
                Vector3.zero,
                Vector3.one,
                false,
                true,
                0.18f);

            var shrinemaidenActive = CreateShrinemaidenActiveSkill(overwriteExistingContent);
            var shrinemaidenUltimateSkill = CreateShrinemaidenUltimateSkill(overwriteExistingContent, out _);

            var shrinemaiden = CreateHero(
                "support_004_shrinemaiden",
                "Shrinemaiden",
                HeroClass.Support,
                310f, 24f, 10f, 1f / 1.05f, 4f, 0.05f, 1.5f, ScaleRangedHeroDistance(8.2f),
                shrinemaidenActive,
                shrinemaidenUltimateSkill,
                overwriteExistingContent,
                out var shrinemaidenHeroExisted,
                HeroTag.Ranged, HeroTag.Heal, HeroTag.AreaDamage);
            ConfigureShrinemaidenBasicAttack(shrinemaiden, overwriteExistingContent, shrinemaidenHeroExisted);
            EnsureHeroSkillReferences(shrinemaiden, shrinemaidenActive, shrinemaidenUltimateSkill);
            EnsureHeroBattlePrefabReference(shrinemaiden, LoadBattlePrefab("support_004_shrinemaiden", HeroClass.Support));

            var chef = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ChefHeroAssetPath);
            var chefActive = AssetDatabase.LoadAssetAtPath<SkillData>(ChefActiveSkillAssetPath);
            var chefUltimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(ChefUltimateSkillAssetPath);
            EnsureHeroSkillReferences(chef, chefActive, chefUltimateSkill);
            EnsureHeroBattlePrefabReference(chef, LoadBattlePrefab("support_005_chef", HeroClass.Support));
            ConfigureChefProjectilePresentation(chef, chefActive, chefUltimateSkill);

            var commanderActive = CreateCommanderActiveSkill(overwriteExistingContent);
            var commanderUltimateSkill = CreateCommanderUltimateSkill(overwriteExistingContent, out var commanderUltimateExisted);

            var commander = CreateHero(
                "support_006_commander",
                "Commander",
                HeroClass.Support,
                310f, 23f, 11f, 1f / 1.12f, 4.05f, 0.05f, 1.5f, ScaleRangedHeroDistance(5.6666667f),
                commanderActive,
                ConfigureCommanderUltimate(commanderUltimateSkill, overwriteExistingContent, commanderUltimateExisted),
                overwriteExistingContent,
                out var commanderHeroExisted,
                HeroTag.Ranged, HeroTag.Buff, HeroTag.Control);
            ConfigureCommanderBasicAttack(commander, overwriteExistingContent, commanderHeroExisted);
            EnsureHeroSkillReferences(commander, commanderActive, commanderUltimateSkill);
            EnsureHeroBattlePrefabReference(commander, LoadBattlePrefab("support_006_commander", HeroClass.Support));

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

            var boomerangerActive = CreateBoomerangerActiveSkill(overwriteExistingContent);
            var boomerangerUltimateSkill = CreateBoomerangerUltimateSkill(overwriteExistingContent, out var boomerangerUltimateExisted);

            var boomeranger = CreateHero(
                "marksman_004_boomeranger",
                "Boomeranger",
                HeroClass.Marksman,
                300f, 31f, 8f, 1.053f, 3.9f, 0.12f, 1.7f, ScaleRangedHeroDistance(5.4666667f),
                boomerangerActive,
                ConfigureBoomerangerUltimate(boomerangerUltimateSkill, overwriteExistingContent, boomerangerUltimateExisted),
                overwriteExistingContent,
                out var boomerangerHeroExisted,
                HeroTag.Ranged, HeroTag.SustainedDamage, HeroTag.AreaDamage);
            ConfigureBoomerangerBasicAttack(boomeranger, overwriteExistingContent, boomerangerHeroExisted);
            EnsureHeroSkillReferences(boomeranger, boomerangerActive, boomerangerUltimateSkill);
            EnsureHeroBattlePrefabReference(boomeranger, LoadBattlePrefab("marksman_004_boomeranger", HeroClass.Marksman));

            var sniperActive = CreateSniperActiveSkill(overwriteExistingContent);
            var sniperUltimateSkill = CreateSniperUltimateSkill(overwriteExistingContent, out var sniperUltimateExisted);

            var sniper = CreateHero(
                "marksman_005_sniper",
                "Sniper",
                HeroClass.Marksman,
                275f, 42f, 5f, 1f / 1.8f, 3.1f, 0.2f, 1.85f, ScaleRangedHeroDistance(8f),
                sniperActive,
                ConfigureSniperUltimate(sniperUltimateSkill, overwriteExistingContent, sniperUltimateExisted),
                overwriteExistingContent,
                out var sniperHeroExisted,
                HeroTag.Ranged, HeroTag.SustainedDamage);
            ConfigureSniperBasicAttack(sniper, overwriteExistingContent, sniperHeroExisted);
            EnsureHeroSkillReferences(sniper, sniperActive, sniperUltimateSkill);
            EnsureHeroBattlePrefabReference(sniper, LoadBattlePrefab("marksman_005_sniper", HeroClass.Marksman));

            var demoHeroes = new[]
            {
                warrior, bladesman, berserker, spellblade, trollwarlord,
                mage, frostmage, sandemperor, lightningmage,
                assassin, tidefin, butcher, loner, demon,
                tank, shieldwarden, tidehunter, mundo, blastshield,
                support, windchime, monk, shrinemaiden, chef, commander,
                marksman, rifleman, venomshooter, boomeranger, sniper,
            };
            ApplyDefaultDisplayDescriptions(demoHeroes);
            CreateHeroCatalog(demoHeroes);
            CreateAthleteRoster(overwriteExistingContent);

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
            if (showCompletionDialog && !Application.isBatchMode)
            {
                var message = overwriteExistingContent
                    ? "Demo content regenerated and existing tuning was overwritten.\nUse Fight/Play/Open Main Menu for the formal flow, or Fight/Dev/Open Battle Scene for direct battle scene access."
                    : "Demo content ensured without overwriting existing tuning.\nUse Fight/Play/Open Main Menu for the formal flow, or Fight/Dev/Open Battle Scene for direct battle scene access.";
                EditorUtility.DisplayDialog("Stage 01", message, "OK");
            }
        }

        public static void GenerateDemoContentForBuild()
        {
            if (!CanMutateDemoContent(showDialog: false))
            {
                throw new InvalidOperationException("Stage-01 demo content generation cannot run while Unity is in Play Mode.");
            }

            GenerateDemoContentInternal(overwriteExistingContent: false, showCompletionDialog: false);
            // Build export should only fail on missing or broken demo asset wiring.
            // Tuned numbers are expected to drift away from the bootstrap defaults.
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
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoEnsureDemoContentIfNeeded();
                return;
            }

            // Always run the non-overwrite ensure pass so newly added sample heroes and skills
            // get materialized in older projects even when the original bootstrap already exists.
            GenerateDemoContent();
            ValidateDemoContentConsistency(logFailures: true);
        }

        private static void EnsureDemoContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // Do not gate this behind one-time bootstrap checks. Demo content generation is
            // intentionally non-destructive when overwriteExistingContent is false, so we can
            // safely resync new sample assets and catalog entries before opening scenes.
            GenerateDemoContent();
            ValidateDemoContentConsistency(logFailures: true);
        }

        private static bool CanMutateDemoContent(bool showDialog)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return true;
            }

            const string message = "Exit Play Mode before regenerating or resyncing stage-01 demo content.";
            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Stage 01", message, "OK");
            }
            else
            {
                Debug.LogWarning(message);
            }

            return false;
        }

        private static void OpenSceneForCurrentEditorState(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene(Path.GetFileNameWithoutExtension(scenePath));
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"Skipped opening scene '{scenePath}' while Unity is switching play mode.");
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
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

            CollectHeroCatalogValidationIssues(issues);
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

        private static void CollectHeroCatalogValidationIssues(List<string> issues)
        {
            if (issues == null)
            {
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<HeroCatalogData>(DefaultHeroCatalogAssetPath);
            if (catalog == null)
            {
                issues.Add("Hero catalog asset is missing.");
                return;
            }

            if (catalog.heroes == null)
            {
                issues.Add("Hero catalog hero list is missing.");
                return;
            }

            var expectedHeroIds = new HashSet<string>(StringComparer.Ordinal);
            var heroGuids = AssetDatabase.FindAssets("t:HeroDefinition", new[] { HeroesRootFolder });
            for (var i = 0; i < heroGuids.Length; i++)
            {
                var heroPath = AssetDatabase.GUIDToAssetPath(heroGuids[i]);
                var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroPath);
                if (hero == null)
                {
                    continue;
                }

                var heroId = string.IsNullOrWhiteSpace(hero.heroId) ? hero.name : hero.heroId;
                if (!expectedHeroIds.Add(heroId))
                {
                    issues.Add($"Duplicate hero asset id detected under demo heroes: {heroId} ({heroPath}).");
                    continue;
                }

                if (!CatalogContainsHero(catalog, heroId))
                {
                    issues.Add($"Hero catalog is missing demo hero '{heroId}' ({heroPath}).");
                }
            }

            var catalogHeroIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.heroes.Count; i++)
            {
                var hero = catalog.heroes[i];
                if (hero == null)
                {
                    issues.Add($"Hero catalog contains a null entry at index {i}.");
                    continue;
                }

                var heroId = string.IsNullOrWhiteSpace(hero.heroId) ? hero.name : hero.heroId;
                if (!catalogHeroIds.Add(heroId))
                {
                    issues.Add($"Hero catalog contains duplicate hero '{heroId}'.");
                    continue;
                }

                if (!expectedHeroIds.Contains(heroId))
                {
                    issues.Add($"Hero catalog references '{heroId}' but no matching demo hero asset exists under {HeroesRootFolder}.");
                }
            }
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

            if (monkActiveSkill == null)
            {
                issues.Add("Monk active skill asset is missing.");
            }

            if (monkUltimateSkill == null)
            {
                issues.Add("Monk ultimate skill asset is missing.");
                return;
            }

            if (monkHero == null)
            {
                return;
            }

            if (monkHero.baseStats == null)
            {
                issues.Add("Monk baseStats block is missing.");
            }

            if (monkHero.basicAttack == null)
            {
                issues.Add("Monk basicAttack block is missing.");
            }

            if (!HeroHasExpectedSkillReferences(monkHero, monkActiveSkill, monkUltimateSkill))
            {
                issues.Add("Monk hero skill references are not aligned with the stage-01 demo skill assets.");
            }

            if (monkHero.visualConfig == null || monkHero.visualConfig.battlePrefab == null)
            {
                issues.Add("Monk battle prefab reference is missing.");
            }

            if (!SkillHasExpectedCastImpactVfxPresentation(
                    monkActiveSkill,
                    AssetDatabase.LoadAssetAtPath<GameObject>(MonkActiveImpactVfxPrefabPath),
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one,
                    false,
                    true,
                    1f))
            {
                issues.Add("Monk active cast impact VFX presentation is missing or misconfigured.");
            }

            if (!SkillHasExpectedCastImpactVfxPresentation(
                    monkUltimateSkill,
                    AssetDatabase.LoadAssetAtPath<GameObject>(MonkUltimateImpactVfxPrefabPath),
                    new Vector3(0f, 0.02f, 0f),
                    Vector3.zero,
                    Vector3.one,
                    false,
                    true,
                    0.18f))
            {
                issues.Add("Monk ultimate cast impact VFX presentation is missing or misconfigured.");
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
            if (ShouldReplaceDisplayDescription(hero.description))
            {
                hero.description = ResolveDefaultHeroDescription(heroId, displayName);
            }

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
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            ResetSameTargetStacking(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.bounce.maxAdditionalTargets = 0;
            hero.basicAttack.bounce.searchRadius = 0f;
            hero.basicAttack.bounce.powerMultiplier = 0f;
            hero.basicAttack.bounce.bounceVariantKey = string.Empty;

            hero.activeSkill = activeSkill;
            hero.ultimateSkill = ultimateSkill;
            hero.aiTemplateId = $"{heroClass.ToString().ToLowerInvariant()}_default";
            hero.usesSpecialLogic = false;
            hero.specialLogicNotes = string.Empty;
            hero.debugNotes = $"Stage-01 demo hero for {heroClass}.";
            var battlePrefab = LoadBattlePrefab(heroId, heroClass);
            hero.visualConfig.battlePrefab = battlePrefab;
            hero.visualConfig.animatorController = heroId == "assassin_001_shadowstep"
                || heroId == "assassin_002_tidefin"
                || heroId == "assassin_003_butcher"
                || heroId == "assassin_004_loner"
                || heroId == "warrior_001_skybreaker"
                || heroId == "warrior_002_bladesman"
                || heroId == "warrior_003_berserker"
                || heroId == "warrior_004_spellblade"
                || heroId == "warrior_005_trollwarlord"
                || heroId == "mage_001_firemage"
                || heroId == "mage_002_frostmage"
                || heroId == "mage_004_lightningmage"
                || heroId == "marksman_001_longshot"
                || heroId == "marksman_002_rifleman"
                || heroId == "marksman_003_venomshooter"
                || heroId == "marksman_004_boomeranger"
                || heroId == "marksman_005_sniper"
                || heroId == "support_002_windchime"
                || heroId == "support_004_shrinemaiden"
                || heroId == "support_005_chef"
                || heroId == "tank_001_ironwall"
                || heroId == "tank_002_shieldwarden"
                || heroId == "tank_003_tidehunter"
                || heroId == "tank_004_mundo"
                || heroId == "tank_005_blastshield"
                ? null
                : battlePrefab != null
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroEditorControllerPath)
                    : null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            hero.visualConfig.projectilePrefab = heroId switch
            {
                "mage_001_firemage" => AssetDatabase.LoadAssetAtPath<GameObject>(FireMageProjectilePrefabPath),
                "mage_002_frostmage" => AssetDatabase.LoadAssetAtPath<GameObject>(FrostMageProjectilePrefabPath),
                "mage_003_sandemperor" => AssetDatabase.LoadAssetAtPath<GameObject>(FireMageProjectilePrefabPath),
                "mage_004_lightningmage" => AssetDatabase.LoadAssetAtPath<GameObject>(FrostMageProjectilePrefabPath),
                "marksman_001_longshot" => AssetDatabase.LoadAssetAtPath<GameObject>(LongshotProjectilePrefabPath),
                "marksman_002_rifleman" => AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanProjectilePrefabPath),
                "marksman_003_venomshooter" => AssetDatabase.LoadAssetAtPath<GameObject>(LongshotProjectilePrefabPath),
                "marksman_004_boomeranger" => AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangerBasicProjectilePrefabPath),
                "marksman_005_sniper" => AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanProjectilePrefabPath),
                "support_001_sunpriest" => AssetDatabase.LoadAssetAtPath<GameObject>(SunpriestProjectilePrefabPath),
                "support_002_windchime" => AssetDatabase.LoadAssetAtPath<GameObject>(SunpriestProjectilePrefabPath),
                "support_005_chef" => AssetDatabase.LoadAssetAtPath<GameObject>(ChefPizzaProjectilePrefabPath),
                "support_006_commander" => AssetDatabase.LoadAssetAtPath<GameObject>(SunpriestProjectilePrefabPath),
                _ => null,
            };
            hero.visualConfig.projectileAlignToMovement =
                heroId == "mage_001_firemage"
                || heroId == "mage_002_frostmage"
                || heroId == "mage_003_sandemperor"
                || heroId == "mage_004_lightningmage"
                || heroId == "marksman_001_longshot"
                || heroId == "marksman_002_rifleman"
                || heroId == "marksman_003_venomshooter"
                || heroId == "marksman_004_boomeranger"
                || heroId == "marksman_005_sniper"
                || heroId == "support_001_sunpriest"
                || heroId == "support_002_windchime"
                || heroId == "support_005_chef"
                || heroId == "support_006_commander";
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            hero.visualConfig.hitVfxPrefab = null;
            hero.visualConfig.basicAttackVariantVisuals = Array.Empty<BasicAttackVariantVisualConfig>();
            HeroPortraitSyncUtility.TryAssignPortraitFromPrefabFolder(hero);
            EditorUtility.SetDirty(hero);
            return hero;
        }

        private static void ApplyDefaultDisplayDescriptions(IEnumerable<HeroDefinition> heroes)
        {
            if (heroes == null)
            {
                return;
            }

            foreach (var hero in heroes)
            {
                if (hero == null)
                {
                    continue;
                }

                if (ShouldReplaceDisplayDescription(hero.description))
                {
                    hero.description = ResolveDefaultHeroDescription(hero.heroId, hero.displayName);
                    EditorUtility.SetDirty(hero);
                }

                ApplyDefaultSkillDescription(hero.activeSkill);
                ApplyDefaultSkillDescription(hero.ultimateSkill);
            }
        }

        private static void ApplyDefaultSkillDescription(SkillData skill)
        {
            if (skill == null || !ShouldReplaceDisplayDescription(skill.description))
            {
                return;
            }

            skill.description = ResolveDefaultSkillDescription(skill.skillId, skill.displayName);
            EditorUtility.SetDirty(skill);
        }

        private static bool ShouldReplaceDisplayDescription(string description)
        {
            return string.IsNullOrWhiteSpace(description)
                || description.StartsWith("Stage-01 demo", StringComparison.OrdinalIgnoreCase)
                || description.StartsWith("Stage-01 archived", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveDefaultHeroDescription(string heroId, string displayName)
        {
            return heroId switch
            {
                "warrior_001_skybreaker" => "冲阵型战士，开局倾向主动贴近敌方核心，用突进和震地控制打散站位。适合需要先手开团、压缩敌方后排空间的阵容，但需要队友及时跟上输出。如果缺少后续控制，开团收益会明显下降。",
                "warrior_002_bladesman" => "爆发型近战，擅长盯住前排或落单目标，先削弱防御再用直线斩击穿透敌阵。适合配合控制制造突破口，但需要避免过早被集火，对站位和切入路线要求较高。",
                "warrior_003_berserker" => "持续作战型战士，血线越低越能打出威胁，适合在前排长时间缠斗并吸引火力。队友提供保护时，他能把危险局面拖成反打机会，但不适合被远程阵容长期拉扯。",
                "warrior_004_spellblade" => "范围压制型战士，依靠剑气和固定魔剑切割战场，让敌人难以舒服站位。适合慢慢挤压阵型，也能在混战中补充稳定范围伤害。队友把敌人留在范围内时收益最高。",
                "warrior_005_trollwarlord" => "单挑升温型前排，持续盯住同一目标时威胁会不断抬高，适合拉长战斗节奏。需要阵容给他进入近身战的时间，避免被风筝；一旦追不上目标，节奏会被明显拖慢。",
                "mage_001_firemage" => "范围爆发法师，擅长惩罚扎堆敌人，用火焰区域制造高压站位选择。适合配合控制或拉拽打出团战爆点，但自身需要保护。敌方越密集，他的威慑力越明显，也需要队友帮他争取施法空间。",
                "mage_002_frostmage" => "控场法师，依靠冰霜区域减缓敌人推进，让前排和射手获得更安全的输出窗口。适合防守反打，也能限制突进阵容的节奏，队伍缺少控制时也能补足减速压力。",
                "mage_003_sandemperor" => "部署型法师，通过沙卫配合自身普攻建立持续火力点，越能站住阵地越有价值。适合中后排稳定压制，但需要避免被快速切入；阵线被打乱时，沙卫价值会下降。",
                "mage_004_lightningmage" => "连锁控制法师，频繁施加感电标记并用雷击打断敌方节奏。适合处理多目标混战，在敌人分散时也能保持持续干扰。队伍需要拖慢敌方技能循环时很有价值，在拉扯战里能不断制造小规模优势。",
                "assassin_001_shadowstep" => "切入型刺客，依靠闪现和隐蔽窗口绕过前排，优先威胁敌方后排输出。适合快节奏进攻，但进场时机错误会很快陷入危险，需要队伍在正面牵制敌方注意力，最好搭配能制造正面压力的队友。",
                "assassin_002_tidefin" => "压制型刺客，通过突进与减益持续削弱关键目标，让敌人难以稳定输出或回复。适合盯防核心英雄，也能在混战中补足单点压力，对单核阵容有较强限制作用。",
                "assassin_003_butcher" => "拉拽型刺客，擅长把后排或孤立目标拖入近身战，再用压制回复的手段扩大击杀窗口。适合打乱阵型，但需要队友跟进集火；如果拉到错误目标，后续压力会变小。",
                "assassin_004_loner" => "收割型刺客，拒绝正向保护换取更强的单兵追击能力，适合在战场边缘寻找残血目标。阵容要给他制造混乱和收割空间，他不依赖队友保护，但很吃目标选择和入场路径。",
                "assassin_005_demon" => "变身型刺客，通过换位制造混乱并逼迫敌方阵型脱节，恶魔形态下持续追击能力更强。适合扰乱后排，但需要承担较高进场风险；如果换位后无人跟进，自己也容易被包围。",
                "tank_001_ironwall" => "保护型坦克，稳定承伤并为队友建立阵地防护，能把敌方爆发分摊成更可控的压力。适合保护核心输出，节奏偏稳。当团队需要稳住阵地而不是强行开团时，他更可靠。",
                "tank_002_shieldwarden" => "反开型坦克，擅长守住后排并用群体护盾顶住爆发，敌人贴近时还能制造反制空间。适合对抗刺客或强突进阵容。他不追求主动击杀，更适合等敌人先交突进。",
                "tank_003_tidehunter" => "受击成长型坦克，面对多来源攻击时会逐渐变得更难击穿，适合站在最前面吸收火力。敌方越想集火，他越能拖住节奏，并为后排争取稳定输出时间，很适合作为团队承接第一波火力的支点。",
                "tank_004_mundo" => "自愈型坦克，依靠持续回复长时间占住前排，在拉锯战中不断消耗敌方输出资源。适合缺少爆发的对局，但害怕被快速集火压倒；拖到后段时会持续逼迫敌方分配火力。",
                "tank_005_blastshield" => "布雷型坦克，通过盾牌姿态和雷区限制敌方推进，把战场变成不容易穿越的防线。适合防守阵地，也能惩罚贸然突进。敌人越想从正面突破，越容易被区域限制牵制。",
                "support_001_sunpriest" => "治疗型辅助，持续为队友提供治疗与护盾，让前排和核心输出拥有更长的作战时间。适合稳健阵容，但需要避免被刺客优先处理；一旦站位被切散，保护效率会明显下降。",
                "support_002_windchime" => "反突进辅助，专门保护被威胁的后排，并用领域削弱敌方切入节奏。适合搭配远程核心，在对手强开时提供关键缓冲。她的价值主要体现在阻止敌方第一波切入成功。",
                "support_003_monk" => "近战守护辅助，站在前线用治疗和群体护盾稳住团队，能和坦克一起承接压力。适合抱团推进，也能让前排阵容更难被突破。当队伍愿意抱团作战时，他能稳定抬高容错。",
                "support_004_shrinemaiden" => "节奏型辅助，在伤害与治疗之间轮转，并用图腾扩大支援覆盖。适合需要持续拉扯的阵容，能同时补足消耗和续航。她不擅长单点爆发，更适合慢慢扩大团队优势。",
                "support_005_chef" => "随机增益辅助，用不同料理强化合适的队友，并在关键时刻提供团队回复。适合灵活阵容，效果不完全稳定但上限很高。在 BP 中适合作为万金油支援，但不能完全替代稳定治疗。",
                "support_006_commander" => "指挥型辅助，强化己方表现突出的核心，并引导全队集火高价值目标。适合围绕单点输出展开战术，让团队伤害更集中。如果队伍已经有明确主输出，他能进一步放大战术重心。",
                "marksman_001_longshot" => "远程持续输出射手，依靠稳定射程和连续射击终结目标，越安全输出越能体现价值。适合前排保护充足的阵容，被贴近时需要队友解围。他不负责开团，更依赖队友创造安全射击环境。",
                "marksman_002_rifleman" => "爆发射手，普攻节奏偏慢但单发威胁高，并能用爆破处理聚集敌人。适合配合控制打短时间压制，但需要良好站位。被迫频繁移动时输出会下降，需要前排帮他固定战场。",
                "marksman_003_venomshooter" => "持续伤害射手，给敌人叠加毒性压力并寻找引爆时机，适合拉长战斗制造连锁收益。面对低血目标时威胁会迅速放大。他不是瞬间秒杀型角色，更适合持续压低敌方血线。",
                "marksman_004_boomeranger" => "中距离弹射射手，用回旋武器在近身范围内持续切割敌人，适合处理密集阵型。需要保持合适距离，太远或被贴脸都会影响表现。在狭窄交战区域里，他的回旋压力最容易兑现。",
                "marksman_005_sniper" => "超远程点杀射手，优先寻找远端目标并逐个狙击，能迫使敌方后排承受持续压力。适合保护严密的阵容，被突进时很脆弱。阵容需要提前规划保护线，否则容易被刺客针对。",
                _ => string.IsNullOrWhiteSpace(displayName)
                    ? "用于 BP 展示的英雄特点说明，建议控制在 80 字左右，描述定位、强势场景和主要风险，不填写具体数值。"
                    : $"{displayName} 的 BP 展示说明，建议描述定位、强势场景和主要风险，不填写具体数值。",
            };
        }

        private static string ResolveDefaultSkillDescription(string skillId, string displayName)
        {
            return skillId switch
            {
                "skill_warrior_active_breakerrush" => "冲向目标并造成范围控制，帮助战士快速打开接战位置。",
                "skill_warrior_ultimate_skyquake" => "跃入敌群造成大范围冲击，适合打乱密集阵型。",
                "skill_bladesman_active_rendingslash" => "先削弱目标防御，再打出一次沉重斩击。",
                "skill_bladesman_ultimate_flyingswallowsever" => "锁定直线路径突进斩击，穿过敌阵造成爆发伤害。",
                "skill_berserker_active_bloodfury" => "被动强化自身，生命越危险时输出和续战能力越强。",
                "skill_berserker_ultimate_titanrage" => "进入狂怒状态，短时间强化近战输出和生存压迫。",
                "skill_spellblade_active_riftwave" => "沿当前目标方向释放剑气，打击直线上的敌人。",
                "skill_spellblade_ultimate_boundblade" => "在战场留下魔剑领域，持续伤害并牵制附近敌人。",
                "skill_trollwarlord_active_warlordreach" => "在近身战中临时强化攻击距离和输出能力。",
                "skill_trollwarlord_ultimate_deathlessfrenzy" => "濒死时进入狂热窗口，短时间避免倒下并保持高压输出。",
                "skill_mage_active_emberburst" => "在目标区域引爆火焰，处理聚集敌人。",
                "skill_mage_ultimate_meteor" => "召唤持续火焰区域，长时间压制敌方站位。",
                "skill_mage_active_firebolt" => "发射火焰弹，对单个敌人造成稳定伤害。",
                "skill_frostmage_active_frostburst" => "在目标区域释放冰霜爆发，造成伤害并干扰走位。",
                "skill_frostmage_ultimate_blizzard" => "制造暴风雪区域，持续伤害并减缓敌人推进。",
                "skill_sandemperor_active_raisesandguard" => "在目标附近部署沙卫，配合自身普攻补充打击。",
                "skill_sandemperor_ultimate_imperialencirclement" => "重建沙卫阵线，围绕敌方制造一轮爆发压制。",
                "skill_lightningmage_active_thunderline" => "释放直线雷击并叠加感电标记。",
                "skill_lightningmage_ultimate_stormverdict" => "连续召唤雷击，反复检查敌人并触发感电控制。",
                "skill_assassin_active_shadowblink" => "闪现切入关键目标，快速贴近后排。",
                "skill_assassin_ultimate_smokeveil" => "制造隐蔽窗口，让刺客更安全地切入或脱离。",
                "skill_tidefin_active_tidalpounce" => "扑向目标并施加压制，削弱其持续作战能力。",
                "skill_tidefin_ultimate_ruintide" => "释放潮汐冲击，对周围敌人造成压制性打击。",
                "skill_butcher_active_gorehook" => "钩回后排目标并压制其回复能力。",
                "skill_butcher_ultimate_carnagereel" => "把敌人强行拉向自己，制造集体近身混战。",
                "skill_loner_active_lonepursuit" => "短暂规避锁定后扑向残血敌人，完成追击收割。",
                "skill_loner_ultimate_loneinstinct" => "被动拒绝队友正向效果，并在击杀参与后滚起自身强化。",
                "skill_demon_active_infernalexchange" => "与远端敌人交换位置，打乱阵型并创造切入点。",
                "skill_demon_ultimate_greaterdemonform" => "变身为恶魔形态，改变攻击方式并强化持续追击。",
                "skill_tank_active_shieldbash" => "为附近友军建立分担保护，帮助团队承受爆发。",
                "skill_tank_ultimate_ironoath" => "为全队提供防御强化，支撑关键团战窗口。",
                "skill_shieldwarden_active_wardenscall" => "保护受威胁友军，并在敌人贴近时反制击退。",
                "skill_shieldwarden_ultimate_lastbastion" => "展开守护领域，击飞敌人并强化友方防护。",
                "skill_tidehunter_active_undertowcarapace" => "被动根据近期承受的敌方压力提高自身防御。",
                "skill_tidehunter_ultimate_tidalrebound" => "释放回卷潮汐，在身边来回打击并控制敌人。",
                "skill_mundo_active_brutemetabolism" => "被动持续自我回复，低血时回复压力更明显。",
                "skill_mundo_ultimate_monstrousrecovery" => "短时间快速自愈，强行延长前排站场时间。",
                "skill_blastshield_active_shieldbrace" => "架盾进入防守姿态，反制近身普攻并提高承压能力。",
                "skill_blastshield_ultimate_blastminefield" => "向前方布置雷区，阻止敌人顺利推进。",
                "skill_support_active_heal" => "治疗一名友军并附加护盾，稳定队友血线。",
                "skill_support_ultimate_blessing" => "创造治疗领域，为站在范围内的友军持续恢复。",
                "skill_windchime_active_echocanopy" => "保护被突进威胁的友军，并对贴近敌人进行反制。",
                "skill_windchime_ultimate_stillwinddomain" => "展开反突进领域，击飞敌人并削弱其攻势。",
                "skill_monk_active_renewingpulse" => "以自身为中心治疗附近友军。",
                "skill_monk_ultimate_guardianmantra" => "以自身为中心为友军套上群体护盾。",
                "skill_shrinemaiden_active_prayerbloom" => "治疗一个友军，并在其周围对敌人造成伤害。",
                "skill_shrinemaiden_ultimate_twinritetotem" => "部署图腾，扩大自身轮转攻击和治疗的覆盖。",
                "skill_chef_active_dailyspecial" => "随机准备料理，为合适的友军提供增益。",
                "skill_chef_ultimate_grandfeast" => "发动团队盛宴，为全队提供大范围回复。",
                "skill_commander_active_battleorders" => "强化当前表现突出的友军，放大其输出能力。",
                "skill_commander_ultimate_focusfirecommand" => "标记敌方高威胁目标，引导全队集中攻击。",
                "skill_marksman_active_focusshot" => "打出一次重击，强化射手的单点压制。",
                "skill_marksman_ultimate_arrowrain" => "进入连续射击窗口，对目标持续倾泻火力。",
                "skill_rifleman_active_burstfire" => "锁定目标后进行一轮快速射击。",
                "skill_rifleman_ultimate_fraggrenade" => "投掷爆破弹，处理聚集在一起的敌人。",
                "skill_venomshooter_active_poisonmist" => "制造毒雾区域，快速向敌人施加毒性压力。",
                "skill_venomshooter_ultimate_venomdetonation" => "引爆敌人身上的毒性，并通过击杀继续扩散。",
                "skill_boomeranger_active_returningwheel" => "掷出回旋轮，外放和回收时都能打击路径敌人。",
                "skill_boomeranger_ultimate_wheelstorm" => "让回旋轮环绕自身，持续切割附近敌人。",
                "skill_sniper_active_deadeyeshot" => "瞄准远端敌人，打出高威胁单发狙击。",
                "skill_sniper_ultimate_killzone" => "进入狙击区域，按威胁顺序逐个点名敌人。",
                _ => string.IsNullOrWhiteSpace(displayName)
                    ? "用于 BP 展示的技能效果说明。"
                    : $"{displayName} 的 BP 技能说明。",
            };
        }

        private static GameObject LoadBattlePrefab(string heroId, HeroClass heroClass)
        {
            var prefabPath = heroId switch
            {
                "assassin_001_shadowstep" => AssassinPrefabPath,
                "assassin_002_tidefin" => TidefinPrefabPath,
                "assassin_003_butcher" => ButcherPrefabPath,
                "assassin_004_loner" => LonerPrefabPath,
                "assassin_005_demon" => AssassinPrefabPath,
                "mage_001_firemage" => FireMageSpritePrefabPath,
                "mage_002_frostmage" => FrostMagePrefabPath,
                "mage_003_sandemperor" => FireMagePrefabPath,
                "mage_004_lightningmage" => LightningmagePrefabPath,
                "marksman_001_longshot" => MarksmanPrefabPath,
                "marksman_002_rifleman" => RiflemanPrefabPath,
                "marksman_003_venomshooter" => VenomshooterPrefabPath,
                "marksman_004_boomeranger" => BoomerangerPrefabPath,
                "marksman_005_sniper" => SniperPrefabPath,
                "support_002_windchime" => WindchimePrefabPath,
                "support_003_monk" => MonkPrefabPath,
                "support_004_shrinemaiden" => ShrinemaidenPrefabPath,
                "support_005_chef" => ChefPrefabPath,
                "warrior_002_bladesman" => BladesmanPrefabPath,
                "warrior_003_berserker" => BerserkerPrefabPath,
                "warrior_004_spellblade" => SpellbladePrefabPath,
                "tank_002_shieldwarden" => ShieldwardenPrefabPath,
                "tank_003_tidehunter" => TidehunterPrefabPath,
                "tank_004_mundo" => MundoPrefabPath,
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
            if (ShouldReplaceDisplayDescription(skill.description))
            {
                skill.description = ResolveDefaultSkillDescription(skillId, displayName);
            }

            skill.slotType = slotType;
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = skillType;
            skill.targetType = targetType;
            skill.preferredEnemyHeroClass = HeroClass.Assassin;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.targetPrioritySearchRadius = 0f;
            skill.minimumTargetDistance = 0f;
            skill.targetPriorityRequiredUnitCount = 1;
            skill.castRange = castRange;
            skill.areaRadius = areaRadius;
            skill.cooldownSeconds = slotType == SkillSlotType.Ultimate ? 0f : cooldownSeconds;
            skill.minTargetsToCast = minTargetsToCast;
            skill.effects.Clear();
            skill.allowsSelfCast = targetType == SkillTargetType.Self || targetType == SkillTargetType.AllAllies;
            ResetReactiveGuard(skill);
            ResetReactiveCounter(skill);
            ResetActionSequence(skill);
            ResetPassiveSkillData(skill);
            ResetDamageTriggeredStatusCounter(skill);
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
                SkillTargetType.BackmostEnemy,
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
            skill.targetType = SkillTargetType.BackmostEnemy;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
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

        private static SkillData CreateButcherActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_butcher_active_gorehook",
                "Gore Hook",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.BackmostEnemy,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
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

            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.BackmostEnemy;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 8f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var pullEffect = AddForcedMovementEffect(
                skill,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0.35f,
                0f,
                ForcedMovementDirectionMode.TowardSource);
            pullEffect.targetMode = SkillEffectTargetMode.SkillTargets;

            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.targetMode = SkillEffectTargetMode.SkillTargets;
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.HealTakenModifier,
                durationSeconds = 4f,
                magnitude = -1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.description = "Stage-01 demo skill: pulls the backmost enemy into melee range and applies 100% healing reduction.";
            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButcherHookChainProjectileVfxPrefabPath);
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            ResetActionSequence(skill);
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

        private static SkillData CreateSniperActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_sniper_active_deadeyeshot",
                "Deadeye Shot",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetDamage,
                SkillTargetType.FarthestEnemyFromSelf,
                ScaleRangedHeroDistance(16f),
                0f,
                2.35f,
                7f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.FarthestEnemyFromSelf;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = ScaleRangedHeroDistance(16f);
            skill.minimumTargetDistance = 0f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 7f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddDamageEffect(skill, 2.35f);
            skill.targetIndicatorVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanActiveTargetIndicatorVfxPrefabPath);
            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanProjectilePrefabPath);
            skill.description = "Stage-01 demo skill: target the farthest legal enemy within twice Sniper's basic attack range and fire one high-powered shot.";
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSniperUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_sniper_ultimate_killzone",
                "Kill Zone",
                SkillSlotType.Ultimate,
                SkillType.SingleTargetDamage,
                SkillTargetType.FarthestEnemyFromSelf,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0f,
                3.05f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.FarthestEnemyFromSelf;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.minimumTargetDistance = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddDamageEffect(skill, 3.05f);
            skill.targetIndicatorVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanActiveTargetIndicatorVfxPrefabPath);
            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanProjectilePrefabPath);
            skill.description = "Stage-01 demo skill: enter a scoped kill zone, then snipe each legal enemy once from farthest to nearest.";
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
            detonation.consumeQueriedStatusesOnHit = true;
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

        private static SkillData CreateBoomerangerActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_boomeranger_active_returningwheel",
                "Returning Wheel",
                SkillSlotType.ActiveSkill,
                SkillType.AreaDamage,
                SkillTargetType.CurrentEnemyTarget,
                ScaleRangedHeroDistance(6.133333f),
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

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.CurrentEnemyTarget;
            skill.fallbackTargetType = SkillTargetType.DensestEnemyArea;
            skill.castRange = ScaleRangedHeroDistance(6.133333f);
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 6f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            AddReturningPathStrikeEffect(
                skill,
                1.6f,
                ScaleRangedHeroDistance(7f),
                ScaleRangedHeroDistance(1f),
                0f,
                0.4f,
                ReturningPathStrikePhase.Outbound);
            AddReturningPathStrikeEffect(
                skill,
                1.2f,
                ScaleRangedHeroDistance(7f),
                ScaleRangedHeroDistance(1f),
                0.5f,
                0.4f,
                ReturningPathStrikePhase.Return);

            ConfigureBoomerangerActiveSkillVfx(skill);
            skill.description = "Stage-01 demo skill: throws a fixed-direction wheel that damages once on the outbound path and once again on the return path.";
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBoomerangerUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_boomeranger_ultimate_wheelstorm",
                "Wheelstorm",
                SkillSlotType.Ultimate,
                SkillType.AreaDamage,
                SkillTargetType.Self,
                0f,
                5f,
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
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectDamage, PersistentAreaTargetType.Enemies, 1.2f, skill.areaRadius, 4.8f, 0.4f, true);
            ConfigureBoomerangerUltimateSkillVfx(skill);
            skill.description = "Stage-01 demo skill: creates a short-range rotating wheel storm around the caster that repeatedly cuts nearby enemies.";
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ConfigureBoomerangerActiveSkillVfx(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangerActiveProjectileVfxPrefabPath);
            skill.persistentAreaVfxPrefab = null;
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
        }

        private static void ConfigureBoomerangerUltimateSkillVfx(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            skill.castProjectileVfxPrefab = null;
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangerUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
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

        private static SkillData CreateButcherUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_butcher_ultimate_carnagereel",
                "Carnage Reel",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.AllEnemies,
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

            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var pullEffect = AddForcedMovementEffect(
                skill,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0.45f,
                0f,
                ForcedMovementDirectionMode.TowardSource);
            pullEffect.targetMode = SkillEffectTargetMode.SkillTargets;

            var statusEffect = AddApplyStatusEffectsEffect(skill);
            statusEffect.targetMode = SkillEffectTargetMode.SkillTargets;
            statusEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.HealTakenModifier,
                durationSeconds = 4f,
                magnitude = -1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.description = "Stage-01 demo skill: yanks every enemy toward the caster and applies team-wide 100% healing reduction.";
            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButcherHookChainProjectileVfxPrefabPath);
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateLonerActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_loner_active_lonepursuit",
                "Lone Pursuit",
                SkillSlotType.ActiveSkill,
                SkillType.Dash,
                SkillTargetType.LowestHealthEnemy,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
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
            skill.targetType = SkillTargetType.LowestHealthEnemy;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 5.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var untargetableEffect = AddApplyStatusEffectsEffect(skill);
            untargetableEffect.targetMode = SkillEffectTargetMode.Caster;
            untargetableEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Untargetable,
                durationSeconds = 0.45f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            AddRepositionEffect(skill, 0.45f);
            skill.description = "Stage-01 demo skill: briefly becomes untargetable, then dives the current lowest-health enemy without extra damage.";
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateLonerUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_loner_ultimate_loneinstinct",
                "Lone Instinct",
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
            ResetPassiveSkillData(skill);
            skill.passiveSkill.rejectExternalPositiveEffects = true;
            skill.passiveSkill.killParticipationAttackPowerBonusPerStack = 0.08f;
            skill.passiveSkill.killParticipationAttackSpeedBonusPerStack = 0.10f;
            skill.passiveSkill.killParticipationHealPercentMaxHealth = 0.18f;
            skill.passiveSkill.killParticipationMaxStacks = 5;
            skill.description = "Stage-01 demo passive skill: rejects allied positive effects and snowballs attack power, attack speed, and self-healing on kill participation.";
            ResetTemporaryOverride(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateDemonActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_demon_active_infernalexchange",
                "Infernal Exchange",
                SkillSlotType.ActiveSkill,
                SkillType.Dash,
                SkillTargetType.FarthestEnemyFromSelf,
                Stage01ArenaSpec.FullMapTargetingRangeWorldUnits,
                0f,
                0f,
                9f,
                1,
                overwriteExistingContent,
                out var existedBefore);
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.Dash;
            skill.targetType = SkillTargetType.FarthestEnemyFromSelf;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 9f;
            skill.minTargetsToCast = 1;
            skill.minimumTargetDistance = ScaleRangedHeroDistance(4.5f);
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var swapEffect = AddSwapPositionsEffect(skill, 0.2f, 0f);
            swapEffect.targetMode = SkillEffectTargetMode.PrimaryTarget;
            skill.description = "Stage-01 demo skill: swaps positions with the farthest enemy from self without damage or hard control.";
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateDemonUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_demon_ultimate_greaterdemonform",
                "Greater Demon Form",
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

            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 0f;
            skill.minTargetsToCast = 1;
            skill.minimumTargetDistance = 0f;
            skill.allowsSelfCast = true;
            skill.effects.Clear();

            var formEffect = AddCombatFormOverrideEffect(skill);
            formEffect.targetMode = SkillEffectTargetMode.Caster;
            formEffect.formOverride.formKey = DemonGreaterFormKey;
            formEffect.formOverride.expiresOnDeath = true;
            formEffect.formOverride.durationSeconds = 0f;
            formEffect.formOverride.overrideUsesProjectile = true;
            formEffect.formOverride.usesProjectile = true;
            formEffect.formOverride.attackRangeOverride = ScaleRangedHeroDistance(6.2f);
            formEffect.formOverride.projectileSpeedOverride = 13f;
            formEffect.formOverride.attackPowerModifier = 0.45f;
            formEffect.formOverride.attackSpeedModifier = 0.2f;

            skill.description = "Stage-01 demo ultimate: transforms into a greater demon until death, switching to ranged projectile basic attacks with higher attack power, attack speed, and attack range.";
            ResetUltimateDecision(skill);
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.InCombatDuration;
            skill.ultimateDecision.primaryCondition.durationSeconds = 3f;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureDemonUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (skill == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

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

        private static SkillData CreateLightningmageActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_lightningmage_active_thunderline",
                "Thunderline",
                SkillSlotType.ActiveSkill,
                SkillType.AreaDamage,
                SkillTargetType.CurrentEnemyTarget,
                ScaleRangedHeroDistance(6.3333333f),
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

            skill.description = "Stage-01 demo skill: fires a narrow lightning lane through the current target direction and seeds a shared shock counter on every damaging hit.";
            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.CurrentEnemyTarget;
            skill.fallbackTargetType = SkillTargetType.DensestEnemyArea;
            skill.castRange = ScaleRangedHeroDistance(6.3333333f);
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 6f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddReturningPathStrikeEffect(
                skill,
                1.35f,
                ScaleRangedHeroDistance(7.4666667f),
                ScaleRangedHeroDistance(1.2f),
                0f,
                0.4f,
                ReturningPathStrikePhase.Outbound);
            ConfigureLightningmageShockCounter(skill);
            ResetActionSequence(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateLightningmageUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_lightningmage_ultimate_stormverdict",
                "Storm Verdict",
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

            skill.description = "Stage-01 demo skill: re-checks all living enemies every second and calls down five global lightning strikes in total.";
            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.AllEnemies;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            AddDamageEffect(skill, 1.1f);

            ResetActionSequence(skill);
            skill.actionSequence.enabled = true;
            skill.actionSequence.payloadType = CombatActionSequencePayloadType.SourceSkill;
            skill.actionSequence.repeatMode = CombatActionSequenceRepeatMode.FixedCount;
            skill.actionSequence.repeatCount = 5;
            skill.actionSequence.durationSeconds = 5f;
            skill.actionSequence.intervalSeconds = 1f;
            skill.actionSequence.windupSeconds = 0f;
            skill.actionSequence.recoverySeconds = 0f;
            skill.actionSequence.temporaryBasicAttackRangeOverride = 0f;
            skill.actionSequence.temporarySkillCastRangeOverride = 0f;
            skill.actionSequence.targetRefreshMode = CombatActionSequenceTargetRefreshMode.RefreshEveryIteration;
            skill.actionSequence.interruptFlags =
                CombatActionSequenceInterruptFlags.HardControl | CombatActionSequenceInterruptFlags.ForcedMovement;

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
            ResetPassiveSkillData(skill);
            skill.description = "Stage-01 demo passive skill: missing health increases attack power, and falling below 40% HP grants lifesteal.";
            skill.passiveSkill.missingHealthAttackPowerRatio = 0.6f;
            skill.passiveSkill.maxAttackPowerBonus = 0.6f;
            skill.passiveSkill.lowHealthLifestealThreshold = 0.4f;
            skill.passiveSkill.lowHealthLifestealRatio = 0.35f;
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
            skill.ultimateDecision.minimumSelfHealthPercentToCast = 0.6f;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 3.2f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.None;
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
            skill.castRange = 2.6f;
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
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.MoveSpeedModifier,
                durationSeconds = 6f,
                magnitude = 0.25f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 6f,
                magnitude = 0.25f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetTemporaryOverride(skill);
            skill.temporaryOverride.durationSeconds = 6f;
            skill.temporaryOverride.lifestealMode = SkillTemporaryOverrideLifestealMode.AtLeast;
            skill.temporaryOverride.lifestealRatio = 0.35f;
            skill.temporaryOverride.visualScaleMultiplier = 1.4f;
            skill.temporaryOverride.visualTintColor = new Color(1f, 0.34f, 0.34f, 1f);
            skill.temporaryOverride.visualTintStrength = 0.6f;
            skill.description = "Stage-01 demo skill: enter a short frenzy with bonus damage, attack speed, move speed, defense, guaranteed lifesteal, visual growth, and a red rage tint.";
        }

        private static SkillData CreateSpellbladeActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_spellblade_active_riftwave",
                "裂空剑气",
                SkillSlotType.ActiveSkill,
                SkillType.AreaDamage,
                SkillTargetType.CurrentEnemyTarget,
                4.8f,
                0f,
                0f,
                6.5f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.CurrentEnemyTarget;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = 4.8f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 6.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            AddReturningPathStrikeEffect(
                skill,
                powerMultiplier: 1.25f,
                maxDistance: 7.2f,
                pathWidth: 2.2f,
                delaySeconds: 0f,
                durationSeconds: 0f,
                phase: ReturningPathStrikePhase.Outbound);

            skill.description = "Stage-01 demo skill: fire a straight sword wave along the current target direction and damage every enemy on that line once.";
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateSpellbladeUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_spellblade_ultimate_boundblade",
                "缚阵魔剑",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.Self,
                0f,
                8.5f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplySpellbladeUltimateBaseConfiguration(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureSpellbladeUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplySpellbladeUltimateBaseConfiguration(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 8.5f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.None;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            ApplyCountFallback(skill, 45f, 2, 0f, 0);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplySpellbladeUltimateBaseConfiguration(SkillData skill)
        {
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 8.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();
            ResetPassiveSkillData(skill);
            ResetTemporaryOverride(skill);

            var deployableEffect = AddDeployableProxyEffect(
                skill,
                powerMultiplier: 0f,
                strikeRadius: 8.5f,
                durationSeconds: 4f,
                maxCount: 1,
                spawnMode: DeployableProxySpawnMode.AtTargetPosition,
                spawnOffsetDistance: 0f,
                triggerMode: DeployableProxyTriggerMode.PeriodicEffectPulse,
                replaceOldestWhenLimitReached: true,
                immediateStrikeOnSpawn: false);
            deployableEffect.targetMode = SkillEffectTargetMode.PrimaryTarget;
            deployableEffect.persistentAreaTargetType = PersistentAreaTargetType.Enemies;
            deployableEffect.deployableProxyAttackIntervalSeconds = 1f;
            deployableEffect.forcedMovementDirection = ForcedMovementDirectionMode.TowardSource;
            deployableEffect.forcedMovementDistance = 1.8f;
            deployableEffect.forcedMovementDurationSeconds = 0.28f;
            deployableEffect.forcedMovementPeakHeight = 0f;

            skill.description = "Stage-01 demo skill: leave a fixed cursed sword at the current fight point that pulses every second and drags nearby enemies inward.";
        }

        private static SkillData CreateTrollWarlordActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_trollwarlord_active_warlordreach",
                "Warlord Reach",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.CurrentEnemyTarget,
                2.8f,
                0f,
                0f,
                7.5f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.CurrentEnemyTarget;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = 2.8f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 7.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            ResetPassiveSkillData(skill);
            ResetTemporaryOverride(skill);

            var selfBuffEffect = AddApplyStatusEffectsEffect(skill);
            selfBuffEffect.targetMode = SkillEffectTargetMode.Caster;
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 4f,
                magnitude = 0.25f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackRangeModifier,
                durationSeconds = 4f,
                magnitude = 0.45f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.description = "Stage-01 demo skill: temporarily increase attack power and attack range while a current enemy target is close enough.";
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateTrollWarlordUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_trollwarlord_ultimate_deathlessfrenzy",
                "Deathless Frenzy",
                SkillSlotType.Ultimate,
                SkillType.Buff,
                SkillTargetType.CurrentEnemyTarget,
                2.8f,
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

            ApplyTrollWarlordUltimateBaseConfiguration(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureTrollWarlordUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyTrollWarlordUltimateBaseConfiguration(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.CurrentTargetOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.SelfLowHealth;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.35f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.None;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = 0.5f;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyTrollWarlordUltimateBaseConfiguration(SkillData skill)
        {
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.CurrentEnemyTarget;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 2.8f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            ResetPassiveSkillData(skill);
            ResetTemporaryOverride(skill);

            var selfBuffEffect = AddApplyStatusEffectsEffect(skill);
            selfBuffEffect.targetMode = SkillEffectTargetMode.Caster;
            selfBuffEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DeathPrevent,
                durationSeconds = 4.5f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            skill.description = "Stage-01 demo skill: prevent non-execute lethal damage from reducing health below 1 and treat Fervor attack speed as full stacks for a short window.";
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

        private static SkillEffectData AddSwapPositionsEffect(
            SkillData skill,
            float durationSeconds,
            float peakHeight)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.SwapPositionsWithPrimaryTarget,
                durationSeconds = durationSeconds,
                forcedMovementDurationSeconds = durationSeconds,
                forcedMovementPeakHeight = peakHeight,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddCombatFormOverrideEffect(SkillData skill)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.ApplyCombatFormOverride,
                formOverride = new CombatFormOverrideData(),
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

        private static SkillEffectData AddRadialSweepEffect(
            SkillData skill,
            PersistentAreaTargetType targetType,
            float powerMultiplier,
            float radiusOverride,
            float durationSeconds,
            float startDelaySeconds,
            float ringWidth,
            RadialSweepDirectionMode direction)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.CreateRadialSweep,
                targetMode = SkillEffectTargetMode.Caster,
                powerMultiplier = powerMultiplier,
                radiusOverride = radiusOverride,
                durationSeconds = durationSeconds,
                persistentAreaTargetType = targetType,
                radialSweepDirection = direction,
                radialSweepStartDelaySeconds = startDelaySeconds,
                radialSweepRingWidth = ringWidth,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData AddReturningPathStrikeEffect(
            SkillData skill,
            float powerMultiplier,
            float maxDistance,
            float pathWidth,
            float delaySeconds,
            float durationSeconds,
            ReturningPathStrikePhase phase)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.CreateReturningPathStrike,
                powerMultiplier = powerMultiplier,
                durationSeconds = durationSeconds,
                persistentAreaTargetType = PersistentAreaTargetType.Enemies,
                returningPathStrikePhase = phase,
                returningPathMaxDistance = maxDistance,
                returningPathWidth = pathWidth,
                returningPathDelaySeconds = delaySeconds,
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

        private static void ConfigureLightningmageShockCounter(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            ResetDamageTriggeredStatusCounter(skill);
            skill.damageTriggeredStatusCounter.enabled = true;
            skill.damageTriggeredStatusCounter.countBasicAttackDamage = true;
            skill.damageTriggeredStatusCounter.countSkillDamage = true;
            skill.damageTriggeredStatusCounter.countSkillAreaPulseDamage = true;
            skill.damageTriggeredStatusCounter.countStatusEffectDamage = true;
            skill.damageTriggeredStatusCounter.countCounterTriggerDamage = false;
            skill.damageTriggeredStatusCounter.triggerThreshold = 3;
            skill.damageTriggeredStatusCounter.clearCountedStatusesOnTrigger = true;
            skill.damageTriggeredStatusCounter.countedStatus.effectType = StatusEffectType.Marker;
            skill.damageTriggeredStatusCounter.countedStatus.durationSeconds = 4f;
            skill.damageTriggeredStatusCounter.countedStatus.maxStacks = 3;
            skill.damageTriggeredStatusCounter.countedStatus.stackGroupKey = LightningmageShockStackGroupKey;
            skill.damageTriggeredStatusCounter.countedStatus.statusThemeKey = ShockStatusThemeKey;
            skill.damageTriggeredStatusCounter.countedStatus.refreshDurationOnReapply = true;
            skill.damageTriggeredStatusCounter.triggerStatusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.Stun,
                durationSeconds = 0.6f,
                magnitude = 0f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
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

        private static void ConfigureShrinemaidenBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.95f;
            hero.basicAttack.attackInterval = 1.05f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(8.2f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 14f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            hero.basicAttack.startingVariantIndex = 0;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.variants.Clear();
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.variants.Add(new BasicAttackVariantData
            {
                variantKey = "attack_damage",
                effectType = BasicAttackEffectType.Damage,
                targetType = BasicAttackTargetType.NearestEnemy,
                powerMultiplier = 0.95f,
                targetPrioritySearchRadius = 0f,
                missingTargetFallbackVariantIndex = 1,
                onHitStatusEffects = new List<StatusEffectData>(),
            });
            hero.basicAttack.variants.Add(new BasicAttackVariantData
            {
                variantKey = "attack_heal",
                effectType = BasicAttackEffectType.Heal,
                targetType = BasicAttackTargetType.LowestHealthAlly,
                powerMultiplier = 0.9f,
                targetPrioritySearchRadius = 0f,
                missingTargetFallbackVariantIndex = -1,
                onHitStatusEffects = new List<StatusEffectData>(),
            });

            hero.visualConfig ??= new HeroVisualConfig();
            hero.visualConfig.battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShrinemaidenPrefabPath);
            hero.visualConfig.animatorController = null;
            hero.visualConfig.projectilePrefab = null;
            hero.visualConfig.projectileAlignToMovement = false;
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            hero.visualConfig.basicAttackVariantVisuals = Array.Empty<BasicAttackVariantVisualConfig>();
            hero.debugNotes = "Stage-01 demo hero for Support. Shrinemaiden validates alternating damage/heal projectile basic attacks, missing-enemy fallback healing, and periodic deployable proxy attack sequences.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureChefProjectilePresentation(HeroDefinition hero, SkillData activeSkill, SkillData ultimateSkill)
        {
            var pizza = AssetDatabase.LoadAssetAtPath<GameObject>(ChefPizzaProjectilePrefabPath);
            if (hero != null)
            {
                hero.visualConfig ??= new HeroVisualConfig();
                hero.visualConfig.projectilePrefab = pizza;
                hero.visualConfig.projectileAlignToMovement = pizza != null;
                hero.visualConfig.projectileEulerAngles = Vector3.zero;
                EditorUtility.SetDirty(hero);
            }

            if (activeSkill != null)
            {
                activeSkill.playCastProjectileOnSkillCast = true;
                activeSkill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChefBurgerProjectilePrefabPath);
                activeSkill.castProjectileVfxFlightDurationSeconds = 0.38f;
                activeSkill.castProjectileVfxScaleMultiplier = Vector3.one;
                activeSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                ConfigureSkillVariantProjectile(activeSkill, "burger", ChefBurgerProjectilePrefabPath);
                ConfigureSkillVariantProjectile(activeSkill, "hotdog", ChefHotdogProjectilePrefabPath);
                ConfigureSkillVariantProjectile(activeSkill, "fries", ChefFriesProjectilePrefabPath);
                EditorUtility.SetDirty(activeSkill);
            }

            if (ultimateSkill != null)
            {
                ultimateSkill.playCastProjectileOnSkillCast = true;
                ultimateSkill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChefBigmacProjectilePrefabPath);
                ultimateSkill.castProjectileVfxFlightDurationSeconds = 0.44f;
                ultimateSkill.castProjectileVfxScaleMultiplier = Vector3.one;
                ultimateSkill.skillAreaPresentationType = SkillAreaPresentationType.None;
                EditorUtility.SetDirty(ultimateSkill);
            }
        }

        private static void ConfigureSkillVariantProjectile(SkillData skill, string variantKey, string prefabPath)
        {
            var variant = skill != null ? skill.FindVariant(variantKey) : null;
            if (variant == null)
            {
                return;
            }

            variant.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            variant.castProjectileVfxScaleMultiplier = Vector3.one;
        }

        private static void ConfigureCommanderBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.9f;
            hero.basicAttack.attackInterval = 1.12f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(5.6666667f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 14f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.variants.Clear();
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Support. Commander validates output amplification and team focus-fire target priority without using hard taunt.";
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
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
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

        private static void ConfigureButcherBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1.05f;
            hero.basicAttack.attackInterval = 0.91f;
            hero.basicAttack.rangeOverride = 1.4f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Assassin. Butcher validates pull-to-caster forced movement and target-side healing denial.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureLonerBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 0.83f;
            hero.basicAttack.rangeOverride = 1.35f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Assassin. Loner validates lowest-health dive targeting, allied positive-effect rejection, and kill-participation passive stacking.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureDemonBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 0.88f;
            hero.basicAttack.rangeOverride = 1.35f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.variants.Clear();
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Assassin. Demon validates farthest-enemy position exchange, death-ended combat form overrides, and transformed ranged kiting logic.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureDemonVisualForms(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.visualConfig.battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssassinPrefabPath);
            hero.visualConfig.animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroEditorControllerPath);
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            hero.visualConfig.projectilePrefab = null;
            hero.visualConfig.projectileAlignToMovement = false;
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            hero.visualConfig.basicAttackVariantVisuals = Array.Empty<BasicAttackVariantVisualConfig>();
            hero.visualConfig.formVisuals = new[]
            {
                new HeroFormVisualConfig
                {
                    formKey = DemonGreaterFormKey,
                    battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButcherPrefabPath),
                    animatorController = null,
                    battlePrefabFacesLeftByDefault = false,
                    projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LongshotProjectilePrefabPath),
                    projectileAlignToMovement = true,
                    projectileEulerAngles = Vector3.zero,
                    basicAttackVariantVisuals = Array.Empty<BasicAttackVariantVisualConfig>(),
                },
            };

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

        private static void ConfigureSpellbladeBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1.05f;
            hero.basicAttack.attackInterval = 1.04f;
            hero.basicAttack.rangeOverride = 1.8f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Warrior. Spellblade validates current-target line strikes and periodic proxy pulses that compress enemy formation toward a fixed anchor.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureTrollWarlordBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 1.2f;
            hero.basicAttack.rangeOverride = 2.0f;
            hero.basicAttack.usesProjectile = false;
            hero.basicAttack.projectileSpeed = 0f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.sameTargetStacking.enabled = true;
            hero.basicAttack.sameTargetStacking.maxStacks = 6;
            hero.basicAttack.sameTargetStacking.modifierEffectType = StatusEffectType.AttackSpeedModifier;
            hero.basicAttack.sameTargetStacking.magnitudePerStack = 0.16f;
            hero.basicAttack.sameTargetStacking.targetRetentionRange = 4.8f;
            hero.basicAttack.sameTargetStacking.fullStackOverrideStatusEffectType = StatusEffectType.DeathPrevent;
            hero.debugNotes = "Stage-01 demo hero for Warrior. TrollWarlord validates same-target basic attack speed stacking, current-target retention, temporary reach, and death-prevent self ultimate.";
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

        private static void ConfigureBoomerangerBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.85f;
            hero.basicAttack.attackInterval = 0.95f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(5.4666667f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 15f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.variants.Clear();
            hero.basicAttack.bounce.maxAdditionalTargets = 3;
            hero.basicAttack.bounce.searchRadius = ScaleRangedHeroDistance(2.1333333f);
            hero.basicAttack.bounce.powerMultiplier = 0.48f;
            hero.basicAttack.bounce.bounceVariantKey = "bounce";

            hero.visualConfig ??= new HeroVisualConfig();
            hero.visualConfig.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangerBasicProjectilePrefabPath);
            hero.visualConfig.projectileAlignToMovement = true;
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            hero.visualConfig.basicAttackVariantVisuals = new[]
            {
                new BasicAttackVariantVisualConfig
                {
                    variantKey = "bounce",
                    projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangerBounceProjectilePrefabPath),
                    hitVfxPrefab = null,
                },
            };
            hero.debugNotes = "Stage-01 demo hero for Marksman. Boomeranger validates short-range projectile bounce chains, fixed-direction returning path damage, and self-following close-range area pressure.";
            EditorUtility.SetDirty(hero);
        }

        private static void ConfigureSniperBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 1f;
            hero.basicAttack.attackInterval = 1.8f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(8f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 24f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.basicAttack.variants.Clear();
            hero.basicAttack.bounce.maxAdditionalTargets = 0;
            hero.basicAttack.bounce.searchRadius = 0f;
            hero.basicAttack.bounce.powerMultiplier = 0f;
            hero.basicAttack.bounce.bounceVariantKey = string.Empty;
            hero.debugNotes = "Stage-01 demo hero for Marksman. Sniper validates longest-range slow projectile basics, farthest-enemy targeting, and unique-target ultimate shot sequences.";
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

        private static void ConfigureLightningmageBasicAttack(HeroDefinition hero, bool overwriteExistingContent, bool existedBefore)
        {
            if (hero == null || ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return;
            }

            hero.basicAttack.damageMultiplier = 0.75f;
            hero.basicAttack.attackInterval = 1.05f;
            hero.basicAttack.rangeOverride = ScaleRangedHeroDistance(5.9333333f);
            hero.basicAttack.usesProjectile = true;
            hero.basicAttack.projectileSpeed = 15f;
            hero.basicAttack.effectType = BasicAttackEffectType.Damage;
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;
            hero.basicAttack.targetPrioritySearchRadius = 0f;
            EnsureBasicAttackStatusList(hero.basicAttack);
            hero.basicAttack.onHitStatusEffects.Clear();
            hero.debugNotes = "Stage-01 demo hero for Mage. Lightningmage validates shared damage-triggered shock counters, frequent short stuns, and global action-sequence ult pulses.";
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

        private static SkillData CreateCommanderActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_commander_active_battleorders",
                "Battle Orders",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.HighestDamageAllyInRange,
                ScaleRangedHeroDistance(6.6666667f),
                0f,
                0f,
                7f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: buff the allied unit that has dealt the most damage so far.";
            skill.targetType = SkillTargetType.HighestDamageAllyInRange;
            skill.fallbackTargetType = SkillTargetType.LowestHealthAlly;
            skill.castRange = ScaleRangedHeroDistance(6.6666667f);
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 7f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();

            var effect = AddApplyStatusEffectsEffect(skill);
            effect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.AttackPowerModifier,
                durationSeconds = 5f,
                magnitude = 0.25f,
                maxStacks = 1,
                stackGroupKey = "commander_battle_orders",
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateCommanderUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_commander_ultimate_focusfirecommand",
                "Focus Fire Command",
                SkillSlotType.Ultimate,
                SkillType.FocusFireCommand,
                SkillTargetType.HighestDamageEnemyInRange,
                ScaleRangedHeroDistance(7.3333335f),
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

            skill.description = "Stage-01 demo ultimate: mark the enemy high-output unit as the team's focus target and reduce its defense. If it dies before the command expires, retarget to the next high-output enemy.";
            skill.targetType = SkillTargetType.HighestDamageEnemyInRange;
            skill.fallbackTargetType = SkillTargetType.NearestEnemy;
            skill.castRange = ScaleRangedHeroDistance(7.3333335f);
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var commandEffect = new SkillEffectData
            {
                effectType = SkillEffectType.CreateFocusFireCommand,
                targetMode = SkillEffectTargetMode.PrimaryTarget,
                durationSeconds = 6f,
            };
            commandEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.FocusFireMark,
                durationSeconds = 6f,
                magnitude = 1f,
                maxStacks = 1,
                stackGroupKey = "focus_fire_command",
                refreshDurationOnReapply = true,
            });
            commandEffect.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 6f,
                magnitude = -0.35f,
                maxStacks = 1,
                stackGroupKey = "focus_fire_command",
                refreshDurationOnReapply = true,
            });
            skill.effects.Add(commandEffect);
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
            skill.ultimateDecision.minimumSelfHealthPercentToCast = 0f;
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
            skill.passiveSkill.lowHealthLifestealThreshold = 0f;
            skill.passiveSkill.lowHealthLifestealRatio = 0f;
            skill.passiveSkill.recentDirectHostileSourceWindowSeconds = 0f;
            skill.passiveSkill.recentDirectHostileSourceDefenseBonusPerSource = 0f;
            skill.passiveSkill.maxDefenseBonus = 0f;
            skill.passiveSkill.periodicSelfHealIntervalSeconds = 0f;
            skill.passiveSkill.periodicSelfHealMidHealthThreshold = 0f;
            skill.passiveSkill.periodicSelfHealLowHealthThreshold = 0f;
            skill.passiveSkill.periodicSelfHealHighHealthPercentMaxHealth = 0f;
            skill.passiveSkill.periodicSelfHealMidHealthPercentMaxHealth = 0f;
            skill.passiveSkill.periodicSelfHealLowHealthPercentMaxHealth = 0f;
            skill.passiveSkill.rejectExternalPositiveEffects = false;
            skill.passiveSkill.killParticipationAttackPowerBonusPerStack = 0f;
            skill.passiveSkill.killParticipationAttackSpeedBonusPerStack = 0f;
            skill.passiveSkill.killParticipationHealPercentMaxHealth = 0f;
            skill.passiveSkill.killParticipationMaxStacks = 0;
        }

        private static void ResetDamageTriggeredStatusCounter(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.damageTriggeredStatusCounter == null)
            {
                skill.damageTriggeredStatusCounter = new DamageTriggeredStatusCounterData();
            }

            skill.damageTriggeredStatusCounter.enabled = false;
            skill.damageTriggeredStatusCounter.countBasicAttackDamage = true;
            skill.damageTriggeredStatusCounter.countSkillDamage = true;
            skill.damageTriggeredStatusCounter.countSkillAreaPulseDamage = true;
            skill.damageTriggeredStatusCounter.countStatusEffectDamage = true;
            skill.damageTriggeredStatusCounter.countCounterTriggerDamage = false;
            skill.damageTriggeredStatusCounter.triggerThreshold = 3;
            skill.damageTriggeredStatusCounter.clearCountedStatusesOnTrigger = true;
            if (skill.damageTriggeredStatusCounter.countedStatus == null)
            {
                skill.damageTriggeredStatusCounter.countedStatus = new StatusEffectData();
            }

            var countedStatus = skill.damageTriggeredStatusCounter.countedStatus;
            countedStatus.effectType = StatusEffectType.None;
            countedStatus.durationSeconds = 1f;
            countedStatus.magnitude = 0f;
            countedStatus.sourceAttackPowerMultiplier = 0f;
            countedStatus.activeSkillCooldownCapSeconds = 0f;
            countedStatus.tickIntervalSeconds = 1f;
            countedStatus.maxStacks = 1;
            countedStatus.stackGroupKey = string.Empty;
            countedStatus.statusThemeKey = string.Empty;
            countedStatus.refreshDurationOnReapply = true;
            skill.damageTriggeredStatusCounter.triggerStatusEffects.Clear();
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
            skill.temporaryOverride.lifestealMode = SkillTemporaryOverrideLifestealMode.Additive;
            skill.temporaryOverride.lifestealRatio = 0f;
            skill.temporaryOverride.visualScaleMultiplier = 1f;
            skill.temporaryOverride.visualTintColor = Color.white;
            skill.temporaryOverride.visualTintStrength = 0f;
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

        private static void ResetReactiveCounter(SkillData skill)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.reactiveCounter == null)
            {
                skill.reactiveCounter = new ReactiveCounterData();
            }

            skill.reactiveCounter.enabled = false;
            skill.reactiveCounter.durationSeconds = 0f;
            skill.reactiveCounter.blocksBasicAttacks = true;
            skill.reactiveCounter.blocksSkillCasts = true;
            skill.reactiveCounter.triggerOnBasicAttackDamage = true;
            skill.reactiveCounter.requireNonProjectileBasicAttack = true;
            skill.reactiveCounter.sourceTriggerCooldownSeconds = 0f;
            skill.reactiveCounter.counterDamagePowerMultiplier = 0f;
            skill.reactiveCounter.forcedMovementDistance = 0f;
            skill.reactiveCounter.forcedMovementDurationSeconds = 0f;
            skill.reactiveCounter.forcedMovementPeakHeight = 0f;
            skill.reactiveCounter.onTriggerStatusEffects.Clear();
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

        private static SkillData ConfigureCommanderUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.castRange;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 1;
            ApplyCountFallback(skill, 40f, 1, 0f, 0);
            EditorUtility.SetDirty(skill);
            return skill;
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

        private static SkillData ConfigureLightningmageUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
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
            skill.castProjectileVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButcherHookChainProjectileVfxPrefabPath);
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyWithStatusInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.statusEffectTypeFilter = StatusEffectType.Marker;
            skill.ultimateDecision.secondaryCondition.statusThemeKey = ShockStatusThemeKey;
            skill.ultimateDecision.secondaryCondition.minimumStatusStacks = 2;
            ApplyCountFallback(skill, 40f, 2, 50f, 1);
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

        private static SkillData ConfigureButcherUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyLowHealthInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.secondaryCondition.healthPercentThreshold = 0.7f;
            ApplyCountFallback(skill, 40f, 2, 0f, 0);
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

        private static SkillData CreateShrinemaidenActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_shrinemaiden_active_prayerbloom",
                "Prayer Bloom",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetHeal,
                SkillTargetType.ThreatenedAlly,
                ScaleRangedHeroDistance(9f),
                ScaleRangedHeroDistance(3.6f),
                1.35f,
                8f,
                1,
                overwriteExistingContent,
                out var existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.description = "Stage-01 demo skill: burst-heal one ally, then damage nearby enemies around that anchor.";
            skill.targetType = SkillTargetType.ThreatenedAlly;
            skill.fallbackTargetType = SkillTargetType.LowestHealthAlly;
            skill.castRange = ScaleRangedHeroDistance(9f);
            skill.areaRadius = ScaleRangedHeroDistance(3.6f);
            skill.cooldownSeconds = 8f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.targetPrioritySearchRadius = skill.areaRadius;
            skill.targetPriorityRequiredUnitCount = 1;
            skill.effects.Clear();

            var healEffect = AddHealEffect(skill, 1.35f);
            healEffect.targetMode = SkillEffectTargetMode.PrimaryTarget;

            var damageEffect = AddDamageEffect(skill, 1f);
            damageEffect.targetMode = SkillEffectTargetMode.EnemiesInRadiusAroundPrimaryTarget;
            damageEffect.radiusOverride = skill.areaRadius;

            skill.castImpactVfxPrefab = null;
            skill.castImpactVfxLocalOffset = Vector3.zero;
            skill.castImpactVfxEulerAngles = Vector3.zero;
            skill.castImpactVfxScaleMultiplier = Vector3.one;
            skill.castImpactVfxAlignToTargetDirection = false;
            skill.castImpactVfxScaleWithSkillArea = true;
            skill.castImpactVfxAreaDiameterScaleMultiplier = 1f;

            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateShrinemaidenUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_shrinemaiden_ultimate_twinritetotem",
                "Twin Rite Totem",
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

            skill.description = "Stage-01 demo skill: deploy a short-lived totem beside Shrinemaiden that rapidly repeats her alternating attack sequence across the full map.";
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.targetPrioritySearchRadius = 0f;
            skill.targetPriorityRequiredUnitCount = 1;
            skill.effects.Clear();

            var deployableEffect = AddDeployableProxyEffect(
                skill,
                powerMultiplier: 0f,
                strikeRadius: 0f,
                durationSeconds: 6f,
                maxCount: 1,
                spawnMode: DeployableProxySpawnMode.AroundTarget,
                spawnOffsetDistance: 1.2f,
                triggerMode: DeployableProxyTriggerMode.PeriodicBasicAttackSequence,
                replaceOldestWhenLimitReached: true,
                immediateStrikeOnSpawn: false);
            deployableEffect.targetMode = SkillEffectTargetMode.PrimaryTarget;
            deployableEffect.deployableProxyPowerMultiplierScale = 0.8f;
            deployableEffect.deployableProxyAttackIntervalSeconds = 0.5f;
            deployableEffect.deployableProxyAttackRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            deployableEffect.deployableProxyProjectileSpeedOverride = 16f;
            deployableEffect.deployableProxyStartingVariantIndex = 0;
            deployableEffect.deployableProxySpawnVfxPrefab = null;
            deployableEffect.deployableProxyLoopVfxPrefab = null;
            deployableEffect.deployableProxyRemovalVfxPrefab = null;
            deployableEffect.deployableProxyVfxLocalOffset = new Vector3(0f, 0.28f, 0f);
            deployableEffect.deployableProxyVfxEulerAngles = Vector3.zero;
            deployableEffect.deployableProxyVfxScaleMultiplier = Vector3.one;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 1f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.None;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.None;
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

        private static SkillData ConfigureBoomerangerUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.AreaDamage;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            ConfigureBoomerangerUltimateSkillVfx(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = 5f;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 1;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureSniperUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.skillType = SkillType.SingleTargetDamage;
            skill.targetType = SkillTargetType.FarthestEnemyFromSelf;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.minimumTargetDistance = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;

            ResetActionSequence(skill);
            skill.actionSequence.enabled = true;
            skill.actionSequence.payloadType = CombatActionSequencePayloadType.SourceSkill;
            skill.actionSequence.repeatMode = CombatActionSequenceRepeatMode.FixedCount;
            skill.actionSequence.repeatCount = 4;
            skill.actionSequence.durationSeconds = 1.4f;
            skill.actionSequence.intervalSeconds = 0.35f;
            skill.actionSequence.windupSeconds = 0f;
            skill.actionSequence.recoverySeconds = 0f;
            skill.actionSequence.temporaryBasicAttackRangeOverride = 0f;
            skill.actionSequence.temporarySkillCastRangeOverride = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.actionSequence.targetRefreshMode = CombatActionSequenceTargetRefreshMode.RefreshEveryIterationUniqueTarget;
            skill.actionSequence.interruptFlags =
                CombatActionSequenceInterruptFlags.HardControl | CombatActionSequenceInterruptFlags.ForcedMovement;

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 4;
            ApplyCountFallback(skill, 38f, 3, 52f, 1);
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

        private static SkillData CreateTidehunterActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_tidehunter_active_undertowcarapace",
                "Undertow Carapace",
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

            skill.description = "Stage-01 demo passive skill: recent direct hostile damage sources increase defense.";
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
            ResetPassiveSkillData(skill);
            skill.passiveSkill.recentDirectHostileSourceWindowSeconds = 1.5f;
            skill.passiveSkill.recentDirectHostileSourceDefenseBonusPerSource = 0.2f;
            skill.passiveSkill.maxDefenseBonus = 0.8f;
            ResetTemporaryOverride(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateTidehunterUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_tidehunter_ultimate_tidalrebound",
                "Tidal Rebound",
                SkillSlotType.Ultimate,
                SkillType.KnockUp,
                SkillTargetType.Self,
                0f,
                8.5f,
                0f,
                0f,
                1,
                overwriteExistingContent,
                out existedBefore);

            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyTidehunterUltimateBaseConfiguration(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureTidehunterUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyTidehunterUltimateBaseConfiguration(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = 5.5f;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 2;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyTidehunterUltimateBaseConfiguration(SkillData skill)
        {
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.KnockUp;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 8.5f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();

            var outwardSweep = AddRadialSweepEffect(
                skill,
                PersistentAreaTargetType.Enemies,
                4f,
                skill.areaRadius,
                0.75f,
                0f,
                1.4f,
                RadialSweepDirectionMode.Outward);
            outwardSweep.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = 0.75f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            var inwardSweep = AddRadialSweepEffect(
                skill,
                PersistentAreaTargetType.Enemies,
                2.5f,
                skill.areaRadius,
                0.75f,
                0.95f,
                1.4f,
                RadialSweepDirectionMode.Inward);
            inwardSweep.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.KnockUp,
                durationSeconds = 0.5f,
                magnitude = 1f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
        }

        private static SkillData CreateMundoActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_mundo_active_brutemetabolism",
                "Brute Metabolism",
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

            skill.description = "Stage-01 demo passive skill: periodically heal self based on max health, with stronger recovery at lower health.";
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
            ResetPassiveSkillData(skill);
            skill.passiveSkill.periodicSelfHealIntervalSeconds = 1f;
            skill.passiveSkill.periodicSelfHealMidHealthThreshold = 0.6f;
            skill.passiveSkill.periodicSelfHealLowHealthThreshold = 0.3f;
            skill.passiveSkill.periodicSelfHealHighHealthPercentMaxHealth = 0.015f;
            skill.passiveSkill.periodicSelfHealMidHealthPercentMaxHealth = 0.028f;
            skill.passiveSkill.periodicSelfHealLowHealthPercentMaxHealth = 0.045f;
            ResetTemporaryOverride(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateMundoUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_mundo_ultimate_monstrousrecovery",
                "Monstrous Recovery",
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

            ApplyMundoUltimateBaseConfiguration(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureMundoUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyMundoUltimateBaseConfiguration(skill);

            ResetUltimateDecision(skill);
            skill.ultimateDecision.targetingType = UltimateTargetingType.Self;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AllMustPass;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.SelfLowHealth;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.45f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.secondaryCondition.searchRadius = 6.5f;
            skill.ultimateDecision.secondaryCondition.requiredUnitCount = 2;
            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.LowerPrimaryThreshold;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 45f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 1;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = 0.6f;
            // Stage-01's shared ultimate template cannot express the exact
            // "(<=25% HP and 1 enemy) OR (<=45% HP and 2 enemies)" pairing
            // without hero-only branching, so Mundo currently uses the default
            // 45%/2-enemy gate and relaxes to 60%/1-enemy late in regulation.
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyMundoUltimateBaseConfiguration(SkillData skill)
        {
            skill.description = "Stage-01 demo skill: rapidly heal self for a short duration based on max health.";
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.fallbackTargetType = SkillTargetType.None;
            skill.castRange = 0f;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = true;
            skill.effects.Clear();

            var selfHealOverTime = AddApplyStatusEffectsEffect(skill);
            selfHealOverTime.targetMode = SkillEffectTargetMode.Caster;
            selfHealOverTime.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.HealOverTime,
                durationSeconds = 6f,
                magnitude = 0f,
                targetMaxHealthMultiplier = 0.06f,
                tickIntervalSeconds = 0.5f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
        }

        private static SkillData CreateBlastshieldActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_blastshield_active_shieldbrace",
                "Shield Brace",
                SkillSlotType.ActiveSkill,
                SkillType.Buff,
                SkillTargetType.NearestEnemy,
                3.0f,
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

            skill.description = "Stage-01 demo skill: brace behind the shield, gaining defense and slowing down while countering non-projectile basic attacks from enemy melee heroes.";
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.fallbackTargetType = SkillTargetType.CurrentEnemyTarget;
            skill.castRange = 3.0f;
            skill.areaRadius = 0f;
            skill.cooldownSeconds = 8f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();

            var braceStatuses = AddApplyStatusEffectsEffect(skill);
            braceStatuses.targetMode = SkillEffectTargetMode.Caster;
            braceStatuses.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.DefenseModifier,
                durationSeconds = 4f,
                magnitude = 1.8f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });
            braceStatuses.statusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.MoveSpeedModifier,
                durationSeconds = 4f,
                magnitude = -0.65f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            ResetReactiveCounter(skill);
            skill.reactiveCounter.enabled = true;
            skill.reactiveCounter.durationSeconds = 4f;
            skill.reactiveCounter.blocksBasicAttacks = true;
            skill.reactiveCounter.blocksSkillCasts = true;
            skill.reactiveCounter.triggerOnBasicAttackDamage = true;
            skill.reactiveCounter.requireNonProjectileBasicAttack = true;
            skill.reactiveCounter.sourceTriggerCooldownSeconds = 0.8f;
            skill.reactiveCounter.counterDamagePowerMultiplier = 0.75f;
            skill.reactiveCounter.forcedMovementDistance = 1.8f;
            skill.reactiveCounter.forcedMovementDurationSeconds = 0.25f;
            skill.reactiveCounter.forcedMovementPeakHeight = 0f;
            skill.reactiveCounter.onTriggerStatusEffects.Add(new StatusEffectData
            {
                effectType = StatusEffectType.HealTakenModifier,
                durationSeconds = 3f,
                magnitude = -0.5f,
                maxStacks = 1,
                refreshDurationOnReapply = true,
            });

            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData CreateBlastshieldUltimateSkill(bool overwriteExistingContent, out bool existedBefore)
        {
            var skill = CreateSkill(
                "skill_blastshield_ultimate_blastminefield",
                "Blast Minefield",
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

            ApplyBlastshieldUltimateBaseConfiguration(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillData ConfigureBlastshieldUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            ApplyBlastshieldUltimateBaseConfiguration(skill);
            ResetUltimateDecision(skill);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void ApplyBlastshieldUltimateBaseConfiguration(SkillData skill)
        {
            skill.description = "Stage-01 demo skill: scatter fixed mines into a random forward field; each mine persists until an enemy steps near it, then explodes for area damage.";
            skill.activationMode = SkillActivationMode.Active;
            skill.skillType = SkillType.Buff;
            skill.targetType = SkillTargetType.NearestEnemy;
            skill.fallbackTargetType = SkillTargetType.CurrentEnemyTarget;
            skill.castRange = Stage01ArenaSpec.FullMapTargetingRangeWorldUnits;
            skill.areaRadius = 0f;
            skill.minTargetsToCast = 1;
            skill.allowsSelfCast = false;
            skill.effects.Clear();
            ResetReactiveCounter(skill);

            var minefield = AddDeployableProxyEffect(
                skill,
                powerMultiplier: 2.4f,
                strikeRadius: 2.0f,
                durationSeconds: 0f,
                maxCount: 7,
                spawnMode: DeployableProxySpawnMode.RandomForwardArea,
                spawnOffsetDistance: 0f,
                triggerMode: DeployableProxyTriggerMode.ProximityExplosion,
                replaceOldestWhenLimitReached: true,
                immediateStrikeOnSpawn: false);
            minefield.targetMode = SkillEffectTargetMode.PrimaryTarget;
            minefield.radiusOverride = 2.0f;
            minefield.persistentAreaTargetType = PersistentAreaTargetType.Enemies;
            minefield.deployableProxySpawnCount = 7;
            minefield.deployableProxyPersistUntilTriggered = true;
            minefield.deployableProxyTriggerRadius = 1.0f;
            minefield.deployableProxyEffectRadius = 2.0f;
            minefield.deployableProxyRandomForwardMinDistance = 2.5f;
            minefield.deployableProxyRandomForwardMaxDistance = 8.0f;
            minefield.deployableProxyRandomForwardWidth = 5.5f;
            minefield.deployableProxyRandomForwardMinSpacing = 2.0f;
            minefield.deployableProxySpawnVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanActiveTargetIndicatorVfxPrefabPath);
            minefield.deployableProxyLoopVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanActiveTargetIndicatorVfxPrefabPath);
            minefield.deployableProxyRemovalVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflemanUltimateAreaVfxPrefabPath);
            minefield.deployableProxyVfxLocalOffset = new Vector3(0f, 0.08f, 0f);
            minefield.deployableProxyVfxEulerAngles = Vector3.zero;
            minefield.deployableProxyVfxScaleMultiplier = new Vector3(0.55f, 0.55f, 1f);
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
            if (basicAttack == null)
            {
                return;
            }

            if (basicAttack.onHitStatusEffects == null)
            {
                basicAttack.onHitStatusEffects = new List<StatusEffectData>();
            }

            if (basicAttack.variants == null)
            {
                basicAttack.variants = new List<BasicAttackVariantData>();
            }

            if (basicAttack.bounce == null)
            {
                basicAttack.bounce = new BasicAttackBounceData();
            }

            if (basicAttack.sameTargetStacking == null)
            {
                basicAttack.sameTargetStacking = new BasicAttackSameTargetStackData();
            }
        }

        private static void ResetSameTargetStacking(BasicAttackData basicAttack)
        {
            if (basicAttack == null)
            {
                return;
            }

            EnsureBasicAttackStatusList(basicAttack);
            basicAttack.sameTargetStacking.enabled = false;
            basicAttack.sameTargetStacking.maxStacks = 1;
            basicAttack.sameTargetStacking.modifierEffectType = StatusEffectType.AttackSpeedModifier;
            basicAttack.sameTargetStacking.magnitudePerStack = 0f;
            basicAttack.sameTargetStacking.targetRetentionRange = 0f;
            basicAttack.sameTargetStacking.fullStackOverrideStatusEffectType = StatusEffectType.None;
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
            input.blueTeam.ultimateTimingStrategy = BattleUltimateTimingStrategy.Standard;
            input.blueTeam.ultimateComboStrategy = BattleUltimateComboStrategy.Standard;
            input.blueTeam.heroes.Clear();
            input.blueTeam.heroes.AddRange(new[] { tank, warrior, mage, support, marksman });

            input.redTeam.side = TeamSide.Red;
            input.redTeam.ultimateTimingStrategy = BattleUltimateTimingStrategy.Standard;
            input.redTeam.ultimateComboStrategy = BattleUltimateComboStrategy.Standard;
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
                resourcesInput.blueTeam.ultimateTimingStrategy = input.blueTeam.ultimateTimingStrategy;
                resourcesInput.blueTeam.ultimateComboStrategy = input.blueTeam.ultimateComboStrategy;
                resourcesInput.blueTeam.heroes.Clear();
                resourcesInput.blueTeam.heroes.AddRange(input.blueTeam.heroes);

                resourcesInput.redTeam.side = input.redTeam.side;
                resourcesInput.redTeam.ultimateTimingStrategy = input.redTeam.ultimateTimingStrategy;
                resourcesInput.redTeam.ultimateComboStrategy = input.redTeam.ultimateComboStrategy;
                resourcesInput.redTeam.heroes.Clear();
                resourcesInput.redTeam.heroes.AddRange(input.redTeam.heroes);

                EditorUtility.SetDirty(resourcesInput);
            }

            return input;
        }

        private static HeroCatalogData CreateHeroCatalog(params HeroDefinition[] heroes)
        {
            var catalog = LoadOrCreateAsset<HeroCatalogData>(DefaultHeroCatalogAssetPath);
            var desiredHeroes = new List<HeroDefinition>();
            if (heroes != null)
            {
                for (var i = 0; i < heroes.Length; i++)
                {
                    var hero = heroes[i];
                    if (hero == null || desiredHeroes.Contains(hero))
                    {
                        continue;
                    }

                    desiredHeroes.Add(hero);
                }
            }

            if (HasSameHeroCatalogEntries(catalog.heroes, desiredHeroes))
            {
                return catalog;
            }

            catalog.heroes.Clear();
            catalog.heroes.AddRange(desiredHeroes);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static AthleteRosterData CreateAthleteRoster(bool overwriteExistingContent)
        {
            if (!overwriteExistingContent && TryLoadAsset(DefaultAthleteRosterAssetPath, out AthleteRosterData existingRoster))
            {
                return existingRoster;
            }

            var roster = LoadOrCreateAsset<AthleteRosterData>(DefaultAthleteRosterAssetPath);
            roster.blueTeamAthletes.Clear();
            roster.blueTeamAthletes.AddRange(new[]
            {
                WithTraits(CreateAthlete("blue_001_ren", "Ren", "Blue", 24f, 43f, 12f,
                    Mastery("tank_001_ironwall", 24f),
                    Mastery("tank_002_shieldwarden", 18f),
                    Mastery("tank_003_tidehunter", 16f),
                    Mastery("warrior_001_skybreaker", 10f)),
                    AthleteTraitCatalog.HeavyShieldTraitId),
                WithTraits(CreateAthlete("blue_002_mika", "Mika", "Blue", 38f, 27f, -6f,
                    Mastery("warrior_001_skybreaker", 28f),
                    Mastery("warrior_003_berserker", 22f),
                    Mastery("assassin_001_shadowstep", 12f),
                    Mastery("tank_005_blastshield", 10f)),
                    AthleteTraitCatalog.LateBloomingTraitId,
                    AthleteTraitCatalog.FastHandsTraitId),
                WithTraits(CreateAthlete("blue_003_cora", "Cora", "Blue", 45f, 18f, 8f,
                    Mastery("mage_001_firemage", 30f),
                    Mastery("mage_002_frostmage", 22f),
                    Mastery("mage_004_lightningmage", 18f),
                    Mastery("marksman_002_rifleman", 10f)),
                    AthleteTraitCatalog.FavoriteBlueTraitId),
                WithTraits(CreateAthlete("blue_004_sena", "Sena", "Blue", 22f, 35f, 18f,
                    Mastery("support_001_sunpriest", 32f),
                    Mastery("support_003_monk", 24f),
                    Mastery("support_002_windchime", 18f),
                    Mastery("support_004_shrinemaiden", 14f)),
                    AthleteTraitCatalog.MediumShieldTraitId),
                WithTraits(CreateAthlete("blue_005_tao", "Tao", "Blue", 41f, 24f, 5f,
                    Mastery("marksman_001_longshot", 34f),
                    Mastery("marksman_003_venomshooter", 20f),
                    Mastery("marksman_004_boomeranger", 18f),
                    Mastery("marksman_005_sniper", 16f)),
                    AthleteTraitCatalog.WindStepTraitId),
            });

            roster.redTeamAthletes.Clear();
            roster.redTeamAthletes.AddRange(new[]
            {
                WithTraits(CreateAthlete("red_001_axel", "Axel", "Red", 26f, 45f, -4f,
                    Mastery("tank_001_ironwall", 20f),
                    Mastery("tank_004_mundo", 28f),
                    Mastery("tank_003_tidehunter", 22f),
                    Mastery("warrior_005_trollwarlord", 12f)),
                    AthleteTraitCatalog.HeavyShieldTraitId),
                WithTraits(CreateAthlete("red_002_nox", "Nox", "Red", 44f, 20f, 16f,
                    Mastery("assassin_001_shadowstep", 35f),
                    Mastery("assassin_003_butcher", 22f),
                    Mastery("assassin_004_loner", 20f),
                    Mastery("warrior_002_bladesman", 12f)),
                    AthleteTraitCatalog.FavoriteRedTraitId,
                    AthleteTraitCatalog.WindStepTraitId),
                WithTraits(CreateAthlete("red_003_luma", "Luma", "Red", 40f, 22f, -12f,
                    Mastery("mage_001_firemage", 18f),
                    Mastery("mage_003_sandemperor", 30f),
                    Mastery("mage_004_lightningmage", 24f),
                    Mastery("support_006_commander", 12f)),
                    AthleteTraitCatalog.LateBloomingTraitId,
                    AthleteTraitCatalog.FavoriteRedTraitId),
                WithTraits(CreateAthlete("red_004_iris", "Iris", "Red", 20f, 38f, 10f,
                    Mastery("support_001_sunpriest", 20f),
                    Mastery("support_002_windchime", 30f),
                    Mastery("support_006_commander", 24f),
                    Mastery("support_005_chef", 14f)),
                    AthleteTraitCatalog.MediumShieldTraitId),
                WithTraits(CreateAthlete("red_005_vex", "Vex", "Red", 46f, 16f, 4f,
                    Mastery("marksman_001_longshot", 20f),
                    Mastery("marksman_002_rifleman", 32f),
                    Mastery("marksman_005_sniper", 24f),
                    Mastery("marksman_003_venomshooter", 18f)),
                    AthleteTraitCatalog.FastHandsTraitId,
                    AthleteTraitCatalog.LightShieldTraitId),
            });

            EditorUtility.SetDirty(roster);
            return roster;
        }

        private static AthleteDefinition CreateAthlete(
            string athleteId,
            string displayName,
            string teamName,
            float attack,
            float defense,
            float condition,
            params HeroMasteryEntry[] masteries)
        {
            var athlete = new AthleteDefinition
            {
                athleteId = athleteId,
                displayName = displayName,
                teamName = teamName,
                attack = Mathf.Clamp(attack, 0f, 50f),
                defense = Mathf.Clamp(defense, 0f, 50f),
                condition = Mathf.Clamp(condition, -50f, 50f),
                heroMasteries = new List<HeroMasteryEntry>(),
                traitIds = new List<string>(),
            };

            if (masteries != null)
            {
                for (var i = 0; i < masteries.Length && athlete.heroMasteries.Count < 4; i++)
                {
                    if (masteries[i] != null)
                    {
                        athlete.heroMasteries.Add(masteries[i]);
                    }
                }
            }

            return athlete;
        }

        private static AthleteDefinition WithTraits(AthleteDefinition athlete, params string[] traitIds)
        {
            if (athlete == null)
            {
                return null;
            }

            if (athlete.traitIds == null)
            {
                athlete.traitIds = new List<string>();
            }

            if (traitIds == null)
            {
                return athlete;
            }

            for (var i = 0; i < traitIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(traitIds[i]))
                {
                    athlete.traitIds.Add(traitIds[i]);
                }
            }

            return athlete;
        }

        private static HeroMasteryEntry Mastery(string heroId, float mastery)
        {
            return new HeroMasteryEntry
            {
                heroId = heroId,
                mastery = Mathf.Clamp(mastery, 0f, 50f),
            };
        }

        private static bool HasSameHeroCatalogEntries(IList<HeroDefinition> existingHeroes, IList<HeroDefinition> desiredHeroes)
        {
            if (ReferenceEquals(existingHeroes, desiredHeroes))
            {
                return true;
            }

            if (existingHeroes == null || desiredHeroes == null || existingHeroes.Count != desiredHeroes.Count)
            {
                return false;
            }

            for (var i = 0; i < existingHeroes.Count; i++)
            {
                if (existingHeroes[i] != desiredHeroes[i])
                {
                    return false;
                }
            }

            return true;
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
                "skill_spellblade_active_riftwave" => "Rift Wave",
                "skill_spellblade_ultimate_boundblade" => "Bound Blade",
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
                "skill_butcher_active_gorehook" => "assassin_003_butcher",
                "skill_butcher_ultimate_carnagereel" => "assassin_003_butcher",
                "skill_loner_active_lonepursuit" => "assassin_004_loner",
                "skill_loner_ultimate_loneinstinct" => "assassin_004_loner",
                "skill_demon_active_infernalexchange" => "assassin_005_demon",
                "skill_demon_ultimate_greaterdemonform" => "assassin_005_demon",
                "skill_shieldwarden_active_wardenscall" => "tank_002_shieldwarden",
                "skill_shieldwarden_ultimate_lastbastion" => "tank_002_shieldwarden",
                "skill_windchime_active_echocanopy" => "support_002_windchime",
                "skill_windchime_ultimate_stillwinddomain" => "support_002_windchime",
                "skill_monk_active_renewingpulse" => "support_003_monk",
                "skill_monk_ultimate_guardianmantra" => "support_003_monk",
                "skill_shrinemaiden_active_prayerbloom" => "support_004_shrinemaiden",
                "skill_shrinemaiden_ultimate_twinritetotem" => "support_004_shrinemaiden",
                "skill_commander_active_battleorders" => "support_006_commander",
                "skill_commander_ultimate_focusfirecommand" => "support_006_commander",
                "skill_rifleman_active_burstfire" => "marksman_002_rifleman",
                "skill_rifleman_ultimate_fraggrenade" => "marksman_002_rifleman",
                "skill_venomshooter_active_poisonmist" => "marksman_003_venomshooter",
                "skill_venomshooter_ultimate_venomdetonation" => "marksman_003_venomshooter",
                "skill_boomeranger_active_returningwheel" => "marksman_004_boomeranger",
                "skill_boomeranger_ultimate_wheelstorm" => "marksman_004_boomeranger",
                "skill_sniper_active_deadeyeshot" => "marksman_005_sniper",
                "skill_sniper_ultimate_killzone" => "marksman_005_sniper",
                "skill_sandemperor_active_raisesandguard" => "mage_003_sandemperor",
                "skill_sandemperor_ultimate_imperialencirclement" => "mage_003_sandemperor",
                "skill_lightningmage_active_thunderline" => "mage_004_lightningmage",
                "skill_lightningmage_ultimate_stormverdict" => "mage_004_lightningmage",
                "skill_berserker_active_bloodfury" => "warrior_003_berserker",
                "skill_berserker_ultimate_titanrage" => "warrior_003_berserker",
                "skill_spellblade_active_riftwave" => "warrior_004_spellblade",
                "skill_spellblade_ultimate_boundblade" => "warrior_004_spellblade",
                "skill_trollwarlord_active_warlordreach" => "warrior_005_trollwarlord",
                "skill_trollwarlord_ultimate_deathlessfrenzy" => "warrior_005_trollwarlord",
                "skill_tidehunter_active_undertowcarapace" => "tank_003_tidehunter",
                "skill_tidehunter_ultimate_tidalrebound" => "tank_003_tidehunter",
                "skill_mundo_active_brutemetabolism" => "tank_004_mundo",
                "skill_mundo_ultimate_monstrousrecovery" => "tank_004_mundo",
                "skill_blastshield_active_shieldbrace" => "tank_005_blastshield",
                "skill_blastshield_ultimate_blastminefield" => "tank_005_blastshield",
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
