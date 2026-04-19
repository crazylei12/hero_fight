using Fight.Battle;
using Fight.Data;
using UnityEngine;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleHud : MonoBehaviour
    {
        private const string RuntimeBaseTexturePath = "UI/BattleHud/top_scoreboard_runtime_base";
        private const float DesignWidth = 1880f;
        private const float DesignHeight = 184f;
        private const int MaxRoundDots = 3;

        [Header("Top Scoreboard")]
        [SerializeField] private Vector2 topOffset = new Vector2(0f, 6f);
        [SerializeField] private float screenWidthRatio = 0.98f;
        [SerializeField] private float maxHudWidth = DesignWidth;
        [SerializeField] private string blueTeamLabel = "蓝方";
        [SerializeField] private string redTeamLabel = "红方";
        [SerializeField] [Range(0, MaxRoundDots)] private int blueRoundWins;
        [SerializeField] [Range(0, MaxRoundDots)] private int redRoundWins;

        [Header("Result Banner")]
        [SerializeField] private Vector2 bannerSize = new Vector2(420f, 60f);

        private static readonly Color BlueColor = new Color32(88, 173, 255, 255);
        private static readonly Color RedColor = new Color32(255, 126, 126, 255);
        private static readonly Color MainTextColor = new Color32(244, 246, 250, 255);
        private static readonly Color PhaseColor = new Color32(236, 210, 170, 255);
        private static readonly Color DotInactiveColor = new Color32(110, 116, 130, 235);
        private static readonly Color DotOutlineColor = new Color32(255, 255, 255, 170);
        private static readonly Color ShadowColor = new Color32(0, 0, 0, 210);

        private BattleManager battleManager;
        private BattleEventBus boundEventBus;
        private Texture2D runtimeBaseTexture;
        private Texture2D dotTexture;
        private GUIStyle titleStyle;
        private GUIStyle scoreStyle;
        private GUIStyle timerStyle;
        private GUIStyle phaseStyle;
        private GUIStyle logoStyle;
        private GUIStyle bannerStyle;
        private GUIStyle fallbackTitleStyle;
        private GUIStyle fallbackBodyStyle;
        private string endBannerText;
        private float lastStyleScale = -1f;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            runtimeBaseTexture = Resources.Load<Texture2D>(RuntimeBaseTexturePath);
            dotTexture = CreateCircleTexture(32);
            if (battleManager != null)
            {
                battleManager.ContextInitialized += OnContextInitialized;
            }
        }

        private void Update()
        {
            BindToBattleEvents(battleManager != null ? battleManager.Context?.EventBus : null);
        }

        private void OnGUI()
        {
            var context = battleManager != null ? battleManager.Context : null;
            if (context == null)
            {
                return;
            }

            if (runtimeBaseTexture != null)
            {
                DrawTopScoreboard(context);
            }
            else
            {
                DrawLegacyFallback(context);
            }

            DrawEndBanner();
        }

        private void OnDisable()
        {
            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
                boundEventBus = null;
            }

            if (battleManager != null)
            {
                battleManager.ContextInitialized -= OnContextInitialized;
            }
        }

        private void OnContextInitialized(BattleContext context)
        {
            BindToBattleEvents(context?.EventBus);
        }

        private void BindToBattleEvents(BattleEventBus eventBus)
        {
            if (eventBus == null || ReferenceEquals(boundEventBus, eventBus))
            {
                return;
            }

            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
            }

            boundEventBus = eventBus;
            boundEventBus.Published += OnBattleEvent;
        }

        private void DrawTopScoreboard(BattleContext context)
        {
            var hudWidth = Mathf.Min(maxHudWidth, Screen.width * screenWidthRatio);
            hudWidth = Mathf.Min(hudWidth, Screen.width - 16f);
            var hudHeight = hudWidth * (DesignHeight / DesignWidth);
            var hudRect = new Rect(
                (Screen.width - hudWidth) * 0.5f + topOffset.x,
                topOffset.y,
                hudWidth,
                hudHeight);

            var scale = hudRect.width / DesignWidth;
            EnsureStyles(scale);

            GUI.DrawTexture(hudRect, runtimeBaseTexture, ScaleMode.StretchToFill, true);

            DrawShadowedLabel(ScaleRect(hudRect, 216f, 66f, 420f, 44f), GetTeamLabel(TeamSide.Blue), titleStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(hudRect, 1244f, 66f, 420f, 44f), GetTeamLabel(TeamSide.Red), titleStyle, MainTextColor);

            DrawShadowedLabel(
                ScaleRect(hudRect, 780f, 2f, 320f, 28f),
                context.Clock.IsOvertime ? "加时赛" : "常规时间",
                phaseStyle,
                PhaseColor);
            DrawShadowedLabel(
                ScaleRect(hudRect, 770f, 28f, 340f, 58f),
                FormatClockText(context),
                timerStyle,
                MainTextColor);
            DrawShadowedLabel(ScaleRect(hudRect, 901f, 115f, 78f, 34f), "VS", phaseStyle, PhaseColor);

            DrawShadowedLabel(ScaleRect(hudRect, 754f, 32f, 84f, 80f), context.ScoreSystem.BlueKills.ToString(), scoreStyle, BlueColor);
            DrawShadowedLabel(ScaleRect(hudRect, 1022f, 32f, 84f, 80f), context.ScoreSystem.RedKills.ToString(), scoreStyle, RedColor);

            DrawRoundDots(hudRect, 796f, 139f, blueRoundWins, BlueColor);
            DrawRoundDots(hudRect, 1064f, 139f, redRoundWins, RedColor);

            DrawShadowedLabel(ScaleRect(hudRect, 36f, 72f, 102f, 72f), "蓝", logoStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(hudRect, 1742f, 72f, 102f, 72f), "红", logoStyle, MainTextColor);
        }

        private void DrawLegacyFallback(BattleContext context)
        {
            EnsureStyles(1f);

            var headerRect = new Rect((Screen.width - 420f) * 0.5f, 16f, 420f, 84f);
            GUI.Box(headerRect, string.Empty);
            GUI.Label(new Rect(headerRect.x + 16f, headerRect.y + 8f, headerRect.width - 32f, 24f), "Arena Battle", fallbackTitleStyle);
            GUI.Label(new Rect(headerRect.x + 16f, headerRect.y + 34f, headerRect.width - 32f, 26f), $"Blue {context.ScoreSystem.BlueKills} - {context.ScoreSystem.RedKills} Red", fallbackTitleStyle);
            GUI.Label(new Rect(headerRect.x + 16f, headerRect.y + 58f, headerRect.width - 32f, 18f), FormatFallbackStateText(context), fallbackBodyStyle);
        }

        private void DrawEndBanner()
        {
            if (string.IsNullOrEmpty(endBannerText))
            {
                return;
            }

            EnsureStyles(1f);
            var bannerRect = new Rect((Screen.width - bannerSize.x) * 0.5f, Screen.height - bannerSize.y - 20f, bannerSize.x, bannerSize.y);
            GUI.Box(bannerRect, string.Empty);
            DrawShadowedLabel(new Rect(bannerRect.x + 12f, bannerRect.y + 14f, bannerRect.width - 24f, 32f), endBannerText, bannerStyle, MainTextColor);
        }

        private void DrawRoundDots(Rect hudRect, float groupCenterX, float y, int filledCount, Color activeColor)
        {
            if (dotTexture == null)
            {
                return;
            }

            const float dotSize = 18f;
            const float dotSpacing = 28f;
            var startX = groupCenterX - ((dotSize + (dotSpacing * (MaxRoundDots - 1))) * 0.5f);

            for (var index = 0; index < MaxRoundDots; index++)
            {
                var dotRect = ScaleRect(hudRect, startX + (index * dotSpacing), y, dotSize, dotSize);
                var dotColor = index < filledCount ? activeColor : DotInactiveColor;
                DrawTintedTexture(dotRect, dotTexture, dotColor);

                var outlineRect = new Rect(dotRect.x - 1f, dotRect.y - 1f, dotRect.width + 2f, dotRect.height + 2f);
                DrawTintedTexture(outlineRect, dotTexture, DotOutlineColor * new Color(1f, 1f, 1f, 0.4f));
                DrawTintedTexture(dotRect, dotTexture, dotColor);
            }
        }

        private void DrawTintedTexture(Rect rect, Texture texture, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void OnBattleEvent(IBattleEvent battleEvent)
        {
            if (battleEvent is BattleStartedEvent)
            {
                endBannerText = string.Empty;
                return;
            }

            if (battleEvent is BattleEndedEvent ended)
            {
                endBannerText = $"Winner: {ended.Result.winner}  |  {ended.Result.endReason}";
            }
        }

        private void EnsureStyles(float scale)
        {
            if (Mathf.Abs(lastStyleScale - scale) < 0.01f && titleStyle != null)
            {
                return;
            }

            lastStyleScale = scale;
            titleStyle = BuildStyle(38, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreStyle = BuildStyle(86, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            timerStyle = BuildStyle(48, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            phaseStyle = BuildStyle(22, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            logoStyle = BuildStyle(36, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerStyle = BuildStyle(18, 1f, TextAnchor.MiddleCenter, FontStyle.Bold);
            fallbackTitleStyle = BuildStyle(18, 1f, TextAnchor.MiddleCenter, FontStyle.Bold);
            fallbackBodyStyle = BuildStyle(12, 1f, TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        private GUIStyle BuildStyle(int baseSize, float scale, TextAnchor alignment, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(baseSize * Mathf.Max(0.6f, scale))),
                fontStyle = fontStyle,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = MainTextColor }
            };
        }

        private void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color mainColor)
        {
            if (string.IsNullOrEmpty(text) || style == null)
            {
                return;
            }

            var previousColor = style.normal.textColor;
            style.normal.textColor = ShadowColor;
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            style.normal.textColor = mainColor;
            GUI.Label(rect, text, style);
            style.normal.textColor = previousColor;
        }

        private string GetTeamLabel(TeamSide side)
        {
            return side == TeamSide.Red ? redTeamLabel : blueTeamLabel;
        }

        private static string FormatClockText(BattleContext context)
        {
            if (context?.Clock == null)
            {
                return "00:00";
            }

            var seconds = context.Clock.IsOvertime
                ? Mathf.Max(0f, context.Clock.ElapsedTimeSeconds - context.Clock.RegulationDurationSeconds)
                : Mathf.Max(0f, context.Clock.RegulationDurationSeconds - context.Clock.ElapsedTimeSeconds);

            return FormatClockSeconds(seconds);
        }

        private static string FormatFallbackStateText(BattleContext context)
        {
            if (context?.Clock == null)
            {
                return string.Empty;
            }

            return context.Clock.IsOvertime
                ? $"Overtime  {FormatClockSeconds(Mathf.Max(0f, context.Clock.ElapsedTimeSeconds - context.Clock.RegulationDurationSeconds))}"
                : $"Regulation  {FormatClockSeconds(Mathf.Max(0f, context.Clock.RegulationDurationSeconds - context.Clock.ElapsedTimeSeconds))}";
        }

        private static string FormatClockSeconds(float seconds)
        {
            var clampedSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            var minutes = clampedSeconds / 60;
            var remainingSeconds = clampedSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static Rect ScaleRect(Rect parent, float x, float y, float width, float height)
        {
            var scale = parent.width / DesignWidth;
            return new Rect(
                parent.x + (x * scale),
                parent.y + (y * scale),
                width * scale,
                height * scale);
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = Mathf.Clamp01(radius - distance);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
