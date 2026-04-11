using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleBootstrapper
    {
        private const float SpawnXOffset = 8f;
        private const float SpawnZSpacing = 2.5f;
        private const float CenteredRowOffset = 5f;

        public static List<RuntimeHero> CreateRuntimeHeroes(BattleInputConfig input)
        {
            var heroes = new List<RuntimeHero>();
            AddTeamHeroes(heroes, input.blueTeam, TeamSide.Blue);
            AddTeamHeroes(heroes, input.redTeam, TeamSide.Red);
            return heroes;
        }

        private static void AddTeamHeroes(List<RuntimeHero> destination, BattleTeamLoadout loadout, TeamSide side)
        {
            if (loadout == null || loadout.heroes == null)
            {
                return;
            }

            for (var i = 0; i < loadout.heroes.Count; i++)
            {
                var heroDefinition = loadout.heroes[i];
                if (heroDefinition == null)
                {
                    continue;
                }

                destination.Add(new RuntimeHero(heroDefinition, side, GetSpawnPosition(side, i), i));
            }
        }

        private static Vector3 GetSpawnPosition(TeamSide side, int slotIndex)
        {
            var x = side == TeamSide.Blue ? -SpawnXOffset : SpawnXOffset;
            var z = (slotIndex * SpawnZSpacing) - CenteredRowOffset;
            return new Vector3(x, 0f, z);
        }
    }
}
