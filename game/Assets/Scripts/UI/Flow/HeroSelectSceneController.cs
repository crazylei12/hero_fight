using System.Collections.Generic;
using Fight.Core;
using Fight.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fight.UI.Flow
{
    [DisallowMultipleComponent]
    public class HeroSelectSceneController : MonoBehaviour
    {
        [SerializeField] private string battleSceneName = "Battle";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private const string PlayerStatusGoodArrowTexturePath = "UI/PlayerStatusArrows/player_status_good_up";
        private const string PlayerStatusNormalArrowTexturePath = "UI/PlayerStatusArrows/player_status_normal_right";
        private const string PlayerStatusBadArrowTexturePath = "UI/PlayerStatusArrows/player_status_bad_down";
        private const string TopScoreboardTexturePath = "UI/BattleHud/top_scoreboard_runtime_base";
        private const float TopScoreboardDesignWidth = 1880f;
        private const float TopScoreboardDesignHeight = 184f;
        private const int MaxBanDots = 3;
        private const int AthleteTraitSlotCount = 3;

        private HeroDefinition highlightedHero;
        private TeamSide? swapSourceSide;
        private int swapSourceIndex = -1;
        private bool classFilterActive;
        private HeroClass classFilter;
        private Texture2D topScoreboardTexture;
        private Texture2D topScoreboardDotTexture;
        private Sprite playerStatusGoodArrowSprite;
        private Sprite playerStatusNormalArrowSprite;
        private Sprite playerStatusBadArrowSprite;
        private Camera fallbackUiCamera;
        private GUIStyle topTeamStyle;
        private GUIStyle topScoreStyle;
        private GUIStyle topPhaseStyle;
        private GUIStyle topLogoStyle;
        private GUIStyle topButtonStyle;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallBodyStyle;
        private GUIStyle slotStyle;
        private GUIStyle focusedSlotStyle;
        private GUIStyle heroCardButtonStyle;
        private GUIStyle filterButtonStyle;
        private GUIStyle strategyButtonStyle;
        private GUIStyle detailHeaderStyle;
        private GUIStyle athleteInitialStyle;
        private Vector2 catalogScroll;
        private string draftNotice;
        private float draftNoticeUntil;
        private float lastTopStyleScale = -1f;

        private static readonly Color BlueAccent = new Color32(88, 173, 255, 255);
        private static readonly Color RedAccent = new Color32(255, 126, 126, 255);
        private static readonly Color MainTextColor = new Color32(244, 246, 250, 255);
        private static readonly Color PhaseTextColor = new Color32(236, 210, 170, 255);
        private static readonly Color ShadowColor = new Color32(0, 0, 0, 210);
        private static readonly Color DotInactiveColor = new Color32(95, 102, 116, 235);
        private static readonly Color DotOutlineColor = new Color32(255, 255, 255, 96);

        private void Awake()
        {
            GameFlowState.RefreshHeroCatalog();
            GameFlowState.ResetDraft();
            GameFlowState.ClearBattleResult();
            topScoreboardTexture = Resources.Load<Texture2D>(TopScoreboardTexturePath);
            topScoreboardDotTexture = CreateCircleTexture(32);
            playerStatusGoodArrowSprite = Resources.Load<Sprite>(PlayerStatusGoodArrowTexturePath);
            playerStatusNormalArrowSprite = Resources.Load<Sprite>(PlayerStatusNormalArrowTexturePath);
            playerStatusBadArrowSprite = Resources.Load<Sprite>(PlayerStatusBadArrowTexturePath);
            highlightedHero = FindFirstCatalogHero();
            EnsureFallbackUiCamera();
        }

        private void OnDestroy()
        {
            if (fallbackUiCamera != null)
            {
                Destroy(fallbackUiCamera.gameObject);
                fallbackUiCamera = null;
            }

            if (topScoreboardDotTexture != null)
            {
                Destroy(topScoreboardDotTexture);
                topScoreboardDotTexture = null;
            }
        }

        private void EnsureFallbackUiCamera()
        {
            if (FindObjectsByType<Camera>(FindObjectsSortMode.None).Length > 0)
            {
                return;
            }

            var cameraObject = new GameObject("HeroSelect UI Fallback Camera");
            cameraObject.transform.SetParent(transform, false);
            fallbackUiCamera = cameraObject.AddComponent<Camera>();
            fallbackUiCamera.clearFlags = CameraClearFlags.SolidColor;
            fallbackUiCamera.backgroundColor = new Color(0.055f, 0.06f, 0.068f, 1f);
            fallbackUiCamera.cullingMask = 0;
            fallbackUiCamera.orthographic = true;
            fallbackUiCamera.depth = -100f;
            fallbackUiCamera.useOcclusionCulling = false;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (highlightedHero == null)
            {
                highlightedHero = FindFirstCatalogHero();
            }

            var margin = 12f;
            var headerWidth = Screen.width - (margin * 2f) - 12f;
            var headerHeight = ResolveTopScoreboardHeight(headerWidth);
            var phaseHeight = Mathf.Clamp(Screen.height * 0.05f, 46f, 54f);
            var bottomHeight = Mathf.Clamp(Screen.height * 0.13f, 110f, 140f);
            var gap = 10f;
            var sideWidth = Mathf.Clamp(Screen.width * 0.145f, 228f, 286f);
            var contentY = margin + headerHeight + gap;
            var contentBottom = Screen.height - margin;
            var bottomY = contentBottom - bottomHeight;
            var sideHeight = Mathf.Max(460f, contentBottom - contentY);

            var leftRect = new Rect(margin + 10f, contentY, sideWidth, sideHeight);
            var rightRect = new Rect(Screen.width - margin - 10f - sideWidth, contentY, sideWidth, sideHeight);
            var centerX = leftRect.xMax + gap;
            var centerWidth = rightRect.xMin - centerX - gap;
            var centerY = contentY + phaseHeight + gap;
            var centerContentHeight = Mathf.Max(360f, bottomY - gap - centerY);
            var centerRect = new Rect(centerX, centerY, centerWidth, centerContentHeight);
            var bottomRect = new Rect(centerX, bottomY, centerWidth, bottomHeight);
            var outerRect = new Rect(margin, margin, Screen.width - (margin * 2f), Screen.height - (margin * 2f));

            DrawScreenBackdrop(outerRect, leftRect, rightRect, centerRect, bottomRect);

            DrawHeader(new Rect(margin + 6f, margin + 4f, headerWidth, headerHeight));
            DrawPhaseBanner(new Rect(centerX, contentY, centerWidth, phaseHeight));

            DrawTeamPanel(leftRect, TeamSide.Blue, new Color(0.12f, 0.34f, 0.86f, 0.92f));
            DrawTeamPanel(rightRect, TeamSide.Red, new Color(0.88f, 0.16f, 0.18f, 0.92f));
            DrawCenterPanel(centerRect);
            DrawBanPanel(bottomRect);
        }

        private void DrawHeader(Rect rect)
        {
            var scale = rect.width / TopScoreboardDesignWidth;
            EnsureTopStyles(scale);

            if (topScoreboardTexture != null)
            {
                GUI.DrawTexture(rect, topScoreboardTexture, ScaleMode.StretchToFill, true);
            }
            else
            {
                DrawTopScoreboardFallbackBase(rect);
            }

            DrawShadowedLabel(ScaleTopRect(rect, 216f, 43f, 420f, 78f), "Blue Team", topTeamStyle, MainTextColor);
            DrawShadowedLabel(ScaleTopRect(rect, 1244f, 43f, 420f, 78f), "Red Team", topTeamStyle, MainTextColor);

            DrawShadowedLabel(ScaleTopRect(rect, 901f, 115f, 78f, 34f), "BP", topPhaseStyle, PhaseTextColor);

            DrawShadowedLabel(ScaleTopRect(rect, 754f, 32f, 84f, 80f), CountPicked(GameFlowState.BlueSelection).ToString(), topScoreStyle, BlueAccent);
            DrawShadowedLabel(ScaleTopRect(rect, 1022f, 32f, 84f, 80f), CountPicked(GameFlowState.RedSelection).ToString(), topScoreStyle, RedAccent);
            DrawBanDots(rect, 796f, 124f, CountPicked(GameFlowState.BlueBans), BlueAccent);
            DrawBanDots(rect, 1064f, 124f, CountPicked(GameFlowState.RedBans), RedAccent);

            DrawShadowedLabel(ScaleTopRect(rect, 36f, 52f, 102f, 72f), "蓝", topLogoStyle, MainTextColor);
            DrawShadowedLabel(ScaleTopRect(rect, 1742f, 52f, 102f, 72f), "红", topLogoStyle, MainTextColor);

        }

        private static float ResolveTopScoreboardHeight(float width)
        {
            return Mathf.Clamp(width * (TopScoreboardDesignHeight / TopScoreboardDesignWidth), 96f, 148f);
        }

        private void DrawTopScoreboardFallbackBase(Rect rect)
        {
            DrawTintedRect(rect, new Color(0.08f, 0.09f, 0.12f, 0.98f));
            DrawTintedRect(ScaleTopRect(rect, 0f, 0f, 730f, 184f), new Color(0.11f, 0.15f, 0.23f, 0.92f));
            DrawTintedRect(ScaleTopRect(rect, 1150f, 0f, 730f, 184f), new Color(0.22f, 0.11f, 0.12f, 0.92f));
            DrawTintedRect(ScaleTopRect(rect, 720f, 0f, 440f, 154f), new Color(0.13f, 0.15f, 0.19f, 0.98f));
            DrawTintedRect(ScaleTopRect(rect, 720f, 0f, 88f, 154f), new Color(0.10f, 0.30f, 0.72f, 0.95f));
            DrawTintedRect(ScaleTopRect(rect, 1072f, 0f, 88f, 154f), new Color(0.72f, 0.12f, 0.14f, 0.95f));
            DrawTintedRect(ScaleTopRect(rect, 732f, 124f, 416f, 30f), new Color(0.05f, 0.06f, 0.08f, 0.90f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.9f), Mathf.Max(1f, rect.width / TopScoreboardDesignWidth * 3f));
            DrawOutline(ScaleTopRect(rect, 720f, 0f, 440f, 154f), new Color(0f, 0f, 0f, 0.86f), Mathf.Max(1f, rect.width / TopScoreboardDesignWidth * 2f));
        }

        private void DrawBanDots(Rect hudRect, float groupCenterX, float y, int filledCount, Color activeColor)
        {
            if (topScoreboardDotTexture == null)
            {
                return;
            }

            const float dotSize = 18f;
            const float dotSpacing = 28f;
            var startX = groupCenterX - ((dotSize + (dotSpacing * (MaxBanDots - 1))) * 0.5f);
            for (var index = 0; index < MaxBanDots; index++)
            {
                var dotRect = ScaleTopRect(hudRect, startX + (index * dotSpacing), y, dotSize, dotSize);
                DrawTintedTexture(new Rect(dotRect.x - 1f, dotRect.y - 1f, dotRect.width + 2f, dotRect.height + 2f), topScoreboardDotTexture, DotOutlineColor);
                DrawTintedTexture(dotRect, topScoreboardDotTexture, index < filledCount ? activeColor : DotInactiveColor);
            }
        }

        private static int CountPicked(IReadOnlyList<HeroDefinition> heroes)
        {
            if (heroes == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static Rect ScaleTopRect(Rect parent, float x, float y, float width, float height)
        {
            var scale = parent.width / TopScoreboardDesignWidth;
            return PixelSnap(new Rect(
                parent.x + (x * scale),
                parent.y + (y * scale),
                width * scale,
                height * scale));
        }

        private void DrawTeamPanelBackground(Rect rect, Color accentColor)
        {
            DrawTintedRect(rect, new Color(0.075f, 0.083f, 0.095f, 0.98f));
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.84f));
            DrawTintedRect(new Rect(rect.x, rect.y, 4f, rect.height), accentColor);
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.86f), 2f);
        }

        private void DrawScreenBackdrop(Rect outerRect, Rect leftRect, Rect rightRect, Rect centerRect, Rect bottomRect)
        {
            DrawTintedRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.14f, 0.14f, 0.14f, 1f));
            DrawTintedRect(outerRect, new Color(0.046f, 0.049f, 0.055f, 1f));
            DrawTintedRect(new Rect(outerRect.x + 4f, outerRect.y + 4f, outerRect.width - 8f, outerRect.height - 8f), new Color(0.072f, 0.078f, 0.086f, 1f));

            var centerBand = new Rect(
                centerRect.x - 8f,
                centerRect.y - 8f,
                centerRect.width + 16f,
                bottomRect.yMax - centerRect.y + 8f);
            DrawTintedRect(centerBand, new Color(0.052f, 0.057f, 0.065f, 1f));
            DrawTintedRect(new Rect(leftRect.x - 5f, leftRect.y, 3f, leftRect.height), new Color(0.10f, 0.34f, 0.86f, 0.62f));
            DrawTintedRect(new Rect(rightRect.xMax + 2f, rightRect.y, 3f, rightRect.height), new Color(0.86f, 0.16f, 0.18f, 0.62f));
            DrawOutline(outerRect, new Color(0f, 0f, 0f, 0.82f), 2f);
            DrawOutline(new Rect(outerRect.x + 4f, outerRect.y + 4f, outerRect.width - 8f, outerRect.height - 8f), new Color(1f, 1f, 1f, 0.08f), 1f);
        }

        private void DrawPhaseBanner(Rect rect)
        {
            var step = GameFlowState.CurrentDraftStep;
            var color = step == null
                ? new Color(0.22f, 0.28f, 0.32f, 0.95f)
                : step.Side == TeamSide.Blue
                    ? new Color(0.12f, 0.34f, 0.86f, 0.95f)
                    : new Color(0.9f, 0.18f, 0.18f, 0.95f);

            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            DrawLabel(new Rect(rect.x + 20f, rect.y + 8f, rect.width - 40f, 34f), GetPhaseLabel(), titleStyle);

            if (!string.IsNullOrEmpty(draftNotice) && Time.realtimeSinceStartup < draftNoticeUntil)
            {
                DrawLabel(new Rect(rect.x + 20f, rect.y + 30f, rect.width - 40f, 18f), draftNotice, smallBodyStyle);
            }
        }

        private void DrawTeamPanel(Rect rect, TeamSide side, Color accentColor)
        {
            var selection = side == TeamSide.Red ? GameFlowState.RedSelection : GameFlowState.BlueSelection;
            var athletes = side == TeamSide.Red ? GameFlowState.RedAthletes : GameFlowState.BlueAthletes;
            var currentStep = GameFlowState.CurrentDraftStep;
            var isSwapPhase = GameFlowState.IsDraftComplete;
            var isTeamReady = GameFlowState.IsTeamExchangeReady(side);
            DrawTeamPanelBackground(rect, accentColor);

            var slotGap = 6f;
            var headerBlockHeight = 8f;
            var strategyBlockHeight = isSwapPhase ? 92f : 62f;
            var availableSlotHeight = rect.height - headerBlockHeight - strategyBlockHeight - (slotGap * (BattleInputConfig.DefaultTeamSize - 1));
            var slotHeight = Mathf.Clamp(availableSlotHeight / BattleInputConfig.DefaultTeamSize, 92f, 190f);
            for (var i = 0; i < BattleInputConfig.DefaultTeamSize; i++)
            {
                var slotRect = new Rect(rect.x + 10f, rect.y + headerBlockHeight + (i * (slotHeight + slotGap)), rect.width - 20f, slotHeight);
                var hero = selection.Count > i ? selection[i] : null;
                var athlete = athletes.Count > i ? athletes[i] : null;
                var isCurrent = currentStep != null
                    && currentStep.ActionType == BattleDraftActionType.Pick
                    && currentStep.Side == side
                    && currentStep.SlotIndex == i;
                var isSwapSource = swapSourceSide.HasValue && swapSourceSide.Value == side && swapSourceIndex == i;
                DrawTeamSlot(slotRect, i, hero, athlete, isCurrent, isSwapSource, isTeamReady, accentColor);
                if (isSwapPhase && !isTeamReady && hero != null && DrawButton(slotRect, GUIContent.none, heroCardButtonStyle))
                {
                    HandleSwapSlotClicked(side, i);
                }
            }

            var strategyY = rect.yMax - strategyBlockHeight;
            if (isSwapPhase)
            {
                DrawExchangeReadyControls(new Rect(rect.x + 10f, strategyY, rect.width - 20f, 26f), side);
                strategyY += 32f;
            }

            DrawStrategySelector(
                new Rect(rect.x + 10f, strategyY, rect.width - 20f, 24f),
                "Ult Timing",
                GetUltimateTimingLabel(GameFlowState.GetUltimateTimingStrategy(side)),
                () => CycleUltimateTimingStrategy(side, -1),
                () => CycleUltimateTimingStrategy(side, 1));

            DrawStrategySelector(
                new Rect(rect.x + 10f, strategyY + 30f, rect.width - 20f, 24f),
                "Ult Combo",
                GetUltimateComboLabel(GameFlowState.GetUltimateComboStrategy(side)),
                () => CycleUltimateComboStrategy(side, -1),
                () => CycleUltimateComboStrategy(side, 1));
        }

        private void DrawTeamSlot(Rect rect, int index, HeroDefinition hero, AthleteDefinition athlete, bool isCurrent, bool isSwapSource, bool isTeamReady, Color accentColor)
        {
            var previousColor = GUI.color;
            var slotAccent = isSwapSource ? new Color(0.26f, 0.78f, 0.42f, 1f) : accentColor;
            var isFocused = isCurrent || isSwapSource;
            DrawTintedRect(rect, new Color(0.055f, 0.06f, 0.07f, 0.96f));
            GUI.color = Color.Lerp(Color.white, slotAccent, isFocused ? 0.88f : isTeamReady ? 0.48f : 0.28f);
            DrawBox(rect, string.Empty, isFocused ? focusedSlotStyle : slotStyle);
            if (isFocused || isTeamReady)
            {
                GUI.color = new Color(slotAccent.r, slotAccent.g, slotAccent.b, isTeamReady && !isFocused ? 0.2f : 0.38f);
                GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.14f);
                GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 3f), Texture2D.whiteTexture);
            }

            GUI.color = previousColor;

            var compact = rect.height < 110f;
            var padding = compact ? 5f : 7f;
            var gap = compact ? 5f : 6f;
            var headerHeight = compact ? 17f : 20f;
            var statusSize = compact ? 17f : 20f;
            var statHeight = compact ? 18f : 22f;
            var masteryIconSize = compact ? 28f : 42f;
            var masteryY = rect.yMax - padding - masteryIconSize;
            var statY = masteryY - statHeight - 4f;
            var contentY = rect.y + headerHeight + padding;
            var topContentHeight = Mathf.Max(18f, statY - contentY - 4f);
            var maxPortraitSize = compact ? 40f : 58f;
            var heroPortraitSize = Mathf.Max(26f, Mathf.Min(Mathf.Min(topContentHeight, rect.width * 0.23f), maxPortraitSize));

            var heroRect = new Rect(rect.x + padding, contentY, heroPortraitSize, heroPortraitSize);
            if (hero != null)
            {
                DrawHeroPortrait(heroRect, hero);
            }
            else
            {
                DrawAthletePortrait(heroRect, athlete, accentColor);
            }

            var infoX = heroRect.xMax + gap;
            var infoWidth = rect.xMax - padding - infoX;
            var nameRect = new Rect(rect.x + padding, rect.y + 4f, rect.width - (padding * 2f) - statusSize - 4f, headerHeight);
            DrawLabel(nameRect, $"{index + 1}. {GetAthleteDisplayName(athlete)}", bodyStyle);
            DrawAthleteConditionArrow(new Rect(rect.xMax - padding - statusSize, rect.y + 5f, statusSize, statusSize), athlete);

            var traitRect = new Rect(infoX, contentY, infoWidth, topContentHeight);
            if (traitRect.height >= 18f)
            {
                DrawAthleteTraitRows(traitRect, athlete);
            }

            var masteryBonus = GetSelectedHeroMastery(athlete, hero);
            var statWidth = (rect.width - (padding * 2f) - 6f) * 0.5f;
            DrawStatChip(new Rect(rect.x + padding, statY, statWidth, statHeight), true, athlete != null ? athlete.attack : 0f, masteryBonus);
            DrawStatChip(new Rect(rect.x + padding + statWidth + 6f, statY, statWidth, statHeight), false, athlete != null ? athlete.defense : 0f, masteryBonus);

            DrawAthleteMasteryIcons(new Rect(rect.x + padding, masteryY, rect.width - (padding * 2f), masteryIconSize), athlete, hero);
        }

        private void DrawCenterPanel(Rect rect)
        {
            DrawTintedRect(rect, new Color(0.058f, 0.064f, 0.073f, 0.98f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.74f), 2f);
            var detailHeight = Mathf.Min(Mathf.Clamp(rect.height * 0.45f, 324f, 380f), rect.height - 262f);
            var catalogHeight = Mathf.Max(240f, rect.height - detailHeight - 22f);
            DrawCatalogPanel(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, catalogHeight - 12f));
            DrawHeroDetailPanel(new Rect(rect.x + 12f, rect.y + catalogHeight + 10f, rect.width - 24f, rect.height - catalogHeight - 22f));
        }

        private void DrawCatalogPanel(Rect rect)
        {
            DrawTintedRect(rect, new Color(0.075f, 0.082f, 0.092f, 0.98f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.70f), 1f);
            DrawClassFilters(new Rect(rect.x + 12f, rect.y + 18f, rect.width - 24f, 30f));

            var visibleRect = new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, rect.height - 68f);
            var cardWidth = 108f;
            var cardHeight = 130f;
            var spacing = 8f;
            var columns = Mathf.Max(1, Mathf.FloorToInt((visibleRect.width + spacing) / (cardWidth + spacing)));
            var visibleHeroes = BuildVisibleHeroList();
            var rows = Mathf.CeilToInt(visibleHeroes.Count / (float)columns);
            var contentHeight = Mathf.Max(visibleRect.height, rows * (cardHeight + spacing));
            var viewRect = new Rect(0f, 0f, visibleRect.width - 18f, contentHeight);

            catalogScroll = GUI.BeginScrollView(visibleRect, catalogScroll, viewRect);

            for (var i = 0; i < visibleHeroes.Count; i++)
            {
                var row = i / columns;
                var column = i % columns;
                var cardRect = new Rect(column * (cardWidth + spacing), row * (cardHeight + spacing), cardWidth, cardHeight);
                DrawHeroCard(cardRect, visibleHeroes[i]);
            }

            GUI.EndScrollView();
        }

        private void DrawClassFilters(Rect rect)
        {
            var labels = new[] { "All", "Warrior", "Mage", "Assassin", "Tank", "Support", "Marksman" };
            var width = rect.width / labels.Length;
            for (var i = 0; i < labels.Length; i++)
            {
                var buttonRect = new Rect(rect.x + (i * width) + 2f, rect.y, width - 4f, rect.height);
                var selected = i == 0 ? !classFilterActive : classFilterActive && (int)classFilter == i - 1;
                var previousColor = GUI.color;
                GUI.color = selected ? new Color(0.72f, 0.86f, 1f) : Color.white;
                if (DrawButton(buttonRect, labels[i], filterButtonStyle))
                {
                    classFilterActive = i != 0;
                    if (classFilterActive)
                    {
                        classFilter = (HeroClass)(i - 1);
                    }
                }

                GUI.color = previousColor;
            }
        }

        private void DrawHeroCard(Rect rect, HeroDefinition hero)
        {
            var isBanned = GameFlowState.IsHeroBanned(hero);
            var isPicked = GameFlowState.IsHeroPicked(hero);
            var isHighlighted = hero == highlightedHero;
            var baseColor = GetClassColor(hero.heroClass);
            var previousColor = GUI.color;
            DrawTintedRect(rect, new Color(0.09f, 0.10f, 0.115f, 0.98f));
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, 4f), new Color(baseColor.r, baseColor.g, baseColor.b, isHighlighted ? 0.95f : 0.56f));
            DrawOutline(rect, new Color(baseColor.r, baseColor.g, baseColor.b, isHighlighted ? 0.90f : 0.36f), isHighlighted ? 2f : 1f);
            GUI.color = previousColor;

            var portraitRect = new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 68f);
            DrawHeroPortrait(portraitRect, hero);

            DrawLabel(new Rect(rect.x + 6f, rect.y + 82f, rect.width - 12f, 20f), hero.displayName, smallBodyStyle);
            DrawLabel(new Rect(rect.x + 6f, rect.y + 102f, rect.width - 12f, 18f), GetHeroClassLabel(hero.heroClass), smallBodyStyle);

            if (isBanned || isPicked)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.58f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                DrawLabel(new Rect(rect.x + 4f, rect.y + 48f, rect.width - 8f, 24f), isBanned ? "BANNED" : "PICKED", sectionStyle);
            }

            if (DrawButton(rect, GUIContent.none, heroCardButtonStyle))
            {
                highlightedHero = hero;
            }
        }

        private void DrawHeroDetailPanel(Rect rect)
        {
            DrawTintedRect(rect, new Color(0.07f, 0.077f, 0.087f, 0.98f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.70f), 1f);
            DrawLabel(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 26f), "Hero Detail", sectionStyle);

            if (highlightedHero == null)
            {
                DrawLabel(new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 30f), "Select a hero from the pool.", bodyStyle);
                return;
            }

            var hero = highlightedHero;
            var portraitRect = new Rect(rect.x + 16f, rect.y + 46f, 96f, 96f);
            DrawHeroPortrait(portraitRect, hero);
            DrawLabel(new Rect(portraitRect.xMax + 16f, rect.y + 42f, rect.width - portraitRect.width - 220f, 28f), hero.displayName, detailHeaderStyle);
            DrawLabel(new Rect(portraitRect.xMax + 16f, rect.y + 74f, rect.width - portraitRect.width - 220f, 22f), $"{GetHeroClassLabel(hero.heroClass)} | {BuildHeroTagLine(hero.tags)}", bodyStyle);
            DrawLabel(new Rect(portraitRect.xMax + 16f, rect.y + 102f, rect.width - portraitRect.width - 220f, 44f), BuildStatsLine(hero), smallBodyStyle);

            var confirmRect = new Rect(rect.xMax - 170f, rect.y + 56f, 150f, 36f);
            var canConfirm = GameFlowState.CanDraftHero(hero);
            GUI.enabled = canConfirm;
            if (DrawButton(confirmRect, GetConfirmButtonLabel()))
            {
                if (GameFlowState.TryApplyDraftHero(hero))
                {
                    SetDraftNotice($"{hero.displayName} confirmed.");
                }
            }

            GUI.enabled = true;
            DrawLabel(new Rect(rect.xMax - 188f, rect.y + 98f, 170f, 42f), GetHeroAvailabilityLabel(hero), smallBodyStyle);
            DrawLabel(new Rect(rect.xMax - 188f, rect.y + 142f, 170f, 76f), BuildCurrentPickAthleteFitLine(hero), smallBodyStyle);

            var description = !string.IsNullOrWhiteSpace(hero.description)
                ? hero.description
                : "No hero description.";
            var descriptionWidth = Mathf.Max(120f, rect.width - 220f);
            DrawLabel(new Rect(rect.x + 16f, rect.y + 150f, descriptionWidth, 64f), ClampText(description, 180), smallBodyStyle);

            var skillY = rect.y + 224f;
            var skillHeight = Mathf.Max(42f, (rect.yMax - skillY - 12f) / 2f - 4f);
            DrawSkillSummary(new Rect(rect.x + 16f, skillY, rect.width - 32f, skillHeight), "Skill", hero.activeSkill);
            DrawSkillSummary(new Rect(rect.x + 16f, skillY + skillHeight + 8f, rect.width - 32f, skillHeight), "Ultimate", hero.ultimateSkill);
        }

        private void DrawSkillSummary(Rect rect, string label, SkillData skill)
        {
            DrawBox(rect, string.Empty);
            var name = skill != null ? skill.displayName : "None";
            var description = skill != null && !string.IsNullOrWhiteSpace(skill.description)
                ? skill.description
                : "No description.";
            DrawLabel(new Rect(rect.x + 10f, rect.y + 6f, 140f, 22f), $"{label}: {name}", bodyStyle);
            DrawLabel(new Rect(rect.x + 160f, rect.y + 6f, rect.width - 170f, rect.height - 12f), ClampText(description, 160), smallBodyStyle);
        }

        private void DrawBanPanel(Rect rect)
        {
            DrawTintedRect(rect, new Color(0.062f, 0.067f, 0.076f, 0.99f));
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(1f, 1f, 1f, 0.08f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.82f), 2f);
            DrawLabel(new Rect(rect.x + rect.width * 0.43f, rect.y + 8f, rect.width * 0.14f, 26f), "BANNED", sectionStyle);
            DrawFooterControls(rect);

            DrawBanSlots(new Rect(rect.x + 16f, rect.y + 16f, rect.width * 0.36f, rect.height - 32f), TeamSide.Blue, GameFlowState.BlueBans, new Color(0.12f, 0.34f, 0.86f, 0.9f));
            DrawBanSlots(new Rect(rect.xMax - 16f - (rect.width * 0.36f), rect.y + 16f, rect.width * 0.36f, rect.height - 32f), TeamSide.Red, GameFlowState.RedBans, new Color(0.9f, 0.18f, 0.18f, 0.9f));
        }

        private void DrawFooterControls(Rect rect)
        {
            var controlWidth = Mathf.Clamp(rect.width * 0.22f, 270f, 360f);
            var controlX = rect.center.x - (controlWidth * 0.5f);
            var buttonGap = 8f;
            var halfButtonWidth = (controlWidth - buttonGap) * 0.5f;
            var smallButtonHeight = 26f;
            var startButtonHeight = 30f;
            var firstRowY = rect.y + 38f;
            var secondRowY = Mathf.Max(firstRowY + smallButtonHeight + 6f, rect.yMax - startButtonHeight - 10f);

            DrawTintedRect(new Rect(controlX - 8f, firstRowY - 8f, controlWidth + 16f, rect.yMax - firstRowY - 2f), new Color(0.035f, 0.038f, 0.044f, 0.76f));
            DrawOutline(new Rect(controlX - 8f, firstRowY - 8f, controlWidth + 16f, rect.yMax - firstRowY - 2f), new Color(1f, 1f, 1f, 0.08f), 1f);

            if (DrawButton(new Rect(controlX, firstRowY, halfButtonWidth, smallButtonHeight), "Back", topButtonStyle))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }

            if (DrawButton(new Rect(controlX + halfButtonWidth + buttonGap, firstRowY, halfButtonWidth, smallButtonHeight), "Reset BP", topButtonStyle))
            {
                GameFlowState.ResetDraft();
                ClearSwapSelection();
                highlightedHero = FindFirstCatalogHero();
                catalogScroll = Vector2.zero;
                SetDraftNotice("BP has been reset.");
            }

            var previousEnabled = GUI.enabled;
            var canStart = GameFlowState.IsDraftComplete && GameFlowState.CanPrepareBattleInput();
            GUI.enabled = canStart;
            var startButtonLabel = GameFlowState.IsDraftComplete && !GameFlowState.AreBothTeamsExchangeReady
                ? "Waiting Ready"
                : "Start Battle";
            if (DrawButton(new Rect(controlX, secondRowY, controlWidth, startButtonHeight), startButtonLabel, topButtonStyle))
            {
                if (GameFlowState.TryPrepareBattleInput(out _))
                {
                    SceneManager.LoadScene(battleSceneName);
                }
            }

            GUI.enabled = previousEnabled;
        }

        private void DrawBanSlots(Rect rect, TeamSide side, IReadOnlyList<HeroDefinition> bans, Color accentColor)
        {
            var currentStep = GameFlowState.CurrentDraftStep;
            var slotSpacing = 10f;
            var slotWidth = (rect.width - ((GameFlowState.DraftBansPerSide - 1) * slotSpacing)) / GameFlowState.DraftBansPerSide;
            for (var i = 0; i < GameFlowState.DraftBansPerSide; i++)
            {
                var slotRect = new Rect(rect.x + i * (slotWidth + slotSpacing), rect.y, slotWidth, rect.height);
                var isCurrent = currentStep != null
                    && currentStep.ActionType == BattleDraftActionType.Ban
                    && currentStep.Side == side
                    && currentStep.SlotIndex == i;
                var hero = bans.Count > i ? bans[i] : null;

                var previousColor = GUI.color;
                GUI.color = Color.Lerp(Color.white, accentColor, isCurrent ? 0.68f : 0.32f);
                DrawTintedRect(slotRect, new Color(0.09f, 0.10f, 0.12f, 0.96f));
                DrawOutline(slotRect, new Color(accentColor.r, accentColor.g, accentColor.b, isCurrent ? 0.86f : 0.34f), isCurrent ? 2f : 1f);
                GUI.color = previousColor;

                var portraitSize = Mathf.Clamp(slotRect.height - 22f, 48f, 66f);
                var portraitRect = new Rect(slotRect.x + 9f, slotRect.y + (slotRect.height - portraitSize) * 0.5f, portraitSize, portraitSize);
                DrawHeroPortrait(portraitRect, hero);
                DrawLabel(new Rect(portraitRect.xMax + 8f, slotRect.y + 14f, slotRect.width - portraitSize - 25f, 22f), hero != null ? hero.displayName : $"{side} Ban {i + 1}", bodyStyle);
                DrawLabel(new Rect(portraitRect.xMax + 8f, slotRect.y + 40f, slotRect.width - portraitSize - 25f, 18f), hero != null ? GetHeroClassLabel(hero.heroClass) : "Waiting", smallBodyStyle);
            }
        }

        private void DrawExchangeReadyControls(Rect rect, TeamSide side)
        {
            var ready = GameFlowState.IsTeamExchangeReady(side);
            var statusText = ready ? "Ready locked" : "Swap heroes";
            DrawLabel(new Rect(rect.x, rect.y, 96f, rect.height), statusText, smallBodyStyle);

            var previousEnabled = GUI.enabled;
            GUI.enabled = GameFlowState.HasValidSelections();
            var buttonLabel = ready ? "Edit" : "Ready";
            if (DrawButton(new Rect(rect.xMax - 78f, rect.y, 78f, rect.height), buttonLabel, strategyButtonStyle))
            {
                if (GameFlowState.TrySetTeamExchangeReady(side, !ready))
                {
                    ClearSwapSelection(side);
                    SetDraftNotice(ready
                        ? $"{GetTeamLabel(side)} can swap heroes again."
                        : $"{GetTeamLabel(side)} is ready.");
                }
            }

            GUI.enabled = previousEnabled;

            var hint = ready
                ? "Locked until Edit"
                : "Click two slots";
            DrawBox(new Rect(rect.x + 100f, rect.y, rect.width - 184f, rect.height), hint);
        }

        private void DrawStrategySelector(Rect rect, string label, string value, System.Action previous, System.Action next)
        {
            var buttonWidth = 26f;
            var labelWidth = 88f;
            DrawLabel(new Rect(rect.x, rect.y, labelWidth, rect.height), label, smallBodyStyle);

            if (DrawButton(new Rect(rect.x + labelWidth, rect.y, buttonWidth, rect.height), "<", strategyButtonStyle))
            {
                previous?.Invoke();
            }

            DrawBox(new Rect(rect.x + labelWidth + buttonWidth + 6f, rect.y, rect.width - labelWidth - (buttonWidth * 2f) - 12f, rect.height), value);

            if (DrawButton(new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height), ">", strategyButtonStyle))
            {
                next?.Invoke();
            }
        }

        private List<HeroDefinition> BuildVisibleHeroList()
        {
            var heroes = new List<HeroDefinition>();
            var catalog = GameFlowState.HeroCatalog;
            for (var i = 0; i < catalog.Count; i++)
            {
                var hero = catalog[i];
                if (hero == null)
                {
                    continue;
                }

                if (classFilterActive && hero.heroClass != classFilter)
                {
                    continue;
                }

                heroes.Add(hero);
            }

            return heroes;
        }

        private HeroDefinition FindFirstCatalogHero()
        {
            var catalog = GameFlowState.HeroCatalog;
            for (var i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null)
                {
                    return catalog[i];
                }
            }

            return null;
        }

        private void HandleSwapSlotClicked(TeamSide side, int slotIndex)
        {
            if (!GameFlowState.IsDraftComplete || GameFlowState.IsTeamExchangeReady(side))
            {
                return;
            }

            if (!swapSourceSide.HasValue || swapSourceSide.Value != side)
            {
                swapSourceSide = side;
                swapSourceIndex = slotIndex;
                SetDraftNotice($"{GetTeamLabel(side)} selected slot {slotIndex + 1}. Pick another slot to swap.");
                return;
            }

            if (swapSourceIndex == slotIndex)
            {
                ClearSwapSelection();
                SetDraftNotice("Swap selection cleared.");
                return;
            }

            var firstIndex = swapSourceIndex;
            if (GameFlowState.TrySwapDraftHeroes(side, firstIndex, slotIndex))
            {
                ClearSwapSelection();
                SetDraftNotice($"{GetTeamLabel(side)} swapped slots {firstIndex + 1} and {slotIndex + 1}.");
                return;
            }

            SetDraftNotice("Cannot swap these slots.");
        }

        private void ClearSwapSelection()
        {
            swapSourceSide = null;
            swapSourceIndex = -1;
        }

        private void ClearSwapSelection(TeamSide side)
        {
            if (swapSourceSide.HasValue && swapSourceSide.Value == side)
            {
                ClearSwapSelection();
            }
        }

        private string GetPhaseLabel()
        {
            var step = GameFlowState.CurrentDraftStep;
            if (step == null)
            {
                if (GameFlowState.AreBothTeamsExchangeReady)
                {
                    return "Both Teams Ready - Start Battle";
                }

                var blueState = GameFlowState.IsTeamExchangeReady(TeamSide.Blue) ? "Blue Ready" : "Blue Swapping";
                var redState = GameFlowState.IsTeamExchangeReady(TeamSide.Red) ? "Red Ready" : "Red Swapping";
                return $"Hero Swap Phase - {blueState} / {redState}";
            }

            var side = step.Side == TeamSide.Blue ? "Blue" : "Red";
            var action = step.ActionType == BattleDraftActionType.Ban ? "Ban" : "Pick";
            var total = step.ActionType == BattleDraftActionType.Ban ? GameFlowState.DraftBansPerSide : BattleInputConfig.DefaultTeamSize;
            return $"{side} {action} {step.SlotIndex + 1}/{total}";
        }

        private static string GetTeamLabel(TeamSide side)
        {
            return side == TeamSide.Red ? "Red" : "Blue";
        }

        private string GetConfirmButtonLabel()
        {
            var step = GameFlowState.CurrentDraftStep;
            if (step == null)
            {
                return "Swap Phase";
            }

            return step.ActionType == BattleDraftActionType.Ban ? "Confirm Ban" : "Confirm Pick";
        }

        private string GetHeroAvailabilityLabel(HeroDefinition hero)
        {
            if (hero == null)
            {
                return string.Empty;
            }

            var bannedSide = GameFlowState.GetHeroBannedSide(hero);
            if (bannedSide.HasValue)
            {
                return $"Already banned by {bannedSide.Value}.";
            }

            var pickedSide = GameFlowState.GetHeroPickedSide(hero);
            if (pickedSide.HasValue)
            {
                return $"Already picked by {pickedSide.Value}.";
            }

            return GameFlowState.IsDraftComplete ? "Swap phase: use team slots." : "Available.";
        }

        private static string BuildHeroTagLine(IReadOnlyList<HeroTag> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return "No tags";
            }

            var parts = new string[tags.Count];
            for (var i = 0; i < tags.Count; i++)
            {
                parts[i] = tags[i].ToString();
            }

            return string.Join(" / ", parts);
        }

        private static string BuildStatsLine(HeroDefinition hero)
        {
            if (hero == null || hero.baseStats == null)
            {
                return "No stats.";
            }

            var stats = hero.baseStats;
            var range = hero.basicAttack != null && hero.basicAttack.rangeOverride > 0f
                ? hero.basicAttack.rangeOverride
                : stats.attackRange;
            return $"HP {stats.maxHealth:0}   ATK {stats.attackPower:0}   DEF {stats.defense:0}\nAS {stats.attackSpeed:0.00}   Range {range:0.0}   Move {stats.moveSpeed:0.0}";
        }

        private static string GetAthleteDisplayName(AthleteDefinition athlete)
        {
            return athlete != null && !string.IsNullOrWhiteSpace(athlete.displayName)
                ? athlete.displayName
                : "No athlete";
        }

        private void DrawAthleteConditionArrow(Rect rect, AthleteDefinition athlete)
        {
            var condition = athlete != null ? athlete.condition : 0f;
            var sprite = condition > 20f
                ? playerStatusGoodArrowSprite
                : condition < 0f
                    ? playerStatusBadArrowSprite
                    : playerStatusNormalArrowSprite;

            if (sprite != null)
            {
                DrawSprite(rect, sprite);
                return;
            }

            DrawBox(rect, condition > 20f ? "+" : condition < 0f ? "-" : "=");
        }

        private void DrawStatChip(Rect rect, bool isAttack, float value, float masteryBonus)
        {
            DrawTintedRect(rect, new Color(0.018f, 0.021f, 0.027f, 0.96f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.72f), 1f);
            var accent = isAttack
                ? new Color(1f, 0.58f, 0.16f, 1f)
                : new Color(0.34f, 0.64f, 1f, 1f);
            var iconBadgeRect = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Min(24f, rect.height + 4f), rect.height - 4f);
            DrawTintedRect(iconBadgeRect, new Color(accent.r, accent.g, accent.b, 0.26f));
            DrawOutline(iconBadgeRect, new Color(accent.r, accent.g, accent.b, 0.90f), 1f);
            var iconRect = new Rect(iconBadgeRect.x + 2f, iconBadgeRect.y + 1f, iconBadgeRect.width - 4f, iconBadgeRect.height - 2f);
            if (isAttack)
            {
                DrawAttackIcon(iconRect);
            }
            else
            {
                DrawDefenseIcon(iconRect);
            }

            var valueText = masteryBonus > 0f
                ? $"{value:0} (+{masteryBonus:0})"
                : value.ToString("0");
            DrawLabel(new Rect(iconBadgeRect.xMax + 2f, rect.y, rect.xMax - iconBadgeRect.xMax - 3f, rect.height), valueText, smallBodyStyle);
        }

        private static void DrawAttackIcon(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.06f, 0.03f, 0.86f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.43f, rect.y + rect.height * 0.06f, rect.width * 0.22f, rect.height * 0.56f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.96f, 0.78f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.37f, rect.y + rect.height * 0.05f, rect.width * 0.26f, rect.height * 0.54f), Texture2D.whiteTexture);
            GUI.color = new Color(0.70f, 0.80f, 0.96f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.48f, rect.y + rect.height * 0.05f, rect.width * 0.10f, rect.height * 0.54f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.64f, 0.18f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.20f, rect.y + rect.height * 0.60f, rect.width * 0.62f, rect.height * 0.16f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.41f, rect.y + rect.height * 0.70f, rect.width * 0.20f, rect.height * 0.24f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void DrawDefenseIcon(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.07f, 0.10f, 0.18f, 0.88f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.08f, rect.width * 0.70f, rect.height * 0.68f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.28f, rect.y + rect.height * 0.70f, rect.width * 0.46f, rect.height * 0.20f), Texture2D.whiteTexture);
            GUI.color = new Color(0.86f, 0.94f, 1f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.10f, rect.width * 0.58f, rect.height * 0.12f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.22f, rect.width * 0.12f, rect.height * 0.46f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.68f, rect.y + rect.height * 0.22f, rect.width * 0.12f, rect.height * 0.46f), Texture2D.whiteTexture);
            GUI.color = new Color(0.34f, 0.68f, 1f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.34f, rect.y + rect.height * 0.24f, rect.width * 0.34f, rect.height * 0.48f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawAthleteTraitRows(Rect rect, AthleteDefinition athlete)
        {
            var gap = rect.height < 34f ? 2f : 4f;
            var rowHeight = Mathf.Max(5f, (rect.height - (gap * (AthleteTraitSlotCount - 1))) / AthleteTraitSlotCount);
            for (var i = 0; i < AthleteTraitSlotCount; i++)
            {
                var rowRect = new Rect(rect.x, rect.y + (i * (rowHeight + gap)), rect.width, rowHeight);
                DrawTintedRect(rowRect, new Color(0.018f, 0.021f, 0.027f, 0.82f));
                DrawOutline(rowRect, new Color(0f, 0f, 0f, 0.60f), 1f);
                var traitLabel = GetAthleteTraitDisplayName(athlete, i);
                if (!string.IsNullOrWhiteSpace(traitLabel))
                {
                    DrawLabel(rowRect, traitLabel, smallBodyStyle);
                }
            }
        }

        private void DrawAthleteMasteryIcons(Rect rect, AthleteDefinition athlete, HeroDefinition hero)
        {
            if (athlete?.heroMasteries == null || athlete.heroMasteries.Count == 0)
            {
                return;
            }

            var maxCount = Mathf.Min(athlete.heroMasteries.Count, 4);
            var spacing = 5f;
            var iconSize = Mathf.Min(rect.height, (rect.width - (spacing * (maxCount - 1))) / maxCount);

            for (var i = 0; i < maxCount; i++)
            {
                var mastery = athlete.heroMasteries[i];
                if (mastery == null)
                {
                    continue;
                }

                var iconRect = new Rect(rect.x + (i * (iconSize + spacing)), rect.y, iconSize, iconSize);
                var masteryHero = FindHeroById(mastery.heroId);
                var isSelectedHero = hero != null && IsSameHero(hero, masteryHero);
                var previousColor = GUI.color;
                if (isSelectedHero)
                {
                    GUI.color = new Color(0.2f, 0.66f, 0.34f, 0.95f);
                    GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    DrawHeroPortraitTopHalf(new Rect(iconRect.x + 3f, iconRect.y + 3f, iconRect.width - 6f, iconRect.height - 6f), masteryHero);
                }
                else
                {
                    GUI.color = Color.white;
                    DrawHeroPortraitTopHalf(iconRect, masteryHero);
                }

                GUI.color = previousColor;

                if (isSelectedHero)
                {
                    GUI.color = new Color(0.64f, 1f, 0.64f, 0.85f);
                    GUI.DrawTexture(new Rect(iconRect.x - 1f, iconRect.y - 1f, iconRect.width + 2f, 2f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(iconRect.x - 1f, iconRect.yMax - 1f, iconRect.width + 2f, 2f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(iconRect.x - 1f, iconRect.y - 1f, 2f, iconRect.height + 2f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(iconRect.xMax - 1f, iconRect.y - 1f, 2f, iconRect.height + 2f), Texture2D.whiteTexture);
                    GUI.color = previousColor;
                }

                var valueRect = new Rect(iconRect.xMax - 24f, iconRect.yMax - 16f, 24f, 16f);
                GUI.color = new Color(0f, 0f, 0f, 0.68f);
                GUI.DrawTexture(valueRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
                DrawLabel(valueRect, mastery.mastery.ToString("0"), smallBodyStyle);
            }
        }

        private static float GetSelectedHeroMastery(AthleteDefinition athlete, HeroDefinition hero)
        {
            return athlete != null && hero != null && athlete.TryGetMastery(GetHeroIdentity(hero), out var mastery)
                ? mastery
                : 0f;
        }

        private static HeroDefinition FindHeroById(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return null;
            }

            var catalog = GameFlowState.HeroCatalog;
            for (var i = 0; i < catalog.Count; i++)
            {
                var hero = catalog[i];
                if (hero != null && string.Equals(GetHeroIdentity(hero), heroId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return hero;
                }
            }

            return null;
        }

        private static bool IsSameHero(HeroDefinition left, HeroDefinition right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return string.Equals(GetHeroIdentity(left), GetHeroIdentity(right), System.StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHeroIdentity(HeroDefinition hero)
        {
            if (hero == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(hero.heroId)
                ? hero.name
                : hero.heroId;
        }

        private static string BuildAthleteSummary(AthleteDefinition athlete)
        {
            if (athlete == null)
            {
                return "No athlete";
            }

            var displayName = !string.IsNullOrWhiteSpace(athlete.displayName)
                ? athlete.displayName
                : "Athlete";
            var traitSummary = BuildAthleteTraitSummary(athlete);
            return string.IsNullOrWhiteSpace(traitSummary)
                ? $"{displayName}  A{athlete.attack:0} D{athlete.defense:0} C{athlete.condition:+0;-0;0}"
                : $"{displayName}  A{athlete.attack:0} D{athlete.defense:0} C{athlete.condition:+0;-0;0}  {traitSummary}";
        }

        private static string BuildAthleteTraitSummary(AthleteDefinition athlete)
        {
            return AthleteTraitCatalog.BuildDisplayNameSummary(athlete, 2);
        }

        private static string BuildAthleteTraitDescriptionSummary(AthleteDefinition athlete)
        {
            return AthleteTraitCatalog.BuildDescriptionSummary(athlete, 2);
        }

        private static string GetAthleteTraitDisplayName(AthleteDefinition athlete, int index)
        {
            if (athlete?.traitIds == null || index < 0 || index >= athlete.traitIds.Count)
            {
                return string.Empty;
            }

            return AthleteTraitCatalog.GetDisplayName(athlete.traitIds[index]);
        }

        private static string BuildAthleteFitLine(AthleteDefinition athlete, HeroDefinition hero, TeamSide? side = null)
        {
            if (athlete == null || hero == null)
            {
                return "Fit --";
            }

            var modifier = AthleteCombatModifierResolver.Resolve(athlete, hero, side ?? TeamSide.None);
            return $"Fit {modifier.BpFitScore}  M{modifier.MasteryScore:0}  A{modifier.EffectiveAttackScore:0} D{modifier.EffectiveDefenseScore:0}";
        }

        private static string BuildCurrentPickAthleteFitLine(HeroDefinition hero)
        {
            var step = GameFlowState.CurrentDraftStep;
            if (step == null || step.ActionType != BattleDraftActionType.Pick || hero == null)
            {
                return string.Empty;
            }

            var athletes = step.Side == TeamSide.Red ? GameFlowState.RedAthletes : GameFlowState.BlueAthletes;
            var athlete = athletes.Count > step.SlotIndex ? athletes[step.SlotIndex] : null;
            if (athlete == null)
            {
                return "No athlete bound.";
            }

            var traitDetails = BuildAthleteTraitDescriptionSummary(athlete);
            return string.IsNullOrWhiteSpace(traitDetails)
                ? $"{BuildAthleteSummary(athlete)}\n{BuildAthleteFitLine(athlete, hero, step.Side)}"
                : $"{BuildAthleteSummary(athlete)}\n{BuildAthleteFitLine(athlete, hero, step.Side)}\n{ClampText(traitDetails, 76)}";
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                DrawBox(rect, string.Empty);
                return;
            }

            var textureRect = sprite.textureRect;
            var texCoords = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, texCoords, true);
        }

        private void DrawAthletePortrait(Rect rect, AthleteDefinition athlete, Color accentColor)
        {
            if (athlete?.portrait != null)
            {
                DrawSprite(rect, athlete.portrait);
                return;
            }

            DrawTintedRect(rect, Color.Lerp(new Color(0.12f, 0.14f, 0.18f, 0.96f), accentColor, 0.30f));
            DrawTintedRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(0.05f, 0.06f, 0.08f, 0.70f));
            DrawOutline(rect, new Color(accentColor.r, accentColor.g, accentColor.b, 0.76f), 2f);
            DrawLabel(rect, GetAthleteInitial(athlete), athleteInitialStyle);
        }

        private void DrawHeroPortrait(Rect rect, HeroDefinition hero)
        {
            var portrait = Fight.UI.HeroPortraitResolver.ResolvePortrait(hero);
            if (portrait == null)
            {
                DrawHeroPortraitPlaceholder(rect, hero);
                return;
            }

            var sprite = portrait;
            var texture = sprite.texture;
            if (texture == null)
            {
                DrawHeroPortraitPlaceholder(rect, hero);
                return;
            }

            var textureRect = sprite.textureRect;
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(rect, texture, texCoords, true);
        }

        private void DrawHeroPortraitTopHalf(Rect rect, HeroDefinition hero)
        {
            var portrait = Fight.UI.HeroPortraitResolver.ResolvePortrait(hero);
            if (portrait == null)
            {
                DrawHeroPortraitPlaceholder(rect, hero);
                return;
            }

            var texture = portrait.texture;
            if (texture == null)
            {
                DrawHeroPortraitPlaceholder(rect, hero);
                return;
            }

            var textureRect = portrait.textureRect;
            var topHalfRect = new Rect(
                textureRect.x,
                textureRect.y + (textureRect.height * 0.5f),
                textureRect.width,
                textureRect.height * 0.5f);
            var texCoords = new Rect(
                topHalfRect.x / texture.width,
                topHalfRect.y / texture.height,
                topHalfRect.width / texture.width,
                topHalfRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(PixelSnap(rect), texture, texCoords, true);
        }

        private void DrawHeroPortraitPlaceholder(Rect rect, HeroDefinition hero)
        {
            var baseColor = hero != null ? GetClassColor(hero.heroClass) : new Color(0.42f, 0.44f, 0.48f);
            DrawTintedRect(rect, Color.Lerp(new Color(0.10f, 0.11f, 0.13f, 1f), baseColor, 0.55f));
            DrawTintedRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(0.045f, 0.05f, 0.06f, 0.72f));
            DrawOutline(rect, new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f), 2f);

            var previousSize = athleteInitialStyle.fontSize;
            athleteInitialStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(rect.height * 0.42f, 14f, 24f));
            DrawLabel(rect, GetHeroInitial(hero), athleteInitialStyle);
            athleteInitialStyle.fontSize = previousSize;
        }

        private static string GetHeroInitial(HeroDefinition hero)
        {
            var displayName = hero != null ? hero.displayName : string.Empty;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "?";
            }

            return displayName.Substring(0, 1).ToUpperInvariant();
        }

        private static string GetAthleteInitial(AthleteDefinition athlete)
        {
            var displayName = athlete != null ? athlete.displayName : string.Empty;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "?";
            }

            return displayName.Substring(0, 1).ToUpperInvariant();
        }

        private static void DrawTintedRect(Rect rect, Color color)
        {
            rect = PixelSnap(rect);
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private static void DrawTintedTexture(Rect rect, Texture texture, Color color)
        {
            if (texture == null)
            {
                return;
            }

            rect = PixelSnap(rect);
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            rect = PixelSnap(rect);
            var resolvedThickness = Mathf.Max(1f, thickness);
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, resolvedThickness), color);
            DrawTintedRect(new Rect(rect.x, rect.yMax - resolvedThickness, rect.width, resolvedThickness), color);
            DrawTintedRect(new Rect(rect.x, rect.y, resolvedThickness, rect.height), color);
            DrawTintedRect(new Rect(rect.xMax - resolvedThickness, rect.y, resolvedThickness, rect.height), color);
        }

        private static void DrawLabel(Rect rect, string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text) || style == null)
            {
                return;
            }

            GUI.Label(PixelSnap(rect), text, style);
        }

        private static bool DrawButton(Rect rect, string text, GUIStyle style)
        {
            return GUI.Button(PixelSnap(rect), text, style);
        }

        private static bool DrawButton(Rect rect, string text)
        {
            return GUI.Button(PixelSnap(rect), text);
        }

        private static bool DrawButton(Rect rect, GUIContent content, GUIStyle style)
        {
            return GUI.Button(PixelSnap(rect), content, style);
        }

        private static void DrawBox(Rect rect, string text)
        {
            GUI.Box(PixelSnap(rect), text);
        }

        private static void DrawBox(Rect rect, string text, GUIStyle style)
        {
            GUI.Box(PixelSnap(rect), text, style);
        }

        private static Rect PixelSnap(Rect rect)
        {
            return new Rect(
                Mathf.Round(rect.x),
                Mathf.Round(rect.y),
                Mathf.Round(rect.width),
                Mathf.Round(rect.height));
        }

        private static void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color mainColor)
        {
            if (string.IsNullOrWhiteSpace(text) || style == null)
            {
                return;
            }

            rect = PixelSnap(rect);
            var previousColor = style.normal.textColor;
            style.normal.textColor = ShadowColor;
            DrawLabel(PixelSnap(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height)), text, style);
            style.normal.textColor = mainColor;
            DrawLabel(rect, text, style);
            style.normal.textColor = previousColor;
        }

        private static string ClampText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, Mathf.Max(0, maxLength - 3)) + "...";
        }

        private void SetDraftNotice(string message)
        {
            draftNotice = message;
            draftNoticeUntil = Time.realtimeSinceStartup + 2.5f;
        }

        private static Color GetClassColor(HeroClass heroClass)
        {
            return heroClass switch
            {
                HeroClass.Warrior => new Color(0.82f, 0.42f, 0.18f),
                HeroClass.Mage => new Color(0.34f, 0.48f, 0.92f),
                HeroClass.Assassin => new Color(0.58f, 0.38f, 0.78f),
                HeroClass.Tank => new Color(0.28f, 0.62f, 0.54f),
                HeroClass.Support => new Color(0.9f, 0.74f, 0.32f),
                HeroClass.Marksman => new Color(0.55f, 0.72f, 0.28f),
                _ => Color.gray
            };
        }

        private static string GetHeroClassLabel(HeroClass heroClass)
        {
            return heroClass switch
            {
                HeroClass.Warrior => "Warrior",
                HeroClass.Mage => "Mage",
                HeroClass.Assassin => "Assassin",
                HeroClass.Tank => "Tank",
                HeroClass.Support => "Support",
                HeroClass.Marksman => "Marksman",
                _ => heroClass.ToString()
            };
        }

        private static void CycleUltimateTimingStrategy(TeamSide side, int step)
        {
            var strategies = (BattleUltimateTimingStrategy[])System.Enum.GetValues(typeof(BattleUltimateTimingStrategy));
            var current = GameFlowState.GetUltimateTimingStrategy(side);
            var nextIndex = WrapEnumIndex(System.Array.IndexOf(strategies, current) + step, strategies.Length);
            GameFlowState.SetUltimateTimingStrategy(side, strategies[nextIndex]);
        }

        private static void CycleUltimateComboStrategy(TeamSide side, int step)
        {
            var strategies = (BattleUltimateComboStrategy[])System.Enum.GetValues(typeof(BattleUltimateComboStrategy));
            var current = GameFlowState.GetUltimateComboStrategy(side);
            var nextIndex = WrapEnumIndex(System.Array.IndexOf(strategies, current) + step, strategies.Length);
            GameFlowState.SetUltimateComboStrategy(side, strategies[nextIndex]);
        }

        private static int WrapEnumIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static string GetUltimateTimingLabel(BattleUltimateTimingStrategy strategy)
        {
            return strategy switch
            {
                BattleUltimateTimingStrategy.Early => "Early",
                BattleUltimateTimingStrategy.Late => "Late",
                _ => "Standard",
            };
        }

        private static string GetUltimateComboLabel(BattleUltimateComboStrategy strategy)
        {
            return strategy switch
            {
                BattleUltimateComboStrategy.Together => "Together",
                BattleUltimateComboStrategy.Standard => "Standard",
                _ => "Separate",
            };
        }

        private void EnsureTopStyles(float scale)
        {
            if (Mathf.Abs(lastTopStyleScale - scale) < 0.01f && topTeamStyle != null)
            {
                return;
            }

            lastTopStyleScale = scale;
            topTeamStyle = BuildTopStyle(38, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            topScoreStyle = BuildTopStyle(86, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            topPhaseStyle = BuildTopStyle(22, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            topLogoStyle = BuildTopStyle(36, scale, TextAnchor.MiddleCenter, FontStyle.Bold);
            topButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(14 * Mathf.Max(0.70f, scale))),
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                normal = { textColor = MainTextColor }
            };
        }

        private static GUIStyle BuildTopStyle(int baseSize, float scale, TextAnchor alignment, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(baseSize * Mathf.Max(0.60f, scale))),
                fontStyle = fontStyle,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = MainTextColor }
            };
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.9f, 0.96f) }
            };

            smallBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.82f, 0.9f) }
            };

            slotStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            focusedSlotStyle = new GUIStyle(slotStyle)
            {
                fontSize = 13
            };

            heroCardButtonStyle = new GUIStyle(GUIStyle.none)
            {
                normal = { textColor = Color.clear },
                hover = { textColor = Color.clear },
                active = { textColor = Color.clear },
                focused = { textColor = Color.clear }
            };

            filterButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            strategyButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            detailHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            athleteInitialStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = MainTextColor }
            };
        }
    }
}
