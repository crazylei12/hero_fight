using System.Collections.Generic;
using DG.Tweening;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public partial class BattleCanvasHud : MonoBehaviour
    {
        private const float LabelWorldYOffset = 1.05f;
        private static readonly string[] PreferredFontNames =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "PingFang SC",
            "SimHei",
        };

        [Header("Theme")]
        [SerializeField] private string themeResourcePath = "UI/BattleHudTheme";

        [Header("Colors")]
        [SerializeField] private Color blueColor = new Color(0.2f, 0.47f, 0.92f, 1f);
        [SerializeField] private Color redColor = new Color(0.9f, 0.3f, 0.27f, 1f);
        [SerializeField] private Color panelBackground = new Color(0.07f, 0.09f, 0.13f, 0.94f);
        [SerializeField] private Color softPanelBackground = new Color(0.1f, 0.12f, 0.17f, 0.92f);
        [SerializeField] private Color mutedTextColor = new Color(0.79f, 0.84f, 0.9f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.93f, 0.27f, 0.22f, 1f);
        [SerializeField] private Color healthyColor = new Color(0.22f, 0.87f, 0.44f, 1f);
        [SerializeField] private Color shieldColor = new Color(1f, 0.86f, 0.32f, 1f);
        [SerializeField] private Color deadOverlayColor = new Color(0f, 0f, 0f, 0.5f);

        private readonly List<RuntimeHero> blueHeroes = new List<RuntimeHero>(BattleInputConfig.DefaultTeamSize);
        private readonly List<RuntimeHero> redHeroes = new List<RuntimeHero>(BattleInputConfig.DefaultTeamSize);
        private readonly Dictionary<string, NameplateView> nameplates = new Dictionary<string, NameplateView>(BattleInputConfig.DefaultTeamSize * 2);

        private BattleManager battleManager;
        private BattleEventBus boundEventBus;
        private Camera battleCamera;
        private BattleHudTheme theme;
        private Font uiFont;

        private RectTransform canvasRoot;
        private Canvas overlayCanvas;
        private CanvasScaler canvasScaler;
        private CanvasGroup canvasGroup;
        private TopBarView topBar;
        private TeamSidebarView blueSidebar;
        private TeamSidebarView redSidebar;
        private RectTransform nameplateLayer;
        private EndBannerView endBanner;

        private string endBannerText = string.Empty;
        private bool introAnimationPlayed;
        private bool themeWarningLogged;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private int lastBlueKills = -1;
        private int lastRedKills = -1;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            if (battleManager != null)
            {
                battleManager.ContextInitialized += OnContextInitialized;
            }

            theme = Resources.Load<BattleHudTheme>(themeResourcePath);
            if (theme == null && !themeWarningLogged)
            {
                themeWarningLogged = true;
                Debug.LogWarning($"BattleCanvasHud could not find theme at Resources/{themeResourcePath}. Falling back to plain UI shapes.");
            }

            uiFont = CreatePreferredFont();
            EnsureHudCanvas();
        }

        private void Update()
        {
            EnsureHudCanvas();
            BindToBattleEvents(battleManager != null ? battleManager.Context?.EventBus : null);

            if (battleCamera == null || !battleCamera.isActiveAndEnabled)
            {
                battleCamera = FindFirstObjectByType<Camera>();
            }

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                ApplyResponsiveLayout();
            }

            var context = battleManager != null ? battleManager.Context : null;
            if (context == null)
            {
                UpdateIdlePresentation();
                return;
            }

            RefreshHeroLists(context);
            UpdateTopBar(context);
            UpdateSidebar(blueSidebar, blueHeroes, TeamSide.Blue, blueColor);
            UpdateSidebar(redSidebar, redHeroes, TeamSide.Red, redColor);
            UpdateNameplates(context);
            UpdateEndBanner();
        }

        private void OnDisable()
        {
            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
                boundEventBus = null;
            }
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.ContextInitialized -= OnContextInitialized;
            }

            if (canvasRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(canvasRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(canvasRoot.gameObject);
                }
            }
        }

        private void OnContextInitialized(BattleContext context)
        {
            BindToBattleEvents(context?.EventBus);
            ClearNameplates();
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

        private void OnBattleEvent(IBattleEvent battleEvent)
        {
            if (battleEvent is BattleStartedEvent)
            {
                endBannerText = string.Empty;
                lastBlueKills = -1;
                lastRedKills = -1;
                ClearNameplates();
                PlayIntroAnimation(forceReplay: true);
                return;
            }

            if (battleEvent is ScoreChangedEvent scoreChangedEvent)
            {
                if (topBar != null)
                {
                    if (lastBlueKills >= 0 && scoreChangedEvent.BlueKills != lastBlueKills)
                    {
                        PunchScore(topBar.BlueScoreText);
                    }

                    if (lastRedKills >= 0 && scoreChangedEvent.RedKills != lastRedKills)
                    {
                        PunchScore(topBar.RedScoreText);
                    }
                }

                lastBlueKills = scoreChangedEvent.BlueKills;
                lastRedKills = scoreChangedEvent.RedKills;
                return;
            }

            if (battleEvent is BattleEndedEvent endedEvent)
            {
                endBannerText = BuildEndBannerText(endedEvent.Result);
                ShowEndBanner();
            }
        }

        private Font CreatePreferredFont()
        {
            for (var i = 0; i < PreferredFontNames.Length; i++)
            {
                try
                {
                    var font = Font.CreateDynamicFontFromOSFont(PreferredFontNames[i], 16);
                    if (font != null)
                    {
                        return font;
                    }
                }
                catch
                {
                    // Ignore unavailable OS fonts and keep trying the next candidate.
                }
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
