using System.Collections.Generic;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public sealed class BattleSideHeroSidebarHud : MonoBehaviour
    {
        private const string RuntimeBaseTexturePath = "UI/BattleHud/side_hero_sidebar_runtime_base";
        private const int TeamSize = BattleInputConfig.DefaultTeamSize;
        private const float DesignCardWidth = 139f;
        private const float DesignCardHeight = 88f;
        private const float PortraitCropTopInsetRatio = 0.18f;
        private const float PortraitVisibleHeightRatio = 0.50f;
        private const float PortraitVerticalPlacementBias = 0.15f;

        [SerializeField] private float sideMargin = 12f;
        [SerializeField] private float bottomMargin = 12f;
        [SerializeField] private float verticalGap = 10f;
        [SerializeField] private float topHudReservedHeightAt1080p = 194f;
        [SerializeField] private float maxCardWidth = 280f;
        [SerializeField] private float maxCardHeight = 176f;
        [SerializeField] private float minCardHeight = 96f;
        [SerializeField] private float maxSideWidthRatio = 0.19f;
        [SerializeField] private float toggleButtonWidth = 18f;
        [SerializeField] private float toggleButtonMinHeight = 72f;
        [SerializeField] private float toggleButtonMaxHeight = 132f;
        [SerializeField] private float toggleButtonScreenInset = 2f;

        private static readonly Color BlueAccent = new Color32(88, 173, 255, 255);
        private static readonly Color RedAccent = new Color32(255, 126, 126, 255);
        private static readonly Color MainTextColor = new Color32(242, 244, 248, 255);
        private static readonly Color MutedTextColor = new Color32(170, 176, 188, 255);
        private static readonly Color DimTextColor = new Color32(120, 127, 141, 255);
        private static readonly Color BlueHeaderTint = new Color32(57, 113, 198, 255);
        private static readonly Color RedHeaderTint = new Color32(198, 47, 49, 255);
        private static readonly Color StatusBadgeFill = new Color32(214, 161, 46, 255);
        private static readonly Color StatusBadgeOutline = new Color32(120, 63, 18, 255);
        private static readonly Color StatusBadgeHighlight = new Color32(255, 220, 112, 170);
        private static readonly Color StatusBadgeIconColor = new Color32(77, 42, 14, 255);
        private static readonly Color PositiveStatColor = new Color32(129, 226, 170, 255);
        private static readonly Color NegativeStatColor = new Color32(255, 153, 147, 255);
        private static readonly Color ShadowColor = new Color32(0, 0, 0, 196);
        private static readonly Color DeadOverlayColor = new Color32(0, 0, 0, 116);
        private static readonly Color PortraitFallbackColor = new Color32(31, 40, 54, 235);
        private static readonly Color PortraitInnerFallbackColor = new Color32(53, 65, 84, 255);
        private static readonly Color ToggleButtonFillColor = new Color32(22, 28, 38, 220);
        private static readonly Color ToggleButtonOutlineColor = new Color32(112, 122, 140, 255);
        private static readonly string[] TraitPlaceholders = { "Reserved 1", "Reserved 2", "Reserved 3" };

        private readonly List<RuntimeHero> blueHeroes = new List<RuntimeHero>(TeamSize);
        private readonly List<RuntimeHero> redHeroes = new List<RuntimeHero>(TeamSize);

        private BattleManager battleManager;
        private Texture2D runtimeBaseTexture;
        private GUIStyle tabStyle;
        private GUIStyle titleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle kdaHeaderStyle;
        private GUIStyle kdaValueStyle;
        private GUIStyle statValueStyle;
        private GUIStyle traitStyle;
        private GUIStyle coreValueAlignedStyle;
        private GUIStyle portraitFallbackStyle;
        private GUIStyle toggleButtonStyle;
        private float lastStyleScale = -1f;
        private bool isBlueSidebarExpanded = true;
        private bool isRedSidebarExpanded = true;

        private sealed class HeroSidebarViewData
        {
            public string DisplayName;
            public string StateText;
            public Sprite Portrait;
            public int Kills;
            public int Deaths;
            public string AssistsText;
            public string DamageDealtText;
            public string DamageTakenText;
            public string HealingText;
            public string AttackText;
            public string DefenseText;
            public Color AttackColor = MainTextColor;
            public Color DefenseColor = MainTextColor;
            public bool IsDead;
        }

        private readonly struct SidebarLayout
        {
            public SidebarLayout(float cardWidth, float cardHeight, float leftX, float rightX, float startY, float totalHeight, float toggleButtonWidth, float toggleButtonHeight)
            {
                CardWidth = cardWidth;
                CardHeight = cardHeight;
                LeftX = leftX;
                RightX = rightX;
                StartY = startY;
                TotalHeight = totalHeight;
                ToggleButtonWidth = toggleButtonWidth;
                ToggleButtonHeight = toggleButtonHeight;
            }

            public float CardWidth { get; }

            public float CardHeight { get; }

            public float LeftX { get; }

            public float RightX { get; }

            public float StartY { get; }

            public float TotalHeight { get; }

            public float ToggleButtonWidth { get; }

            public float ToggleButtonHeight { get; }

            public Rect GetCardRect(TeamSide side, int slotIndex, float verticalGap)
            {
                var resolvedX = side == TeamSide.Blue ? LeftX : RightX;
                var resolvedY = StartY + (Mathf.Clamp(slotIndex, 0, TeamSize - 1) * (CardHeight + verticalGap));
                return new Rect(resolvedX, resolvedY, CardWidth, CardHeight);
            }
        }

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            runtimeBaseTexture = Resources.Load<Texture2D>(RuntimeBaseTexturePath);
        }

        private void OnGUI()
        {
            var context = battleManager != null ? battleManager.Context : null;
            if (context == null)
            {
                return;
            }

            CollectTeamHeroes(context, TeamSide.Blue, blueHeroes);
            CollectTeamHeroes(context, TeamSide.Red, redHeroes);

            if (!TryGetSidebarLayout(out var layout))
            {
                return;
            }

            var styleScale = layout.CardWidth / DesignCardWidth;
            EnsureStyles(styleScale);

            if (isBlueSidebarExpanded)
            {
                DrawSidebarCards(layout, blueHeroes, TeamSide.Blue, mirrorLayout: false);
            }

            if (isRedSidebarExpanded)
            {
                DrawSidebarCards(layout, redHeroes, TeamSide.Red, mirrorLayout: true);
            }

            DrawSideToggleButton(layout, TeamSide.Blue);
            DrawSideToggleButton(layout, TeamSide.Red);
        }

        private void DrawSidebarCards(SidebarLayout layout, List<RuntimeHero> heroes, TeamSide side, bool mirrorLayout)
        {
            for (var slotIndex = 0; slotIndex < TeamSize; slotIndex++)
            {
                DrawHeroCard(
                    layout.GetCardRect(side, slotIndex, verticalGap),
                    heroes.Count > slotIndex ? heroes[slotIndex] : null,
                    side,
                    mirrorLayout);
            }
        }

        private void DrawHeroCard(Rect cardRect, RuntimeHero hero, TeamSide side, bool mirrorLayout)
        {
            var viewData = BuildViewData(hero);
            var scale = cardRect.width / DesignCardWidth;

            GUI.BeginGroup(cardRect);
            DrawCardBackground(new Rect(0f, 0f, cardRect.width, cardRect.height), side, mirrorLayout);
            DrawSidebarText(scale, viewData, hero, side, mirrorLayout, cardRect.width, cardRect.height);
            GUI.EndGroup();
        }

        private void DrawCardBackground(Rect rect, TeamSide side, bool mirrorLayout)
        {
            if (runtimeBaseTexture != null)
            {
                var previousColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(
                    rect,
                    runtimeBaseTexture,
                    mirrorLayout ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f),
                    true);
                GUI.color = previousColor;
            }
            else
            {
                DrawTintedRect(rect, new Color(0.08f, 0.1f, 0.14f, 0.96f));
                DrawOutline(rect, new Color(0.21f, 0.26f, 0.34f, 1f));
            }

            DrawTeamHeaderTint(rect, side, mirrorLayout);

            DrawTintedRect(
                mirrorLayout
                    ? new Rect(rect.width - Mathf.Max(2f, rect.width * 0.015f), 0f, Mathf.Max(2f, rect.width * 0.015f), rect.height)
                    : new Rect(0f, 0f, Mathf.Max(2f, rect.width * 0.015f), rect.height),
                side == TeamSide.Red ? RedAccent : BlueAccent);
        }

        private void DrawSidebarText(
            float scale,
            HeroSidebarViewData viewData,
            RuntimeHero hero,
            TeamSide side,
            bool mirrorLayout,
            float cardWidth,
            float cardHeight)
        {
            DrawShadowedLabel(ScaleRect(1f, 0f, 27f, 23f, scale, mirrorLayout), "INFO", tabStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(28f, 0f, 93f, 23f, scale, mirrorLayout), viewData.DisplayName, titleStyle, MainTextColor);
            DrawStateBadge(
                ScaleRect(121f, 0f, 18f, 23f, scale, mirrorLayout),
                viewData.StateText,
                hero != null && hero.IsDead ? RedAccent : (side == TeamSide.Red ? RedAccent : BlueAccent));

            DrawShadowedLabel(ScaleRect(0f, 24f, 9.33f, 8.5f, scale, mirrorLayout), "K", kdaHeaderStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(9.33f, 24f, 9.33f, 8.5f, scale, mirrorLayout), "D", kdaHeaderStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(18.66f, 24f, 9.34f, 8.5f, scale, mirrorLayout), "A", kdaHeaderStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(0f, 34.5f, 9.33f, 9.5f, scale, mirrorLayout), viewData.Kills.ToString(), kdaValueStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(9.33f, 34.5f, 9.33f, 9.5f, scale, mirrorLayout), viewData.Deaths.ToString(), kdaValueStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(18.66f, 34.5f, 9.34f, 9.5f, scale, mirrorLayout), viewData.AssistsText, kdaValueStyle, MutedTextColor);

            DrawShadowedLabel(ScaleRect(10.2f, 48.4f, 15f, 8f, scale, mirrorLayout), viewData.DamageDealtText, statValueStyle, MainTextColor);
            DrawShadowedLabel(ScaleRect(10.2f, 61.9f, 15f, 8f, scale, mirrorLayout), viewData.DamageTakenText, statValueStyle, MutedTextColor);
            DrawShadowedLabel(ScaleRect(10.2f, 75.2f, 15f, 8f, scale, mirrorLayout), viewData.HealingText, statValueStyle, MainTextColor);

            DrawPortrait(ScaleRect(34f, 28f, 31f, 31f, scale, mirrorLayout), viewData.Portrait, viewData.DisplayName);

            for (var index = 0; index < TraitPlaceholders.Length; index++)
            {
                var rowY = 27f + (index * 10.5f);
                DrawShadowedLabel(
                    ScaleRect(72f, rowY, 65f, 8f, scale, mirrorLayout),
                    TraitPlaceholders[index],
                    traitStyle,
                    DimTextColor);
            }

            var attackValueX = mirrorLayout ? 54f : 58f;
            var defenseValueX = mirrorLayout ? 107f : 111f;
            DrawShadowedLabel(ScaleRect(attackValueX, 68f, 18f, 12f, scale, mirrorLayout), viewData.AttackText, coreValueAlignedStyle, viewData.AttackColor);
            DrawShadowedLabel(ScaleRect(defenseValueX, 68f, 18f, 12f, scale, mirrorLayout), viewData.DefenseText, coreValueAlignedStyle, viewData.DefenseColor);

            if (viewData.IsDead)
            {
                DrawTintedRect(new Rect(0f, 0f, cardWidth, cardHeight), DeadOverlayColor);
                DrawShadowedLabel(new Rect(0f, 0f, cardWidth, cardHeight), "DOWN", titleStyle, MainTextColor);
            }
        }

        private HeroSidebarViewData BuildViewData(RuntimeHero hero)
        {
            var viewData = new HeroSidebarViewData
            {
                DisplayName = hero?.Definition != null && !string.IsNullOrWhiteSpace(hero.Definition.displayName)
                    ? hero.Definition.displayName
                    : "Unknown",
                StateText = GetStateText(hero),
                Portrait = hero?.Definition?.visualConfig != null ? hero.Definition.visualConfig.portrait : null,
                Kills = hero != null ? hero.Kills : 0,
                Deaths = hero != null ? hero.Deaths : 0,
                AssistsText = hero != null ? hero.Assists.ToString() : "0",
                DamageDealtText = hero != null ? Mathf.RoundToInt(hero.DamageDealt).ToString() : "0",
                DamageTakenText = hero != null ? Mathf.RoundToInt(hero.DamageTaken).ToString() : "0",
                HealingText = hero != null ? Mathf.RoundToInt(hero.HealingAndShieldingDone).ToString() : "0",
                AttackText = hero != null ? Mathf.RoundToInt(hero.AttackPower).ToString() : "0",
                DefenseText = hero != null ? Mathf.RoundToInt(hero.Defense).ToString() : "0",
                IsDead = hero != null && hero.IsDead,
            };

            if (hero?.Definition?.baseStats != null)
            {
                viewData.AttackColor = ResolveStatColor(hero.AttackPower, hero.Definition.baseStats.attackPower);
                viewData.DefenseColor = ResolveStatColor(hero.Defense, hero.Definition.baseStats.defense);
            }

            return viewData;
        }

        private void DrawPortrait(Rect rect, Sprite portrait, string displayName)
        {
            if (portrait != null)
            {
                DrawSprite(rect, portrait);
                return;
            }

            DrawTintedRect(rect, PortraitFallbackColor);
            DrawTintedRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), PortraitInnerFallbackColor);
            DrawShadowedLabel(rect, GetPortraitFallbackText(displayName), portraitFallbackStyle, MainTextColor);
        }

        private void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = Color.white;
            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var visibleHeight = textureRect.height * PortraitVisibleHeightRatio;
            if (visibleHeight <= Mathf.Epsilon)
            {
                visibleHeight = textureRect.height;
            }

            var topInset = textureRect.height * PortraitCropTopInsetRatio;
            var maxTopInset = Mathf.Max(0f, textureRect.height - visibleHeight);
            topInset = Mathf.Clamp(topInset, 0f, maxTopInset);

            var texCoords = new Rect(
                textureRect.x / texture.width,
                (textureRect.y + textureRect.height - topInset - visibleHeight) / texture.height,
                textureRect.width / texture.width,
                visibleHeight / texture.height);

            var croppedAspect = textureRect.width / visibleHeight;
            var drawWidth = rect.height * croppedAspect;
            var drawHeight = rect.height;
            if (drawWidth < rect.width)
            {
                drawWidth = rect.width;
                drawHeight = rect.width / croppedAspect;
            }

            var drawX = rect.x + ((rect.width - drawWidth) * 0.5f);
            var drawY = rect.y + ((rect.height - drawHeight) * PortraitVerticalPlacementBias);

            GUI.BeginGroup(rect);
            GUI.DrawTextureWithTexCoords(
                new Rect(drawX - rect.x, drawY - rect.y, drawWidth, drawHeight),
                texture,
                texCoords,
                true);
            GUI.EndGroup();
            GUI.color = previousColor;
        }

        private void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color mainColor)
        {
            if (string.IsNullOrWhiteSpace(text) || style == null)
            {
                return;
            }

            var previousColor = style.normal.textColor;
            style.normal.textColor = ShadowColor;
            GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), text, style);
            style.normal.textColor = mainColor;
            GUI.Label(rect, text, style);
            style.normal.textColor = previousColor;
        }

        private void EnsureStyles(float scale)
        {
            if (Mathf.Abs(lastStyleScale - scale) < 0.01f && titleStyle != null)
            {
                return;
            }

            lastStyleScale = scale;
            tabStyle = BuildStyle(4.2f, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleStyle = BuildStyle(4.6f, scale, TextAnchor.MiddleCenter, FontStyle.Bold, allowShrink: true);
            badgeStyle = BuildStyle(2.8f, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            kdaHeaderStyle = BuildStyle(3.4f, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            kdaValueStyle = BuildStyle(4.1f, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            statValueStyle = BuildStyle(3.7f, scale, TextAnchor.MiddleRight, FontStyle.Bold);
            traitStyle = BuildStyle(2.5f, scale, TextAnchor.MiddleCenter, FontStyle.Normal, allowShrink: true);
            coreValueAlignedStyle = BuildStyle(5.2f, scale, TextAnchor.MiddleLeft, FontStyle.Bold);
            portraitFallbackStyle = BuildStyle(6f, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            toggleButtonStyle = BuildStyle(8.2f, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private GUIStyle BuildStyle(float designFontSize, float scale, TextAnchor alignment, FontStyle fontStyle, bool allowShrink = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = Mathf.Max(9, Mathf.RoundToInt(designFontSize * Mathf.Max(1f, scale) * 1.6f)),
                fontStyle = fontStyle,
                clipping = allowShrink ? TextClipping.Clip : TextClipping.Overflow,
                wordWrap = false,
                normal = { textColor = MainTextColor }
            };
        }

        private static void CollectTeamHeroes(BattleContext context, TeamSide side, List<RuntimeHero> buffer)
        {
            buffer.Clear();
            if (context?.Heroes == null)
            {
                return;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var hero = context.Heroes[i];
                if (hero != null && hero.Side == side)
                {
                    buffer.Add(hero);
                }
            }

            buffer.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
        }

        private bool TryGetSidebarLayout(out SidebarLayout layout)
        {
            layout = default;

            var topReserved = Mathf.Clamp((Screen.height / 1080f) * topHudReservedHeightAt1080p, 96f, 220f);
            var availableHeight = Mathf.Max(0f, Screen.height - topReserved - bottomMargin);
            if (availableHeight <= 0f)
            {
                return false;
            }

            var maxHeightByAvailable = (availableHeight - (verticalGap * (TeamSize - 1))) / TeamSize;
            var cardHeight = Mathf.Min(maxCardHeight, maxHeightByAvailable);
            if (maxHeightByAvailable >= minCardHeight)
            {
                cardHeight = Mathf.Max(minCardHeight, cardHeight);
            }

            if (cardHeight < 72f)
            {
                return false;
            }

            var cardWidth = Mathf.Min(maxCardWidth, cardHeight * (DesignCardWidth / DesignCardHeight));
            cardWidth = Mathf.Min(cardWidth, Screen.width * maxSideWidthRatio);
            var maxWidthByScreen = Mathf.Max(120f, (Screen.width - (sideMargin * 2f) - 12f) * 0.5f);
            cardWidth = Mathf.Min(cardWidth, maxWidthByScreen);
            cardHeight = cardWidth * (DesignCardHeight / DesignCardWidth);

            var totalHeight = (cardHeight * TeamSize) + (verticalGap * (TeamSize - 1));
            var startY = topReserved + Mathf.Max(0f, (availableHeight - totalHeight) * 0.5f);
            var leftX = sideMargin;
            var rightX = Screen.width - sideMargin - cardWidth;
            var resolvedButtonWidth = Mathf.Clamp(cardWidth * 0.13f, toggleButtonWidth, 28f);
            var resolvedButtonHeight = Mathf.Clamp(totalHeight * 0.22f, toggleButtonMinHeight, toggleButtonMaxHeight);

            layout = new SidebarLayout(cardWidth, cardHeight, leftX, rightX, startY, totalHeight, resolvedButtonWidth, resolvedButtonHeight);
            return true;
        }

        private static Rect ScaleRect(float x, float y, float width, float height, float scale, bool mirrorLayout)
        {
            var resolvedX = mirrorLayout ? DesignCardWidth - x - width : x;
            return new Rect(resolvedX * scale, y * scale, width * scale, height * scale);
        }

        private void DrawSideToggleButton(SidebarLayout layout, TeamSide side)
        {
            var expanded = side == TeamSide.Blue ? isBlueSidebarExpanded : isRedSidebarExpanded;
            var rect = GetToggleButtonRect(layout, side);
            var accentColor = side == TeamSide.Red ? RedAccent : BlueAccent;

            DrawTintedRect(rect, ToggleButtonFillColor);
            DrawOutline(rect, ToggleButtonOutlineColor);
            DrawTintedRect(
                side == TeamSide.Blue
                    ? new Rect(rect.xMax - 2f, rect.y, 2f, rect.height)
                    : new Rect(rect.x, rect.y, 2f, rect.height),
                accentColor);

            var arrow = side == TeamSide.Blue
                ? (expanded ? "<" : ">")
                : (expanded ? ">" : "<");
            DrawShadowedLabel(rect, arrow, toggleButtonStyle, MainTextColor);

            if (!GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                return;
            }

            if (side == TeamSide.Blue)
            {
                isBlueSidebarExpanded = !isBlueSidebarExpanded;
            }
            else
            {
                isRedSidebarExpanded = !isRedSidebarExpanded;
            }
        }

        private Rect GetToggleButtonRect(SidebarLayout layout, TeamSide side)
        {
            var buttonY = layout.StartY + ((layout.TotalHeight - layout.ToggleButtonHeight) * 0.5f);
            if (side == TeamSide.Blue)
            {
                return new Rect(toggleButtonScreenInset, buttonY, layout.ToggleButtonWidth, layout.ToggleButtonHeight);
            }

            return new Rect(
                Screen.width - toggleButtonScreenInset - layout.ToggleButtonWidth,
                buttonY,
                layout.ToggleButtonWidth,
                layout.ToggleButtonHeight);
        }

        private static string GetStateText(RuntimeHero hero)
        {
            if (hero == null)
            {
                return string.Empty;
            }

            if (!hero.IsDead)
            {
                return "UP";
            }

            return Mathf.Max(0, Mathf.CeilToInt(hero.RespawnRemainingSeconds)).ToString();
        }

        private static string GetPortraitFallbackText(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "?";
            }

            return displayName.Substring(0, 1).ToUpperInvariant();
        }

        private static Color ResolveStatColor(float currentValue, float baseValue)
        {
            if (currentValue > baseValue + Mathf.Epsilon)
            {
                return PositiveStatColor;
            }

            if (currentValue + Mathf.Epsilon < baseValue)
            {
                return NegativeStatColor;
            }

            return MainTextColor;
        }

        private static void DrawTintedRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private static void DrawTeamHeaderTint(Rect rect, TeamSide side, bool mirrorLayout)
        {
            var designRect = ScaleRect(28f, 0f, 111f, 23f, rect.width / DesignCardWidth, mirrorLayout);
            var color = side == TeamSide.Red ? RedHeaderTint : BlueHeaderTint;
            DrawTintedRect(designRect, color);
        }

        private void DrawStateBadge(Rect rect, string stateText, Color textColor)
        {
            DrawTintedRect(rect, StatusBadgeFill);
            DrawOutline(rect, StatusBadgeOutline);
            DrawTintedRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1.5f), StatusBadgeHighlight);

            var arrowWidth = Mathf.Max(4f, rect.width * 0.34f);
            var arrowInset = Mathf.Max(2f, rect.width * 0.18f);
            var arrowTop = rect.y + Mathf.Max(3f, rect.height * 0.18f);
            var arrowBottom = rect.y + rect.height - Mathf.Max(4f, rect.height * 0.18f);
            var arrowRight = rect.x + rect.width - arrowInset;
            var arrowLeft = arrowRight - arrowWidth;

            DrawLine(new Vector2(arrowLeft, arrowBottom), new Vector2(arrowRight, arrowTop), StatusBadgeIconColor, 1.4f);
            DrawLine(new Vector2(arrowRight - arrowWidth * 0.55f, arrowTop), new Vector2(arrowRight, arrowTop), StatusBadgeIconColor, 1.4f);
            DrawLine(new Vector2(arrowRight, arrowTop + arrowWidth * 0.55f), new Vector2(arrowRight, arrowTop), StatusBadgeIconColor, 1.4f);

            var textRect = new Rect(
                rect.x + arrowInset,
                rect.y + (rect.height * 0.36f),
                rect.width - arrowWidth - (arrowInset * 2f) - 1f,
                rect.height * 0.44f);
            DrawShadowedLabel(textRect, stateText, badgeStyle, textColor);
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            var matrix = GUI.matrix;
            var previousColor = GUI.color;
            var angle = Vector3.Angle(to - from, Vector2.right);
            if (from.y > to.y)
            {
                angle = -angle;
            }

            var length = Vector2.Distance(from, to);
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - (width * 0.5f), length, width), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = previousColor;
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawTintedRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawTintedRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawTintedRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }
    }
}
