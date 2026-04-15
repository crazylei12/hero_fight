using UnityEngine;

namespace Fight.Data
{
    public static class Stage01ArenaSpec
    {
        public const string ArenaId = "arena_stage01_flat";
        public const float WidthWorldUnits = 32f;
        public const float HeightWorldUnits = 18f;
        public const float HalfWidthWorldUnits = WidthWorldUnits * 0.5f;
        public const float HalfHeightWorldUnits = HeightWorldUnits * 0.5f;
        public const float CameraOrthographicSize = HeightWorldUnits * 0.5f;
        public const float ImportedSpritePixelsPerUnit = 100f;
        public const float SpawnSideInsetWorldUnits = 6f;
        public const float SpawnTopInsetWorldUnits = 3f;
        public const float FloorWidthWorldUnits = 30f;
        public const float FloorHeightWorldUnits = 16f;
        public const float SkyWidthWorldUnits = WidthWorldUnits + 10f;
        public const float SkyHeightWorldUnits = HeightWorldUnits + 8f;
        public const float BackdropShadeWidthWorldUnits = WidthWorldUnits + 0.5f;
        public const float BackdropShadeHeightWorldUnits = HeightWorldUnits + 0.4f;
        public const float DustWidthWorldUnits = 22f;
        public const float DustHeightWorldUnits = 12f;
        public const float RingWidthWorldUnits = 24f;
        public const float RingHeightWorldUnits = 12.6f;

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

            var clampedSlotIndex = Mathf.Clamp(slotIndex, 0, teamSize - 1);
            var verticalExtent = HalfHeightWorldUnits - SpawnTopInsetWorldUnits;
            var spacing = (verticalExtent * 2f) / (teamSize - 1);
            var z = -verticalExtent + (clampedSlotIndex * spacing);
            return new Vector3(x, 0f, z);
        }
    }
}
