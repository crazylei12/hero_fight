using System.Collections.Generic;
using Fight.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fight.UI
{
    public partial class BattleCanvasHud
    {
        private sealed class TopBarView
        {
            public RectTransform Root;
            public RectTransform LeftPanel;
            public Image LeftBackground;
            public Image LeftAccentLine;
            public Text LeftTeamText;
            public Text LeftAliveText;
            public RectTransform RightPanel;
            public Image RightBackground;
            public Image RightAccentLine;
            public Text RightTeamText;
            public Text RightAliveText;
            public RectTransform CenterPanel;
            public Image CenterBackground;
            public Image CenterBlueFill;
            public Image CenterRedFill;
            public Image CenterBanner;
            public Image CenterFrame;
            public Text BlueScoreText;
            public Text RedScoreText;
            public Text TimerText;
            public Text PhaseText;
            public Text CaptionText;
        }

        private sealed class TeamSidebarView
        {
            public RectTransform Root;
            public Image Background;
            public Image Frame;
            public Image HeaderRibbon;
            public Text HeaderText;
            public readonly List<HeroCardView> Cards = new List<HeroCardView>(BattleInputConfig.DefaultTeamSize);
        }

        private sealed class HeroCardView
        {
            public RectTransform Root;
            public Image Background;
            public Image Border;
            public Image AccentBar;
            public RectTransform PortraitRoot;
            public Image PortraitBackdrop;
            public Image PortraitImage;
            public Image PortraitFrame;
            public Text PortraitFallbackText;
            public Text NameText;
            public Text MetaText;
            public Text HealthText;
            public RectTransform HealthBarRoot;
            public RectTransform HealthFillRect;
            public Image HealthFill;
            public RectTransform ShieldBarRoot;
            public RectTransform ShieldFillRect;
            public Image ShieldFill;
            public Text StatText;
            public Image SkillChipBackground;
            public Text SkillChipText;
            public Image UltimateChipBackground;
            public Text UltimateChipText;
            public Image ControlChipBackground;
            public Text ControlChipText;
            public Image DeadOverlay;
            public Image DeadIcon;
            public Text RespawnText;
        }

        private sealed class NameplateView
        {
            public RectTransform Root;
            public Image Background;
            public Image AccentLine;
            public Image DeadIcon;
            public Text LabelText;
        }

        private sealed class EndBannerView
        {
            public RectTransform Root;
            public CanvasGroup CanvasGroup;
            public Image Background;
            public Image Frame;
            public Text LabelText;
        }

        private void EnsureHudCanvas()
        {
            if (canvasRoot != null)
            {
                return;
            }

            var canvasObject = new GameObject("BattleCanvasHudRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            canvasRoot = canvasObject.GetComponent<RectTransform>();
            overlayCanvas = canvasObject.GetComponent<Canvas>();
            canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 20;
            overlayCanvas.pixelPerfect = false;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGroup.alpha = 1f;

            topBar = BuildTopBar(canvasRoot);
            blueSidebar = BuildSidebar(canvasRoot, "BlueSidebar");
            redSidebar = BuildSidebar(canvasRoot, "RedSidebar");
            nameplateLayer = CreateRect("Nameplates", canvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            endBanner = BuildEndBanner(canvasRoot);

            ApplyResponsiveLayout();
            PlayIntroAnimation(forceReplay: false);
        }

        private TopBarView BuildTopBar(RectTransform parent)
        {
            var view = new TopBarView
            {
                Root = CreateRect("TopBar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 124f), Vector2.zero),
            };

            view.LeftPanel = CreateRect("LeftPanel", view.Root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(420f, 86f), new Vector2(20f, -12f));
            view.LeftBackground = CreateImage("Background", view.LeftPanel, null, Tint(blueColor, 0.2f));
            StretchToParent(view.LeftBackground.rectTransform, 0f);
            view.LeftAccentLine = CreateImage("AccentLine", view.LeftPanel, theme != null ? theme.topLineLeft : null, Tint(blueColor, 0.88f), true);
            view.LeftTeamText = CreateText("TeamText", view.LeftPanel, 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.LeftAliveText = CreateText("AliveText", view.LeftPanel, 16, FontStyle.Bold, TextAnchor.MiddleCenter, mutedTextColor);

            view.RightPanel = CreateRect("RightPanel", view.Root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(420f, 86f), new Vector2(-20f, -12f));
            view.RightBackground = CreateImage("Background", view.RightPanel, null, Tint(redColor, 0.2f));
            StretchToParent(view.RightBackground.rectTransform, 0f);
            view.RightAccentLine = CreateImage("AccentLine", view.RightPanel, theme != null ? theme.topLineRight : null, Tint(redColor, 0.88f), true);
            view.RightTeamText = CreateText("TeamText", view.RightPanel, 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.RightAliveText = CreateText("AliveText", view.RightPanel, 16, FontStyle.Bold, TextAnchor.MiddleCenter, mutedTextColor);

            view.CenterPanel = CreateRect("CenterPanel", view.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, 98f), new Vector2(0f, -8f));
            view.CenterBackground = CreateImage("CenterBackground", view.CenterPanel, null, Tint(panelBackground, 0.95f));
            StretchToParent(view.CenterBackground.rectTransform, 0f);
            view.CenterBlueFill = CreateImage("BlueFill", view.CenterPanel, null, Tint(blueColor, 0.84f));
            view.CenterRedFill = CreateImage("RedFill", view.CenterPanel, null, Tint(redColor, 0.84f));
            view.CenterBanner = CreateImage("Banner", view.CenterPanel, theme != null ? theme.topBanner : null, Tint(Color.white, 0.28f));
            view.CenterFrame = CreateImage("Frame", view.CenterPanel, theme != null ? theme.topFrame : null, Tint(Color.white, 0.76f));

            view.BlueScoreText = CreateText("BlueScore", view.CenterPanel, 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.RedScoreText = CreateText("RedScore", view.CenterPanel, 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.TimerText = CreateText("TimerText", view.CenterPanel, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.PhaseText = CreateText("PhaseText", view.CenterPanel, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f, 1f));
            view.CaptionText = CreateText("CaptionText", view.CenterPanel, 12, FontStyle.Bold, TextAnchor.MiddleCenter, mutedTextColor);

            return view;
        }

        private TeamSidebarView BuildSidebar(RectTransform parent, string name)
        {
            var view = new TeamSidebarView
            {
                Root = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(332f, 820f), Vector2.zero),
            };

            view.Background = CreateImage("Background", view.Root, null, Tint(panelBackground, 0.95f));
            StretchToParent(view.Background.rectTransform, 0f);
            view.Frame = CreateImage("Frame", view.Root, theme != null ? theme.sidebarFrame : null, Tint(Color.white, 0.36f));
            StretchToParent(view.Frame.rectTransform, 0f);
            view.HeaderRibbon = CreateImage("HeaderRibbon", view.Root, theme != null ? theme.topBanner : null, Color.white);
            view.HeaderText = CreateText("HeaderText", view.Root, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            for (var i = 0; i < BattleInputConfig.DefaultTeamSize; i++)
            {
                view.Cards.Add(BuildHeroCard(view.Root, $"Card_{i + 1}"));
            }

            return view;
        }

        private HeroCardView BuildHeroCard(RectTransform parent, string name)
        {
            var view = new HeroCardView
            {
                Root = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, 120f), Vector2.zero),
            };

            view.Background = CreateImage("Background", view.Root, theme != null ? theme.cardBackground : null, Tint(softPanelBackground, 0.94f));
            StretchToParent(view.Background.rectTransform, 0f);
            view.Border = CreateImage("Border", view.Root, theme != null ? theme.cardBorder : null, Tint(Color.white, 0.78f));
            StretchToParent(view.Border.rectTransform, 0f);
            view.AccentBar = CreateImage("AccentBar", view.Root, null, Color.white);

            view.PortraitRoot = CreateRect("PortraitRoot", view.Root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, 78f), new Vector2(10f, -10f));
            view.PortraitBackdrop = CreateImage("PortraitBackdrop", view.PortraitRoot, null, new Color(0.04f, 0.05f, 0.08f, 0.98f));
            StretchToParent(view.PortraitBackdrop.rectTransform, 0f);
            view.PortraitImage = CreateImage("PortraitImage", view.PortraitRoot, null, Color.white, true);
            StretchToParent(view.PortraitImage.rectTransform, new Vector4(6f, 6f, 6f, 6f));
            view.PortraitFrame = CreateImage("PortraitFrame", view.PortraitRoot, theme != null ? theme.portraitFrame : null, Tint(Color.white, 0.92f));
            StretchToParent(view.PortraitFrame.rectTransform, 0f);
            view.PortraitFallbackText = CreateText("PortraitFallback", view.PortraitRoot, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            StretchToParent(view.PortraitFallbackText.rectTransform, new Vector4(6f, 6f, 6f, 6f));

            view.NameText = CreateText("NameText", view.Root, 18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            view.MetaText = CreateText("MetaText", view.Root, 11, FontStyle.Bold, TextAnchor.MiddleLeft, mutedTextColor);
            view.HealthText = CreateText("HealthText", view.Root, 12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            view.StatText = CreateText("StatText", view.Root, 11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            view.StatText.resizeTextForBestFit = true;
            view.StatText.resizeTextMinSize = 9;
            view.StatText.resizeTextMaxSize = 11;

            view.HealthBarRoot = CreateRect("HealthBarRoot", view.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-106f, 10f), new Vector2(96f, -54f));
            var healthBackground = CreateImage("HealthBackground", view.HealthBarRoot, null, new Color(1f, 1f, 1f, 0.08f));
            StretchToParent(healthBackground.rectTransform, 0f);
            view.HealthFill = CreateImage("HealthFill", view.HealthBarRoot, null, healthyColor);
            view.HealthFillRect = view.HealthFill.rectTransform;
            AnchorLeftFill(view.HealthFillRect);

            view.ShieldBarRoot = CreateRect("ShieldBarRoot", view.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-106f, 4f), new Vector2(96f, -67f));
            var shieldBackground = CreateImage("ShieldBackground", view.ShieldBarRoot, null, new Color(1f, 1f, 1f, 0.04f));
            StretchToParent(shieldBackground.rectTransform, 0f);
            view.ShieldFill = CreateImage("ShieldFill", view.ShieldBarRoot, null, shieldColor);
            view.ShieldFillRect = view.ShieldFill.rectTransform;
            AnchorLeftFill(view.ShieldFillRect);

            view.SkillChipBackground = CreateImage("SkillChip", view.Root, null, Tint(blueColor, 0.2f));
            view.SkillChipText = CreateText("SkillChipText", view.Root, 10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.UltimateChipBackground = CreateImage("UltimateChip", view.Root, null, Tint(shieldColor, 0.18f));
            view.UltimateChipText = CreateText("UltimateChipText", view.Root, 10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            view.ControlChipBackground = CreateImage("ControlChip", view.Root, null, Tint(lowHealthColor, 0.2f));
            view.ControlChipText = CreateText("ControlChipText", view.Root, 10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            view.DeadOverlay = CreateImage("DeadOverlay", view.Root, null, deadOverlayColor);
            StretchToParent(view.DeadOverlay.rectTransform, 0f);
            view.DeadIcon = CreateImage("DeadIcon", view.Root, theme != null ? theme.deadIcon : null, Tint(Color.white, 0.95f), true);
            view.RespawnText = CreateText("RespawnText", view.Root, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            return view;
        }

        private EndBannerView BuildEndBanner(RectTransform parent)
        {
            var view = new EndBannerView
            {
                Root = CreateRect("EndBanner", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(520f, 74f), new Vector2(0f, 28f)),
            };

            view.CanvasGroup = view.Root.gameObject.AddComponent<CanvasGroup>();
            view.Background = CreateImage("Background", view.Root, null, Tint(panelBackground, 0.96f));
            StretchToParent(view.Background.rectTransform, 0f);
            view.Frame = CreateImage("Frame", view.Root, theme != null ? theme.topFrame : null, Tint(Color.white, 0.72f));
            StretchToParent(view.Frame.rectTransform, 0f);
            view.LabelText = CreateText("LabelText", view.Root, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            StretchToParent(view.LabelText.rectTransform, new Vector4(18f, 12f, 18f, 12f));
            view.Root.gameObject.SetActive(false);
            view.CanvasGroup.alpha = 0f;
            return view;
        }

        private void ApplyResponsiveLayout()
        {
            if (canvasRoot == null || topBar == null || blueSidebar == null || redSidebar == null)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            var canvasWidth = Mathf.Max(1280f, canvasRoot.rect.width);
            var canvasHeight = Mathf.Max(720f, canvasRoot.rect.height);
            var topHeight = Mathf.Clamp(canvasHeight * 0.108f, 102f, 128f);
            var sideWidth = Mathf.Clamp(canvasWidth * 0.172f, 292f, 352f);
            var sideHeight = canvasHeight - topHeight - 34f;
            var sideTopOffset = topHeight + 18f;
            var centerWidth = Mathf.Clamp(canvasWidth * 0.24f, 390f, 470f);
            var centerHeight = Mathf.Clamp(topHeight * 0.84f, 92f, 106f);
            var sideBannerWidth = Mathf.Clamp((canvasWidth - centerWidth - (sideWidth * 2f) - 90f) * 0.5f + 160f, 260f, 480f);

            SetRect(topBar.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, topHeight), Vector2.zero);
            SetRect(topBar.LeftPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sideBannerWidth, topHeight - 18f), new Vector2(18f, -(topHeight * 0.52f)));
            SetRect(topBar.RightPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(sideBannerWidth, topHeight - 18f), new Vector2(-18f, -(topHeight * 0.52f)));
            SetRect(topBar.CenterPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(centerWidth, centerHeight), new Vector2(0f, -(topHeight * 0.48f)));

            StretchToParent(topBar.LeftBackground.rectTransform, 0f);
            StretchToParent(topBar.RightBackground.rectTransform, 0f);
            StretchToParent(topBar.CenterBackground.rectTransform, 0f);
            StretchToParent(topBar.CenterFrame.rectTransform, 0f);
            StretchToParent(topBar.CenterBanner.rectTransform, new Vector4(28f, 14f, 28f, 14f));
            SetRect(topBar.LeftAccentLine.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(128f, 26f), new Vector2(-12f, 6f));
            SetRect(topBar.RightAccentLine.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(128f, 26f), new Vector2(12f, 6f));
            StretchToParent(topBar.LeftTeamText.rectTransform, new Vector4(18f, 10f, 18f, 44f));
            StretchToParent(topBar.RightTeamText.rectTransform, new Vector4(18f, 10f, 18f, 44f));
            SetRect(topBar.LeftAliveText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-24f, 22f), new Vector2(0f, 10f));
            SetRect(topBar.RightAliveText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-24f, 22f), new Vector2(0f, 10f));

            SetRect(topBar.CenterBlueFill.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), Vector2.zero);
            SetRect(topBar.CenterRedFill.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), Vector2.zero);
            SetRect(topBar.BlueScoreText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, 48f), new Vector2(0f, 4f));
            SetRect(topBar.RedScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, 48f), new Vector2(0f, 4f));
            SetRect(topBar.PhaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(160f, 20f), new Vector2(0f, -10f));
            SetRect(topBar.TimerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 36f), new Vector2(0f, 3f));
            SetRect(topBar.CaptionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 18f), new Vector2(0f, 8f));

            SetRect(blueSidebar.Root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(sideWidth, sideHeight), new Vector2(16f, -sideTopOffset));
            SetRect(redSidebar.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(sideWidth, sideHeight), new Vector2(-16f, -sideTopOffset));
            LayoutSidebar(blueSidebar, sideWidth, sideHeight);
            LayoutSidebar(redSidebar, sideWidth, sideHeight);

            SetRect(endBanner.Root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(Mathf.Clamp(canvasWidth * 0.3f, 420f, 640f), 72f), new Vector2(0f, 24f));
        }

        private static void LayoutSidebar(TeamSidebarView sidebar, float width, float height)
        {
            StretchToParent(sidebar.Background.rectTransform, 0f);
            StretchToParent(sidebar.Frame.rectTransform, 0f);
            SetRect(sidebar.HeaderRibbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(width - 26f, 44f), new Vector2(0f, -10f));
            StretchToParent(sidebar.HeaderText.rectTransform, new Vector4(18f, 10f, 18f, 0f));

            var topPadding = 60f;
            var bottomPadding = 12f;
            var spacing = 10f;
            var cardHeight = Mathf.Max(102f, (height - topPadding - bottomPadding - (spacing * (BattleInputConfig.DefaultTeamSize - 1))) / BattleInputConfig.DefaultTeamSize);

            for (var i = 0; i < sidebar.Cards.Count; i++)
            {
                var card = sidebar.Cards[i];
                var yOffset = topPadding + (i * (cardHeight + spacing));
                SetRect(card.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, cardHeight), new Vector2(0f, -yOffset));
                LayoutHeroCard(card);
            }
        }

        private static void LayoutHeroCard(HeroCardView card)
        {
            StretchToParent(card.Background.rectTransform, 0f);
            StretchToParent(card.Border.rectTransform, 0f);
            SetRect(card.AccentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), Vector2.zero);
            SetRect(card.PortraitRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, 78f), new Vector2(10f, -10f));
            StretchToParent(card.PortraitBackdrop.rectTransform, 0f);
            StretchToParent(card.PortraitImage.rectTransform, new Vector4(6f, 6f, 6f, 6f));
            StretchToParent(card.PortraitFrame.rectTransform, 0f);
            StretchToParent(card.PortraitFallbackText.rectTransform, new Vector4(6f, 6f, 6f, 6f));

            var contentX = 98f;
            SetRect(card.NameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-contentX - 12f, 22f), new Vector2(contentX, -8f));
            SetRect(card.MetaText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-contentX - 12f, 16f), new Vector2(contentX, -30f));
            SetRect(card.HealthText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-contentX - 12f, 16f), new Vector2(contentX, -44f));
            SetRect(card.HealthBarRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-contentX - 12f, 10f), new Vector2(contentX, -60f));
            SetRect(card.ShieldBarRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-contentX - 12f, 4f), new Vector2(contentX, -74f));
            SetRect(card.StatText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(-contentX - 12f, 16f), new Vector2(contentX, 28f));
            SetRect(card.SkillChipBackground.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(74f, 18f), new Vector2(contentX, 8f));
            SetRect(card.SkillChipText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(74f, 18f), new Vector2(contentX, 8f));
            SetRect(card.UltimateChipBackground.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(80f, 18f), new Vector2(-12f, 8f));
            SetRect(card.UltimateChipText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(80f, 18f), new Vector2(-12f, 8f));
            SetRect(card.ControlChipBackground.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(52f, 16f), new Vector2(10f, 8f));
            SetRect(card.ControlChipText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(52f, 16f), new Vector2(10f, 8f));
            StretchToParent(card.DeadOverlay.rectTransform, 0f);
            SetRect(card.DeadIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 30f), new Vector2(0f, 10f));
            SetRect(card.RespawnText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-24f, 24f), new Vector2(0f, 12f));
        }
    }
}
