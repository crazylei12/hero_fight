using System;
using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleOfflineSimulationService
    {
        private sealed class HeroAggregateAccumulator
        {
            public HeroDefinition Definition;
            public string HeroId;
            public string DisplayName;
            public HeroClass HeroClass;
            public int PickCount;
            public int WinCount;
            public int LossCount;
            public int AppearancesAsBlue;
            public int AppearancesAsRed;
            public int TotalKills;
            public int TotalDeaths;
            public int TotalAssists;
            public float TotalDamageDealt;
            public float TotalDamageTaken;
            public float TotalHealingDone;
            public float TotalShieldingDone;
            public int TotalActiveSkillCastCount;
            public int TotalUltimateCastCount;
        }

        public static BattleOfflineSimulationRunResult Run(BattleOfflineSimulationRequest request)
        {
            ValidateRequest(request);

            var heroPool = request.SelectionMode == BattleOfflineSelectionMode.RandomCatalog
                ? BuildValidatedHeroPool(request.HeroCatalog)
                : GetValidatedHeroesFromInput(request.TemplateInput);
            var aggregateAccumulators = CreateAggregateAccumulators(heroPool);
            var report = new BattleOfflineSimulationReport
            {
                runMeta = new BattleOfflineSimulationRunMeta
                {
                    generatedAt = DateTimeOffset.Now.ToString("O"),
                    selectionMode = request.SelectionMode.ToString(),
                    inputAssetPath = request.InputAssetPath ?? string.Empty,
                    heroCatalogPath = request.HeroCatalogAssetPath ?? string.Empty,
                    matchCount = request.MatchCount,
                    completedMatchCount = 0,
                    seedStart = request.SeedStart,
                    fixedDeltaTime = request.FixedDeltaTimeSeconds,
                    exportFullLogs = request.ExportFullLogs,
                    includeMatchRecords = request.IncludeMatchRecords,
                    uniqueHeroValidation = true,
                }
            };
            var matchLogs = request.ExportFullLogs
                ? new List<BattleOfflineMatchLogExport>()
                : new List<BattleOfflineMatchLogExport>(0);
            var completedMatchCount = 0;

            for (var matchIndex = 0; matchIndex < request.MatchCount; matchIndex++)
            {
                var seed = request.SeedStart + matchIndex;
                var matchInput = request.SelectionMode == BattleOfflineSelectionMode.RandomCatalog
                    ? CreateRandomCatalogInput(request.TemplateInput, heroPool, seed)
                    : CloneInput(
                        request.TemplateInput,
                        request.TemplateInput.blueTeam != null ? request.TemplateInput.blueTeam.heroes : Array.Empty<HeroDefinition>(),
                        request.TemplateInput.redTeam != null ? request.TemplateInput.redTeam.heroes : Array.Empty<HeroDefinition>(),
                        "OfflineFixedInput");

                ValidateUniqueHeroIds(matchInput, request.SelectionMode);

                var runner = new BattleSessionRunner(matchInput, seed);
                BattleLogSession logSession = null;

                if (request.ExportFullLogs)
                {
                    logSession = new BattleLogSession();
                    logSession.SetTimeProvider(() => runner.Context?.Clock?.ElapsedTimeSeconds ?? 0f);
                    runner.Context.EventBus.Published += logSession.HandleBattleEvent;
                }

                try
                {
                    var result = runner.RunToCompletion(request.FixedDeltaTimeSeconds, request.MaxTickCount);
                    BattleOfflineSimulationMatchRecord matchRecord = null;

                    if (request.IncludeMatchRecords)
                    {
                        matchRecord = BuildMatchRecord(matchIndex, seed, matchInput, result);
                        report.matches.Add(matchRecord);
                    }

                    if (logSession != null)
                    {
                        var logFileName = BuildMatchLogFileName(matchIndex, seed);
                        if (matchRecord != null)
                        {
                            matchRecord.fullLogFile = logFileName;
                        }

                        matchLogs.Add(new BattleOfflineMatchLogExport(matchIndex, seed, logFileName, logSession.BuildExportText()));
                    }

                    UpdateAggregateAccumulators(aggregateAccumulators, result);
                    completedMatchCount++;
                }
                finally
                {
                    if (logSession != null && runner.Context?.EventBus != null)
                    {
                        runner.Context.EventBus.Published -= logSession.HandleBattleEvent;
                    }
                }
            }

            report.runMeta.completedMatchCount = completedMatchCount;
            report.heroAggregatesByClass = BuildHeroAggregateGroups(aggregateAccumulators);
            return new BattleOfflineSimulationRunResult(report, matchLogs);
        }

        private static void ValidateRequest(BattleOfflineSimulationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.TemplateInput == null)
            {
                throw new InvalidOperationException("Offline simulation requires a template BattleInputConfig.");
            }

            if (request.MatchCount <= 0)
            {
                throw new InvalidOperationException("Offline simulation match count must be greater than zero.");
            }

            if (request.FixedDeltaTimeSeconds <= 0f)
            {
                throw new InvalidOperationException("Offline simulation fixed delta time must be greater than zero.");
            }

            if (request.MaxTickCount <= 0)
            {
                throw new InvalidOperationException("Offline simulation max tick count must be greater than zero.");
            }

            if (request.SelectionMode == BattleOfflineSelectionMode.RandomCatalog && request.HeroCatalog == null)
            {
                throw new InvalidOperationException("RandomCatalog mode requires a HeroCatalogData asset.");
            }
        }

        private static List<HeroDefinition> BuildValidatedHeroPool(HeroCatalogData heroCatalog)
        {
            if (heroCatalog == null || heroCatalog.heroes == null || heroCatalog.heroes.Count == 0)
            {
                throw new InvalidOperationException("Hero catalog is empty. RandomCatalog mode cannot build a legal 5v5 match.");
            }

            var heroes = new List<HeroDefinition>();
            var seenHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < heroCatalog.heroes.Count; i++)
            {
                var hero = heroCatalog.heroes[i];
                if (hero == null)
                {
                    continue;
                }

                var heroId = hero.heroId?.Trim();
                if (string.IsNullOrWhiteSpace(heroId))
                {
                    throw new InvalidOperationException("Hero catalog contains an entry with an empty heroId.");
                }

                if (!seenHeroIds.Add(heroId))
                {
                    throw new InvalidOperationException($"Hero catalog contains duplicate heroId [{heroId}]. Random sampling requires a unique hero pool.");
                }

                heroes.Add(hero);
            }

            heroes.Sort(CompareHeroDefinitions);
            if (heroes.Count < BattleInputConfig.DefaultTeamSize * 2)
            {
                throw new InvalidOperationException(
                    $"Hero catalog must contain at least {BattleInputConfig.DefaultTeamSize * 2} unique heroes for a legal 5v5 unique-hero match.");
            }

            return heroes;
        }

        private static List<HeroDefinition> GetValidatedHeroesFromInput(BattleInputConfig inputConfig)
        {
            ValidateUniqueHeroIds(inputConfig, BattleOfflineSelectionMode.FixedInput);

            var heroes = new List<HeroDefinition>();
            heroes.AddRange(inputConfig.blueTeam.heroes);
            heroes.AddRange(inputConfig.redTeam.heroes);
            heroes.Sort(CompareHeroDefinitions);
            return heroes;
        }

        private static void ValidateUniqueHeroIds(BattleInputConfig inputConfig, BattleOfflineSelectionMode selectionMode)
        {
            if (inputConfig == null)
            {
                throw new InvalidOperationException("Battle input is null.");
            }

            if (!inputConfig.HasValidTeamCounts())
            {
                throw new InvalidOperationException(
                    $"Battle input requires exactly {BattleInputConfig.DefaultTeamSize} heroes on each side before offline simulation can start.");
            }

            var seenHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ValidateTeamHeroes(inputConfig.blueTeam, TeamSide.Blue, selectionMode, seenHeroIds);
            ValidateTeamHeroes(inputConfig.redTeam, TeamSide.Red, selectionMode, seenHeroIds);
        }

        private static void ValidateTeamHeroes(
            BattleTeamLoadout loadout,
            TeamSide expectedSide,
            BattleOfflineSelectionMode selectionMode,
            HashSet<string> seenHeroIds)
        {
            if (loadout == null || loadout.heroes == null)
            {
                throw new InvalidOperationException($"{selectionMode} mode received an empty team loadout for {expectedSide}.");
            }

            for (var i = 0; i < loadout.heroes.Count; i++)
            {
                var hero = loadout.heroes[i];
                if (hero == null)
                {
                    throw new InvalidOperationException($"{selectionMode} mode found a null hero in {expectedSide} slot {i}.");
                }

                var heroId = hero.heroId?.Trim();
                if (string.IsNullOrWhiteSpace(heroId))
                {
                    throw new InvalidOperationException($"{selectionMode} mode found an empty heroId in {expectedSide} slot {i}.");
                }

                if (!seenHeroIds.Add(heroId))
                {
                    throw new InvalidOperationException(
                        $"{selectionMode} mode requires globally unique heroes per match, but heroId [{heroId}] was repeated.");
                }
            }
        }

        private static BattleInputConfig CreateRandomCatalogInput(
            BattleInputConfig templateInput,
            IReadOnlyList<HeroDefinition> heroPool,
            int seed)
        {
            var selectionRandom = new BattleRandomService(seed);
            var availableHeroes = new List<HeroDefinition>(heroPool);
            var blueHeroes = new List<HeroDefinition>(BattleInputConfig.DefaultTeamSize);
            var redHeroes = new List<HeroDefinition>(BattleInputConfig.DefaultTeamSize);

            for (var slotIndex = 0; slotIndex < BattleInputConfig.DefaultTeamSize * 2; slotIndex++)
            {
                if (availableHeroes.Count <= 0)
                {
                    throw new InvalidOperationException("Hero pool was exhausted while building a unique-hero battle input.");
                }

                var heroIndex = selectionRandom.Range(0, availableHeroes.Count);
                var selectedHero = availableHeroes[heroIndex];
                availableHeroes.RemoveAt(heroIndex);

                if (slotIndex < BattleInputConfig.DefaultTeamSize)
                {
                    blueHeroes.Add(selectedHero);
                }
                else
                {
                    redHeroes.Add(selectedHero);
                }
            }

            return CloneInput(templateInput, blueHeroes, redHeroes, "OfflineRandomCatalogInput");
        }

        private static BattleInputConfig CloneInput(
            BattleInputConfig templateInput,
            IReadOnlyList<HeroDefinition> blueHeroes,
            IReadOnlyList<HeroDefinition> redHeroes,
            string runtimeName)
        {
            var runtimeInput = ScriptableObject.CreateInstance<BattleInputConfig>();
            runtimeInput.name = runtimeName;
            runtimeInput.regulationDurationSeconds = templateInput.regulationDurationSeconds;
            runtimeInput.respawnDelaySeconds = templateInput.respawnDelaySeconds;
            runtimeInput.enableBattleEventLogs = templateInput.enableBattleEventLogs;
            runtimeInput.enableSkills = templateInput.enableSkills;
            runtimeInput.arenaId = templateInput.arenaId;

            runtimeInput.blueTeam = new BattleTeamLoadout
            {
                side = TeamSide.Blue,
                ultimateTimingStrategy = templateInput.blueTeam != null
                    ? templateInput.blueTeam.ultimateTimingStrategy
                    : BattleUltimateTimingStrategy.Standard,
                ultimateComboStrategy = templateInput.blueTeam != null
                    ? templateInput.blueTeam.ultimateComboStrategy
                    : BattleUltimateComboStrategy.Standard,
            };
            runtimeInput.redTeam = new BattleTeamLoadout
            {
                side = TeamSide.Red,
                ultimateTimingStrategy = templateInput.redTeam != null
                    ? templateInput.redTeam.ultimateTimingStrategy
                    : BattleUltimateTimingStrategy.Standard,
                ultimateComboStrategy = templateInput.redTeam != null
                    ? templateInput.redTeam.ultimateComboStrategy
                    : BattleUltimateComboStrategy.Standard,
            };

            for (var i = 0; i < blueHeroes.Count; i++)
            {
                runtimeInput.blueTeam.heroes.Add(blueHeroes[i]);
            }

            for (var i = 0; i < redHeroes.Count; i++)
            {
                runtimeInput.redTeam.heroes.Add(redHeroes[i]);
            }

            return runtimeInput;
        }

        private static BattleOfflineSimulationMatchRecord BuildMatchRecord(
            int matchIndex,
            int seed,
            BattleInputConfig matchInput,
            BattleResultData result)
        {
            var record = new BattleOfflineSimulationMatchRecord
            {
                matchIndex = matchIndex,
                seed = seed,
                winner = result.winner.ToString(),
                endReason = result.endReason.ToString(),
                enteredOvertime = result.enteredOvertime,
                elapsedTimeSeconds = result.elapsedTimeSeconds,
                blueKills = result.blueKills,
                redKills = result.redKills,
                blueHeroes = BuildHeroReferences(matchInput.blueTeam.heroes, TeamSide.Blue),
                redHeroes = BuildHeroReferences(matchInput.redTeam.heroes, TeamSide.Red),
            };

            var sortedHeroStats = new List<HeroBattleStatLine>(result.heroStats);
            sortedHeroStats.Sort(CompareHeroStatLines);
            for (var i = 0; i < sortedHeroStats.Count; i++)
            {
                var heroStat = sortedHeroStats[i];
                record.heroStats.Add(new BattleOfflineHeroMatchStat
                {
                    heroId = heroStat.heroId ?? string.Empty,
                    displayName = string.IsNullOrWhiteSpace(heroStat.displayName) ? heroStat.heroId ?? string.Empty : heroStat.displayName,
                    heroClass = heroStat.heroClass.ToString(),
                    side = heroStat.side.ToString(),
                    slotIndex = heroStat.slotIndex,
                    won = heroStat.won,
                    kills = heroStat.kills,
                    deaths = heroStat.deaths,
                    assists = heroStat.assists,
                    damageDealt = heroStat.damageDealt,
                    damageTaken = heroStat.damageTaken,
                    healingDone = heroStat.healingDone,
                    shieldingDone = heroStat.shieldingDone,
                    activeSkillCastCount = heroStat.activeSkillCastCount,
                    ultimateCastCount = heroStat.ultimateCastCount,
                });
            }

            return record;
        }

        private static List<BattleOfflineHeroReference> BuildHeroReferences(IReadOnlyList<HeroDefinition> heroes, TeamSide side)
        {
            var results = new List<BattleOfflineHeroReference>();
            if (heroes == null)
            {
                return results;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null)
                {
                    continue;
                }

                results.Add(new BattleOfflineHeroReference
                {
                    heroId = hero.heroId ?? string.Empty,
                    displayName = string.IsNullOrWhiteSpace(hero.displayName) ? hero.heroId ?? string.Empty : hero.displayName,
                    heroClass = hero.heroClass.ToString(),
                    side = side.ToString(),
                    slotIndex = i,
                });
            }

            return results;
        }

        private static Dictionary<string, HeroAggregateAccumulator> CreateAggregateAccumulators(IReadOnlyList<HeroDefinition> heroes)
        {
            var accumulators = new Dictionary<string, HeroAggregateAccumulator>(StringComparer.OrdinalIgnoreCase);
            if (heroes == null)
            {
                return accumulators;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null || string.IsNullOrWhiteSpace(hero.heroId) || accumulators.ContainsKey(hero.heroId))
                {
                    continue;
                }

                accumulators.Add(hero.heroId, new HeroAggregateAccumulator
                {
                    Definition = hero,
                    HeroId = hero.heroId,
                    DisplayName = hero.displayName,
                    HeroClass = hero.heroClass,
                });
            }

            return accumulators;
        }

        private static void UpdateAggregateAccumulators(
            Dictionary<string, HeroAggregateAccumulator> accumulators,
            BattleResultData result)
        {
            if (result?.heroStats == null)
            {
                return;
            }

            for (var i = 0; i < result.heroStats.Count; i++)
            {
                var heroStat = result.heroStats[i];
                if (!accumulators.TryGetValue(heroStat.heroId, out var accumulator))
                {
                    accumulator = new HeroAggregateAccumulator
                    {
                        HeroId = heroStat.heroId,
                        DisplayName = heroStat.displayName,
                        HeroClass = heroStat.heroClass,
                    };
                    accumulators.Add(heroStat.heroId, accumulator);
                }

                accumulator.PickCount++;
                if (heroStat.side == TeamSide.Blue)
                {
                    accumulator.AppearancesAsBlue++;
                }
                else if (heroStat.side == TeamSide.Red)
                {
                    accumulator.AppearancesAsRed++;
                }

                if (heroStat.won)
                {
                    accumulator.WinCount++;
                }
                else
                {
                    accumulator.LossCount++;
                }

                accumulator.TotalKills += heroStat.kills;
                accumulator.TotalDeaths += heroStat.deaths;
                accumulator.TotalAssists += heroStat.assists;
                accumulator.TotalDamageDealt += heroStat.damageDealt;
                accumulator.TotalDamageTaken += heroStat.damageTaken;
                accumulator.TotalHealingDone += heroStat.healingDone;
                accumulator.TotalShieldingDone += heroStat.shieldingDone;
                accumulator.TotalActiveSkillCastCount += heroStat.activeSkillCastCount;
                accumulator.TotalUltimateCastCount += heroStat.ultimateCastCount;
            }
        }

        private static List<BattleOfflineSimulationHeroAggregateGroup> BuildHeroAggregateGroups(
            Dictionary<string, HeroAggregateAccumulator> accumulators)
        {
            var groups = new List<BattleOfflineSimulationHeroAggregateGroup>();
            var orderedClasses = new[]
            {
                HeroClass.Warrior,
                HeroClass.Mage,
                HeroClass.Assassin,
                HeroClass.Tank,
                HeroClass.Support,
                HeroClass.Marksman,
            };

            for (var classIndex = 0; classIndex < orderedClasses.Length; classIndex++)
            {
                var heroClass = orderedClasses[classIndex];
                var group = new BattleOfflineSimulationHeroAggregateGroup
                {
                    heroClass = heroClass.ToString(),
                };

                var classAccumulators = new List<HeroAggregateAccumulator>();
                foreach (var pair in accumulators)
                {
                    if (GetAccumulatorHeroClass(pair.Value) == heroClass)
                    {
                        classAccumulators.Add(pair.Value);
                    }
                }

                classAccumulators.Sort(CompareAccumulators);
                for (var i = 0; i < classAccumulators.Count; i++)
                {
                    group.heroes.Add(BuildAggregate(classAccumulators[i]));
                }

                groups.Add(group);
            }

            return groups;
        }

        private static BattleOfflineHeroAggregate BuildAggregate(HeroAggregateAccumulator accumulator)
        {
            var pickCount = Mathf.Max(0, accumulator.PickCount);
            var heroId = accumulator.Definition != null && !string.IsNullOrWhiteSpace(accumulator.Definition.heroId)
                ? accumulator.Definition.heroId
                : accumulator.HeroId ?? string.Empty;
            var displayName = accumulator.Definition != null && !string.IsNullOrWhiteSpace(accumulator.Definition.displayName)
                ? accumulator.Definition.displayName
                : !string.IsNullOrWhiteSpace(accumulator.DisplayName)
                    ? accumulator.DisplayName
                    : heroId;
            var heroClass = accumulator.Definition != null ? accumulator.Definition.heroClass : accumulator.HeroClass;

            return new BattleOfflineHeroAggregate
            {
                heroId = heroId ?? string.Empty,
                displayName = displayName ?? string.Empty,
                heroClass = heroClass.ToString(),
                side = "Combined",
                pickCount = pickCount,
                winCount = accumulator.WinCount,
                lossCount = accumulator.LossCount,
                appearancesAsBlue = accumulator.AppearancesAsBlue,
                appearancesAsRed = accumulator.AppearancesAsRed,
                winRate = pickCount > 0 ? (float)accumulator.WinCount / pickCount : 0f,
                totalKills = accumulator.TotalKills,
                totalDeaths = accumulator.TotalDeaths,
                totalAssists = accumulator.TotalAssists,
                totalDamageDealt = accumulator.TotalDamageDealt,
                totalDamageTaken = accumulator.TotalDamageTaken,
                totalHealingDone = accumulator.TotalHealingDone,
                totalShieldingDone = accumulator.TotalShieldingDone,
                totalActiveSkillCastCount = accumulator.TotalActiveSkillCastCount,
                totalUltimateCastCount = accumulator.TotalUltimateCastCount,
                averageKills = pickCount > 0 ? (float)accumulator.TotalKills / pickCount : 0f,
                averageDeaths = pickCount > 0 ? (float)accumulator.TotalDeaths / pickCount : 0f,
                averageAssists = pickCount > 0 ? (float)accumulator.TotalAssists / pickCount : 0f,
                averageDamageDealt = pickCount > 0 ? accumulator.TotalDamageDealt / pickCount : 0f,
                averageDamageTaken = pickCount > 0 ? accumulator.TotalDamageTaken / pickCount : 0f,
                averageHealingDone = pickCount > 0 ? accumulator.TotalHealingDone / pickCount : 0f,
                averageShieldingDone = pickCount > 0 ? accumulator.TotalShieldingDone / pickCount : 0f,
                averageActiveSkillCastCount = pickCount > 0 ? (float)accumulator.TotalActiveSkillCastCount / pickCount : 0f,
                averageUltimateCastCount = pickCount > 0 ? (float)accumulator.TotalUltimateCastCount / pickCount : 0f,
            };
        }

        private static string BuildMatchLogFileName(int matchIndex, int seed)
        {
            return $"match_{matchIndex + 1:0000}_seed_{seed}.txt";
        }

        private static int CompareHeroDefinitions(HeroDefinition left, HeroDefinition right)
        {
            if (ReferenceEquals(left, right))
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

            return string.Compare(left.heroId, right.heroId, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareAccumulators(HeroAggregateAccumulator left, HeroAggregateAccumulator right)
        {
            var leftClass = GetAccumulatorHeroClass(left);
            var rightClass = GetAccumulatorHeroClass(right);
            var classComparison = leftClass.CompareTo(rightClass);
            if (classComparison != 0)
            {
                return classComparison;
            }

            return string.Compare(GetAccumulatorHeroId(left), GetAccumulatorHeroId(right), StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareHeroStatLines(HeroBattleStatLine left, HeroBattleStatLine right)
        {
            var sideComparison = left.side.CompareTo(right.side);
            if (sideComparison != 0)
            {
                return sideComparison;
            }

            var slotComparison = left.slotIndex.CompareTo(right.slotIndex);
            if (slotComparison != 0)
            {
                return slotComparison;
            }

            return string.Compare(left.heroId, right.heroId, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetAccumulatorHeroId(HeroAggregateAccumulator accumulator)
        {
            if (accumulator?.Definition != null && !string.IsNullOrWhiteSpace(accumulator.Definition.heroId))
            {
                return accumulator.Definition.heroId;
            }

            return accumulator?.HeroId ?? string.Empty;
        }

        private static HeroClass GetAccumulatorHeroClass(HeroAggregateAccumulator accumulator)
        {
            return accumulator?.Definition != null
                ? accumulator.Definition.heroClass
                : accumulator != null
                    ? accumulator.HeroClass
                    : HeroClass.Warrior;
        }
    }
}
