using Fight.Data;
using UnityEngine;

namespace Fight.UI
{
    public static class BattleScreenLayout
    {
        public const float DesignCardWidth = 139f;
        public const float DesignCardHeight = 88f;

        private const int TeamSize = BattleInputConfig.DefaultTeamSize;
        private const float SideMargin = 12f;
        private const float BottomMargin = 12f;
        private const float VerticalGap = 10f;
        private const float TopHudReservedHeightAt1080p = 194f;
        private const float MaxCardWidth = 176f;
        private const float MaxCardHeight = 112f;
        private const float MinCardHeight = 96f;
        private const float MaxSideWidthRatio = 0.19f;
        private const float ViewportGap = 8f;

        public readonly struct Metrics
        {
            public Metrics(float screenWidth, float cardWidth, float cardHeight, float startY, float verticalGap, Rect battleViewportPixels, Rect battleViewportNormalized)
            {
                ScreenWidth = screenWidth;
                CardWidth = cardWidth;
                CardHeight = cardHeight;
                StartY = startY;
                VerticalGap = verticalGap;
                BattleViewportPixels = battleViewportPixels;
                BattleViewportNormalized = battleViewportNormalized;
            }

            public float ScreenWidth { get; }

            public float CardWidth { get; }

            public float CardHeight { get; }

            public float StartY { get; }

            public float VerticalGap { get; }

            public Rect BattleViewportPixels { get; }

            public Rect BattleViewportNormalized { get; }

            public float BattleViewportAspect => BattleViewportPixels.height > Mathf.Epsilon
                ? BattleViewportPixels.width / BattleViewportPixels.height
                : 0f;

            public Rect GetCardRect(TeamSide side, int slotIndex)
            {
                var clampedSlotIndex = Mathf.Clamp(slotIndex, 0, TeamSize - 1);
                var x = side == TeamSide.Blue
                    ? SideMargin
                    : (ScreenWidth - SideMargin - CardWidth);
                var y = StartY + (clampedSlotIndex * (CardHeight + VerticalGap));
                return new Rect(x, y, CardWidth, CardHeight);
            }
        }

        public static bool TryGetMetrics(out Metrics metrics)
        {
            return TryGetMetrics(Screen.width, Screen.height, out metrics);
        }

        public static bool TryGetMetrics(float screenWidth, float screenHeight, out Metrics metrics)
        {
            metrics = default;
            if (screenWidth <= Mathf.Epsilon || screenHeight <= Mathf.Epsilon)
            {
                return false;
            }

            var topReserved = Mathf.Clamp((screenHeight / 1080f) * TopHudReservedHeightAt1080p, 96f, 220f);
            var availableHeight = Mathf.Max(0f, screenHeight - topReserved - BottomMargin);
            if (availableHeight <= Mathf.Epsilon)
            {
                return false;
            }

            var maxHeightByAvailable = (availableHeight - (VerticalGap * (TeamSize - 1))) / TeamSize;
            var cardHeight = Mathf.Min(MaxCardHeight, maxHeightByAvailable);
            if (maxHeightByAvailable >= MinCardHeight)
            {
                cardHeight = Mathf.Max(MinCardHeight, cardHeight);
            }

            if (cardHeight < 72f)
            {
                return false;
            }

            var cardWidth = Mathf.Min(MaxCardWidth, cardHeight * (DesignCardWidth / DesignCardHeight));
            cardWidth = Mathf.Min(cardWidth, screenWidth * MaxSideWidthRatio);
            var maxWidthByScreen = Mathf.Max(120f, (screenWidth - ((SideMargin + ViewportGap) * 2f)) * 0.5f);
            cardWidth = Mathf.Min(cardWidth, maxWidthByScreen);
            cardHeight = cardWidth * (DesignCardHeight / DesignCardWidth);

            if (cardWidth < 120f || cardHeight < 72f)
            {
                return false;
            }

            var totalHeight = (cardHeight * TeamSize) + (VerticalGap * (TeamSize - 1));
            var startY = topReserved + Mathf.Max(0f, (availableHeight - totalHeight) * 0.5f);
            var leftViewportEdge = SideMargin + cardWidth + ViewportGap;
            var rightViewportEdge = screenWidth - SideMargin - cardWidth - ViewportGap;
            if (rightViewportEdge - leftViewportEdge <= Mathf.Epsilon)
            {
                return false;
            }

            var viewportPixels = new Rect(leftViewportEdge, 0f, rightViewportEdge - leftViewportEdge, screenHeight);
            var viewportNormalized = new Rect(
                viewportPixels.x / screenWidth,
                0f,
                viewportPixels.width / screenWidth,
                1f);

            metrics = new Metrics(screenWidth, cardWidth, cardHeight, startY, VerticalGap, viewportPixels, viewportNormalized);
            return true;
        }

        public static float GetRequiredOrthographicSize(float viewportAspect)
        {
            if (viewportAspect <= Mathf.Epsilon)
            {
                return Stage01ArenaSpec.CameraOrthographicSize;
            }

            var widthFitSize = Stage01ArenaSpec.WidthWorldUnits / (viewportAspect * 2f);
            return Mathf.Max(Stage01ArenaSpec.CameraOrthographicSize, widthFitSize);
        }
    }
}
