using System.Collections.Generic;
using Fight.Data;
using UnityEngine;

namespace Fight.UI.Flow
{
    public enum BattleDraftActionType
    {
        Ban = 0,
        Pick = 1,
    }

    public sealed class BattleDraftStep
    {
        public BattleDraftStep(TeamSide side, BattleDraftActionType actionType, int slotIndex)
        {
            Side = side;
            ActionType = actionType;
            SlotIndex = slotIndex;
        }

        public TeamSide Side { get; }

        public BattleDraftActionType ActionType { get; }

        public int SlotIndex { get; }
    }

    public static class GameFlowState
    {
        public const string DefaultInputResourcesPath = "Stage01Demo/Stage01DemoBattleInput";
        public const string DefaultHeroCatalogResourcesPath = "Stage01Demo/Stage01HeroCatalog";
        public const int DraftBansPerSide = 3;

        private static readonly BattleDraftStep[] DraftSteps =
        {
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Ban, 0),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Ban, 0),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Ban, 1),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Ban, 1),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Pick, 0),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Pick, 0),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Pick, 1),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Pick, 1),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Pick, 2),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Pick, 2),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Ban, 2),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Ban, 2),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Pick, 3),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Pick, 3),
            new BattleDraftStep(TeamSide.Blue, BattleDraftActionType.Pick, 4),
            new BattleDraftStep(TeamSide.Red, BattleDraftActionType.Pick, 4),
        };

        private static BattleInputConfig defaultBattleTemplate;
        private static HeroCatalogData defaultHeroCatalog;
        private static List<HeroDefinition> heroCatalog;
        private static List<HeroDefinition> blueSelection;
        private static List<HeroDefinition> redSelection;
        private static List<HeroDefinition> blueBans;
        private static List<HeroDefinition> redBans;
        private static int draftStepIndex;
        private static bool draftInitialized;
        private static BattleUltimateTimingStrategy blueUltimateTimingStrategy = BattleUltimateTimingStrategy.Standard;
        private static BattleUltimateTimingStrategy redUltimateTimingStrategy = BattleUltimateTimingStrategy.Standard;
        private static BattleUltimateComboStrategy blueUltimateComboStrategy = BattleUltimateComboStrategy.Standard;
        private static BattleUltimateComboStrategy redUltimateComboStrategy = BattleUltimateComboStrategy.Standard;
        private static BattleInputConfig pendingBattleInput;
        private static BattleInputConfig lastUsedBattleInput;
        private static string lastBattleLogSessionId;
        private static string lastBattleLogExportText;

        public static IReadOnlyList<HeroDefinition> HeroCatalog => heroCatalog ??= BuildHeroCatalog();

        public static IReadOnlyList<HeroDefinition> BlueSelection
        {
            get
            {
                EnsureSelectionsInitialized();
                return blueSelection;
            }
        }

        public static IReadOnlyList<HeroDefinition> RedSelection
        {
            get
            {
                EnsureSelectionsInitialized();
                return redSelection;
            }
        }

        public static IReadOnlyList<HeroDefinition> BlueBans
        {
            get
            {
                EnsureDraftInitialized();
                return blueBans;
            }
        }

        public static IReadOnlyList<HeroDefinition> RedBans
        {
            get
            {
                EnsureDraftInitialized();
                return redBans;
            }
        }

        public static BattleDraftStep CurrentDraftStep
        {
            get
            {
                EnsureDraftInitialized();
                return draftStepIndex >= 0 && draftStepIndex < DraftSteps.Length
                    ? DraftSteps[draftStepIndex]
                    : null;
            }
        }

        public static int DraftStepNumber
        {
            get
            {
                EnsureDraftInitialized();
                return Mathf.Min(draftStepIndex + 1, DraftSteps.Length);
            }
        }

        public static int DraftTotalSteps => DraftSteps.Length;

        public static bool IsDraftComplete
        {
            get
            {
                EnsureDraftInitialized();
                return draftStepIndex >= DraftSteps.Length;
            }
        }

        public static BattleResultData LastBattleResult { get; private set; }

        public static string LastBattleLogSessionId => lastBattleLogSessionId;

        public static string LastBattleLogExportText => lastBattleLogExportText;

        public static bool HasBattleLogExportData => !string.IsNullOrWhiteSpace(lastBattleLogExportText);

        public static bool HasBattleTemplate => GetDefaultBattleTemplate() != null;

        public static void RefreshHeroCatalog()
        {
            defaultHeroCatalog = null;
            heroCatalog = BuildHeroCatalog();
        }

        public static void EnsureSelectionsInitialized()
        {
            if (HasSelectionSlots(blueSelection) && HasSelectionSlots(redSelection))
            {
                return;
            }

            ResetSelectionsToDefault();
        }

        public static void EnsureDraftInitialized()
        {
            if (draftInitialized
                && HasSelectionSlots(blueSelection)
                && HasSelectionSlots(redSelection)
                && HasBanSlots(blueBans)
                && HasBanSlots(redBans))
            {
                return;
            }

            ResetDraft();
        }

        public static void ResetDraft()
        {
            var template = GetDefaultBattleTemplate();
            blueSelection = CreateEmptyTeam();
            redSelection = CreateEmptyTeam();
            blueBans = CreateEmptyBanList();
            redBans = CreateEmptyBanList();
            draftStepIndex = 0;
            draftInitialized = true;
            pendingBattleInput = null;

            blueUltimateTimingStrategy = template?.blueTeam != null
                ? template.blueTeam.ultimateTimingStrategy
                : BattleUltimateTimingStrategy.Standard;
            redUltimateTimingStrategy = template?.redTeam != null
                ? template.redTeam.ultimateTimingStrategy
                : BattleUltimateTimingStrategy.Standard;
            blueUltimateComboStrategy = template?.blueTeam != null
                ? template.blueTeam.ultimateComboStrategy
                : BattleUltimateComboStrategy.Standard;
            redUltimateComboStrategy = template?.redTeam != null
                ? template.redTeam.ultimateComboStrategy
                : BattleUltimateComboStrategy.Standard;
        }

        public static void ResetSelectionsToDefault()
        {
            var template = GetDefaultBattleTemplate();
            blueSelection = CloneTeam(template?.blueTeam?.heroes);
            redSelection = CloneTeam(template?.redTeam?.heroes);
            blueBans = CreateEmptyBanList();
            redBans = CreateEmptyBanList();
            draftStepIndex = DraftSteps.Length;
            draftInitialized = false;
            pendingBattleInput = null;
            blueUltimateTimingStrategy = template?.blueTeam != null
                ? template.blueTeam.ultimateTimingStrategy
                : BattleUltimateTimingStrategy.Standard;
            redUltimateTimingStrategy = template?.redTeam != null
                ? template.redTeam.ultimateTimingStrategy
                : BattleUltimateTimingStrategy.Standard;
            blueUltimateComboStrategy = template?.blueTeam != null
                ? template.blueTeam.ultimateComboStrategy
                : BattleUltimateComboStrategy.Standard;
            redUltimateComboStrategy = template?.redTeam != null
                ? template.redTeam.ultimateComboStrategy
                : BattleUltimateComboStrategy.Standard;
        }

        public static bool TryApplyDraftHero(HeroDefinition hero)
        {
            EnsureDraftInitialized();
            var step = CurrentDraftStep;
            if (step == null || !CanDraftHero(hero))
            {
                return false;
            }

            if (step.ActionType == BattleDraftActionType.Ban)
            {
                var bans = step.Side == TeamSide.Red ? redBans : blueBans;
                if (step.SlotIndex < 0 || step.SlotIndex >= bans.Count)
                {
                    return false;
                }

                bans[step.SlotIndex] = hero;
            }
            else
            {
                var team = step.Side == TeamSide.Red ? redSelection : blueSelection;
                if (step.SlotIndex < 0 || step.SlotIndex >= team.Count)
                {
                    return false;
                }

                team[step.SlotIndex] = hero;
            }

            draftStepIndex++;
            return true;
        }

        public static bool CanDraftHero(HeroDefinition hero)
        {
            EnsureDraftInitialized();
            return hero != null
                && !IsDraftComplete
                && !IsHeroBanned(hero)
                && !IsHeroPicked(hero);
        }

        public static bool IsHeroBanned(HeroDefinition hero)
        {
            EnsureDraftInitialized();
            return ContainsHero(blueBans, hero) || ContainsHero(redBans, hero);
        }

        public static bool IsHeroPicked(HeroDefinition hero)
        {
            EnsureDraftInitialized();
            return ContainsHero(blueSelection, hero) || ContainsHero(redSelection, hero);
        }

        public static TeamSide? GetHeroPickedSide(HeroDefinition hero)
        {
            EnsureDraftInitialized();
            if (ContainsHero(blueSelection, hero))
            {
                return TeamSide.Blue;
            }

            if (ContainsHero(redSelection, hero))
            {
                return TeamSide.Red;
            }

            return null;
        }

        public static TeamSide? GetHeroBannedSide(HeroDefinition hero)
        {
            EnsureDraftInitialized();
            if (ContainsHero(blueBans, hero))
            {
                return TeamSide.Blue;
            }

            if (ContainsHero(redBans, hero))
            {
                return TeamSide.Red;
            }

            return null;
        }

        public static void SetSelectedHero(TeamSide side, int slotIndex, HeroDefinition hero)
        {
            EnsureSelectionsInitialized();
            var team = side == TeamSide.Red ? redSelection : blueSelection;
            if (team == null || slotIndex < 0 || slotIndex >= BattleInputConfig.DefaultTeamSize)
            {
                return;
            }

            team[slotIndex] = hero;
        }

        public static void ClearSelectedHero(TeamSide side, int slotIndex)
        {
            SetSelectedHero(side, slotIndex, null);
        }

        public static BattleUltimateTimingStrategy GetUltimateTimingStrategy(TeamSide side)
        {
            return side == TeamSide.Red
                ? redUltimateTimingStrategy
                : blueUltimateTimingStrategy;
        }

        public static void SetUltimateTimingStrategy(TeamSide side, BattleUltimateTimingStrategy strategy)
        {
            if (side == TeamSide.Red)
            {
                redUltimateTimingStrategy = strategy;
                return;
            }

            blueUltimateTimingStrategy = strategy;
        }

        public static BattleUltimateComboStrategy GetUltimateComboStrategy(TeamSide side)
        {
            return side == TeamSide.Red
                ? redUltimateComboStrategy
                : blueUltimateComboStrategy;
        }

        public static void SetUltimateComboStrategy(TeamSide side, BattleUltimateComboStrategy strategy)
        {
            if (side == TeamSide.Red)
            {
                redUltimateComboStrategy = strategy;
                return;
            }

            blueUltimateComboStrategy = strategy;
        }

        public static bool HasValidSelections()
        {
            EnsureSelectionsInitialized();
            return IsSelectionReady(blueSelection) && IsSelectionReady(redSelection);
        }

        public static bool TryPrepareBattleInput(out BattleInputConfig input)
        {
            input = null;
            if (!HasValidSelections())
            {
                return false;
            }

            var template = GetDefaultBattleTemplate();
            if (template == null)
            {
                return false;
            }

            var runtimeInput = ScriptableObject.CreateInstance<BattleInputConfig>();
            runtimeInput.name = "RuntimeBattleInput";
            runtimeInput.regulationDurationSeconds = template.regulationDurationSeconds;
            runtimeInput.respawnDelaySeconds = template.respawnDelaySeconds;
            runtimeInput.enableBattleEventLogs = template.enableBattleEventLogs;
            runtimeInput.enableSkills = template.enableSkills;
            runtimeInput.arenaId = template.arenaId;

            runtimeInput.blueTeam = new BattleTeamLoadout
            {
                side = TeamSide.Blue,
                ultimateTimingStrategy = blueUltimateTimingStrategy,
                ultimateComboStrategy = blueUltimateComboStrategy,
            };
            runtimeInput.blueTeam.heroes.AddRange(blueSelection);

            runtimeInput.redTeam = new BattleTeamLoadout
            {
                side = TeamSide.Red,
                ultimateTimingStrategy = redUltimateTimingStrategy,
                ultimateComboStrategy = redUltimateComboStrategy,
            };
            runtimeInput.redTeam.heroes.AddRange(redSelection);

            pendingBattleInput = runtimeInput;
            input = runtimeInput;
            return true;
        }

        public static BattleInputConfig ConsumePendingBattleInput()
        {
            var preparedInput = pendingBattleInput;
            pendingBattleInput = null;
            return preparedInput;
        }

        public static void RememberLastUsedInput(BattleInputConfig input)
        {
            lastUsedBattleInput = input;
        }

        public static BattleInputConfig GetLastUsedInput()
        {
            return lastUsedBattleInput;
        }

        public static void StoreBattleResult(BattleResultData result)
        {
            LastBattleResult = result;
        }

        public static void StoreBattleLogExport(string sessionId, string exportText)
        {
            lastBattleLogSessionId = sessionId;
            lastBattleLogExportText = exportText;
        }

        public static void ClearBattleResult()
        {
            LastBattleResult = null;
            lastBattleLogSessionId = null;
            lastBattleLogExportText = null;
        }

        private static BattleInputConfig GetDefaultBattleTemplate()
        {
            if (defaultBattleTemplate == null)
            {
                defaultBattleTemplate = Resources.Load<BattleInputConfig>(DefaultInputResourcesPath);
            }

            return defaultBattleTemplate;
        }

        private static HeroCatalogData GetDefaultHeroCatalog()
        {
            if (defaultHeroCatalog == null)
            {
                defaultHeroCatalog = Resources.Load<HeroCatalogData>(DefaultHeroCatalogResourcesPath);
            }

            return defaultHeroCatalog;
        }

        private static List<HeroDefinition> BuildHeroCatalog()
        {
            var template = GetDefaultBattleTemplate();
            var catalogAsset = GetDefaultHeroCatalog();
            var catalog = new List<HeroDefinition>();
            var seenHeroIds = new HashSet<string>();

            AppendUniqueHeroes(catalog, seenHeroIds, catalogAsset?.heroes);
            AppendUniqueHeroes(catalog, seenHeroIds, template?.blueTeam?.heroes);
            AppendUniqueHeroes(catalog, seenHeroIds, template?.redTeam?.heroes);
            catalog.Sort(CompareHeroes);
            return catalog;
        }

        private static void AppendUniqueHeroes(List<HeroDefinition> destination, HashSet<string> seenHeroIds, IList<HeroDefinition> source)
        {
            if (destination == null || seenHeroIds == null || source == null)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var hero = source[i];
                if (hero == null)
                {
                    continue;
                }

                var heroId = GetHeroIdentity(hero);
                if (!seenHeroIds.Add(heroId))
                {
                    continue;
                }

                destination.Add(hero);
            }
        }

        private static int CompareHeroes(HeroDefinition left, HeroDefinition right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var classComparison = left.heroClass.CompareTo(right.heroClass);
            if (classComparison != 0)
            {
                return classComparison;
            }

            return string.Compare(left.displayName, right.displayName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static List<HeroDefinition> CloneTeam(IList<HeroDefinition> source)
        {
            var clone = new List<HeroDefinition>(BattleInputConfig.DefaultTeamSize);
            for (var i = 0; i < BattleInputConfig.DefaultTeamSize; i++)
            {
                clone.Add(source != null && i < source.Count ? source[i] : null);
            }

            return clone;
        }

        private static List<HeroDefinition> CreateEmptyTeam()
        {
            var team = new List<HeroDefinition>(BattleInputConfig.DefaultTeamSize);
            for (var i = 0; i < BattleInputConfig.DefaultTeamSize; i++)
            {
                team.Add(null);
            }

            return team;
        }

        private static List<HeroDefinition> CreateEmptyBanList()
        {
            var bans = new List<HeroDefinition>(DraftBansPerSide);
            for (var i = 0; i < DraftBansPerSide; i++)
            {
                bans.Add(null);
            }

            return bans;
        }

        private static bool HasSelectionSlots(IList<HeroDefinition> source)
        {
            return source != null && source.Count == BattleInputConfig.DefaultTeamSize;
        }

        private static bool HasBanSlots(IList<HeroDefinition> source)
        {
            return source != null && source.Count == DraftBansPerSide;
        }

        private static bool IsSelectionReady(IList<HeroDefinition> source)
        {
            if (!HasSelectionSlots(source))
            {
                return false;
            }

            for (var i = 0; i < source.Count; i++)
            {
                if (source[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsHero(IList<HeroDefinition> source, HeroDefinition hero)
        {
            if (source == null || hero == null)
            {
                return false;
            }

            var heroId = GetHeroIdentity(hero);
            for (var i = 0; i < source.Count; i++)
            {
                if (source[i] != null && GetHeroIdentity(source[i]) == heroId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetHeroIdentity(HeroDefinition hero)
        {
            return hero == null
                ? string.Empty
                : string.IsNullOrWhiteSpace(hero.heroId)
                    ? hero.name
                    : hero.heroId;
        }
    }
}
