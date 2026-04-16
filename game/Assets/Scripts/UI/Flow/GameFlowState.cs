using System.Collections.Generic;
using Fight.Data;
using UnityEngine;

namespace Fight.UI.Flow
{
    public static class GameFlowState
    {
        public const string DefaultInputResourcesPath = "Stage01Demo/Stage01DemoBattleInput";

        private static BattleInputConfig defaultBattleTemplate;
        private static List<HeroDefinition> heroCatalog;
        private static List<HeroDefinition> blueSelection;
        private static List<HeroDefinition> redSelection;
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

        public static BattleResultData LastBattleResult { get; private set; }

        public static string LastBattleLogSessionId => lastBattleLogSessionId;

        public static string LastBattleLogExportText => lastBattleLogExportText;

        public static bool HasBattleLogExportData => !string.IsNullOrWhiteSpace(lastBattleLogExportText);

        public static bool HasBattleTemplate => GetDefaultBattleTemplate() != null;

        public static void EnsureSelectionsInitialized()
        {
            if (IsSelectionReady(blueSelection) && IsSelectionReady(redSelection))
            {
                return;
            }

            ResetSelectionsToDefault();
        }

        public static void ResetSelectionsToDefault()
        {
            var template = GetDefaultBattleTemplate();
            blueSelection = CloneTeam(template?.blueTeam?.heroes);
            redSelection = CloneTeam(template?.redTeam?.heroes);
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

            runtimeInput.blueTeam = new BattleTeamLoadout { side = TeamSide.Blue };
            runtimeInput.blueTeam.heroes.AddRange(blueSelection);

            runtimeInput.redTeam = new BattleTeamLoadout { side = TeamSide.Red };
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

        private static List<HeroDefinition> BuildHeroCatalog()
        {
            var template = GetDefaultBattleTemplate();
            var catalog = new List<HeroDefinition>();
            var seenHeroIds = new HashSet<string>();

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

                var heroId = string.IsNullOrWhiteSpace(hero.heroId) ? hero.name : hero.heroId;
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

        private static bool IsSelectionReady(IList<HeroDefinition> source)
        {
            if (source == null || source.Count != BattleInputConfig.DefaultTeamSize)
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
    }
}
