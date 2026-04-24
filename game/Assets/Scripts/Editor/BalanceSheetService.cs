using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private const string SkillsFileName = "skills.csv";
        private const string BasicAttacksFileName = "basic_attacks.csv";
        private const string SkillEffectsFileName = "skill_effects.csv";
        private const string SkillStatusEffectsFileName = "skill_status_effects.csv";
        private const string ReadmeFileName = "README_批量调数说明.md";

        private static readonly Regex EffectValueColumnPattern = new Regex(
            @"^effect(?<effectIndex>\d+)(?<suffix>PowerMultiplier|RadiusOverride|DurationSeconds|TickIntervalSeconds|ForcedMovementDistance|ForcedMovementDurationSeconds|ForcedMovementPeakHeight)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex EffectStatusValueColumnPattern = new Regex(
            @"^effect(?<effectIndex>\d+)Status(?<statusIndex>\d+)(?<suffix>DurationSeconds|Magnitude|TickIntervalSeconds|MaxStacks)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly CsvColumn[] HeroColumns =
        {
            new("heroId", "英雄ID", "唯一且稳定的英雄ID，用来定位资产。"),
            new("displayName", "英雄名", "显示名，主要用于人工识别。"),
            new("heroClass", "职业", "只读说明列，帮助识别英雄职业。"),
            new("maxHealth", "最大生命", "基础最大生命值。"),
            new("attackPower", "攻击力", "基础攻击力。"),
            new("defense", "防御力", "基础防御力。"),
            new("attackSpeed", "攻速", "基础攻速。"),
            new("moveSpeed", "移速", "基础移动速度。"),
            new("criticalChance", "暴击率", "0 到 1 之间的小数，例如 0.25 = 25%。"),
            new("criticalDamageMultiplier", "暴击伤害", "暴击伤害倍率，例如 1.5。"),
            new("attackRange", "攻击距离", "基础攻击距离。"),
            new("basicAttackDamageMultiplier", "普攻倍率", "普攻伤害或治疗倍率。"),
            new("basicAttackRangeOverride", "普攻射程覆盖", "0 表示沿用基础攻击距离。"),
            new("basicAttackProjectileSpeed", "普攻投射物速度", "使用投射物时的飞行速度。"),
        };

        private static readonly CsvColumn[] SkillFixedColumns =
        {
            new("skillId", "技能ID", "唯一且稳定的技能ID，用来定位资产。"),
            new("displayName", "技能名", "显示名，主要用于人工识别。"),
            new("ownerHeroes", "所属英雄", "只读说明列，显示哪些英雄在使用这个技能。"),
            new("slotType", "技能槽位", "只读说明列，显示这个技能当前配置的小技能/大招槽位。"),
            new("castRange", "施法距离", "技能施法距离。"),
            new("areaRadius", "范围半径", "技能作用范围半径。"),
            new("cooldownSeconds", "冷却秒数", "技能冷却时间。"),
            new("minTargetsToCast", "最少目标数", "满足该人数条件后才施放。"),
            new("actionSequenceRepeatCount", "动作序列重复次数", "动作序列采用固定次数模式时的重复次数。"),
            new("actionSequenceDurationSeconds", "动作序列持续秒数", "动作序列采用固定时长模式时的总时长。"),
            new("actionSequenceIntervalSeconds", "动作序列间隔", "动作序列每次重复之间的间隔。"),
            new("actionSequenceWindupSeconds", "动作序列前摇", "动作序列开始前的准备时间。"),
            new("actionSequenceRecoverySeconds", "动作序列后摇", "动作序列完成后的恢复时间。"),
            new("actionSequenceTemporaryBasicAttackRangeOverride", "动作序列临时普攻射程", "动作序列期间临时覆盖的普攻射程。"),
            new("actionSequenceTemporarySkillCastRangeOverride", "动作序列临时技能射程", "动作序列期间临时覆盖的技能施法距离。"),
        };

        public static void Export(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("导出目录不能为空。", nameof(folderPath));
            }

            var assetIndex = BuildAssetIndex();
            var heroes = assetIndex.Heroes.Values
                .OrderBy(hero => hero.heroId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var skills = assetIndex.Skills.Values
                .OrderBy(skill => skill.skillId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Directory.CreateDirectory(folderPath);
            CleanupLegacyFiles(folderPath);

            WriteHeroesCsv(folderPath, heroes);
            WriteSkillsCsv(folderPath, skills, assetIndex);

            AssetDatabase.Refresh();
            Debug.Log($"[BalanceSheets] 已导出两张调数表到 {folderPath}（heroes.csv / skills.csv）。");
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

            if (TryGetTablePath(folderPath, SkillsFileName, out var skillsPath))
            {
                ImportSkills(skillsPath, assetIndex, dirtyAssets);
                importedFiles.Add(SkillsFileName);
            }

            foreach (var asset in dirtyAssets)
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var importedSummary = importedFiles.Count > 0
                ? string.Join(", ", importedFiles)
                : "没有找到 heroes.csv 或 skills.csv";
            Debug.Log($"[BalanceSheets] 已从 {folderPath} 导入：{importedSummary}。变更资产数：{dirtyAssets.Count}");
        }

        private static void WriteHeroesCsv(string folderPath, IReadOnlyList<HeroDefinition> heroes)
        {
            var rows = new List<IReadOnlyList<string>>(heroes.Count);
            foreach (var hero in heroes)
            {
                rows.Add(new[]
                {
                    hero.heroId,
                    hero.displayName,
                    FormatEnum(hero.heroClass),
                    FormatFloat(hero.baseStats?.maxHealth),
                    FormatFloat(hero.baseStats?.attackPower),
                    FormatFloat(hero.baseStats?.defense),
                    FormatFloat(hero.baseStats?.attackSpeed),
                    FormatFloat(hero.baseStats?.moveSpeed),
                    FormatFloat(hero.baseStats?.criticalChance),
                    FormatFloat(hero.baseStats?.criticalDamageMultiplier),
                    FormatFloat(hero.baseStats?.attackRange),
                    FormatFloat(hero.basicAttack?.damageMultiplier),
                    FormatFloat(hero.basicAttack?.rangeOverride),
                    FormatFloat(hero.basicAttack?.projectileSpeed),
                });
            }

            WriteCsv(
                Path.Combine(folderPath, HeroesFileName),
                "只展示英雄基础属性与普攻数值；非数值配置不在这张表里维护，空白单元格导入时会跳过。",
                HeroColumns,
                rows);
        }

        private static void WriteSkillsCsv(string folderPath, IReadOnlyList<SkillData> skills, AssetIndex assetIndex)
        {
            var layout = SkillSheetLayout.Create(skills);
            var columns = BuildSkillColumns(layout);
            var rows = new List<IReadOnlyList<string>>(skills.Count);

            foreach (var skill in skills)
            {
                var actionSequence = skill.actionSequence;
                var row = new List<string>(columns.Count)
                {
                    skill.skillId,
                    skill.displayName,
                    assetIndex.GetSkillOwnerSummary(skill.skillId),
                    FormatEnum(skill.slotType),
                    FormatFloat(skill.castRange),
                    FormatFloat(skill.areaRadius),
                    FormatFloat(skill.cooldownSeconds),
                    FormatInt(skill.minTargetsToCast),
                    FormatInt(actionSequence?.repeatCount),
                    FormatFloat(actionSequence?.durationSeconds),
                    FormatFloat(actionSequence?.intervalSeconds),
                    FormatFloat(actionSequence?.windupSeconds),
                    FormatFloat(actionSequence?.recoverySeconds),
                    FormatFloat(actionSequence?.temporaryBasicAttackRangeOverride),
                    FormatFloat(actionSequence?.temporarySkillCastRangeOverride),
                };

                for (var effectIndex = 0; effectIndex < layout.MaxEffectCount; effectIndex++)
                {
                    var effect = TryGetSkillEffect(skill, effectIndex);
                    row.Add(BuildSkillEffectLabel(effect));
                    row.Add(FormatFloat(effect?.powerMultiplier));
                    row.Add(FormatFloat(effect?.radiusOverride));
                    row.Add(FormatFloat(effect?.durationSeconds));
                    row.Add(FormatFloat(effect?.tickIntervalSeconds));
                    row.Add(FormatFloat(effect?.forcedMovementDistance));
                    row.Add(FormatFloat(effect?.forcedMovementDurationSeconds));
                    row.Add(FormatFloat(effect?.forcedMovementPeakHeight));

                    var maxStatusCount = layout.GetMaxStatusCount(effectIndex);
                    for (var statusIndex = 0; statusIndex < maxStatusCount; statusIndex++)
                    {
                        var status = TryGetStatusEffect(effect, statusIndex);
                        row.Add(BuildStatusEffectLabel(status));
                        row.Add(FormatFloat(status?.durationSeconds));
                        row.Add(FormatFloat(status?.magnitude));
                        row.Add(FormatFloat(status?.tickIntervalSeconds));
                        row.Add(FormatInt(status?.maxStacks));
                    }
                }

                rows.Add(row);
            }

            WriteCsv(
                Path.Combine(folderPath, SkillsFileName),
                "只展示技能主数值，以及现有 effect/status 的数值槽位；导入不会创建、补齐或删除结构，给不存在的槽位填值会直接报错。",
                columns,
                rows);
        }

        private static List<CsvColumn> BuildSkillColumns(SkillSheetLayout layout)
        {
            var columns = new List<CsvColumn>(SkillFixedColumns);
            for (var effectIndex = 0; effectIndex < layout.MaxEffectCount; effectIndex++)
            {
                columns.Add(new CsvColumn($"effect{effectIndex}Label", $"效果{effectIndex}说明", "只读说明列，帮助识别这个效果是什么。"));
                columns.Add(new CsvColumn($"effect{effectIndex}PowerMultiplier", $"效果{effectIndex}倍率", "该效果的倍率/强度。"));
                columns.Add(new CsvColumn($"effect{effectIndex}RadiusOverride", $"效果{effectIndex}半径覆盖", "该效果自身的范围半径；0 通常表示沿用技能主半径。"));
                columns.Add(new CsvColumn($"effect{effectIndex}DurationSeconds", $"效果{effectIndex}持续时间", "该效果的持续时间。"));
                columns.Add(new CsvColumn($"effect{effectIndex}TickIntervalSeconds", $"效果{effectIndex}跳动间隔", "该效果的周期跳动间隔。"));
                columns.Add(new CsvColumn($"effect{effectIndex}ForcedMovementDistance", $"效果{effectIndex}位移距离", "强制位移的水平距离。"));
                columns.Add(new CsvColumn($"effect{effectIndex}ForcedMovementDurationSeconds", $"效果{effectIndex}位移时长", "强制位移过程持续时间。"));
                columns.Add(new CsvColumn($"effect{effectIndex}ForcedMovementPeakHeight", $"效果{effectIndex}位移峰值高度", "强制位移的抬升高度。"));

                var maxStatusCount = layout.GetMaxStatusCount(effectIndex);
                for (var statusIndex = 0; statusIndex < maxStatusCount; statusIndex++)
                {
                    columns.Add(new CsvColumn($"effect{effectIndex}Status{statusIndex}Label", $"效果{effectIndex}-状态{statusIndex}说明", "只读说明列，帮助识别这个状态是什么。"));
                    columns.Add(new CsvColumn($"effect{effectIndex}Status{statusIndex}DurationSeconds", $"效果{effectIndex}-状态{statusIndex}持续时间", "状态持续时间。"));
                    columns.Add(new CsvColumn($"effect{effectIndex}Status{statusIndex}Magnitude", $"效果{effectIndex}-状态{statusIndex}强度", "状态强度；百分比类通常是小数，护盾是原始值，分担伤害用 0~1 比例。"));
                    columns.Add(new CsvColumn($"effect{effectIndex}Status{statusIndex}TickIntervalSeconds", $"效果{effectIndex}-状态{statusIndex}跳动间隔", "DOT/HOT 等周期状态的跳动间隔。"));
                    columns.Add(new CsvColumn($"effect{effectIndex}Status{statusIndex}MaxStacks", $"效果{effectIndex}-状态{statusIndex}最大层数", "状态最大叠层数。"));
                }
            }

            return columns;
        }

        private static void ImportHeroes(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var heroId = RequireValue(row, "heroId");
                var hero = assetIndex.GetHero(heroId, filePath);
                var changed = false;

                changed |= TryApplyFloat(row, "maxHealth", value => GetBaseStatsOrThrow(hero, row).maxHealth = value);
                changed |= TryApplyFloat(row, "attackPower", value => GetBaseStatsOrThrow(hero, row).attackPower = value);
                changed |= TryApplyFloat(row, "defense", value => GetBaseStatsOrThrow(hero, row).defense = value);
                changed |= TryApplyFloat(row, "attackSpeed", value => GetBaseStatsOrThrow(hero, row).attackSpeed = value);
                changed |= TryApplyFloat(row, "moveSpeed", value => GetBaseStatsOrThrow(hero, row).moveSpeed = value);
                changed |= TryApplyFloat(row, "criticalChance", value => GetBaseStatsOrThrow(hero, row).criticalChance = value);
                changed |= TryApplyFloat(row, "criticalDamageMultiplier", value => GetBaseStatsOrThrow(hero, row).criticalDamageMultiplier = value);
                changed |= TryApplyFloat(row, "attackRange", value => GetBaseStatsOrThrow(hero, row).attackRange = value);
                changed |= TryApplyFloat(row, "basicAttackDamageMultiplier", value => GetBasicAttackOrThrow(hero, row).damageMultiplier = value);
                changed |= TryApplyFloat(row, "basicAttackRangeOverride", value => GetBasicAttackOrThrow(hero, row).rangeOverride = value);
                changed |= TryApplyFloat(row, "basicAttackProjectileSpeed", value => GetBasicAttackOrThrow(hero, row).projectileSpeed = value);

                if (changed)
                {
                    dirtyAssets.Add(hero);
                }
            }
        }

        private static void ImportSkills(string filePath, AssetIndex assetIndex, ISet<ScriptableObject> dirtyAssets)
        {
            foreach (var row in ReadRows(filePath))
            {
                var skillId = RequireValue(row, "skillId");
                var skill = assetIndex.GetSkill(skillId, filePath);
                var changed = false;

                changed |= TryApplyFloat(row, "castRange", value => skill.castRange = value);
                changed |= TryApplyFloat(row, "areaRadius", value => skill.areaRadius = value);
                changed |= TryApplyFloat(row, "cooldownSeconds", value => skill.cooldownSeconds = value);
                changed |= TryApplyInt(row, "minTargetsToCast", value => skill.minTargetsToCast = value);
                changed |= TryApplyInt(row, "actionSequenceRepeatCount", value => GetActionSequenceOrThrow(skill, row).repeatCount = value);
                changed |= TryApplyFloat(row, "actionSequenceDurationSeconds", value => GetActionSequenceOrThrow(skill, row).durationSeconds = value);
                changed |= TryApplyFloat(row, "actionSequenceIntervalSeconds", value => GetActionSequenceOrThrow(skill, row).intervalSeconds = value);
                changed |= TryApplyFloat(row, "actionSequenceWindupSeconds", value => GetActionSequenceOrThrow(skill, row).windupSeconds = value);
                changed |= TryApplyFloat(row, "actionSequenceRecoverySeconds", value => GetActionSequenceOrThrow(skill, row).recoverySeconds = value);
                changed |= TryApplyFloat(row, "actionSequenceTemporaryBasicAttackRangeOverride", value => GetActionSequenceOrThrow(skill, row).temporaryBasicAttackRangeOverride = value);
                changed |= TryApplyFloat(row, "actionSequenceTemporarySkillCastRangeOverride", value => GetActionSequenceOrThrow(skill, row).temporarySkillCastRangeOverride = value);
                changed |= TryApplyDynamicSkillValues(row, skill);

                if (changed)
                {
                    dirtyAssets.Add(skill);
                }
            }
        }

        private static AssetIndex BuildAssetIndex()
        {
            Dictionary<string, HeroDefinition> heroes = LoadAssetsById<HeroDefinition>(
                "t:HeroDefinition",
                static (HeroDefinition hero) => hero.heroId,
                "HeroDefinition");
            Dictionary<string, SkillData> skills = LoadAssetsById<SkillData>(
                "t:SkillData",
                static (SkillData skill) => skill.skillId,
                "SkillData");
            Dictionary<string, string> skillOwnerSummaries = BuildSkillOwnerSummaries(heroes.Values);
            return new AssetIndex(heroes, skills, skillOwnerSummaries);
        }

        private static Dictionary<string, TAsset> LoadAssetsById<TAsset>(string searchFilter, Func<TAsset, string> idSelector, string assetLabel)
            where TAsset : ScriptableObject
        {
            var assets = new Dictionary<string, TAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets(searchFilter, new[] { DataSearchRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                var stableId = idSelector(asset)?.Trim();
                if (string.IsNullOrWhiteSpace(stableId))
                {
                    throw new InvalidOperationException($"[BalanceSheets] {assetLabel} 缺少稳定ID：{assetPath}");
                }

                if (assets.TryGetValue(stableId, out var existing))
                {
                    throw new InvalidOperationException(
                        $"[BalanceSheets] 发现重复的 {assetLabel} ID：{stableId}。{AssetDatabase.GetAssetPath(existing)} 与 {assetPath}");
                }

                assets.Add(stableId, asset);
            }

            return assets;
        }

        private static Dictionary<string, string> BuildSkillOwnerSummaries(IEnumerable<HeroDefinition> heroes)
        {
            var ownerEntries = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var hero in heroes)
            {
                RegisterSkillOwner(ownerEntries, hero, hero.activeSkill, SkillSlotType.ActiveSkill);
                RegisterSkillOwner(ownerEntries, hero, hero.ultimateSkill, SkillSlotType.Ultimate);
            }

            return ownerEntries.ToDictionary(
                pair => pair.Key,
                pair => string.Join(" / ", pair.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void CleanupLegacyFiles(string folderPath)
        {
            DeleteIfExists(Path.Combine(folderPath, BasicAttacksFileName));
            DeleteIfExists(Path.Combine(folderPath, SkillEffectsFileName));
            DeleteIfExists(Path.Combine(folderPath, SkillStatusEffectsFileName));
            DeleteIfExists(Path.Combine(folderPath, ReadmeFileName));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static bool TryGetTablePath(string folderPath, string fileName, out string filePath)
        {
            filePath = Path.Combine(folderPath, fileName);
            return File.Exists(filePath);
        }

        private static void RegisterSkillOwner(
            IDictionary<string, List<string>> ownerEntries,
            HeroDefinition hero,
            SkillData skill,
            SkillSlotType slotType)
        {
            if (hero == null || skill == null || string.IsNullOrWhiteSpace(skill.skillId))
            {
                return;
            }

            if (!ownerEntries.TryGetValue(skill.skillId, out var owners))
            {
                owners = new List<string>();
                ownerEntries.Add(skill.skillId, owners);
            }

            owners.Add($"{hero.displayName} [{hero.heroId}] {GetEnumDisplayName(slotType)}");
        }

        private static bool TryApplyDynamicSkillValues(RowData row, SkillData skill)
        {
            var changed = false;

            foreach (var cell in row.Cells)
            {
                if (string.IsNullOrWhiteSpace(cell.Value))
                {
                    continue;
                }

                var effectMatch = EffectValueColumnPattern.Match(cell.Key);
                if (effectMatch.Success)
                {
                    var effectIndex = ParseRequiredIndex(effectMatch, "effectIndex", row);
                    var effect = GetSkillEffectOrThrow(skill, effectIndex, row);
                    var floatValue = ParseFloatValue(row, cell.Key, cell.Value);

                    switch (effectMatch.Groups["suffix"].Value)
                    {
                        case "PowerMultiplier":
                            effect.powerMultiplier = floatValue;
                            break;
                        case "RadiusOverride":
                            effect.radiusOverride = floatValue;
                            break;
                        case "DurationSeconds":
                            effect.durationSeconds = floatValue;
                            break;
                        case "TickIntervalSeconds":
                            effect.tickIntervalSeconds = floatValue;
                            break;
                        case "ForcedMovementDistance":
                            effect.forcedMovementDistance = floatValue;
                            break;
                        case "ForcedMovementDurationSeconds":
                            effect.forcedMovementDurationSeconds = floatValue;
                            break;
                        case "ForcedMovementPeakHeight":
                            effect.forcedMovementPeakHeight = floatValue;
                            break;
                        default:
                            continue;
                    }

                    changed = true;
                    continue;
                }

                var statusMatch = EffectStatusValueColumnPattern.Match(cell.Key);
                if (!statusMatch.Success)
                {
                    continue;
                }

                var effectSlotIndex = ParseRequiredIndex(statusMatch, "effectIndex", row);
                var statusIndex = ParseRequiredIndex(statusMatch, "statusIndex", row);
                var status = GetStatusEffectOrThrow(skill, effectSlotIndex, statusIndex, row);

                switch (statusMatch.Groups["suffix"].Value)
                {
                    case "DurationSeconds":
                        status.durationSeconds = ParseFloatValue(row, cell.Key, cell.Value);
                        break;
                    case "Magnitude":
                        status.magnitude = ParseFloatValue(row, cell.Key, cell.Value);
                        break;
                    case "TickIntervalSeconds":
                        status.tickIntervalSeconds = ParseFloatValue(row, cell.Key, cell.Value);
                        break;
                    case "MaxStacks":
                        status.maxStacks = ParseIntValue(row, cell.Key, cell.Value);
                        break;
                    default:
                        continue;
                }

                changed = true;
            }

            return changed;
        }

        private static HeroStatsData GetBaseStatsOrThrow(HeroDefinition hero, RowData row)
        {
            if (hero.baseStats != null)
            {
                return hero.baseStats;
            }

            throw CreateRowException(row, $"英雄 {hero.heroId} 的 baseStats 为空，无法安全导入数值。");
        }

        private static BasicAttackData GetBasicAttackOrThrow(HeroDefinition hero, RowData row)
        {
            if (hero.basicAttack != null)
            {
                return hero.basicAttack;
            }

            throw CreateRowException(row, $"英雄 {hero.heroId} 的 basicAttack 为空，无法安全导入数值。");
        }

        private static CombatActionSequenceData GetActionSequenceOrThrow(SkillData skill, RowData row)
        {
            if (skill.actionSequence != null)
            {
                return skill.actionSequence;
            }

            throw CreateRowException(row, $"技能 {skill.skillId} 的 actionSequence 为空，无法安全导入数值。");
        }

        private static SkillEffectData GetSkillEffectOrThrow(SkillData skill, int effectIndex, RowData row)
        {
            if (skill.effects == null)
            {
                throw CreateRowException(row, $"技能 {skill.skillId} 没有 effects 列表；不允许通过表格补结构。");
            }

            if (effectIndex < 0 || effectIndex >= skill.effects.Count || skill.effects[effectIndex] == null)
            {
                throw CreateRowException(row, $"技能 {skill.skillId} 不存在 effect{effectIndex}；不允许通过表格补默认 effect。");
            }

            return skill.effects[effectIndex];
        }

        private static StatusEffectData GetStatusEffectOrThrow(SkillData skill, int effectIndex, int statusIndex, RowData row)
        {
            var effect = GetSkillEffectOrThrow(skill, effectIndex, row);
            if (effect.statusEffects == null)
            {
                throw CreateRowException(row, $"技能 {skill.skillId} 的 effect{effectIndex} 没有 statusEffects 列表；不允许通过表格补结构。");
            }

            if (statusIndex < 0 || statusIndex >= effect.statusEffects.Count || effect.statusEffects[statusIndex] == null)
            {
                throw CreateRowException(row, $"技能 {skill.skillId} 的 effect{effectIndex} 不存在 status{statusIndex}；不允许通过表格补默认 status。");
            }

            return effect.statusEffects[statusIndex];
        }

        private static SkillEffectData TryGetSkillEffect(SkillData skill, int effectIndex)
        {
            if (skill.effects == null || effectIndex < 0 || effectIndex >= skill.effects.Count)
            {
                return null;
            }

            return skill.effects[effectIndex];
        }

        private static StatusEffectData TryGetStatusEffect(SkillEffectData effect, int statusIndex)
        {
            if (effect?.statusEffects == null || statusIndex < 0 || statusIndex >= effect.statusEffects.Count)
            {
                return null;
            }

            return effect.statusEffects[statusIndex];
        }

        private static bool TryApplyFloat(RowData row, string key, Action<float> apply)
        {
            if (!TryGetNonEmptyValue(row, key, out var raw))
            {
                return false;
            }

            apply(ParseFloatValue(row, key, raw));
            return true;
        }

        private static bool TryApplyInt(RowData row, string key, Action<int> apply)
        {
            if (!TryGetNonEmptyValue(row, key, out var raw))
            {
                return false;
            }

            apply(ParseIntValue(row, key, raw));
            return true;
        }

        private static string RequireValue(RowData row, string key)
        {
            if (TryGetNonEmptyValue(row, key, out var value))
            {
                return value;
            }

            throw CreateRowException(row, $"缺少必填列 {key}。");
        }

        private static bool TryGetNonEmptyValue(RowData row, string key, out string value)
        {
            if (row.Cells.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                value = raw.Trim();
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static int ParseRequiredIndex(Match match, string groupName, RowData row)
        {
            if (int.TryParse(match.Groups[groupName].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw CreateRowException(row, $"无法解析索引 {groupName}。");
        }

        private static float ParseFloatValue(RowData row, string key, string raw)
        {
            if (float.TryParse(raw.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw CreateRowException(row, $"列 {key} 不是合法数字：{raw}");
        }

        private static int ParseIntValue(RowData row, string key, string raw)
        {
            if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw CreateRowException(row, $"列 {key} 不是合法整数：{raw}");
        }

        private static string BuildSkillEffectLabel(SkillEffectData effect)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            var parts = new List<string>
            {
                FormatEnum(effect.effectType),
                $"目标:{FormatEnum(effect.targetMode)}",
            };

            if (effect.effectType == SkillEffectType.CreatePersistentArea)
            {
                parts.Add($"脉冲:{FormatEnum(effect.persistentAreaPulseEffectType)}");
                parts.Add($"阵营:{FormatEnum(effect.persistentAreaTargetType)}");
                parts.Add($"跟随施法者:{FormatBool(effect.followCaster)}");
            }

            if (effect.effectType == SkillEffectType.ApplyForcedMovement)
            {
                parts.Add($"位移方向:{FormatEnum(effect.forcedMovementDirection)}");
            }

            if (effect.effectType == SkillEffectType.CreateDeployableProxy)
            {
                parts.Add($"触发:{FormatEnum(effect.deployableProxyTriggerMode)}");
                parts.Add($"半径:{FormatFloat(effect.deployableProxyStrikeRadius)}");
                parts.Add($"上限:{effect.deployableProxyMaxCount}");
            }

            if (effect.effectType == SkillEffectType.CreateRadialSweep)
            {
                parts.Add($"方向:{FormatEnum(effect.radialSweepDirection)}");
                parts.Add($"阵营:{FormatEnum(effect.persistentAreaTargetType)}");
                parts.Add($"延迟:{FormatFloat(effect.radialSweepStartDelaySeconds)}");
                parts.Add($"波锋:{FormatFloat(effect.radialSweepRingWidth)}");
            }

            if (effect.effectType == SkillEffectType.CreateReturningPathStrike)
            {
                parts.Add($"阶段:{FormatEnum(effect.returningPathStrikePhase)}");
                parts.Add($"阵营:{FormatEnum(effect.persistentAreaTargetType)}");
                parts.Add($"距离:{FormatFloat(effect.returningPathMaxDistance)}");
                parts.Add($"宽度:{FormatFloat(effect.returningPathWidth)}");
                parts.Add($"延迟:{FormatFloat(effect.returningPathDelaySeconds)}");
            }

            return string.Join(" / ", parts);
        }

        private static string BuildStatusEffectLabel(StatusEffectData status)
        {
            return status == null
                ? string.Empty
                : $"{FormatEnum(status.effectType)} / 重上刷新:{FormatBool(status.refreshDurationOnReapply)}";
        }

        private static void WriteCsv(
            string filePath,
            string description,
            IReadOnlyList<CsvColumn> columns,
            IReadOnlyList<IReadOnlyList<string>> rows)
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
            for (var index = 0; index < rows.Count; index++)
            {
                if (IsSkippableRow(rows[index]))
                {
                    continue;
                }

                headerRowIndex = index;
                headerRow = rows[index];
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
            return string.IsNullOrWhiteSpace(firstNonEmptyCell) ||
                firstNonEmptyCell.TrimStart().StartsWith("#", StringComparison.Ordinal);
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
                        if (index + 1 < content.Length && content[index + 1] == '\n')
                        {
                            index++;
                        }

                        currentRow.Add(currentCell.ToString());
                        currentCell.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
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

            currentRow.Add(currentCell.ToString());
            if (currentRow.Count > 1 || currentRow[0].Length > 0 || rows.Count == 0)
            {
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

        private static string FormatFloat(float value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private static string FormatFloat(float? value)
        {
            return value.HasValue ? FormatFloat(value.Value) : string.Empty;
        }

        private static string FormatInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatInt(int? value)
        {
            return value.HasValue ? FormatInt(value.Value) : string.Empty;
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
                SkillSlotType skillSlotType => skillSlotType switch
                {
                    SkillSlotType.ActiveSkill => "小技能",
                    SkillSlotType.Ultimate => "大招",
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
                    SkillEffectType.CreateDeployableProxy => "创建部署物代理",
                    SkillEffectType.CreateRadialSweep => "创建径向扫波",
                    SkillEffectType.CreateReturningPathStrike => "创建往返路径打击",
                    _ => value.ToString(),
                },
                SkillEffectTargetMode skillEffectTargetMode => skillEffectTargetMode switch
                {
                    SkillEffectTargetMode.SkillTargets => "技能目标",
                    SkillEffectTargetMode.Caster => "施法者",
                    SkillEffectTargetMode.PrimaryTarget => "主目标",
                    SkillEffectTargetMode.EnemiesInRadiusAroundCaster => "施法者周围敌人",
                    SkillEffectTargetMode.AlliesInRadiusAroundCaster => "施法者周围友军",
                    SkillEffectTargetMode.OtherAlliesInRadiusAroundCaster => "施法者周围其他友军",
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
                RadialSweepDirectionMode radialSweepDirectionMode => radialSweepDirectionMode switch
                {
                    RadialSweepDirectionMode.Outward => "向外",
                    RadialSweepDirectionMode.Inward => "向内",
                    _ => value.ToString(),
                },
                ReturningPathStrikePhase returningPathStrikePhase => returningPathStrikePhase switch
                {
                    ReturningPathStrikePhase.Outbound => "外放",
                    ReturningPathStrikePhase.Return => "回收",
                    _ => value.ToString(),
                },
                StatusEffectType statusEffectType => statusEffectType switch
                {
                    StatusEffectType.None => "无",
                    StatusEffectType.Stun => "眩晕",
                    StatusEffectType.AttackPowerModifier => "攻击力修正",
                    StatusEffectType.DefenseModifier => "防御力修正",
                    StatusEffectType.AttackSpeedModifier => "攻速修正",
                    StatusEffectType.MoveSpeedModifier => "移速修正",
                    StatusEffectType.HealOverTime => "持续治疗",
                    StatusEffectType.MaxHealthModifier => "最大生命修正",
                    StatusEffectType.CriticalChanceModifier => "暴击率修正",
                    StatusEffectType.CriticalDamageModifier => "暴击伤害修正",
                    StatusEffectType.AttackRangeModifier => "攻击距离修正",
                    StatusEffectType.KnockUp => "击飞",
                    StatusEffectType.Invulnerable => "无敌",
                    StatusEffectType.Untargetable => "不可选中",
                    StatusEffectType.DamageOverTime => "持续伤害",
                    StatusEffectType.Shield => "护盾",
                    StatusEffectType.DamageShare => "分担伤害",
                    StatusEffectType.HealTakenModifier => "受治疗率修正",
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
            public AssetIndex(
                IReadOnlyDictionary<string, HeroDefinition> heroes,
                IReadOnlyDictionary<string, SkillData> skills,
                IReadOnlyDictionary<string, string> skillOwnerSummaries)
            {
                Heroes = heroes;
                Skills = skills;
                SkillOwnerSummaries = skillOwnerSummaries;
            }

            public IReadOnlyDictionary<string, HeroDefinition> Heroes { get; }

            public IReadOnlyDictionary<string, SkillData> Skills { get; }

            public IReadOnlyDictionary<string, string> SkillOwnerSummaries { get; }

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

            public string GetSkillOwnerSummary(string skillId)
            {
                return SkillOwnerSummaries.TryGetValue(skillId, out var summary) ? summary : string.Empty;
            }
        }

        private sealed class SkillSheetLayout
        {
            public SkillSheetLayout(int maxEffectCount, IReadOnlyDictionary<int, int> maxStatusCountsByEffectIndex)
            {
                MaxEffectCount = maxEffectCount;
                MaxStatusCountsByEffectIndex = maxStatusCountsByEffectIndex;
            }

            public int MaxEffectCount { get; }

            public IReadOnlyDictionary<int, int> MaxStatusCountsByEffectIndex { get; }

            public int GetMaxStatusCount(int effectIndex)
            {
                return MaxStatusCountsByEffectIndex.TryGetValue(effectIndex, out var count) ? count : 0;
            }

            public static SkillSheetLayout Create(IEnumerable<SkillData> skills)
            {
                var maxEffectCount = 0;
                var maxStatusCounts = new Dictionary<int, int>();

                foreach (var skill in skills)
                {
                    var effectCount = skill?.effects?.Count ?? 0;
                    if (effectCount > maxEffectCount)
                    {
                        maxEffectCount = effectCount;
                    }

                    for (var effectIndex = 0; effectIndex < effectCount; effectIndex++)
                    {
                        var statusCount = skill.effects[effectIndex]?.statusEffects?.Count ?? 0;
                        if (maxStatusCounts.TryGetValue(effectIndex, out var currentMax))
                        {
                            if (statusCount > currentMax)
                            {
                                maxStatusCounts[effectIndex] = statusCount;
                            }
                        }
                        else
                        {
                            maxStatusCounts[effectIndex] = statusCount;
                        }
                    }
                }

                return new SkillSheetLayout(maxEffectCount, maxStatusCounts);
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
