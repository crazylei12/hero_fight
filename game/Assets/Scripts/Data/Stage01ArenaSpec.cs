using UnityEngine;

namespace Fight.Data
{
    public static class Stage01ArenaSpec
    {
        public const string ArenaId = "arena_stage01_flat";
        public const float ArenaScaleMultiplier = 1.5f;
        public const float WidthWorldUnits = 32f * ArenaScaleMultiplier;
        public const float HeightWorldUnits = 18f * ArenaScaleMultiplier;
        public const float HalfWidthWorldUnits = WidthWorldUnits * 0.5f;
        public const float HalfHeightWorldUnits = HeightWorldUnits * 0.5f;
        public const float UnitMinimumSeparationWorldUnits = 1f;
        public const float CameraOrthographicSize = HeightWorldUnits * 0.5f;
        public const float ImportedSpritePixelsPerUnit = 100f;
        public const float SpawnSideInsetWorldUnits = 6f * ArenaScaleMultiplier;
        public const float SpawnTopInsetWorldUnits = 3f * ArenaScaleMultiplier;
        public const float FrontlineSpawnMinDistanceFromCenterWorldUnits = 8.2f * ArenaScaleMultiplier;
        public const float FrontlineSpawnMaxDistanceFromCenterWorldUnits = 9.6f * ArenaScaleMultiplier;
        public const float BacklineSpawnMinDistanceFromCenterWorldUnits = 10.4f * ArenaScaleMultiplier;
        public const float BacklineSpawnMaxDistanceFromCenterWorldUnits = 11.8f * ArenaScaleMultiplier;
        public const float SpawnVerticalJitterWorldUnits = 0.45f * ArenaScaleMultiplier;
        public const float FloorWidthWorldUnits = 30f * ArenaScaleMultiplier;
        public const float FloorHeightWorldUnits = 16f * ArenaScaleMultiplier;
        public const float SkyWidthWorldUnits = WidthWorldUnits + (10f * ArenaScaleMultiplier);
        public const float SkyHeightWorldUnits = HeightWorldUnits + (8f * ArenaScaleMultiplier);
        public const float BackdropShadeWidthWorldUnits = WidthWorldUnits + (0.5f * ArenaScaleMultiplier);
        public const float BackdropShadeHeightWorldUnits = HeightWorldUnits + (0.4f * ArenaScaleMultiplier);
        public const float DustWidthWorldUnits = 22f * ArenaScaleMultiplier;
        public const float DustHeightWorldUnits = 12f * ArenaScaleMultiplier;
        public const float RingWidthWorldUnits = 24f * ArenaScaleMultiplier;
        public const float RingHeightWorldUnits = 12.6f * ArenaScaleMultiplier;
        public static readonly float FullMapTargetingRangeWorldUnits = Mathf.Ceil(
            Mathf.Sqrt((WidthWorldUnits * WidthWorldUnits) + (HeightWorldUnits * HeightWorldUnits)));

        public static Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -HalfWidthWorldUnits, HalfWidthWorldUnits);
            position.z = Mathf.Clamp(position.z, -HalfHeightWorldUnits, HalfHeightWorldUnits);
            return position;
        }

        public static Vector3 GetSpawnPosition(TeamSide side, int slotIndex)
        {
            var teamSize = Mathf.Max(1, BattleInputConfig.DefaultTeamSize);
            var x = side == TeamSide.Blue
                ? -(HalfWidthWorldUnits - SpawnSideInsetWorldUnits)
                : HalfWidthWorldUnits - SpawnSideInsetWorldUnits;

            if (teamSize == 1)
            {
                return new Vector3(x, 0f, 0f);
            }

            var z = GetSpawnLaneZ(slotIndex, teamSize);
            return new Vector3(x, 0f, z);
        }

        public static float GetSpawnLaneZ(int slotIndex, int teamSize)
        {
            var resolvedTeamSize = Mathf.Max(1, teamSize);
            if (resolvedTeamSize == 1)
            {
                return 0f;
            }

            var clampedSlotIndex = Mathf.Clamp(slotIndex, 0, resolvedTeamSize - 1);
            var verticalExtent = HalfHeightWorldUnits - SpawnTopInsetWorldUnits;
            var spacing = (verticalExtent * 2f) / (resolvedTeamSize - 1);
            return -verticalExtent + (clampedSlotIndex * spacing);
        }
    }
}
