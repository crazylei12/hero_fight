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

        private HeroDefinition highlightedHero;
        private TeamSide? swapSourceSide;
        private int swapSourceIndex = -1;
        private bool classFilterActive;
        private HeroClass classFilter;
        private Sprite playerStatusGoodArrowSprite;
        private Sprite playerStatusNormalArrowSprite;
        private Sprite playerStatusBadArrowSprite;
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
        private Vector2 catalogScroll;
        private string draftNotice;
        private float draftNoticeUntil;

        private void Awake()
        {
            GameFlowState.RefreshHeroCatalog();
            GameFlowState.ResetDraft();
            GameFlowState.ClearBattleResult();
            playerStatusGoodArrowSprite = Resources.Load<Sprite>(PlayerStatusGoodArrowTexturePath);
            playerStatusNormalArrowSprite = Resources.Load<Sprite>(PlayerStatusNormalArrowTexturePath);
            playerStatusBadArrowSprite = Resources.Load<Sprite>(PlayerStatusBadArrowTexturePath);
            highlightedHero = FindFirstCatalogHero();
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (highlightedHero == null)
            {
                highlightedHero = FindFirstCatalogHero();
            }

            var margin = 18f;
            var headerHeight = 86f;
            var phaseHeight = 50f;
            var bottomHeight = 112f;
            var gap = 12f;
            var sideWidth = Mathf.Clamp(Screen.width * 0.19f, 250f, 318f);
            var contentY = margin + headerHeight + phaseHeight + gap;
            var contentHeight = Mathf.Max(420f, Screen.height - contentY - bottomHeight - margin - gap);

            GUI.Box(new Rect(margin, margin, Screen.width - (margin * 2f), Screen.height - (margin * 2f)), string.Empty);

            DrawHeader(new Rect(margin + 8f, margin + 8f, Screen.width - (margin * 2f) - 16f, headerHeight));
            DrawPhaseBanner(new Rect(margin + 8f, margin + headerHeight + 8f, Screen.width - (margin * 2f) - 16f, phaseHeight));

            var leftRect = new Rect(margin + 12f, contentY, sideWidth, contentHeight);
            var rightRect = new Rect(Screen.width - margin - 12f - sideWidth, contentY, sideWidth, contentHeight);
            var centerX = leftRect.xMax + gap;
            var centerWidth = rightRect.xMin - centerX - gap;
            var centerRect = new Rect(centerX, contentY, centerWidth, contentHeight);

            DrawTeamPanel(leftRect, TeamSide.Blue, new Color(0.12f, 0.34f, 0.86f, 0.92f));
            DrawTeamPanel(rightRect, TeamSide.Red, new Color(0.88f, 0.16f, 0.18f, 0.92f));
            DrawCenterPanel(centerRect);
            DrawBanPanel(new Rect(centerX, centerRect.yMax + gap, centerWidth, bottomHeight - 20f));
        }

        private void DrawHeader(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width * 0.28f, 32f), "BLUE TEAM", sectionStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.36f, rect.y + 8f, rect.width * 0.28f, 36f), "Stage 2 BP", titleStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.72f, rect.y + 10f, rect.width * 0.25f, 32f), "RED TEAM", sectionStyle);

            GUI.Label(
                new Rect(rect.x + (rect.width * 0.42f), rect.y + 46f, rect.width * 0.16f, 24f),
                $"{GameFlowState.DraftStepNumber}/{GameFlowState.DraftTotalSteps}",
                bodyStyle);

            if (GUI.Button(new Rect(rect.x + 16f, rect.y + rect.height - 34f, 112f, 26f), "Back"))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }

            if (GUI.Button(new Rect(rect.x + 140f, rect.y + rect.height - 34f, 112f, 26f), "Reset BP"))
            {
                GameFlowState.ResetDraft();
                ClearSwapSelection();
                highlightedHero = FindFirstCatalogHero();
                catalogScroll = Vector2.zero;
                SetDraftNotice("BP has been reset.");
            }

            var canStart = GameFlowState.IsDraftComplete && GameFlowState.CanPrepareBattleInput();
            GUI.enabled = canStart;
            var startButtonLabel = GameFlowState.IsDraftComplete && !GameFlowState.AreBothTeamsExchangeReady
                ? "Waiting Ready"
                : "Start Battle";
            if (GUI.Button(new Rect(rect.xMax - 148f, rect.y + rect.height - 34f, 132f, 26f), startButtonLabel))
            {
                if (GameFlowState.TryPrepareBattleInput(out _))
                {
                    SceneManager.LoadScene(battleSceneName);
                }
            }

            GUI.enabled = true;
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

            GUI.Label(new Rect(rect.x + 20f, rect.y + 8f, rect.width - 40f, 34f), GetPhaseLabel(), titleStyle);

            if (!string.IsNullOrEmpty(draftNotice) && Time.realtimeSinceStartup < draftNoticeUntil)
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 30f, rect.width - 40f, 18f), draftNotice, smallBodyStyle);
            }
        }

        private void DrawTeamPanel(Rect rect, TeamSide side, Color accentColor)
        {
            GUI.Box(rect, string.Empty);
            var title = side == TeamSide.Blue ? "Blue Draft" : "Red Draft";
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 28f), title, sectionStyle);

            var selection = side == TeamSide.Red ? GameFlowState.RedSelection : GameFlowState.BlueSelection;
            var athletes = side == TeamSide.Red ? GameFlowState.RedAthletes : GameFlowState.BlueAthletes;
            var currentStep = GameFlowState.CurrentDraftStep;
            var isSwapPhase = GameFlowState.IsDraftComplete;
            var isTeamReady = GameFlowState.IsTeamExchangeReady(side);
            var slotGap = 8f;
            var strategyBlockHeight = isSwapPhase ? 106f : 74f;
            var availableSlotHeight = rect.height - 48f - strategyBlockHeight - (slotGap * (BattleInputConfig.DefaultTeamSize - 1));
            var slotHeight = Mathf.Clamp(availableSlotHeight / BattleInputConfig.DefaultTeamSize, 68f, 112f);
            for (var i = 0; i < BattleInputConfig.DefaultTeamSize; i++)
            {
                var slotRect = new Rect(rect.x + 14f, rect.y + 48f + (i * (slotHeight + slotGap)), rect.width - 28f, slotHeight);
                var hero = selection.Count > i ? selection[i] : null;
                var athlete = athletes.Count > i ? athletes[i] : null;
                var isCurrent = currentStep != null
                    && currentStep.ActionType == BattleDraftActionType.Pick
                    && currentStep.Side == side
                    && currentStep.SlotIndex == i;
                var isSwapSource = swapSourceSide.HasValue && swapSourceSide.Value == side && swapSourceIndex == i;
                DrawTeamSlot(slotRect, i, hero, athlete, isCurrent, isSwapSource, isTeamReady, accentColor);
                if (isSwapPhase && !isTeamReady && hero != null && GUI.Button(slotRect, GUIContent.none, heroCardButtonStyle))
                {
                    HandleSwapSlotClicked(side, i);
                }
            }

            var strategyY = rect.yMax - strategyBlockHeight;
            if (isSwapPhase)
            {
                DrawExchangeReadyControls(new Rect(rect.x + 14f, strategyY, rect.width - 28f, 26f), side);
                strategyY += 32f;
            }

            DrawStrategySelector(
                new Rect(rect.x + 14f, strategyY, rect.width - 28f, 24f),
                "Ult Timing",
                GetUltimateTimingLabel(GameFlowState.GetUltimateTimingStrategy(side)),
                () => CycleUltimateTimingStrategy(side, -1),
                () => CycleUltimateTimingStrategy(side, 1));

            DrawStrategySelector(
                new Rect(rect.x + 14f, strategyY + 30f, rect.width - 28f, 24f),
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
            GUI.color = Color.Lerp(Color.white, slotAccent, isFocused ? 0.88f : isTeamReady ? 0.48f : 0.28f);
            GUI.Box(rect, string.Empty, isFocused ? focusedSlotStyle : slotStyle);
            if (isFocused || isTeamReady)
            {
                GUI.color = new Color(slotAccent.r, slotAccent.g, slotAccent.b, isTeamReady && !isFocused ? 0.2f : 0.38f);
                GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.14f);
                GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 3f), Texture2D.whiteTexture);
            }

            GUI.color = previousColor;

            var compact = rect.height < 92f;
            var padding = compact ? 5f : 7f;
            var gap = compact ? 5f : 7f;
            var headerHeight = compact ? 17f : 20f;
            var statusSize = compact ? 17f : 20f;
            var statHeight = compact ? 15f : 18f;
            var masteryIconSize = compact ? 20f : 25f;
            var masteryY = rect.yMax - padding - masteryIconSize;
            var statY = masteryY - statHeight - 4f;
            var contentY = rect.y + headerHeight + padding;
            var topContentHeight = Mathf.Max(18f, statY - contentY - 4f);
            var heroPortraitSize = Mathf.Clamp(topContentHeight, 34f, rect.width * 0.30f);

            var heroRect = new Rect(rect.x + padding, contentY, heroPortraitSize, heroPortraitSize);
            DrawHeroPortrait(heroRect, hero);

            var infoX = heroRect.xMax + gap;
            var infoWidth = rect.xMax - padding - infoX;
            var nameRect = new Rect(rect.x + padding, rect.y + 4f, rect.width - (padding * 2f) - statusSize - 4f, headerHeight);
            GUI.Label(nameRect, GetAthleteDisplayName(athlete), bodyStyle);
            DrawAthleteConditionArrow(new Rect(rect.xMax - padding - statusSize, rect.y + 5f, statusSize, statusSize), athlete);

            var heroNameHeight = Mathf.Min(compact ? 14f : 17f, topContentHeight);
            GUI.Label(new Rect(infoX, contentY, infoWidth, heroNameHeight), hero != null ? hero.displayName : "Waiting pick", smallBodyStyle);
            var traitRect = new Rect(infoX, contentY + heroNameHeight + 2f, infoWidth, topContentHeight - heroNameHeight - 2f);
            if (traitRect.height >= 12f)
            {
                DrawAthleteTraitRows(traitRect);
            }

            var masteryBonus = GetSelectedHeroMastery(athlete, hero);
            var statWidth = (rect.width - (padding * 2f) - 6f) * 0.5f;
            DrawStatChip(new Rect(rect.x + padding, statY, statWidth, statHeight), true, athlete != null ? athlete.attack : 0f, masteryBonus);
            DrawStatChip(new Rect(rect.x + padding + statWidth + 6f, statY, statWidth, statHeight), false, athlete != null ? athlete.defense : 0f, masteryBonus);

            DrawAthleteMasteryIcons(new Rect(rect.x + padding, masteryY, rect.width - (padding * 2f), masteryIconSize), athlete, hero);
        }

        private void DrawCenterPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            var catalogHeight = Mathf.Max(250f, rect.height * 0.58f);
            DrawCatalogPanel(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, catalogHeight - 12f));
            DrawHeroDetailPanel(new Rect(rect.x + 12f, rect.y + catalogHeight + 10f, rect.width - 24f, rect.height - catalogHeight - 22f));
        }

        private void DrawCatalogPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 26f), "Hero Pool", sectionStyle);
            DrawClassFilters(new Rect(rect.x + 12f, rect.y + 42f, rect.width - 24f, 30f));

            var visibleRect = new Rect(rect.x + 12f, rect.y + 80f, rect.width - 24f, rect.height - 92f);
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
                if (GUI.Button(buttonRect, labels[i], filterButtonStyle))
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
            GUI.color = isHighlighted ? Color.Lerp(Color.white, baseColor, 0.85f) : Color.Lerp(Color.white, baseColor, 0.45f);
            GUI.Box(rect, string.Empty);
            GUI.color = previousColor;

            var portraitRect = new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 68f);
            DrawHeroPortrait(portraitRect, hero);

            GUI.Label(new Rect(rect.x + 6f, rect.y + 82f, rect.width - 12f, 20f), hero.displayName, smallBodyStyle);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 102f, rect.width - 12f, 18f), GetHeroClassLabel(hero.heroClass), smallBodyStyle);

            if (isBanned || isPicked)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.58f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 4f, rect.y + 48f, rect.width - 8f, 24f), isBanned ? "BANNED" : "PICKED", sectionStyle);
            }

            if (GUI.Button(rect, GUIContent.none, heroCardButtonStyle))
            {
                highlightedHero = hero;
            }
        }

        private void DrawHeroDetailPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 26f), "Hero Detail", sectionStyle);

            if (highlightedHero == null)
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 30f), "Select a hero from the pool.", bodyStyle);
                return;
            }

            var hero = highlightedHero;
            var portraitRect = new Rect(rect.x + 16f, rect.y + 46f, 96f, 96f);
            DrawHeroPortrait(portraitRect, hero);
            GUI.Label(new Rect(portraitRect.xMax + 16f, rect.y + 42f, rect.width - portraitRect.width - 220f, 28f), hero.displayName, detailHeaderStyle);
            GUI.Label(new Rect(portraitRect.xMax + 16f, rect.y + 74f, rect.width - portraitRect.width - 220f, 22f), $"{GetHeroClassLabel(hero.heroClass)} | {BuildHeroTagLine(hero.tags)}", bodyStyle);
            GUI.Label(new Rect(portraitRect.xMax + 16f, rect.y + 102f, rect.width - portraitRect.width - 220f, 44f), BuildStatsLine(hero), smallBodyStyle);

            var confirmRect = new Rect(rect.xMax - 170f, rect.y + 56f, 150f, 36f);
            var canConfirm = GameFlowState.CanDraftHero(hero);
            GUI.enabled = canConfirm;
            if (GUI.Button(confirmRect, GetConfirmButtonLabel()))
            {
                if (GameFlowState.TryApplyDraftHero(hero))
                {
                    SetDraftNotice($"{hero.displayName} confirmed.");
                }
            }

            GUI.enabled = true;
            GUI.Label(new Rect(rect.xMax - 188f, rect.y + 98f, 170f, 42f), GetHeroAvailabilityLabel(hero), smallBodyStyle);
            GUI.Label(new Rect(rect.xMax - 188f, rect.y + 138f, 170f, 34f), BuildCurrentPickAthleteFitLine(hero), smallBodyStyle);

            var skillY = rect.y + 178f;
            var skillHeight = Mathf.Max(42f, (rect.yMax - skillY - 12f) / 2f - 4f);
            DrawSkillSummary(new Rect(rect.x + 16f, skillY, rect.width - 32f, skillHeight), "Skill", hero.activeSkill);
            DrawSkillSummary(new Rect(rect.x + 16f, skillY + skillHeight + 8f, rect.width - 32f, skillHeight), "Ultimate", hero.ultimateSkill);
        }

        private void DrawSkillSummary(Rect rect, string label, SkillData skill)
        {
            GUI.Box(rect, string.Empty);
            var name = skill != null ? skill.displayName : "None";
            var description = skill != null && !string.IsNullOrWhiteSpace(skill.description)
                ? skill.description
                : "No description.";
            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, 140f, 22f), $"{label}: {name}", bodyStyle);
            GUI.Label(new Rect(rect.x + 160f, rect.y + 6f, rect.width - 170f, rect.height - 12f), ClampText(description, 160), smallBodyStyle);
        }

        private void DrawBanPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + rect.width * 0.42f, rect.y + 12f, rect.width * 0.16f, 34f), "BANNED", titleStyle);

            DrawBanSlots(new Rect(rect.x + 16f, rect.y + 16f, rect.width * 0.34f, rect.height - 28f), TeamSide.Blue, GameFlowState.BlueBans, new Color(0.12f, 0.34f, 0.86f, 0.9f));
            DrawBanSlots(new Rect(rect.xMax - 16f - (rect.width * 0.34f), rect.y + 16f, rect.width * 0.34f, rect.height - 28f), TeamSide.Red, GameFlowState.RedBans, new Color(0.9f, 0.18f, 0.18f, 0.9f));
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
                GUI.Box(slotRect, string.Empty);
                GUI.color = previousColor;

                DrawHeroPortrait(new Rect(slotRect.x + 8f, slotRect.y + 8f, 54f, 54f), hero);
                GUI.Label(new Rect(slotRect.x + 70f, slotRect.y + 14f, slotRect.width - 78f, 22f), hero != null ? hero.displayName : $"{side} Ban {i + 1}", bodyStyle);
                GUI.Label(new Rect(slotRect.x + 70f, slotRect.y + 38f, slotRect.width - 78f, 18f), hero != null ? GetHeroClassLabel(hero.heroClass) : "Waiting", smallBodyStyle);
            }
        }

        private void DrawExchangeReadyControls(Rect rect, TeamSide side)
        {
            var ready = GameFlowState.IsTeamExchangeReady(side);
            var statusText = ready ? "Ready locked" : "Swap heroes";
            GUI.Label(new Rect(rect.x, rect.y, 96f, rect.height), statusText, smallBodyStyle);

            var previousEnabled = GUI.enabled;
            GUI.enabled = GameFlowState.HasValidSelections();
            var buttonLabel = ready ? "Edit" : "Ready";
            if (GUI.Button(new Rect(rect.xMax - 78f, rect.y, 78f, rect.height), buttonLabel, strategyButtonStyle))
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
            GUI.Box(new Rect(rect.x + 100f, rect.y, rect.width - 184f, rect.height), hint);
        }

        private void DrawStrategySelector(Rect rect, string label, string value, System.Action previous, System.Action next)
        {
            var buttonWidth = 26f;
            var labelWidth = 88f;
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), label, smallBodyStyle);

            if (GUI.Button(new Rect(rect.x + labelWidth, rect.y, buttonWidth, rect.height), "<", strategyButtonStyle))
            {
                previous?.Invoke();
            }

            GUI.Box(new Rect(rect.x + labelWidth + buttonWidth + 6f, rect.y, rect.width - labelWidth - (buttonWidth * 2f) - 12f, rect.height), value);

            if (GUI.Button(new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height), ">", strategyButtonStyle))
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

            GUI.Box(rect, condition > 20f ? "+" : condition < 0f ? "-" : "=");
        }

        private void DrawStatChip(Rect rect, bool isAttack, float value, float masteryBonus)
        {
            GUI.Box(rect, string.Empty);
            var iconRect = new Rect(rect.x + 2f, rect.y + 2f, rect.height - 4f, rect.height - 4f);
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
            GUI.Label(new Rect(iconRect.xMax + 1f, rect.y, rect.width - iconRect.width - 2f, rect.height), valueText, smallBodyStyle);
        }

        private static void DrawAttackIcon(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(1f, 0.76f, 0.36f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.44f, rect.y + rect.height * 0.08f, rect.width * 0.16f, rect.height * 0.72f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.30f, rect.y + rect.height * 0.17f, rect.width * 0.44f, rect.height * 0.16f), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.42f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.34f, rect.y + rect.height * 0.76f, rect.width * 0.36f, rect.height * 0.16f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void DrawDefenseIcon(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.46f, 0.72f, 1f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.12f, rect.width * 0.64f, rect.height * 0.52f), Texture2D.whiteTexture);
            GUI.color = new Color(0.23f, 0.42f, 0.74f, 1f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.30f, rect.y + rect.height * 0.58f, rect.width * 0.40f, rect.height * 0.26f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawAthleteTraitRows(Rect rect)
        {
            const int traitSlotCount = 3;
            var gap = rect.height < 34f ? 2f : 4f;
            var rowHeight = Mathf.Max(5f, (rect.height - (gap * (traitSlotCount - 1))) / traitSlotCount);
            for (var i = 0; i < traitSlotCount; i++)
            {
                var rowRect = new Rect(rect.x, rect.y + (i * (rowHeight + gap)), rect.width, rowHeight);
                GUI.Box(rowRect, string.Empty);
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
                    DrawHeroPortrait(new Rect(iconRect.x + 3f, iconRect.y + 3f, iconRect.width - 6f, iconRect.height - 6f), masteryHero);
                }
                else
                {
                    GUI.color = Color.white;
                    DrawHeroPortrait(iconRect, masteryHero);
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

                var valueRect = new Rect(iconRect.xMax - 18f, iconRect.yMax - 13f, 18f, 13f);
                GUI.color = new Color(0f, 0f, 0f, 0.68f);
                GUI.DrawTexture(valueRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
                GUI.Label(valueRect, mastery.mastery.ToString("0"), smallBodyStyle);
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
            return $"{displayName}  A{athlete.attack:0} D{athlete.defense:0} C{athlete.condition:+0;-0;0}";
        }

        private static string BuildAthleteFitLine(AthleteDefinition athlete, HeroDefinition hero)
        {
            if (athlete == null || hero == null)
            {
                return "Fit --";
            }

            var modifier = AthleteCombatModifierResolver.Resolve(athlete, hero);
            return $"Fit {modifier.BpFitScore}  M{modifier.MasteryScore:0}";
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

            return $"{BuildAthleteSummary(athlete)}\n{BuildAthleteFitLine(athlete, hero)}";
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                GUI.Box(rect, string.Empty);
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

        private static void DrawHeroPortrait(Rect rect, HeroDefinition hero)
        {
            if (hero?.visualConfig?.portrait == null)
            {
                GUI.Box(rect, string.Empty);
                return;
            }

            var sprite = hero.visualConfig.portrait;
            var texture = sprite.texture;
            if (texture == null)
            {
                GUI.Box(rect, string.Empty);
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
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.9f, 0.96f) }
            };

            smallBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
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

            heroCardButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = null, textColor = Color.clear },
                hover = { background = null, textColor = Color.clear },
                active = { background = null, textColor = Color.clear },
                focused = { background = null, textColor = Color.clear }
            };

            filterButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            strategyButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            detailHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
