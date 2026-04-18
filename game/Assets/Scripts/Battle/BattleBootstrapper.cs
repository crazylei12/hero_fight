using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleBootstrapper
    {
        private const float FrontlineAttackRangeThreshold = 2.5f;

        private struct TeamHeroEntry
        {
            public TeamHeroEntry(HeroDefinition definition, int slotIndex)
            {
                Definition = definition;
                SlotIndex = slotIndex;
            }

            public HeroDefinition Definition { get; }

            public int SlotIndex { get; }
        }

        public static List<RuntimeHero> CreateRuntimeHeroes(BattleInputConfig input, BattleRandomService randomService)
        {
            if (randomService == null)
            {
                randomService = new BattleRandomService();
            }

            var heroes = new List<RuntimeHero>();
            AddTeamHeroes(heroes, input.blueTeam, TeamSide.Blue, randomService);
            AddTeamHeroes(heroes, input.redTeam, TeamSide.Red, randomService);
            return heroes;
        }

        private static void AddTeamHeroes(
            List<RuntimeHero> destination,
            BattleTeamLoadout loadout,
            TeamSide side,
            BattleRandomService randomService)
        {
            if (loadout == null || loadout.heroes == null)
            {
                return;
            }

            var entries = new List<TeamHeroEntry>();
            for (var i = 0; i < loadout.heroes.Count; i++)
            {
                var heroDefinition = loadout.heroes[i];
                if (heroDefinition == null)
                {
                    continue;
                }

                entries.Add(new TeamHeroEntry(heroDefinition, i));
            }

            if (entries.Count == 0)
            {
                return;
            }

            var spawnPositions = CreateSpawnPositions(side, entries, randomService);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                destination.Add(new RuntimeHero(entry.Definition, side, spawnPositions[i], entry.SlotIndex));
            }
        }

        private static Vector3[] CreateSpawnPositions(
            TeamSide side,
            IReadOnlyList<TeamHeroEntry> entries,
            BattleRandomService randomService)
        {
            var spawnPositions = new Vector3[entries.Count];
            var laneAnchors = BuildLaneAnchors(entries.Count);
            Shuffle(laneAnchors, randomService);

            var frontlineIndices = new List<int>();
            var backlineIndices = new List<int>();
            for (var i = 0; i < entries.Count; i++)
            {
                if (IsFrontlineHero(entries[i].Definition))
                {
                    frontlineIndices.Add(i);
                }
                else
                {
                    backlineIndices.Add(i);
                }
            }

            var laneCursor = 0;
            laneCursor = AssignSpawnPositions(spawnPositions, side, laneAnchors, laneCursor, frontlineIndices, true, randomService);
            AssignSpawnPositions(spawnPositions, side, laneAnchors, laneCursor, backlineIndices, false, randomService);
            return spawnPositions;
        }

        private static int AssignSpawnPositions(
            Vector3[] spawnPositions,
            TeamSide side,
            float[] laneAnchors,
            int laneCursor,
            IReadOnlyList<int> entryIndices,
            bool frontline,
            BattleRandomService randomService)
        {
            var maxVerticalExtent = Stage01ArenaSpec.HalfHeightWorldUnits - Stage01ArenaSpec.SpawnTopInsetWorldUnits;
            var sideDirection = side == TeamSide.Blue ? -1f : 1f;

            for (var i = 0; i < entryIndices.Count; i++)
            {
                var entryIndex = entryIndices[i];
                var z = laneAnchors[laneCursor++];
                z += randomService.Range(
                    -Stage01ArenaSpec.SpawnVerticalJitterWorldUnits,
                    Stage01ArenaSpec.SpawnVerticalJitterWorldUnits);
                z = Mathf.Clamp(z, -maxVerticalExtent, maxVerticalExtent);

                var minDistanceFromCenter = frontline
                    ? Stage01ArenaSpec.FrontlineSpawnMinDistanceFromCenterWorldUnits
                    : Stage01ArenaSpec.BacklineSpawnMinDistanceFromCenterWorldUnits;
                var maxDistanceFromCenter = frontline
                    ? Stage01ArenaSpec.FrontlineSpawnMaxDistanceFromCenterWorldUnits
                    : Stage01ArenaSpec.BacklineSpawnMaxDistanceFromCenterWorldUnits;
                var x = sideDirection * randomService.Range(minDistanceFromCenter, maxDistanceFromCenter);

                spawnPositions[entryIndex] = Stage01ArenaSpec.ClampPosition(new Vector3(x, 0f, z));
            }

            return laneCursor;
        }

        private static float[] BuildLaneAnchors(int heroCount)
        {
            var resolvedHeroCount = Mathf.Max(1, heroCount);
            var anchors = new float[resolvedHeroCount];
            for (var i = 0; i < resolvedHeroCount; i++)
            {
                anchors[i] = Stage01ArenaSpec.GetSpawnLaneZ(i, resolvedHeroCount);
            }

            return anchors;
        }

        private static void Shuffle(float[] values, BattleRandomService randomService)
        {
            if (values == null || values.Length <= 1 || randomService == null)
            {
                return;
            }

            for (var i = values.Length - 1; i > 0; i--)
            {
                var swapIndex = randomService.Range(0, i + 1);
                var swapValue = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = swapValue;
            }
        }

        private static bool IsFrontlineHero(HeroDefinition heroDefinition)
        {
            if (heroDefinition == null)
            {
                return false;
            }

            if (HasTag(heroDefinition, HeroTag.Melee))
            {
                return true;
            }

            if (HasTag(heroDefinition, HeroTag.Ranged))
            {
                return false;
            }

            if (heroDefinition.basicAttack != null && heroDefinition.basicAttack.usesProjectile)
            {
                return false;
            }

            var attackRange = 0f;
            if (heroDefinition.basicAttack != null && heroDefinition.basicAttack.rangeOverride > 0f)
            {
                attackRange = heroDefinition.basicAttack.rangeOverride;
            }
            else if (heroDefinition.baseStats != null)
            {
                attackRange = heroDefinition.baseStats.attackRange;
            }

            return attackRange <= FrontlineAttackRangeThreshold;
        }

        private static bool HasTag(HeroDefinition heroDefinition, HeroTag tag)
        {
            if (heroDefinition?.tags == null)
            {
                return false;
            }

            for (var i = 0; i < heroDefinition.tags.Count; i++)
            {
                if (heroDefinition.tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
