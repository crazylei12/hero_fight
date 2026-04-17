using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Fight.Data;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    internal static class BalanceSheetService
    {
        public const string DefaultRelativeFolder = "BalanceSheets/Stage01";

        private const string DataSearchRoot = "Assets/Data";
        private const string HeroesFileName = "heroes.csv";
        private const string BasicAttacksFileName = "basic_attacks.csv";
        private const string SkillsFileName = "skills.csv";
        private const string SkillEffectsFileName = "skill_effects.csv";
        private const string SkillStatusEffectsFileName = "skill_status_effects.csv";
        private const string ReadmeFileName = "README_批量调数说明.md";

        private static readonly CsvColumn[] HeroColumns =
        {
            new("heroId", "英雄ID", "唯一且稳定的英雄ID，用来定位资产。"),
            new("displayName", "英雄名", "显示名，主要用于人工识别。"),
            new("heroClass", "职业", "职业枚举，通常只读。"),
            new("maxHealth", "最大生命", "基础最大生命值。"),
            new("attackPower", "攻击力", "基础攻击力。"),
            new("defense", "防御力", "基础防御力。"),
            new("attackSpeed", "攻速", "基础攻速，实际普攻节奏主要由它决定。"),
            new("moveSpeed", "移速", "基础移动速度。"),
            new("criticalChance", "暴击率", "0 到 1 之间的小数，例如 0.25 = 25%。"),
            new("criticalDamageMultiplier", "暴击伤害", "暴击伤害倍率，例如 1.5。"),
            new("attackRange", "攻击距离", "基础攻击距离。"),
        };

        private static readonly CsvColumn[] BasicAttackColumns =
        {
            new("heroId", "英雄ID", "唯一且稳定的英雄ID，用来定位资产。"),
            new("displayName", "英雄名", "显示名，主要用于人工识别。"),
            new("effectType", "普攻效果类型", "枚举值，建议保留左侧英文键。"),
            new("targetType", "普攻目标类型", "枚举值，建议保留左侧英文键。"),
            new("damageMultiplier", "普攻倍率", "普攻伤害或治疗倍率。"),
            new("attackInterval", "旧版攻间隔", "兼容字段，通常优先调 attackSpeed。"),
            new("rangeOverride", "普攻射程覆盖", "0 表示沿用基础 attackRange。"),
            new("usesProjectile", "是否投射物", "布尔值，支持 是/否、true/false、1/0。"),
            new("projectileSpeed", "投射物速度", "投射物飞行速度。"),
        };

        private static readonly CsvColumn[] SkillColumns =
        {
            new("skillId", "技能ID", "唯一且稳定的技能ID，用来定位资产。"),
            new("displayName", "技能名", "显示名，主要用于人工识别。"),
            new("slotType", "技能槽位", "枚举值，建议保留左侧英文键。"),
            new("skillType", "技能类型", "枚举值，建议保留左侧英文键。"),
            new("targetType", "目标类型", "枚举值，建议保留左侧英文键。"),
            new("castRange", "施法距离", "技能施法距离。"),
            new("areaRadius", "范围半径", "技能范围半径。"),
            new("cooldownSeconds", "冷却秒数", "技能冷却时间。"),
            new("minTargetsToCast", "最少目标数", "满足该人数条件后才施放。"),
            new("allowsSelfCast", "允许自施法", "布尔值，支持 是/否、true/false、1/0。"),
            new("skillAreaPresentationType", "范围表现类型", "表现层范围显示类型。"),
        };

        private static readonly CsvColumn[] SkillEffectColumns =
        {
            new("skillId", "技能ID", "唯一且稳定的技能ID，用来定位资产。"),
            new("displayName", "技能名", "显示名，主要用于人工识别。"),
            new("effectIndex", "效果序号", "从 0 开始；只能修改已有项，或在末尾连续追加 1 个 effect。"),
            new("effectType", "效果类型", "枚举值，建议保留左侧英文键。"),
            new("targetMode", "效果目标模式", "枚举值，建议保留左侧英文键。"),
            new("powerMultiplier", "倍率", "直接伤害/治疗等效果的倍率。"),
            new("radiusOverride", "半径覆盖", "为 0 时通常沿用技能主半径。"),
            new("durationSeconds", "持续时间", "持续区域或持续效果时长。"),
            new("tickIntervalSeconds", "跳动间隔", "周期效果的 tick 间隔。"),
            new("followCaster", "跟随施法者", "持续区域是否跟随施法者。"),
            new("persistentAreaPulseEffectType", "区域脉冲类型", "持续区域每次脉冲做什么。"),
            new("persistentAreaTargetType", "区域目标阵营", "持续区域命中敌方、友方或双方。"),
            new("forcedMovementDirection", "强制位移方向", "推开还是拉近。"),
            new("forcedMovementDistance", "强制位移距离", "水平位移距离。"),
            new("forcedMovementDurationSeconds", "强制位移时长", "位移过程持续秒数。"),
            new("forcedMovementPeakHeight", "强制位移峰值高度", "表现层抬升高度。"),
        };

        private static readonly CsvColumn[] SkillStatusEffectColumns =
        {
            new("skillId", "技能ID", "唯一且稳定的技能ID，用来定位资产。"),
            new("displayName", "技能名", "显示名，主要用于人工识别。"),
            new("effectIndex", "效果序号", "挂在哪个 SkillEffectData 下。"),
            new("statusIndex", "状态序号", "从 0 开始；只能修改已有项，或在末尾连续追加 1 个 status。"),
            new("effectType", "状态类型", "枚举值，建议保留左侧英文键。"),
            new("durationSeconds", "持续时间", "状态持续秒数。"),
            new("magnitude", "数值强度", "属性类状态通常是小数百分比；护盾是原始值。"),
            new("tickIntervalSeconds", "跳动间隔", "DOT/HOT 等周期状态的 tick 间隔。"),
            new("maxStacks", "最大层数", "状态最多可叠层数。"),
            new("refreshDurationOnReapply", "重上是否刷新", "重复施加时是否刷新持续时间。"),
        };

        public static void Export(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("导出目录不能为空。", nameof(folderPath));
            }

            var assetIndex = BuildAssetIndex();
            Directory.CreateDirectory(folderPath);

            WriteHeroesCsv(folderPath, assetIndex.Heroes.Values.OrderBy(hero => hero.heroId, StringComparer.OrdinalIgnoreCase));
            WriteBasicAttacksCsv(folderPath, assetIndex.Heroes.Values.OrderBy(hero => hero.heroId, StringComparer.OrdinalIgnoreCase));
            WriteSkillsCsv(folderPath, assetIndex.Skills.Values.OrderBy(skill => skill.skillId, StringComparer.OrdinalIgnoreCase));
            WriteSkillEffectsCsv(folderPath, assetIndex.Skills.Values.OrderBy(skill => skill.skillId, StringComparer.OrdinalIgnoreCase));
            WriteSkillStatusEffectsCsv(folderPath, assetIndex.Skills.Values.OrderBy(skill => skill.skillId, StringComparer.OrdinalIgnoreCase));
            WriteReadme(folderPath);

            AssetDatabase.Refresh();
            Debug.Log($"[BalanceSheets] 已导出批量调数表到 {folderPath}");
        }

        public static void Import(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("导入目录不能为空。", nameof(folderPath));
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"找不到批量调数目录：{folderPath}");
            }

            var assetIndex = BuildAssetIndex();
            var dirtyAssets = new HashSet<ScriptableObject>();
            var importedFiles = new List<string>();

            if (TryGetTablePath(folderPath, HeroesFileName, out var heroesPath))
            {
                ImportHeroes(heroesPath, assetIndex, dirtyAssets);
                importedFiles.Add(HeroesFileName);
            }

            if (TryGetTablePath(folderPath, BasicAttacksFileName, out var basicAttacksPath))
            {
                ImportBasicAttacks(basicAttacksPath, assetIndex, dirtyAssets);
                importedFiles.Add(BasicAttacksFileName);
            }

            if (TryGetTablePath(folderPath, SkillsFileName, out var skillsPath))
            {
                ImportSkills(skillsPath, assetIndex, dirtyAssets);
                importedFiles.Add(SkillsFileName);
            }

            if (TryGetTablePath(folderPath, SkillEffectsFileName, out var skillEffectsPath))
            {
                ImportSkillEffects(skillEffectsPath, assetIndex, dirtyAssets);
                importedFiles.Add(SkillEffectsFileName);
            }

            if (TryGetTablePath(folderPath, SkillStatusEffectsFileName, out var skillStatusEffectsPath))
            {
                ImportSkillStatusEffects(skillStatusEffectsPath, assetIndex, dirtyAssets);
                importedFiles.Add(SkillStatusEffectsFileName);
            }

            foreach (var asset in dirtyAssets)
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var importedSummary = importedFiles.Count > 0
                ? string.Join(", ", importedFiles)
                : "没有找到任何已知调数表";
            Debug.Log($"[BalanceSheets] 已从 {folderPath} 导入：{importedSummary}。变更资产数：{dirtyAssets.Count}");
        }

        private static void WriteHeroesCsv(string folderPath, IEnumerable<HeroDefinition> heroes)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var hero in heroes)
            {
                rows.Add(new[]
                {
                    hero.heroId,
                    hero.displayName,
                    FormatEnum(hero.heroClass),
                    FormatFloat(hero.baseStats.maxHealth),
                    FormatFloat(hero.baseStats.attackPower),
                    FormatFloat(hero.baseStats.defense),
                    FormatFloat(hero.baseStats.attackSpeed),
                    FormatFloat(hero.baseStats.moveSpeed),
                    FormatFloat(hero.baseStats.criticalChance),
                    FormatFloat(hero.baseStats.criticalDamageMultiplier),
                    FormatFloat(hero.baseStats.attackRange),
                });
            }

            WriteCsv(
                Path.Combine(folderPath, HeroesFileName),
                "英雄基础属性；heroId 用于定位资产，空白单元格导入时会跳过，不会清空原值。",
                HeroColumns,
                rows);
        }

        private static void WriteBasicAttacksCsv(string folderPath, IEnumerable<HeroDefinition> heroes)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var hero in heroes)
            {
                rows.Add(new[]
                {
                    hero.heroId,
                    hero.displayName,
                    FormatEnum(hero.basicAttack.effectType),
                    FormatEnum(hero.basicAttack.targetType),
                    FormatFloat(hero.basicAttack.damageMultiplier),
                    FormatFloat(hero.basicAttack.attackInterval),
                    FormatFloat(hero.basicAttack.rangeOverride),
                    FormatBool(hero.basicAttack.usesProjectile),
                    FormatFloat(hero.basicAttack.projectileSpeed),
                });
            }

            WriteCsv(
                Path.Combine(folderPath, BasicAttacksFileName),
                "英雄普攻参数；attackInterval 主要为兼容字段，通常优先调英雄 attackSpeed。",
                BasicAttackColumns,
                rows);
        }

        private static void WriteSkillsCsv(string folderPath, IEnumerable<SkillData> skills)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var skill in skills)
            {
                rows.Add(new[]
                {
                    skill.skillId,
                    skill.displayName,
                    FormatEnum(skill.slotType),
                    FormatEnum(skill.skillType),
                    FormatEnum(skill.targetType),
                    FormatFloat(skill.castRange),
                    FormatFloat(skill.areaRadius),
                    FormatFloat(skill.cooldownSeconds),
                    skill.minTargetsToCast.ToString(CultureInfo.InvariantCulture),
                    FormatBool(skill.allowsSelfCast),
                    FormatEnum(skill.skillAreaPresentationType),
                });
            }

            WriteCsv(
                Path.Combine(folderPath, SkillsFileName),
                "技能主配置；枚举和布尔列建议直接沿用导出内容修改。",
                SkillColumns,
                rows);
        }

        private static void WriteSkillEffectsCsv(string folderPath, IEnumerable<SkillData> skills)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var skill in skills)
            {
                for (var effectIndex = 0; effectIndex < skill.effects.Count; effectIndex++)
                {
                    var effect = skill.effects[effectIndex];
                    rows.Add(new[]
                    {
                        skill.skillId,
                        skill.displayName,
                        effectIndex.ToString(CultureInfo.InvariantCulture),
                        FormatEnum(effect.effectType),
                        FormatEnum(effect.targetMode),
                        FormatFloat(effect.powerMultiplier),
                        FormatFloat(effect.radiusOverride),
                        FormatFloat(effect.durationSeconds),
                        FormatFloat(effect.tickIntervalSeconds),
                        FormatBool(effect.followCaster),
                        FormatEnum(effect.persistentAreaPulseEffectType),
                        FormatEnum(effect.persistentAreaTargetType),
                        FormatEnum(effect.forcedMovementDirection),
                        FormatFloat(effect.forcedMovementDistance),
                        FormatFloat(effect.forcedMovementDurationSeconds),
                        FormatFloat(effect.forcedMovementPeakHeight),
                    });
                }
            }

            WriteCsv(
                Path.Combine(folderPath, SkillEffectsFileName),
                "技能效果列表；删除表格行不会自动删除资产里的旧效果，新增 effect 也只允许在末尾连续追加 1 个，不能跳号补洞。",
                SkillEffectColumns,
                rows);
        }

        private static void WriteSkillStatusEffectsCsv(string folderPath, IEnumerable<SkillData> skills)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var skill in skills)
            {
                for (var effectIndex = 0; effectIndex < skill.effects.Count; effectIndex++)
                {
                    var effect = skill.effects[effectIndex];
                    for (var statusIndex = 0; statusIndex < effect.statusEffects.Count; statusIndex++)
                    {
                        var statusEffect = effect.statusEffects[statusIndex];
                        rows.Add(new[]
                        {
                            skill.skillId,
                            skill.displayName,
                            effectIndex.ToString(CultureInfo.InvariantCulture),
                            statusIndex.ToString(CultureInfo.InvariantCulture),
                            FormatEnum(statusEffect.effectType),
                            FormatFloat(statusEffect.durationSeconds),
                            FormatFloat(statusEffect.magnitude),
                            FormatFloat(statusEffect.tickIntervalSeconds),
                            statusEffect.maxStacks.ToString(CultureInfo.InvariantCulture),
                            FormatBool(statusEffect.refreshDurationOnReapply),
                        });
                    }
                }
            }

            WriteCsv(
                Path.Combine(folderPath, SkillStatusEffectsFileName),
                "技能附带状态；删除表格行不会自动删除资产里的旧状态，新增 status 也只允许在末尾连续追加 1 个，不能跳号补洞。",
                SkillStatusEffectColumns,
                rows);
        }

        private static void WriteReadme(string folderPath)
        {
            var readmePath = Path.Combine(folderPath, ReadmeFileName);
            var content = string.Join(
                Environment.NewLine,
                "# Stage-01 批量调数说明",
                string.Empty,
                "导出文件：",
                "- heroes.csv：英雄基础属性",
                "- basic_attacks.csv：普攻参数",
                "- skills.csv：技能主配置",
                "- skill_effects.csv：技能效果列表",
                "- skill_status_effects.csv：技能附带状态列表",
                string.Empty,
                "使用规则：",
                "1. 表头格式是 `稳定英文键|中文名`，导入只识别 `|` 左侧。",
                "2. 枚举列默认导出为 `英文枚举|中文说明`。导入时推荐保留左侧英文键；如果只填中文，工具也会尝试识别。",
                "3. 布尔列支持 `是/否`、`true/false`、`1/0`。",
                "4. 空白单元格在导入时会被跳过，不会把原值清空。",
                "5. `skill_effects.csv` 和 `skill_status_effects.csv` 只允许在末尾连续追加新项，不允许跳号补洞；删除表格行也不会自动删除旧项。",
                "6. `skill_status_effects.csv` 不会凭空创建父 effect；如果目标 effect 不存在，需先在 `skill_effects.csv` 中创建它。",
                "7. 导出是只读文件输出，不会调用 SaveAssets 去顺手保存编辑器里其他脏资产。",
                "8. 当前构建流程和 demo 内容确保流程已经改成“只补缺失，不覆盖已有调数”；只有显式执行覆盖重建菜单时才会回到默认值。");

            File.WriteAllText(readmePath, content, new UTF8Encoding(true));
        }

        private static void ImportHeroes(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var heroId = RequireValue(row, "heroId", filePath);
                var hero = assetIndex.GetHero(heroId, filePath);

                if (TryGetNonEmptyValue(row, "displayName", out var displayName))
                {
                    hero.displayName = displayName;
                    dirtyAssets.Add(hero);
                }

                if (TryGetEnumValue(row, "heroClass", filePath, out HeroClass heroClass))
                {
                    hero.heroClass = heroClass;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "maxHealth", filePath, out var maxHealth))
                {
                    hero.baseStats.maxHealth = maxHealth;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "attackPower", filePath, out var attackPower))
                {
                    hero.baseStats.attackPower = attackPower;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "defense", filePath, out var defense))
                {
                    hero.baseStats.defense = defense;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "attackSpeed", filePath, out var attackSpeed))
                {
                    hero.baseStats.attackSpeed = attackSpeed;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "moveSpeed", filePath, out var moveSpeed))
                {
                    hero.baseStats.moveSpeed = moveSpeed;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "criticalChance", filePath, out var criticalChance))
                {
                    hero.baseStats.criticalChance = criticalChance;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "criticalDamageMultiplier", filePath, out var criticalDamageMultiplier))
                {
                    hero.baseStats.criticalDamageMultiplier = criticalDamageMultiplier;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "attackRange", filePath, out var attackRange))
                {
                    hero.baseStats.attackRange = attackRange;
                    dirtyAssets.Add(hero);
                }
            }
        }

        private static void ImportBasicAttacks(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var heroId = RequireValue(row, "heroId", filePath);
                var hero = assetIndex.GetHero(heroId, filePath);

                if (TryGetEnumValue(row, "effectType", filePath, out BasicAttackEffectType effectType))
                {
                    hero.basicAttack.effectType = effectType;
                    dirtyAssets.Add(hero);
                }

                if (TryGetEnumValue(row, "targetType", filePath, out BasicAttackTargetType targetType))
                {
                    hero.basicAttack.targetType = targetType;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "damageMultiplier", filePath, out var damageMultiplier))
                {
                    hero.basicAttack.damageMultiplier = damageMultiplier;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "attackInterval", filePath, out var attackInterval))
                {
                    hero.basicAttack.attackInterval = attackInterval;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "rangeOverride", filePath, out var rangeOverride))
                {
                    hero.basicAttack.rangeOverride = rangeOverride;
                    dirtyAssets.Add(hero);
                }

                if (TryGetBoolValue(row, "usesProjectile", filePath, out var usesProjectile))
                {
                    hero.basicAttack.usesProjectile = usesProjectile;
                    dirtyAssets.Add(hero);
                }

                if (TryGetFloatValue(row, "projectileSpeed", filePath, out var projectileSpeed))
                {
                    hero.basicAttack.projectileSpeed = projectileSpeed;
                    dirtyAssets.Add(hero);
                }
            }
        }

        private static void ImportSkills(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var skillId = RequireValue(row, "skillId", filePath);
                var skill = assetIndex.GetSkill(skillId, filePath);

                if (TryGetNonEmptyValue(row, "displayName", out var displayName))
                {
                    skill.displayName = displayName;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "slotType", filePath, out SkillSlotType slotType))
                {
                    skill.slotType = slotType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "skillType", filePath, out SkillType skillType))
                {
                    skill.skillType = skillType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "targetType", filePath, out SkillTargetType targetType))
                {
                    skill.targetType = targetType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "castRange", filePath, out var castRange))
                {
                    skill.castRange = castRange;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "areaRadius", filePath, out var areaRadius))
                {
                    skill.areaRadius = areaRadius;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "cooldownSeconds", filePath, out var cooldownSeconds))
                {
                    skill.cooldownSeconds = cooldownSeconds;
                    dirtyAssets.Add(skill);
                }

                if (TryGetIntValue(row, "minTargetsToCast", filePath, out var minTargetsToCast))
                {
                    skill.minTargetsToCast = minTargetsToCast;
                    dirtyAssets.Add(skill);
                }

                if (TryGetBoolValue(row, "allowsSelfCast", filePath, out var allowsSelfCast))
                {
                    skill.allowsSelfCast = allowsSelfCast;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "skillAreaPresentationType", filePath, out SkillAreaPresentationType presentationType))
                {
                    skill.skillAreaPresentationType = presentationType;
                    dirtyAssets.Add(skill);
                }
            }
        }

        private static void ImportSkillEffects(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var skillId = RequireValue(row, "skillId", filePath);
                var skill = assetIndex.GetSkill(skillId, filePath);
                var effectIndex = RequireIntValue(row, "effectIndex", filePath);
                var effect = GetOrAppendSkillEffect(skill, effectIndex, row);

                if (TryGetEnumValue(row, "effectType", filePath, out SkillEffectType effectType))
                {
                    effect.effectType = effectType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "targetMode", filePath, out SkillEffectTargetMode targetMode))
                {
                    effect.targetMode = targetMode;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "powerMultiplier", filePath, out var powerMultiplier))
                {
                    effect.powerMultiplier = powerMultiplier;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "radiusOverride", filePath, out var radiusOverride))
                {
                    effect.radiusOverride = radiusOverride;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "durationSeconds", filePath, out var durationSeconds))
                {
                    effect.durationSeconds = durationSeconds;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "tickIntervalSeconds", filePath, out var tickIntervalSeconds))
                {
                    effect.tickIntervalSeconds = tickIntervalSeconds;
                    dirtyAssets.Add(skill);
                }

                if (TryGetBoolValue(row, "followCaster", filePath, out var followCaster))
                {
                    effect.followCaster = followCaster;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "persistentAreaPulseEffectType", filePath, out PersistentAreaPulseEffectType pulseEffectType))
                {
                    effect.persistentAreaPulseEffectType = pulseEffectType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "persistentAreaTargetType", filePath, out PersistentAreaTargetType persistentAreaTargetType))
                {
                    effect.persistentAreaTargetType = persistentAreaTargetType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetEnumValue(row, "forcedMovementDirection", filePath, out ForcedMovementDirectionMode forcedMovementDirection))
                {
                    effect.forcedMovementDirection = forcedMovementDirection;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "forcedMovementDistance", filePath, out var forcedMovementDistance))
                {
                    effect.forcedMovementDistance = forcedMovementDistance;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "forcedMovementDurationSeconds", filePath, out var forcedMovementDurationSeconds))
                {
                    effect.forcedMovementDurationSeconds = forcedMovementDurationSeconds;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "forcedMovementPeakHeight", filePath, out var forcedMovementPeakHeight))
                {
                    effect.forcedMovementPeakHeight = forcedMovementPeakHeight;
                    dirtyAssets.Add(skill);
                }
            }
        }

        private static void ImportSkillStatusEffects(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var skillId = RequireValue(row, "skillId", filePath);
                var skill = assetIndex.GetSkill(skillId, filePath);
                var effectIndex = RequireIntValue(row, "effectIndex", filePath);
                var statusIndex = RequireIntValue(row, "statusIndex", filePath);
                var effect = RequireExistingSkillEffect(skill, effectIndex, row);
                var statusEffect = GetOrAppendStatusEffect(effect, statusIndex, row);

                if (TryGetEnumValue(row, "effectType", filePath, out StatusEffectType effectType))
                {
                    statusEffect.effectType = effectType;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "durationSeconds", filePath, out var durationSeconds))
                {
                    statusEffect.durationSeconds = durationSeconds;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "magnitude", filePath, out var magnitude))
                {
                    statusEffect.magnitude = magnitude;
                    dirtyAssets.Add(skill);
                }

                if (TryGetFloatValue(row, "tickIntervalSeconds", filePath, out var tickIntervalSeconds))
                {
                    statusEffect.tickIntervalSeconds = tickIntervalSeconds;
                    dirtyAssets.Add(skill);
                }

                if (TryGetIntValue(row, "maxStacks", filePath, out var maxStacks))
                {
                    statusEffect.maxStacks = maxStacks;
                    dirtyAssets.Add(skill);
                }

                if (TryGetBoolValue(row, "refreshDurationOnReapply", filePath, out var refreshDurationOnReapply))
                {
                    statusEffect.refreshDurationOnReapply = refreshDurationOnReapply;
                    dirtyAssets.Add(skill);
                }
            }
        }

        private static SkillEffectData GetOrAppendSkillEffect(SkillData skill, int effectIndex, RowData row)
        {
            if (effectIndex < 0)
            {
                throw CreateRowException(row, $"effectIndex 不能小于 0：{effectIndex}");
            }

            if (effectIndex < skill.effects.Count)
            {
                return skill.effects[effectIndex];
            }

            if (effectIndex > skill.effects.Count)
            {
                throw CreateRowException(
                    row,
                    $"effectIndex={effectIndex} 不能跳号追加。当前 skill.effects.Count={skill.effects.Count}，只允许修改已有项或在末尾连续追加 1 个 effect。");
            }

            if (!TryGetEnumValue(row, "effectType", row.FilePath, out SkillEffectType effectType))
            {
                throw CreateRowException(row, "新增 effect 时必须显式填写 effectType，避免把默认 DirectDamage 静默写进资产。");
            }

            var effect = new SkillEffectData
            {
                effectType = effectType,
            };
            skill.effects.Add(effect);
            return effect;
        }

        private static SkillEffectData RequireExistingSkillEffect(SkillData skill, int effectIndex, RowData row)
        {
            if (effectIndex < 0)
            {
                throw CreateRowException(row, $"effectIndex 不能小于 0：{effectIndex}");
            }

            if (effectIndex >= skill.effects.Count)
            {
                throw CreateRowException(
                    row,
                    $"skill_status_effects.csv 不能凭空创建父 effect。skillId={skill.skillId} 当前只有 {skill.effects.Count} 个 effect，但请求写入 effectIndex={effectIndex}。请先在 skill_effects.csv 中创建对应 effect，或确保资产里已存在该 effect。");
            }

            return skill.effects[effectIndex];
        }

        private static StatusEffectData GetOrAppendStatusEffect(SkillEffectData effect, int statusIndex, RowData row)
        {
            if (statusIndex < 0)
            {
                throw CreateRowException(row, $"statusIndex 不能小于 0：{statusIndex}");
            }

            if (statusIndex < effect.statusEffects.Count)
            {
                return effect.statusEffects[statusIndex];
            }

            if (statusIndex > effect.statusEffects.Count)
            {
                throw CreateRowException(
                    row,
                    $"statusIndex={statusIndex} 不能跳号追加。当前 effect.statusEffects.Count={effect.statusEffects.Count}，只允许修改已有项或在末尾连续追加 1 个 status。");
            }

            if (!TryGetEnumValue(row, "effectType", row.FilePath, out StatusEffectType statusEffectType))
            {
                throw CreateRowException(row, "新增 status 时必须显式填写 effectType，避免把默认 None 静默写进资产。");
            }

            var statusEffect = new StatusEffectData
            {
                effectType = statusEffectType,
            };
            effect.statusEffects.Add(statusEffect);
            return statusEffect;
        }

        private static AssetIndex BuildAssetIndex()
        {
            var heroes = new Dictionary<string, HeroDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var hero in LoadAssets<HeroDefinition>())
            {
                if (hero == null || string.IsNullOrWhiteSpace(hero.heroId))
                {
                    continue;
                }

                if (heroes.ContainsKey(hero.heroId))
                {
                    throw new InvalidOperationException($"检测到重复 heroId：{hero.heroId}");
                }

                heroes.Add(hero.heroId, hero);
            }

            var skills = new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);
            foreach (var skill in LoadAssets<SkillData>())
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
                {
                    continue;
                }

                if (skills.ContainsKey(skill.skillId))
                {
                    throw new InvalidOperationException($"检测到重复 skillId：{skill.skillId}");
                }

                skills.Add(skill.skillId, skill);
            }

            return new AssetIndex(heroes, skills);
        }

        private static IEnumerable<T> LoadAssets<T>() where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DataSearchRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }

        private static bool TryGetTablePath(string folderPath, string fileName, out string filePath)
        {
            filePath = Path.Combine(folderPath, fileName);
            return File.Exists(filePath);
        }

        private static void WriteCsv(string filePath, string description, IReadOnlyList<CsvColumn> columns, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine(BuildCsvLine(new[] { "#说明" }.Concat(columns.Select(column => column.Description))));
            builder.AppendLine(BuildCsvLine(columns.Select(BuildHeaderCell)));
            if (!string.IsNullOrWhiteSpace(description))
            {
                builder.AppendLine(BuildCsvLine(new[] { "#表用途", description }));
            }

            foreach (var row in rows)
            {
                builder.AppendLine(BuildCsvLine(row));
            }

            File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(true));
        }

        private static IEnumerable<RowData> ReadRows(string filePath)
        {
            var rows = ParseCsv(File.ReadAllText(filePath, Encoding.UTF8));
            if (rows.Count == 0)
            {
                yield break;
            }

            var headerRowIndex = -1;
            List<string> headerRow = null;
            for (var i = 0; i < rows.Count; i++)
            {
                if (IsSkippableRow(rows[i]))
                {
                    continue;
                }

                headerRowIndex = i;
                headerRow = rows[i];
                break;
            }

            if (headerRowIndex < 0 || headerRow == null)
            {
                yield break;
            }

            var headers = headerRow.Select(NormalizeHeader).ToList();
            for (var rowIndex = headerRowIndex + 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsSkippableRow(row))
                {
                    continue;
                }

                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    var header = headers[columnIndex];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    cells[header] = value?.Trim() ?? string.Empty;
                }

                yield return new RowData(filePath, rowIndex + 1, cells);
            }
        }

        private static bool IsSkippableRow(IReadOnlyList<string> row)
        {
            if (row == null || row.Count == 0)
            {
                return true;
            }

            var firstNonEmptyCell = row.FirstOrDefault(cell => !string.IsNullOrWhiteSpace(cell));
            return string.IsNullOrWhiteSpace(firstNonEmptyCell) || firstNonEmptyCell.TrimStart().StartsWith("#", StringComparison.Ordinal);
        }

        private static List<List<string>> ParseCsv(string content)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var currentChar = content[index];
                if (inQuotes)
                {
                    if (currentChar == '"')
                    {
                        var hasEscapedQuote = index + 1 < content.Length && content[index + 1] == '"';
                        if (hasEscapedQuote)
                        {
                            currentCell.Append('"');
                            index++;
                            continue;
                        }

                        inQuotes = false;
                        continue;
                    }

                    currentCell.Append(currentChar);
                    continue;
                }

                switch (currentChar)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;
                    default:
                        currentCell.Append(currentChar);
                        break;
                }
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                rows.Add(currentRow);
            }

            return rows;
        }

        private static string BuildCsvLine(IEnumerable<string> cells)
        {
            return string.Join(",", cells.Select(EscapeCsvCell));
        }

        private static string EscapeCsvCell(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!needsQuotes)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string BuildHeaderCell(CsvColumn column)
        {
            return $"{column.Key}|{column.DisplayName}";
        }

        private static string NormalizeHeader(string headerCell)
        {
            if (string.IsNullOrWhiteSpace(headerCell))
            {
                return string.Empty;
            }

            var normalized = headerCell.Trim().Trim('\uFEFF');
            var separatorIndex = normalized.IndexOf('|');
            return separatorIndex >= 0
                ? normalized.Substring(0, separatorIndex).Trim()
                : normalized;
        }

        private static string RequireValue(RowData row, string key, string filePath)
        {
            if (!TryGetNonEmptyValue(row, key, out var value))
            {
                throw CreateRowException(row, $"缺少必填列 {key}。文件：{filePath}");
            }

            return value;
        }

        private static int RequireIntValue(RowData row, string key, string filePath)
        {
            if (!TryGetIntValue(row, key, filePath, out var value))
            {
                throw CreateRowException(row, $"缺少或无法解析整数列 {key}。文件：{filePath}");
            }

            return value;
        }

        private static bool TryGetNonEmptyValue(RowData row, string key, out string value)
        {
            if (row.Cells.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            {
                value = value.Trim();
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static bool TryGetFloatValue(RowData row, string key, string filePath, out float value)
        {
            value = default;
            if (!TryGetNonEmptyValue(row, key, out var raw))
            {
                return false;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            throw CreateRowException(row, $"无法解析浮点数列 {key} 的值：{raw}。文件：{filePath}");
        }

        private static bool TryGetIntValue(RowData row, string key, string filePath, out int value)
        {
            value = default;
            if (!TryGetNonEmptyValue(row, key, out var raw))
            {
                return false;
            }

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            throw CreateRowException(row, $"无法解析整数列 {key} 的值：{raw}。文件：{filePath}");
        }

        private static bool TryGetBoolValue(RowData row, string key, string filePath, out bool value)
        {
            value = default;
            if (!TryGetNonEmptyValue(row, key, out var raw))
            {
                return false;
            }

            var normalized = NormalizeEnumLikeValue(raw);
            switch (normalized.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                case "shi":
                case "是":
                    value = true;
                    return true;
                case "false":
                case "0":
                case "no":
                case "n":
                case "fou":
                case "否":
                    value = false;
                    return true;
            }

            throw CreateRowException(row, $"无法解析布尔列 {key} 的值：{raw}。文件：{filePath}");
        }

        private static bool TryGetEnumValue<TEnum>(RowData row, string key, string filePath, out TEnum value) where TEnum : struct, Enum
        {
            value = default;
            if (!TryGetNonEmptyValue(row, key, out var raw))
            {
                return false;
            }

            if (TryParseEnum(raw, out value))
            {
                return true;
            }

            throw CreateRowException(row, $"无法解析枚举列 {key} 的值：{raw}。文件：{filePath}");
        }

        private static bool TryParseEnum<TEnum>(string raw, out TEnum value) where TEnum : struct, Enum
        {
            value = default;
            var normalized = NormalizeEnumLikeValue(raw);

            if (Enum.TryParse(normalized, ignoreCase: true, out value))
            {
                return true;
            }

            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue) &&
                Enum.IsDefined(typeof(TEnum), intValue))
            {
                value = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
                return true;
            }

            foreach (var candidate in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                if (string.Equals(GetEnumDisplayName(candidate), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    value = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeEnumLikeValue(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var normalized = raw.Trim();
            var separatorIndex = normalized.IndexOf('|');
            if (separatorIndex >= 0)
            {
                return normalized.Substring(0, separatorIndex).Trim();
            }

            return normalized;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatBool(bool value)
        {
            return value ? "是" : "否";
        }

        private static string FormatEnum<TEnum>(TEnum value) where TEnum : Enum
        {
            return $"{value}|{GetEnumDisplayName(value)}";
        }

        private static string GetEnumDisplayName<TEnum>(TEnum value) where TEnum : Enum
        {
            var boxedValue = (Enum)(object)value;
            return boxedValue switch
            {
                HeroClass heroClass => heroClass switch
                {
                    HeroClass.Warrior => "战士",
                    HeroClass.Mage => "法师",
                    HeroClass.Assassin => "刺客",
                    HeroClass.Tank => "坦克",
                    HeroClass.Support => "辅助",
                    HeroClass.Marksman => "射手",
                    _ => value.ToString(),
                },
                BasicAttackEffectType basicAttackEffectType => basicAttackEffectType switch
                {
                    BasicAttackEffectType.Damage => "伤害",
                    BasicAttackEffectType.Heal => "治疗",
                    _ => value.ToString(),
                },
                BasicAttackTargetType basicAttackTargetType => basicAttackTargetType switch
                {
                    BasicAttackTargetType.NearestEnemy => "最近敌人",
                    BasicAttackTargetType.LowestHealthAlly => "最低生命友军",
                    _ => value.ToString(),
                },
                SkillSlotType skillSlotType => skillSlotType switch
                {
                    SkillSlotType.ActiveSkill => "小技能",
                    SkillSlotType.Ultimate => "大招",
                    _ => value.ToString(),
                },
                SkillType skillType => skillType switch
                {
                    SkillType.SingleTargetDamage => "单体伤害",
                    SkillType.AreaDamage => "范围伤害",
                    SkillType.SingleTargetHeal => "单体治疗",
                    SkillType.Dash => "突进",
                    SkillType.Buff => "增益",
                    SkillType.Stun => "眩晕",
                    SkillType.AreaHeal => "范围治疗",
                    SkillType.KnockUp => "击飞",
                    _ => value.ToString(),
                },
                SkillTargetType skillTargetType => skillTargetType switch
                {
                    SkillTargetType.None => "无",
                    SkillTargetType.Self => "自身",
                    SkillTargetType.NearestEnemy => "最近敌人",
                    SkillTargetType.LowestHealthEnemy => "最低生命敌人",
                    SkillTargetType.LowestHealthAlly => "最低生命友军",
                    SkillTargetType.DensestEnemyArea => "敌方最密集区域",
                    SkillTargetType.AllEnemies => "全体敌军",
                    SkillTargetType.AllAllies => "全体友军",
                    _ => value.ToString(),
                },
                SkillAreaPresentationType skillAreaPresentationType => skillAreaPresentationType switch
                {
                    SkillAreaPresentationType.None => "无",
                    SkillAreaPresentationType.FireSea => "火海",
                    _ => value.ToString(),
                },
                SkillEffectType skillEffectType => skillEffectType switch
                {
                    SkillEffectType.DirectDamage => "直接伤害",
                    SkillEffectType.DirectHeal => "直接治疗",
                    SkillEffectType.ApplyStatusEffects => "施加状态",
                    SkillEffectType.RepositionNearPrimaryTarget => "位移到主目标附近",
                    SkillEffectType.CreatePersistentArea => "创建持续区域",
                    SkillEffectType.ApplyForcedMovement => "施加强制位移",
                    _ => value.ToString(),
                },
                SkillEffectTargetMode skillEffectTargetMode => skillEffectTargetMode switch
                {
                    SkillEffectTargetMode.SkillTargets => "技能目标",
                    SkillEffectTargetMode.Caster => "施法者",
                    SkillEffectTargetMode.PrimaryTarget => "主目标",
                    SkillEffectTargetMode.EnemiesInRadiusAroundCaster => "施法者周围敌人",
                    SkillEffectTargetMode.AlliesInRadiusAroundCaster => "施法者周围友军",
                    SkillEffectTargetMode.DashPathEnemies => "突进路径敌人",
                    _ => value.ToString(),
                },
                PersistentAreaPulseEffectType pulseEffectType => pulseEffectType switch
                {
                    PersistentAreaPulseEffectType.None => "无",
                    PersistentAreaPulseEffectType.DirectDamage => "直接伤害",
                    PersistentAreaPulseEffectType.DirectHeal => "直接治疗",
                    _ => value.ToString(),
                },
                PersistentAreaTargetType persistentAreaTargetType => persistentAreaTargetType switch
                {
                    PersistentAreaTargetType.Enemies => "敌方",
                    PersistentAreaTargetType.Allies => "友方",
                    PersistentAreaTargetType.Both => "双方",
                    _ => value.ToString(),
                },
                ForcedMovementDirectionMode forcedMovementDirectionMode => forcedMovementDirectionMode switch
                {
                    ForcedMovementDirectionMode.AwayFromSource => "远离来源",
                    ForcedMovementDirectionMode.TowardSource => "拉向来源",
                    _ => value.ToString(),
                },
                StatusEffectType statusEffectType => statusEffectType switch
                {
                    StatusEffectType.None => "无",
                    StatusEffectType.AttackPowerModifier => "攻击力修正",
                    StatusEffectType.DefenseModifier => "防御力修正",
                    StatusEffectType.AttackSpeedModifier => "攻速修正",
                    StatusEffectType.MoveSpeedModifier => "移速修正",
                    StatusEffectType.Stun => "眩晕",
                    StatusEffectType.KnockUp => "击飞",
                    StatusEffectType.HealOverTime => "持续治疗",
                    StatusEffectType.DamageOverTime => "持续伤害",
                    StatusEffectType.Shield => "护盾",
                    StatusEffectType.Invulnerable => "无敌",
                    StatusEffectType.Untargetable => "不可选中",
                    StatusEffectType.MaxHealthModifier => "最大生命修正",
                    StatusEffectType.CriticalChanceModifier => "暴击率修正",
                    StatusEffectType.CriticalDamageModifier => "暴击伤害修正",
                    StatusEffectType.AttackRangeModifier => "攻击距离修正",
                    _ => value.ToString(),
                },
                _ => value.ToString(),
            };
        }

        private static Exception CreateRowException(RowData row, string message)
        {
            return new InvalidOperationException($"[BalanceSheets] {message} 位置：{row.FilePath} 第 {row.RowNumber} 行。");
        }

        private sealed class AssetIndex
        {
            public AssetIndex(IReadOnlyDictionary<string, HeroDefinition> heroes, IReadOnlyDictionary<string, SkillData> skills)
            {
                Heroes = heroes;
                Skills = skills;
            }

            public IReadOnlyDictionary<string, HeroDefinition> Heroes { get; }

            public IReadOnlyDictionary<string, SkillData> Skills { get; }

            public HeroDefinition GetHero(string heroId, string filePath)
            {
                if (Heroes.TryGetValue(heroId, out var hero))
                {
                    return hero;
                }

                throw new InvalidOperationException($"[BalanceSheets] 在 {filePath} 中引用了不存在的 heroId：{heroId}");
            }

            public SkillData GetSkill(string skillId, string filePath)
            {
                if (Skills.TryGetValue(skillId, out var skill))
                {
                    return skill;
                }

                throw new InvalidOperationException($"[BalanceSheets] 在 {filePath} 中引用了不存在的 skillId：{skillId}");
            }
        }

        private readonly struct CsvColumn
        {
            public CsvColumn(string key, string displayName, string description)
            {
                Key = key;
                DisplayName = displayName;
                Description = description;
            }

            public string Key { get; }

            public string DisplayName { get; }

            public string Description { get; }
        }

        private readonly struct RowData
        {
            public RowData(string filePath, int rowNumber, IReadOnlyDictionary<string, string> cells)
            {
                FilePath = filePath;
                RowNumber = rowNumber;
                Cells = cells;
            }

            public string FilePath { get; }

            public int RowNumber { get; }

            public IReadOnlyDictionary<string, string> Cells { get; }
        }
    }
}
