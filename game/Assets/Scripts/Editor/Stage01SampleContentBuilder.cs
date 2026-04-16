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
        private const string SupportPrefabPath = "Assets/Prefabs/Heroes/support_001_sunpriest/Sunpriest.prefab";
        private const string WarriorPrefabPath = "Assets/Prefabs/Heroes/warrior_001_bladeguard/Bladeguard.prefab";
        private const string MagePrefabPath = "Assets/HeroEditor4D/heroes/FIREMAGE.prefab";
        private const string TankPrefabPath = "Assets/Prefabs/Heroes/tank_001_ironwall/Ironwall.prefab";
        private const string HeroEditorControllerPath = "Assets/HeroEditor4D/Common/Animation/Controller.controller";
        private const string FireMageProjectilePrefabPath = "Assets/Prefabs/VFX/Projectiles/FireMageBasicAttackProjectile.prefab";
        private const string MageActiveAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/FireMageEmberBurst.prefab";
        private const string MageUltimateAreaVfxPrefabPath = "Assets/Prefabs/VFX/Skills/FireMageMeteorField.prefab";

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

        public static void GenerateDemoContent()
        {
            GenerateDemoContentInternal(overwriteExistingContent: false);
        }

        private static void GenerateDemoContentInternal(bool overwriteExistingContent)
        {
            EnsureFolders();

            var warriorActive = CreateSkill("skill_warrior_active_cleave", "Cleave", SkillSlotType.ActiveSkill, SkillType.AreaDamage, SkillTargetType.NearestEnemy, 2f, 2f, 1.3f, 7f, 1, overwriteExistingContent);
            var warriorUltimateSkill = CreateSkill("skill_warrior_ultimate_ironcrash", "Iron Crash", SkillSlotType.Ultimate, SkillType.AreaDamage, SkillTargetType.DensestEnemyArea, 2.4f, 3f, 2f, 0f, 2, overwriteExistingContent, out var warriorUltimateExisted);

            var warrior = CreateHero(
                "warrior_001_bladeguard",
                "Bladeguard",
                HeroClass.Warrior,
                420f, 38f, 24f, 1.1f, 4.2f, 0.15f, 1.6f, 1.7f,
                warriorActive,
                ConfigureWarriorUltimate(warriorUltimateSkill, overwriteExistingContent, warriorUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.SustainedDamage, HeroTag.Control);

            var mageUltimateSkill = CreateSkill("skill_mage_ultimate_meteor", "Meteor Fall", SkillSlotType.Ultimate, SkillType.AreaDamage, SkillTargetType.Self, 0f, 6f, 0.55f, 0f, 3, overwriteExistingContent, out var mageUltimateExisted);

            var mage = CreateHero(
                "mage_001_firemage",
                "FIREMAGE",
                HeroClass.Mage,
                300f, 48f, 10f, 0.8f, 3.8f, 0.08f, 1.5f, 5.8f,
                CreateMageActiveBurstSkill(overwriteExistingContent),
                ConfigureMageUltimate(mageUltimateSkill, overwriteExistingContent, mageUltimateExisted),
                overwriteExistingContent,
                HeroTag.Ranged, HeroTag.Burst, HeroTag.AreaDamage);

            CreateArchivedMageFireboltSkill(overwriteExistingContent);

            var assassinActive = CreateSkill("skill_assassin_active_dash", "Shadow Dash", SkillSlotType.ActiveSkill, SkillType.Dash, SkillTargetType.LowestHealthEnemy, 5.5f, 0f, 1.4f, 8f, 1, overwriteExistingContent);
            var assassinUltimateSkill = CreateSkill("skill_assassin_ultimate_execution", "Execution Mark", SkillSlotType.Ultimate, SkillType.SingleTargetDamage, SkillTargetType.LowestHealthEnemy, 5.5f, 0f, 2.8f, 0f, 1, overwriteExistingContent, out var assassinUltimateExisted);

            var assassin = CreateHero(
                "assassin_001_shadowstep",
                "Shadowstep",
                HeroClass.Assassin,
                290f, 52f, 8f, 1.2f, 5.4f, 0.2f, 1.8f, 1.3f,
                assassinActive,
                ConfigureAssassinUltimate(assassinUltimateSkill, overwriteExistingContent, assassinUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Dive, HeroTag.Burst);

            var tankActive = CreateStunSkill("skill_tank_active_shieldbash", "Shield Bash", SkillSlotType.ActiveSkill, 1.8f, 0f, 0.8f, 8f, 1f, overwriteExistingContent);
            var tankUltimateSkill = CreateBuffSkill("skill_tank_ultimate_ironoath", "Iron Oath", SkillSlotType.Ultimate, SkillTargetType.AllAllies, 6f, 6f, 1f, 0f, StatusEffectType.DefenseModifier, 8f, 2f, overwriteExistingContent, out var tankUltimateExisted);

            var tank = CreateHero(
                "tank_001_ironwall",
                "Ironwall",
                HeroClass.Tank,
                560f, 28f, 40f, 0.9f, 3.6f, 0.05f, 1.5f, 1.8f,
                tankActive,
                ConfigureTankUltimate(tankUltimateSkill, overwriteExistingContent, tankUltimateExisted),
                overwriteExistingContent,
                HeroTag.Melee, HeroTag.Control, HeroTag.Buff);

            var supportActive = CreateSupportActiveSkill(overwriteExistingContent);
            var supportUltimateSkill = CreateSupportUltimateSkill(overwriteExistingContent, out var supportUltimateExisted);

            var support = CreateHero(
                "support_001_sunpriest",
                "Sunpriest",
                HeroClass.Support,
                320f, 22f, 12f, 0.9f, 4.0f, 0.05f, 1.5f, 5.2f,
                supportActive,
                ConfigureSupportUltimate(supportUltimateSkill, overwriteExistingContent, supportUltimateExisted),
                overwriteExistingContent,
                out var supportHeroExisted,
                HeroTag.Ranged, HeroTag.Heal, HeroTag.Buff);
            ConfigureSupportBasicAttack(support, overwriteExistingContent, supportHeroExisted);

            var marksmanActive = CreateSkill("skill_marksman_active_focusshot", "Focus Shot", SkillSlotType.ActiveSkill, SkillType.SingleTargetDamage, SkillTargetType.NearestEnemy, 7.5f, 0f, 1.6f, 6f, 1, overwriteExistingContent);
            var marksmanUltimateSkill = CreateSkill("skill_marksman_ultimate_arrowrain", "Arrow Rain", SkillSlotType.Ultimate, SkillType.AreaDamage, SkillTargetType.DensestEnemyArea, 8f, 3f, 2.1f, 0f, 3, overwriteExistingContent, out var marksmanUltimateExisted);

            var marksman = CreateHero(
                "marksman_001_longshot",
                "Longshot",
                HeroClass.Marksman,
                310f, 34f, 12f, 1.35f, 4.1f, 0.22f, 1.9f, 7.2f,
                marksmanActive,
                ConfigureMarksmanUltimate(marksmanUltimateSkill, overwriteExistingContent, marksmanUltimateExisted),
                overwriteExistingContent,
                HeroTag.Ranged, HeroTag.SustainedDamage, HeroTag.AreaDamage);

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
            GenerateDemoContentInternal(overwriteExistingContent: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateDemoContentBatch()
        {
            GenerateDemoContentForBuild();
            EditorApplication.Exit(0);
        }

        private static void EnsureDemoContent()
        {
            var hasMainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
            var hasHeroSelectScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(HeroSelectScenePath) != null;
            var hasBattleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath) != null;
            var hasResultScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ResultScenePath) != null;
            var hasBasicAttackOnlyScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BasicAttackOnlyBattleScenePath) != null;
            var hasDefaultBattleInput = AssetDatabase.LoadAssetAtPath<BattleInputConfig>(DefaultBattleInputAssetPath) != null;

            if (hasMainMenuScene &&
                hasHeroSelectScene &&
                hasBattleScene &&
                hasResultScene &&
                hasBasicAttackOnlyScene &&
                hasDefaultBattleInput)
            {
                return;
            }

            GenerateDemoContent();
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
            hero.basicAttack.targetType = BasicAttackTargetType.NearestEnemy;

            hero.activeSkill = activeSkill;
            hero.ultimateSkill = ultimateSkill;
            hero.aiTemplateId = $"{heroClass.ToString().ToLowerInvariant()}_default";
            hero.usesSpecialLogic = false;
            hero.specialLogicNotes = string.Empty;
            hero.debugNotes = $"Stage-01 demo hero for {heroClass}.";
            var battlePrefab = LoadBattlePrefab(heroClass);
            hero.visualConfig.battlePrefab = battlePrefab;
            hero.visualConfig.animatorController = battlePrefab != null
                ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroEditorControllerPath)
                : null;
            hero.visualConfig.projectilePrefab = heroId == "mage_001_firemage"
                ? AssetDatabase.LoadAssetAtPath<GameObject>(FireMageProjectilePrefabPath)
                : null;
            hero.visualConfig.projectileAlignToMovement = heroId == "mage_001_firemage";
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            EditorUtility.SetDirty(hero);
            return hero;
        }

        private static GameObject LoadBattlePrefab(HeroClass heroClass)
        {
            var prefabPath = heroClass switch
            {
                HeroClass.Support => SupportPrefabPath,
                HeroClass.Warrior => WarriorPrefabPath,
                HeroClass.Mage => MagePrefabPath,
                HeroClass.Tank => TankPrefabPath,
                _ => null,
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
            skill.skillType = skillType;
            skill.targetType = targetType;
            skill.castRange = castRange;
            skill.areaRadius = areaRadius;
            skill.cooldownSeconds = slotType == SkillSlotType.Ultimate ? 0f : cooldownSeconds;
            skill.minTargetsToCast = minTargetsToCast;
            skill.effects.Clear();
            skill.allowsSelfCast = targetType == SkillTargetType.Self || targetType == SkillTargetType.AllAllies;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            ResetUltimateDecision(skill);

            AddDefaultEffectsForSkill(skill, powerMultiplier);

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
                6f,
                2f,
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
            skill.areaRadius = 2f;
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MageActiveAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectDamage, PersistentAreaTargetType.Enemies, 1.2f, 2f, 0.4f, 1f, false);
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

        private static SkillEffectData AddRepositionEffect(SkillData skill)
        {
            var effect = new SkillEffectData
            {
                effectType = SkillEffectType.RepositionNearPrimaryTarget,
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

        private static SkillData CreateSupportActiveSkill(bool overwriteExistingContent)
        {
            var skill = CreateSkill(
                "skill_support_active_heal",
                "Radiant Heal",
                SkillSlotType.ActiveSkill,
                SkillType.SingleTargetHeal,
                SkillTargetType.LowestHealthAlly,
                6f,
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
                6f,
                5f,
                0.65f,
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
            skill.castRange = 6f;
            skill.areaRadius = 5f;
            skill.allowsSelfCast = false;
            skill.persistentAreaVfxPrefab = null;
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectHeal, PersistentAreaTargetType.Allies, 0.65f, skill.areaRadius, 5f, 1f, false);
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

            skill.ultimateDecision.targetingType = UltimateTargetingType.UseSkillTargetType;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.PrimaryOnly;

            ResetUltimateCondition(skill.ultimateDecision.primaryCondition);
            ResetUltimateCondition(skill.ultimateDecision.secondaryCondition);

            skill.ultimateDecision.fallback.fallbackType = UltimateFallbackType.None;
            skill.ultimateDecision.fallback.triggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.overrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.overrideHealthPercentThreshold = -1f;
            skill.ultimateDecision.fallback.secondaryTriggerAfterSeconds = 0f;
            skill.ultimateDecision.fallback.secondaryOverrideRequiredUnitCount = 0;
            skill.ultimateDecision.fallback.secondaryOverrideHealthPercentThreshold = -1f;
        }

        private static void ResetUltimateCondition(UltimateConditionData condition)
        {
            condition.conditionType = UltimateConditionType.None;
            condition.searchRadius = 0f;
            condition.requiredUnitCount = 1;
            condition.healthPercentThreshold = 1f;
            condition.durationSeconds = 0f;
            condition.highValueTargetType = HighValueTargetType.None;
            condition.requireTargetInCastRange = true;
        }

        private static bool ShouldPreserveExistingAsset(bool overwriteExistingContent, bool existedBefore)
        {
            return !overwriteExistingContent && existedBefore;
        }

        private static SkillData ConfigureWarriorUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.ultimateDecision.targetingType = UltimateTargetingType.EnemyDensestPosition;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 2;
            ApplyCountFallback(skill, 30f, 1, 45f, 1);
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
            skill.areaRadius = 6f;
            skill.persistentAreaVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MageUltimateAreaVfxPrefabPath);
            skill.persistentAreaVfxScaleMultiplier = 1f;
            skill.persistentAreaVfxEulerAngles = Vector3.zero;
            skill.skillAreaPresentationType = SkillAreaPresentationType.None;
            skill.effects.Clear();
            AddPersistentAreaEffect(skill, PersistentAreaPulseEffectType.DirectDamage, PersistentAreaTargetType.Enemies, 0.55f, skill.areaRadius, 5f, 1f, false);

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

        private static SkillData ConfigureAssassinUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.ultimateDecision.targetingType = UltimateTargetingType.LowestHealthEnemyInRange;
            skill.ultimateDecision.combineMode = UltimateConditionCombineMode.AllMustPass;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyLowHealthInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = Mathf.Max(skill.castRange, 4f);
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 1;
            skill.ultimateDecision.primaryCondition.healthPercentThreshold = 0.35f;
            skill.ultimateDecision.secondaryCondition.conditionType = UltimateConditionType.TargetIsHighValue;
            skill.ultimateDecision.secondaryCondition.highValueTargetType = HighValueTargetType.Backline;
            skill.ultimateDecision.secondaryCondition.requireTargetInCastRange = true;
            ApplyHealthFallback(skill, 30f, 0.5f, 45f, 0.7f);
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
                magnitude = 2f,
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

        private static SkillData ConfigureMarksmanUltimate(SkillData skill, bool overwriteExistingContent, bool existedBefore)
        {
            if (ShouldPreserveExistingAsset(overwriteExistingContent, existedBefore))
            {
                return skill;
            }

            skill.ultimateDecision.targetingType = UltimateTargetingType.EnemyDensestPosition;
            skill.ultimateDecision.primaryCondition.conditionType = UltimateConditionType.EnemyCountInRange;
            skill.ultimateDecision.primaryCondition.searchRadius = skill.areaRadius;
            skill.ultimateDecision.primaryCondition.requiredUnitCount = 3;
            ApplyCountFallback(skill, 30f, 2, 45f, 1);
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

            var parts = skillId.Split('_');
            return parts.Length >= 2
                ? $"{parts[1]}_001_{GetDefaultHeroName(parts[1])}"
                : "shared";
        }

        private static string GetDefaultHeroName(string heroClassKey)
        {
            switch (heroClassKey)
            {
                case "warrior":
                    return "bladeguard";
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

        private static void SetPrivateObjectReference(Object targetObject, string propertyName, Object value)
        {
            var so = new SerializedObject(targetObject);
            var property = so.FindProperty(propertyName);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        private static void SetPrivateBool(Object targetObject, string propertyName, bool value)
        {
            var so = new SerializedObject(targetObject);
            var property = so.FindProperty(propertyName);
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        private static void SetPrivateString(Object targetObject, string propertyName, string value)
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
