using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using Fight.UI.Presentation.Statuses;
using Fight.UI.Presentation.Skills;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleView : MonoBehaviour
    {
        private const int HeroSortBase = 1200;
        private const int SkillAreaCircleSortOrder = 360;
        private const int SkillAreaEffectSortOrder = 340;
        private const int DamageShareGroundSortOrder = SkillAreaCircleSortOrder + 12;
        private const string ArenaRootName = "BattleArena2D";
        private const string StunStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/StunStatusLoop";
        private const string KnockUpStatusBurstVfxResourcesPath = "Stage01Demo/VFX/Statuses/KnockUpStatusBurst";
        private const string KnockbackStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/KnockbackStatusLoop";
        private const string TauntStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/TauntStatusLoop";
        private const string AttackPowerUpStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/AttackPowerUpStatusLoop";
        private const string AttackPowerDownStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/AttackPowerDownStatusLoop";
        private const string DefenseUpStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/DefenseUpStatusLoop";
        private const string DefenseDownStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/DefenseDownStatusLoop";
        private const string AttackSpeedUpStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/AttackSpeedUpStatusLoop";
        private const string AttackSpeedDownStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/AttackSpeedDownStatusLoop";
        private const string MoveSpeedUpStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/MoveSpeedUpStatusLoop";
        private const string MoveSpeedDownStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/MoveSpeedDownStatusLoop";
        private const string DamageShareStatusLoopVfxResourcesPath = "Stage01Demo/VFX/Statuses/DamageShareStatusLoop";
        private const string HealReceivedImpactVfxResourcesPath = "Stage01Demo/VFX/Shared/HealReceivedImpact";
        private const string DashChargeTrailVfxResourcesPath = "Stage01Demo/VFX/Shared/DashChargeTrail";
        private const float CorpseVisibleSeconds = 1f;
        private const float HealthBarWidth = 0.9f;
        private const float HealthBarBackgroundHeight = 0.11f;
        private const float HealthBarFillHeight = 0.07f;
        private const float ArenaBackgroundHeight = Stage01ArenaSpec.HeightWorldUnits;
        private const float MinAirborneEffectHeight = 0.12f;
        private const float DefaultTransientVfxLifetime = 1f;
        private const float InstantBlinkDurationThreshold = 0.01f;
        private const float InstantBlinkMinDistance = 0.15f;
        private const float DashChargeMinDistance = 0.2f;
        private const float DashChargeLifetimePaddingSeconds = 0.08f;
        private const float DashChargeMinLifetimeSeconds = 0.18f;
        private const int HealEventVfxSortOrderOffset = 190;
        private const int DashChargeSortOrderOffset = -6;
        private const int SkillTargetIndicatorSortOrderOffset = 214;
        private const int SkillCastImpactVfxSortOrderOffset = 212;
        private const int ReactiveGuardLoopSortOrderOffset = 178;
        private const int ReactiveGuardTriggerSortOrderOffset = 206;
        private const int ThrownProjectileSortOrderOffset = -4;
        private const string HealImpactTransientKey = "heal_received";
        private const string DashChargeTransientKey = "dash_charge";
        private static readonly Dictionary<StatusEffectType, StatusEffectVfxConfig> StatusEffectVfxConfigs = new Dictionary<StatusEffectType, StatusEffectVfxConfig>
        {
            { StatusEffectType.Stun, new StatusEffectVfxConfig(StunStatusLoopVfxResourcesPath, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.85f, Vector3.zero, 180) },
            { StatusEffectType.KnockUp, new StatusEffectVfxConfig(KnockUpStatusBurstVfxResourcesPath, new Vector3(0f, 0.74f, 0f), Vector3.one * 0.9f, Vector3.zero, 165) },
            { StatusEffectType.Taunt, new StatusEffectVfxConfig(TauntStatusLoopVfxResourcesPath, new Vector3(0f, 1.2f, 0f), Vector3.one * 0.92f, Vector3.zero, 186) },
            { StatusEffectType.DamageShare, new StatusEffectVfxConfig(DamageShareStatusLoopVfxResourcesPath, Vector3.zero, Vector3.one, Vector3.zero, -4) },
        };
        private static readonly StatusEffectVfxConfig KnockbackStatusVfxConfig = new StatusEffectVfxConfig(
            KnockbackStatusLoopVfxResourcesPath,
            new Vector3(0f, 0.72f, 0f),
            Vector3.one * 0.92f,
            Vector3.zero,
            170,
            alignToDirection: true);
        private static readonly StatusEffectVfxConfig AttackPowerUpStatusVfxConfig = CreateStatModifierStatusVfxConfig(AttackPowerUpStatusLoopVfxResourcesPath);
        private static readonly StatusEffectVfxConfig AttackPowerDownStatusVfxConfig = new StatusEffectVfxConfig(
            AttackPowerDownStatusLoopVfxResourcesPath,
            new Vector3(0f, 0.64f, 0f),
            Vector3.one,
            Vector3.zero,
            176);
        private static readonly StatusEffectVfxConfig DefenseUpStatusVfxConfig = CreateStatModifierStatusVfxConfig(DefenseUpStatusLoopVfxResourcesPath);
        private static readonly StatusEffectVfxConfig DefenseDownStatusVfxConfig = new StatusEffectVfxConfig(
            DefenseDownStatusLoopVfxResourcesPath,
            new Vector3(0f, 0.64f, 0f),
            Vector3.one,
            Vector3.zero,
            176);
        private static readonly StatusEffectVfxConfig AttackSpeedUpStatusVfxConfig = CreateStatModifierStatusVfxConfig(AttackSpeedUpStatusLoopVfxResourcesPath);
        private static readonly StatusEffectVfxConfig AttackSpeedDownStatusVfxConfig = CreateStatModifierStatusVfxConfig(AttackSpeedDownStatusLoopVfxResourcesPath);
        private static readonly StatusEffectVfxConfig MoveSpeedUpStatusVfxConfig = CreateStatModifierStatusVfxConfig(MoveSpeedUpStatusLoopVfxResourcesPath);
        private static readonly StatusEffectVfxConfig MoveSpeedDownStatusVfxConfig = CreateStatModifierStatusVfxConfig(MoveSpeedDownStatusLoopVfxResourcesPath);
        private static readonly Dictionary<StatusEffectType, SignedStatusEffectVfxConfig> SignedStatusEffectVfxConfigs = new Dictionary<StatusEffectType, SignedStatusEffectVfxConfig>
        {
            { StatusEffectType.AttackPowerModifier, new SignedStatusEffectVfxConfig(AttackPowerUpStatusVfxConfig, AttackPowerDownStatusVfxConfig) },
            { StatusEffectType.DefenseModifier, new SignedStatusEffectVfxConfig(DefenseUpStatusVfxConfig, DefenseDownStatusVfxConfig) },
            { StatusEffectType.AttackSpeedModifier, new SignedStatusEffectVfxConfig(AttackSpeedUpStatusVfxConfig, AttackSpeedDownStatusVfxConfig) },
            { StatusEffectType.MoveSpeedModifier, new SignedStatusEffectVfxConfig(MoveSpeedUpStatusVfxConfig, MoveSpeedDownStatusVfxConfig) },
        };
        [SerializeField] private float heroMarkerScale = 1f;
        [SerializeField] private float prefabVisualScale = 0.9f;
        [SerializeField] private Vector3 footUiOffset = new Vector3(0f, -0.36f, 0f);
        [SerializeField] private Color blueColor = new Color(0.19f, 0.58f, 0.95f);
        [SerializeField] private Color redColor = new Color(0.9f, 0.33f, 0.29f);
        [SerializeField] private Color deadColor = new Color(0.45f, 0.45f, 0.48f);
        [SerializeField] private Color prefabHitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float prefabHitFlashDuration = 0.16f;
        [SerializeField] private float prefabHitFlashStrength = 0.82f;
        [SerializeField] private Color shieldBarColor = new Color(1f, 0.9f, 0.42f, 0.98f);
        [SerializeField] private Color projectileTint = new Color(1f, 0.91f, 0.47f);
        [SerializeField] private Color skillAreaTint = new Color(0.98f, 0.42f, 0.24f, 0.22f);
        [SerializeField] private float skillAreaPulseStrength = 0.12f;
        [SerializeField] private float skillAreaPulseWindowSeconds = 0.28f;
        [SerializeField] private float skillAreaExpiryFadeSeconds = 0.3f;
        [SerializeField] private float airborneUiFollowFactor = 0.35f;
        [SerializeField] private float shadowAirborneScaleMultiplier = 0.68f;
        [SerializeField] private float shadowAirborneAlphaMultiplier = 0.52f;
        [SerializeField] private float launchPulseDuration = 0.18f;
        [SerializeField] private float landingPulseDuration = 0.22f;
        [SerializeField] private float directionalTrailFadeOutSeconds = 0.16f;
        [SerializeField] private float directionalTrailMinDistance = 0.15f;
        [SerializeField] private float directionalTrailMaxLength = 0.92f;
        [SerializeField] private float damageShareGroundYOffset = -0.18f;
        [SerializeField] private float blinkGhostLifetimeSeconds = 0.18f;
        [SerializeField] private float blinkRevealDurationSeconds = 0.14f;
        [SerializeField] private float blinkRevealStartAlpha = 0.22f;
        [SerializeField] private float blinkRevealStartScale = 0.96f;
        [SerializeField] private float blinkGhostStartAlpha = 0.48f;
        [SerializeField] private float blinkGhostEndScale = 1.05f;
        [SerializeField] private float blinkGhostTintStrength = 0.62f;
        [SerializeField] private Color blinkGhostTintColor = new Color(0.82f, 0.88f, 0.96f, 1f);
        [SerializeField] private Color blinkRevealTintColor = new Color(0.86f, 0.9f, 0.96f, 1f);
        [SerializeField] private float blinkRevealTintStrength = 0.5f;
        [SerializeField] private Color airborneGroundRingColor = new Color(1f, 0.87f, 0.56f, 0.22f);
        [SerializeField] private Color launchPulseColor = new Color(1f, 0.95f, 0.8f, 0.72f);
        [SerializeField] private Color landingPulseColor = new Color(1f, 0.68f, 0.34f, 0.78f);
        [SerializeField] private Color directionalTrailColor = new Color(1f, 0.94f, 0.78f, 0.5f);
        [SerializeField] private bool autoCreateArena = true;
        [SerializeField] private string arenaBackgroundResourcesPath = "Battle/jjc_background";
        [SerializeField] private string arenaBackgroundProjectRelativePath = "Assets/Resources/Battle/jjc_background.png";

        private BattleManager battleManager;
        private readonly Dictionary<string, HeroViewState> heroViews = new Dictionary<string, HeroViewState>();
        private readonly Dictionary<string, HeroBattleAnimationDriver> heroAnimationDrivers = new Dictionary<string, HeroBattleAnimationDriver>();
        private readonly Dictionary<string, ProjectileViewState> projectileViews = new Dictionary<string, ProjectileViewState>();
        private readonly Dictionary<string, SkillAreaViewState> skillAreaViews = new Dictionary<string, SkillAreaViewState>();
        private Transform heroRoot;
        private Transform projectileRoot;
        private Transform skillAreaRoot;
        private Transform persistentGroundVfxRoot;
        private Transform transientWorldVfxRoot;
        private BattleEventBus subscribedEventBus;

        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite customArenaBackgroundSprite;
        private static string customArenaBackgroundSourcePath;
        private static GameObject sharedHealImpactPrefab;
        private static GameObject sharedDashChargePrefab;

        protected BattleManager BattleManager => battleManager;

        private sealed class HeroViewState
        {
            public GameObject Root;
            public SortingGroup SortingGroup;
            public Transform VisualRoot;
            public Transform ProjectileLaunchAnchor;
            public Renderer[] VisualRenderers;
            public SpriteRenderer[] VisualSpriteRenderers;
            public Color[] VisualSpriteBaseColors;
            public SpriteRenderer Shadow;
            public SpriteRenderer Halo;
            public SpriteRenderer Body;
            public SpriteRenderer Accent;
            public SpriteRenderer AirborneRing;
            public SpriteRenderer ImpactPulse;
            public SpriteRenderer DirectionalTrail;
            public Transform FootUiRoot;
            public Transform StatusEffectRoot;
            public SpriteRenderer HealthBack;
            public SpriteRenderer HealthFill;
            public SpriteRenderer ShieldFill;
            public SpriteRenderer UltimateIcon;
            public HeroBattleAnimationDriver AnimationDriver;
            public Vector3 ShadowBaseScale = Vector3.one;
            public Color ShadowBaseColor = Color.white;
            public Vector3 AirborneRingBaseScale = Vector3.one;
            public Vector3 ImpactPulseBaseScale = Vector3.one;
            public float LastVisualHeight;
            public float LastForcedMovementPeakHeight;
            public float LastForcedMovementHorizontalDistance;
            public Vector2 LastForcedMovementDirection;
            public float DirectionalTrailUntilSeconds = -1f;
            public float ImpactPulseStartedAtSeconds = -1f;
            public float ImpactPulseDurationSeconds;
            public Color ImpactPulseBaseColor = Color.white;
            public bool WasAirborne;
            public bool LastDeadState;
            public float DeathStartedAtSeconds = -1f;
            public float HitFlashUntilSeconds = -1f;
            public float BlinkRevealStartedAtSeconds = -1f;
            public StatusEffectViewState ForcedMovementStatusView;
            public StatusEffectViewState DamageShareSourceView;
            public readonly Dictionary<StatusEffectType, StatusEffectViewState> StatusEffectViews = new Dictionary<StatusEffectType, StatusEffectViewState>();
            public readonly Dictionary<string, StatusEffectViewState> ReactiveGuardViews = new Dictionary<string, StatusEffectViewState>();
            public readonly List<TransientHeroVfxState> TransientVfx = new List<TransientHeroVfxState>();
        }

        private sealed class SkillAreaViewState
        {
            public SpriteRenderer Renderer;
            public float LastPulseAtSeconds;
            public GameObject EffectInstance;
            public SortingGroup EffectSortingGroup;
            public Animator[] EffectAnimators;
            public ParticleSystem[] EffectParticleSystems;
            public Renderer[] EffectRenderers;
            public Vector3 BaseEffectScale = Vector3.one;
            public SkillAreaPresentationController CustomController;
        }

        private sealed class ProjectileViewState
        {
            public GameObject Root;
            public SpriteRenderer FallbackRenderer;
            public SortingGroup SortingGroup;
            public Renderer[] Renderers;
            public Vector3 LastPosition;
            public bool HasLastPosition;
        }

        private sealed class StatusEffectViewState
        {
            public GameObject Root;
            public SortingGroup SortingGroup;
            public OrbitingStatusIconVfx OrbitController;
            public string LoadedPrefabResourcesPath;
            public Renderer[] Renderers;
            public ParticleSystem[] ParticleSystems;
            public Vector3 BaseLocalPosition;
            public Vector3 BaseLocalScale = Vector3.one;
            public Quaternion BaseLocalRotation = Quaternion.identity;
        }

        private sealed class SignedStatusEffectVfxConfig
        {
            public SignedStatusEffectVfxConfig(StatusEffectVfxConfig positiveConfig, StatusEffectVfxConfig negativeConfig)
            {
                PositiveConfig = positiveConfig;
                NegativeConfig = negativeConfig;
            }

            public StatusEffectVfxConfig PositiveConfig { get; }

            public StatusEffectVfxConfig NegativeConfig { get; }
        }

        private sealed class TransientHeroVfxState
        {
            public GameObject Root;
            public SortingGroup SortingGroup;
            public Renderer[] Renderers;
            public ParticleSystem[] ParticleSystems;
            public string UniqueKey;
            public int SortingOrderOffset;
            public float ExpiresAtSeconds;
        }

        private sealed class StatusEffectVfxConfig
        {
            private GameObject cachedLoopPrefab;

            public StatusEffectVfxConfig(
                string loopPrefabResourcesPath,
                Vector3 localOffset,
                Vector3 localScale,
                Vector3 localEulerAngles,
                int sortingOrderOffset,
                bool alignToDirection = false)
            {
                LoopPrefabResourcesPath = loopPrefabResourcesPath;
                LocalOffset = localOffset;
                LocalScale = localScale;
                LocalEulerAngles = localEulerAngles;
                SortingOrderOffset = sortingOrderOffset;
                AlignToDirection = alignToDirection;
            }

            public string LoopPrefabResourcesPath { get; }

            public Vector3 LocalOffset { get; }

            public Vector3 LocalScale { get; }

            public Vector3 LocalEulerAngles { get; }

            public int SortingOrderOffset { get; }

            public bool AlignToDirection { get; }

            public GameObject LoadLoopPrefab()
            {
                if (cachedLoopPrefab == null && !string.IsNullOrWhiteSpace(LoopPrefabResourcesPath))
                {
                    cachedLoopPrefab = Resources.Load<GameObject>(LoopPrefabResourcesPath);
                }

                return cachedLoopPrefab;
            }
        }

        protected virtual void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            heroRoot = new GameObject("BattleHeroViews").transform;
            heroRoot.SetParent(transform, false);
            projectileRoot = new GameObject("BattleProjectileViews").transform;
            projectileRoot.SetParent(transform, false);
            skillAreaRoot = new GameObject("BattleSkillAreaViews").transform;
            skillAreaRoot.SetParent(transform, false);
            persistentGroundVfxRoot = new GameObject("BattlePersistentGroundVfx").transform;
            persistentGroundVfxRoot.SetParent(transform, false);
            transientWorldVfxRoot = new GameObject("BattleTransientWorldVfx").transform;
            transientWorldVfxRoot.SetParent(transform, false);

            EnsureSprites();
            if (autoCreateArena)
            {
                EnsureArena();
            }
        }

        protected virtual void LateUpdate()
        {
            var context = battleManager.Context;
            if (context == null)
            {
                return;
            }

            EnsureEventSubscription(context);
            for (var i = 0; i < context.Heroes.Count; i++)
            {
                SyncHero(context.Heroes[i]);
            }

            SyncProjectiles(context);
            SyncSkillAreas(context);
        }

        private void SyncHero(RuntimeHero hero)
        {
            if (!heroViews.TryGetValue(hero.RuntimeId, out var view))
            {
                view = CreateHeroView(hero);
                heroViews.Add(hero.RuntimeId, view);
            }

            var pos = Map(hero.CurrentPosition);
            view.Root.transform.position = pos;
            var heroSortingOrder = Sort(pos.y, 0);
            view.SortingGroup.sortingOrder = heroSortingOrder;
            var airborneOffset = new Vector3(0f, hero.VisualHeightOffset, 0f);
            view.VisualRoot.localPosition = airborneOffset;
            if (view.FootUiRoot != null)
            {
                view.FootUiRoot.localPosition = footUiOffset + (airborneOffset * airborneUiFollowFactor);
            }
            UpdateDeathVisibility(hero, view);
            var blinkRevealProgress = GetBlinkRevealProgress(hero, view);
            var blinkRevealScale = hero.IsDead
                ? 1f
                : Mathf.Lerp(blinkRevealStartScale, 1f, blinkRevealProgress);
            view.VisualRoot.localScale = Vector3.one * ((hero.IsDead ? 0.82f : 1f) * blinkRevealScale);

            if (view.Body != null)
            {
                view.Body.color = hero.IsDead ? deadColor : Team(hero.Side);
            }

            if (view.Halo != null)
            {
                var halo = Team(hero.Side);
                halo.a = hero.IsDead
                    ? (ShouldHideCorpse(view) ? 0f : 0.18f)
                    : Mathf.Lerp(0.28f, 0.78f, blinkRevealProgress);
                view.Halo.color = halo;
            }

            if (view.Shadow != null)
            {
                var shadow = view.Shadow.color;
                shadow.a = hero.IsDead
                    ? (ShouldHideCorpse(view) ? 0f : 0.12f)
                    : Mathf.Lerp(0.08f, 0.24f, blinkRevealProgress);
                view.Shadow.color = shadow;
            }

            var healthRatio = hero.MaxHealth > 0f
                ? Mathf.Clamp01(hero.CurrentHealth / hero.MaxHealth)
                : 0f;

            if (view.HealthFill != null)
            {
                var healthColor = hero.Side == TeamSide.Blue
                    ? new Color(0.44f, 0.86f, 0.34f)
                    : new Color(0.92f, 0.29f, 0.24f);
                UpdateHealthFill(view.HealthFill, healthRatio, healthColor);
            }

            if (view.ShieldFill != null)
            {
                var shieldRatio = hero.MaxHealth > 0f
                    ? Mathf.Max(0f, StatusEffectSystem.GetTotalShield(hero) / hero.MaxHealth)
                    : 0f;
                UpdateShieldFill(view.ShieldFill, healthRatio, shieldRatio, shieldBarColor);
            }

            if (view.UltimateIcon != null)
            {
                var ultimateColor = hero.HasCastUltimate
                    ? new Color(0.26f, 0.26f, 0.29f, 0.55f)
                    : new Color(1f, 0.9f, 0.36f, 0.98f);
                view.UltimateIcon.color = ultimateColor;
            }

            UpdateForcedMovementPresentation(hero, view);
            UpdatePrefabHitFlash(hero, view);
            SyncForcedMovementStatusVfx(hero, view, heroSortingOrder);
            SyncStatusEffects(hero, view, heroSortingOrder);
            SyncDamageShareSourceVfx(hero, view);
            SyncReactiveGuardVfx(hero, view, heroSortingOrder);
            SyncTransientVfx(hero, view, heroSortingOrder);

            if (view.AnimationDriver != null)
            {
                view.AnimationDriver.Sync(hero);
            }
        }

        private HeroViewState CreateHeroView(RuntimeHero hero)
        {
            var view = new HeroViewState();
            view.Root = new GameObject($"{hero.Definition.displayName}_{hero.Side}");
            view.Root.transform.SetParent(heroRoot, false);
            view.SortingGroup = view.Root.AddComponent<SortingGroup>();
            view.Shadow = MakeSprite("Shadow", view.Root.transform, circleSprite, new Color(0f, 0f, 0f, 0.24f), -10, new Vector3(0f, -0.22f, 0f), new Vector3(0.92f, 0.36f, 1f) * heroMarkerScale);
            view.Halo = MakeSprite("Halo", view.Root.transform, circleSprite, Team(hero.Side), -9, new Vector3(0f, -0.08f, 0f), new Vector3(1.08f, 0.56f, 1f) * heroMarkerScale);
            view.ShadowBaseScale = view.Shadow.transform.localScale;
            view.ShadowBaseColor = view.Shadow.color;
            view.AirborneRing = MakeSprite("AirborneRing", view.Root.transform, circleSprite, new Color(1f, 1f, 1f, 0f), -8, new Vector3(0f, -0.18f, 0f), new Vector3(1.18f, 0.58f, 1f) * heroMarkerScale);
            view.AirborneRingBaseScale = view.AirborneRing.transform.localScale;
            view.ImpactPulse = MakeSprite("ImpactPulse", view.Root.transform, circleSprite, new Color(1f, 1f, 1f, 0f), -7, new Vector3(0f, -0.18f, 0f), new Vector3(0.72f, 0.34f, 1f) * heroMarkerScale);
            view.ImpactPulseBaseScale = view.ImpactPulse.transform.localScale;
            view.VisualRoot = new GameObject("Visual").transform;
            view.VisualRoot.SetParent(view.Root.transform, false);
            view.StatusEffectRoot = new GameObject("StatusVfx").transform;
            view.StatusEffectRoot.SetParent(view.VisualRoot, false);
            view.DirectionalTrail = MakeSprite("DirectionalTrail", view.VisualRoot, squareSprite, new Color(1f, 1f, 1f, 0f), 19, new Vector3(0f, -0.06f, 0f), new Vector3(0.18f, 0.18f, 1f));

            if (hero.Definition.visualConfig.battlePrefab != null)
            {
                var visual = Instantiate(hero.Definition.visualConfig.battlePrefab, view.VisualRoot);
                visual.name = $"{hero.Definition.displayName}_Visual";
                visual.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                visual.transform.localScale *= prefabVisualScale;
                DisablePrefabBehaviours(visual);
                RemovePrefabPhysics(visual);

                var driver = TryCreateAnimationDriver(view.Root, hero, visual);
                if (driver.IsReady)
                {
                    view.AnimationDriver = driver;
                    heroAnimationDrivers[hero.RuntimeId] = driver;
                }
                else
                {
                    Destroy(driver);
                }

                view.ProjectileLaunchAnchor = FindPreferredVisualAnchor(visual.transform, "HandR", "HandL", "Handle");
            }
            else
            {
                view.Body = MakeSprite("Body", view.VisualRoot, circleSprite, Team(hero.Side), 20, Vector3.zero, Vector3.one * (0.82f * heroMarkerScale));
                view.Accent = MakeSprite("Accent", view.VisualRoot, squareSprite, Accent(hero.Definition.heroClass), 21, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.22f, 1f));
                view.Accent.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            view.ProjectileLaunchAnchor ??= view.VisualRoot;

            view.VisualRenderers = view.VisualRoot.GetComponentsInChildren<Renderer>(true);
            view.VisualSpriteRenderers = view.VisualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            if (view.VisualSpriteRenderers != null && view.VisualSpriteRenderers.Length > 0)
            {
                view.VisualSpriteBaseColors = new Color[view.VisualSpriteRenderers.Length];
                for (var i = 0; i < view.VisualSpriteRenderers.Length; i++)
                {
                    view.VisualSpriteBaseColors[i] = view.VisualSpriteRenderers[i] != null
                        ? view.VisualSpriteRenderers[i].color
                        : Color.white;
                }
            }

            view.FootUiRoot = new GameObject("FootUi").transform;
            view.FootUiRoot.SetParent(view.Root.transform, false);
            view.FootUiRoot.localPosition = footUiOffset;
            view.HealthBack = MakeSprite("HealthBack", view.FootUiRoot, squareSprite, new Color(0.08f, 0.1f, 0.12f, 0.92f), 300, Vector3.zero, new Vector3(HealthBarWidth, HealthBarBackgroundHeight, 1f));
            view.HealthFill = MakeSprite("HealthFill", view.FootUiRoot, squareSprite, Color.green, 301, Vector3.zero, new Vector3(HealthBarWidth, HealthBarFillHeight, 1f));
            UpdateHealthFill(view.HealthFill, 1f, Color.green);
            view.ShieldFill = MakeSprite("ShieldFill", view.FootUiRoot, squareSprite, shieldBarColor, 302, Vector3.zero, new Vector3(0f, HealthBarFillHeight, 1f));
            UpdateShieldFill(view.ShieldFill, 1f, 0f, shieldBarColor);
            view.UltimateIcon = MakeSprite("UltimateIcon", view.FootUiRoot, squareSprite, new Color(1f, 0.9f, 0.36f, 0.98f), 303, new Vector3(-0.58f, 0f, 0f), new Vector3(0.16f, 0.16f, 1f));
            view.UltimateIcon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return view;
        }

        private static HeroBattleAnimationDriver TryCreateAnimationDriver(GameObject host, RuntimeHero hero, GameObject visual)
        {
            var heroEditorDriver = host.AddComponent<HeroEditor4DBattleAnimationDriver>();
            heroEditorDriver.Initialize(hero, visual);
            if (heroEditorDriver.IsReady)
            {
                return heroEditorDriver;
            }

            Destroy(heroEditorDriver);

            var genericDriver = host.AddComponent<GenericAnimatorBattleAnimationDriver>();
            genericDriver.Initialize(hero, visual);
            return genericDriver;
        }

        private static void UpdateHealthFill(SpriteRenderer healthFill, float ratio, Color color)
        {
            if (healthFill == null)
            {
                return;
            }

            var clampedRatio = Mathf.Clamp01(ratio);
            var currentWidth = HealthBarWidth * clampedRatio;
            healthFill.transform.localScale = new Vector3(currentWidth, HealthBarFillHeight, 1f);
            healthFill.transform.localPosition = new Vector3((currentWidth - HealthBarWidth) * 0.5f, 0f, 0f);
            healthFill.color = color;
            healthFill.enabled = clampedRatio > 0f;
        }

        private static void UpdateShieldFill(SpriteRenderer shieldFill, float healthRatio, float shieldRatio, Color color)
        {
            if (shieldFill == null)
            {
                return;
            }

            var clampedHealthRatio = Mathf.Clamp01(healthRatio);
            var normalizedShieldRatio = Mathf.Max(0f, shieldRatio);
            var currentWidth = HealthBarWidth * normalizedShieldRatio;
            var healthRightEdge = (-HealthBarWidth * 0.5f) + (HealthBarWidth * clampedHealthRatio);
            shieldFill.transform.localScale = new Vector3(currentWidth, HealthBarFillHeight, 1f);
            shieldFill.transform.localPosition = new Vector3(healthRightEdge + (currentWidth * 0.5f), 0f, 0f);
            shieldFill.color = color;
            shieldFill.enabled = currentWidth > 0f;
        }

        private void UpdateDeathVisibility(RuntimeHero hero, HeroViewState view)
        {
            if (hero == null || view == null)
            {
                return;
            }

            var elapsedTimeSeconds = battleManager != null && battleManager.Context != null && battleManager.Context.Clock != null
                ? battleManager.Context.Clock.ElapsedTimeSeconds
                : Time.time;

            if (hero.IsDead && !view.LastDeadState)
            {
                view.DeathStartedAtSeconds = elapsedTimeSeconds;
            }
            else if (!hero.IsDead)
            {
                view.DeathStartedAtSeconds = -1f;
            }

            view.LastDeadState = hero.IsDead;

            var hideCorpse = ShouldHideCorpse(view);
            SetRendererVisibility(view.VisualRenderers, !hideCorpse);

            if (view.FootUiRoot != null)
            {
                view.FootUiRoot.gameObject.SetActive(!hideCorpse);
            }
        }

        private void UpdatePrefabHitFlash(RuntimeHero hero, HeroViewState view)
        {
            if (hero == null
                || view == null
                || view.VisualSpriteRenderers == null
                || view.VisualSpriteBaseColors == null
                || view.VisualSpriteRenderers.Length != view.VisualSpriteBaseColors.Length)
            {
                return;
            }

            var intensity = 0f;
            if (!hero.IsDead && view.HitFlashUntilSeconds > 0f)
            {
                var remaining = view.HitFlashUntilSeconds - GetElapsedTimeSeconds();
                if (remaining > 0f)
                {
                    intensity = Mathf.Clamp01(remaining / Mathf.Max(0.01f, prefabHitFlashDuration)) * prefabHitFlashStrength;
                }
                else
                {
                    view.HitFlashUntilSeconds = -1f;
                }
            }

            var blinkRevealProgress = GetBlinkRevealProgress(hero, view);
            var blinkRevealBlend = hero.IsDead ? 0f : 1f - blinkRevealProgress;
            var blinkRevealAlpha = hero.IsDead
                ? 1f
                : Mathf.Lerp(blinkRevealStartAlpha, 1f, blinkRevealProgress);

            for (var i = 0; i < view.VisualSpriteRenderers.Length; i++)
            {
                var spriteRenderer = view.VisualSpriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                var baseColor = view.VisualSpriteBaseColors[i];
                var revealedColor = Color.Lerp(baseColor, blinkRevealTintColor, blinkRevealBlend * blinkRevealTintStrength);
                revealedColor.a = baseColor.a * blinkRevealAlpha;
                if (intensity <= 0f)
                {
                    spriteRenderer.color = revealedColor;
                    continue;
                }

                var flashedColor = Color.Lerp(revealedColor, prefabHitFlashColor, intensity);
                flashedColor.a = revealedColor.a;
                spriteRenderer.color = flashedColor;
            }
        }

        private float GetBlinkRevealProgress(RuntimeHero hero, HeroViewState view)
        {
            if (hero == null || view == null || hero.IsDead || view.BlinkRevealStartedAtSeconds < 0f)
            {
                return 1f;
            }

            var elapsed = GetElapsedTimeSeconds() - view.BlinkRevealStartedAtSeconds;
            if (elapsed >= blinkRevealDurationSeconds)
            {
                view.BlinkRevealStartedAtSeconds = -1f;
                return 1f;
            }

            return Mathf.Clamp01(elapsed / Mathf.Max(0.01f, blinkRevealDurationSeconds));
        }

        private void UpdateForcedMovementPresentation(RuntimeHero hero, HeroViewState view)
        {
            if (hero == null || view == null)
            {
                return;
            }

            if (hero.IsDead)
            {
                ResetForcedMovementPresentation(view);
                return;
            }

            var currentHeight = Mathf.Max(0f, hero.VisualHeightOffset);
            var peakHeight = Mathf.Max(view.LastForcedMovementPeakHeight, currentHeight, MinAirborneEffectHeight);
            var airborneRatio = currentHeight > 0f
                ? Mathf.Clamp01(currentHeight / peakHeight)
                : 0f;
            var isAirborne = currentHeight > MinAirborneEffectHeight;

            if (isAirborne)
            {
                view.WasAirborne = true;
            }
            else if (view.WasAirborne && view.LastVisualHeight > MinAirborneEffectHeight)
            {
                view.WasAirborne = false;
                StartImpactPulse(view, landingPulseColor, landingPulseDuration);
                view.LastForcedMovementPeakHeight = 0f;
            }

            UpdateShadowForForcedMovement(view, airborneRatio);
            UpdateAirborneRing(view, airborneRatio);
            UpdateDirectionalTrail(view);
            UpdateImpactPulse(view);
            view.LastVisualHeight = currentHeight;
        }

        private void ResetForcedMovementPresentation(HeroViewState view)
        {
            if (view == null)
            {
                return;
            }

            view.WasAirborne = false;
            view.LastVisualHeight = 0f;
            view.LastForcedMovementPeakHeight = 0f;
            view.LastForcedMovementHorizontalDistance = 0f;
            view.LastForcedMovementDirection = Vector2.zero;
            view.DirectionalTrailUntilSeconds = -1f;
            view.ImpactPulseStartedAtSeconds = -1f;

            if (view.Shadow != null)
            {
                view.Shadow.transform.localScale = view.ShadowBaseScale;
            }

            if (view.AirborneRing != null)
            {
                var ringColor = view.AirborneRing.color;
                ringColor.a = 0f;
                view.AirborneRing.color = ringColor;
            }

            if (view.DirectionalTrail != null)
            {
                var trailColor = view.DirectionalTrail.color;
                trailColor.a = 0f;
                view.DirectionalTrail.color = trailColor;
            }

            if (view.ImpactPulse != null)
            {
                var pulseColor = view.ImpactPulse.color;
                pulseColor.a = 0f;
                view.ImpactPulse.color = pulseColor;
            }
        }

        private void UpdateShadowForForcedMovement(HeroViewState view, float airborneRatio)
        {
            if (view?.Shadow == null)
            {
                return;
            }

            var shadowScaleMultiplier = Mathf.Lerp(1f, shadowAirborneScaleMultiplier, airborneRatio);
            view.Shadow.transform.localScale = new Vector3(
                view.ShadowBaseScale.x * shadowScaleMultiplier,
                view.ShadowBaseScale.y * shadowScaleMultiplier,
                view.ShadowBaseScale.z);

            var shadowColor = view.Shadow.color;
            var alphaMultiplier = Mathf.Lerp(1f, shadowAirborneAlphaMultiplier, airborneRatio);
            shadowColor.a *= alphaMultiplier;
            view.Shadow.color = shadowColor;
        }

        private void UpdateAirborneRing(HeroViewState view, float airborneRatio)
        {
            if (view?.AirborneRing == null)
            {
                return;
            }

            var ringColor = airborneGroundRingColor;
            ringColor.a *= Mathf.SmoothStep(0f, 1f, airborneRatio);
            view.AirborneRing.color = ringColor;
            var scaleMultiplier = Mathf.Lerp(0.9f, 1.18f, airborneRatio);
            view.AirborneRing.transform.localScale = new Vector3(
                view.AirborneRingBaseScale.x * scaleMultiplier,
                view.AirborneRingBaseScale.y * scaleMultiplier,
                view.AirborneRingBaseScale.z);
        }

        private void UpdateDirectionalTrail(HeroViewState view)
        {
            if (view?.DirectionalTrail == null)
            {
                return;
            }

            var elapsedTimeSeconds = GetElapsedTimeSeconds();
            var remainingSeconds = Mathf.Max(0f, view.DirectionalTrailUntilSeconds - elapsedTimeSeconds);
            var horizontalIntensity = directionalTrailMinDistance > Mathf.Epsilon
                ? Mathf.Clamp01(view.LastForcedMovementHorizontalDistance / directionalTrailMinDistance)
                : 0f;
            var fade = directionalTrailFadeOutSeconds > Mathf.Epsilon
                ? Mathf.Clamp01(remainingSeconds / directionalTrailFadeOutSeconds)
                : 0f;

            if (fade <= 0f || horizontalIntensity <= 0f || view.LastForcedMovementDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                var hiddenColor = view.DirectionalTrail.color;
                hiddenColor.a = 0f;
                view.DirectionalTrail.color = hiddenColor;
                return;
            }

            var trailLength = Mathf.Lerp(0.3f, directionalTrailMaxLength, horizontalIntensity);
            var trailWidth = Mathf.Lerp(0.14f, 0.24f, horizontalIntensity);
            var direction = view.LastForcedMovementDirection.normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            view.DirectionalTrail.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            view.DirectionalTrail.transform.localPosition = new Vector3(-direction.x * trailLength * 0.18f, (-direction.y * trailLength * 0.18f) - 0.06f, 0f);
            view.DirectionalTrail.transform.localScale = new Vector3(trailWidth, trailLength, 1f);

            var trailColor = directionalTrailColor;
            trailColor.a *= fade * horizontalIntensity;
            view.DirectionalTrail.color = trailColor;
        }

        private void UpdateImpactPulse(HeroViewState view)
        {
            if (view?.ImpactPulse == null)
            {
                return;
            }

            var elapsedTimeSeconds = GetElapsedTimeSeconds();
            var ageSeconds = elapsedTimeSeconds - view.ImpactPulseStartedAtSeconds;
            if (view.ImpactPulseStartedAtSeconds < 0f || ageSeconds < 0f || ageSeconds > view.ImpactPulseDurationSeconds)
            {
                var hiddenColor = view.ImpactPulse.color;
                hiddenColor.a = 0f;
                view.ImpactPulse.color = hiddenColor;
                return;
            }

            var progress = view.ImpactPulseDurationSeconds > Mathf.Epsilon
                ? Mathf.Clamp01(ageSeconds / view.ImpactPulseDurationSeconds)
                : 1f;
            var scaleMultiplier = Mathf.Lerp(0.8f, 1.6f, progress);
            view.ImpactPulse.transform.localScale = new Vector3(
                view.ImpactPulseBaseScale.x * scaleMultiplier,
                view.ImpactPulseBaseScale.y * scaleMultiplier,
                view.ImpactPulseBaseScale.z);

            var pulseColor = view.ImpactPulseBaseColor;
            pulseColor.a *= 1f - progress;
            view.ImpactPulse.color = pulseColor;
        }

        private void StartImpactPulse(HeroViewState view, Color color, float durationSeconds)
        {
            if (view == null)
            {
                return;
            }

            view.ImpactPulseStartedAtSeconds = GetElapsedTimeSeconds();
            view.ImpactPulseDurationSeconds = Mathf.Max(0.01f, durationSeconds);
            view.ImpactPulseBaseColor = color;
        }

        private bool ShouldHideCorpse(HeroViewState view)
        {
            if (view == null || !view.LastDeadState || view.DeathStartedAtSeconds < 0f)
            {
                return false;
            }

            var elapsedTimeSeconds = battleManager != null && battleManager.Context != null && battleManager.Context.Clock != null
                ? battleManager.Context.Clock.ElapsedTimeSeconds
                : Time.time;
            return elapsedTimeSeconds - view.DeathStartedAtSeconds >= CorpseVisibleSeconds;
        }

        private void SyncStatusEffects(RuntimeHero hero, HeroViewState view, int heroSortingOrder)
        {
            if (hero == null || view == null || view.StatusEffectRoot == null)
            {
                return;
            }

            List<StatusEffectType> activeEffectTypes = null;
            List<StatusEffectViewState> activeOrbitStatusViews = null;
            var activeStatuses = hero.ActiveStatusEffects;
            for (var i = 0; i < activeStatuses.Count; i++)
            {
                var status = activeStatuses[i];
                if (status?.EffectType == StatusEffectType.DamageShare)
                {
                    continue;
                }

                if (status == null || !TryResolveStatusEffectVfxConfig(hero, status, out var config))
                {
                    continue;
                }

                if (!view.StatusEffectViews.TryGetValue(status.EffectType, out var statusView))
                {
                    statusView = CreateStatusEffectView(status.EffectType, view, config);
                    if (statusView == null)
                    {
                        continue;
                    }

                    view.StatusEffectViews.Add(status.EffectType, statusView);
                }
                else if (!string.Equals(statusView.LoadedPrefabResourcesPath, config.LoopPrefabResourcesPath, StringComparison.Ordinal))
                {
                    DestroyStatusEffectView(statusView);
                    statusView = CreateStatusEffectView(status.EffectType, view, config);
                    if (statusView == null)
                    {
                        view.StatusEffectViews.Remove(status.EffectType);
                        continue;
                    }

                    view.StatusEffectViews[status.EffectType] = statusView;
                }

                ApplyStatusEffectView(statusView, config, heroSortingOrder);
                if (statusView.OrbitController != null)
                {
                    activeOrbitStatusViews ??= new List<StatusEffectViewState>();
                    if (!activeOrbitStatusViews.Contains(statusView))
                    {
                        activeOrbitStatusViews.Add(statusView);
                    }
                }

                activeEffectTypes ??= new List<StatusEffectType>();
                if (!activeEffectTypes.Contains(status.EffectType))
                {
                    activeEffectTypes.Add(status.EffectType);
                }
            }

            List<StatusEffectType> staleEffects = null;
            foreach (var pair in view.StatusEffectViews)
            {
                if (activeEffectTypes != null && activeEffectTypes.Contains(pair.Key))
                {
                    continue;
                }

                staleEffects ??= new List<StatusEffectType>();
                staleEffects.Add(pair.Key);
            }

            if (staleEffects != null)
            {
                for (var i = 0; i < staleEffects.Count; i++)
                {
                    var effectType = staleEffects[i];
                    DestroyStatusEffectView(view.StatusEffectViews[effectType]);
                    view.StatusEffectViews.Remove(effectType);
                }
            }

            ApplyOrbitingStatusEffectLayout(activeOrbitStatusViews);
        }

        private void SyncDamageShareSourceVfx(RuntimeHero hero, HeroViewState view)
        {
            if (hero == null || view == null || persistentGroundVfxRoot == null)
            {
                return;
            }

            if (!TryGetOutgoingDamageShareRadius(hero, out var damageShareRadius))
            {
                if (view.DamageShareSourceView != null)
                {
                    DestroyStatusEffectView(view.DamageShareSourceView);
                    view.DamageShareSourceView = null;
                }

                return;
            }

            if (!TryGetStatusEffectVfxConfig(StatusEffectType.DamageShare, out var config))
            {
                return;
            }

            if (view.DamageShareSourceView == null)
            {
                view.DamageShareSourceView = CreateStatusEffectView("DamageShareSource", persistentGroundVfxRoot, config);
                if (view.DamageShareSourceView == null)
                {
                    return;
                }
            }
            else if (!string.Equals(view.DamageShareSourceView.LoadedPrefabResourcesPath, config.LoopPrefabResourcesPath, StringComparison.Ordinal))
            {
                DestroyStatusEffectView(view.DamageShareSourceView);
                view.DamageShareSourceView = CreateStatusEffectView("DamageShareSource", persistentGroundVfxRoot, config);
                if (view.DamageShareSourceView == null)
                {
                    return;
                }
            }

            ApplyWorldStatusEffectView(
                view.DamageShareSourceView,
                config,
                GetDamageShareSourceWorldPosition(hero),
                DamageShareGroundSortOrder);
            ApplyDamageShareSourceScale(view.DamageShareSourceView, damageShareRadius);
        }

        private bool TryGetOutgoingDamageShareRadius(RuntimeHero sourceHero, out float radius)
        {
            radius = 0f;
            if (sourceHero == null)
            {
                return false;
            }

            var context = battleManager != null ? battleManager.Context : null;
            var heroes = context?.Heroes;
            if (heroes == null)
            {
                return false;
            }

            for (var heroIndex = 0; heroIndex < heroes.Count; heroIndex++)
            {
                var candidate = heroes[heroIndex];
                if (candidate == null)
                {
                    continue;
                }

                var activeStatuses = candidate.ActiveStatusEffects;
                for (var statusIndex = 0; statusIndex < activeStatuses.Count; statusIndex++)
                {
                    var status = activeStatuses[statusIndex];
                    if (status == null
                        || status.EffectType != StatusEffectType.DamageShare
                        || status.Source == null
                        || !string.Equals(status.Source.RuntimeId, sourceHero.RuntimeId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    radius = Mathf.Max(radius, ResolveDamageShareDisplayRadius(status));
                }
            }

            return radius > 0f;
        }

        private static float ResolveDamageShareDisplayRadius(RuntimeStatusEffect status)
        {
            if (status?.SourceSkill == null || status.SourceSkill.effects == null)
            {
                return 0f;
            }

            var resolvedRadius = 0f;
            var effects = status.SourceSkill.effects;
            for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                var effect = effects[effectIndex];
                if (effect == null || effect.statusEffects == null || effect.statusEffects.Count == 0)
                {
                    continue;
                }

                var appliesDamageShare = false;
                for (var statusIndex = 0; statusIndex < effect.statusEffects.Count; statusIndex++)
                {
                    var effectStatus = effect.statusEffects[statusIndex];
                    if (effectStatus != null && effectStatus.effectType == StatusEffectType.DamageShare)
                    {
                        appliesDamageShare = true;
                        break;
                    }
                }

                if (!appliesDamageShare)
                {
                    continue;
                }

                var effectRadius = effect.radiusOverride > 0f
                    ? effect.radiusOverride
                    : status.SourceSkill.areaRadius;
                if (effectRadius > resolvedRadius)
                {
                    resolvedRadius = effectRadius;
                }
            }

            return resolvedRadius;
        }

        private static void ApplyDamageShareSourceScale(StatusEffectViewState viewState, float radius)
        {
            if (viewState?.Root == null || radius <= 0f)
            {
                return;
            }

            var statusTransform = viewState.Root.transform;
            statusTransform.localScale = new Vector3(
                statusTransform.localScale.x * radius,
                statusTransform.localScale.y * radius,
                statusTransform.localScale.z);
        }

        private Vector3 GetDamageShareSourceWorldPosition(RuntimeHero hero)
        {
            return Map(hero.CurrentPosition) + new Vector3(0f, damageShareGroundYOffset, 0f);
        }

        private void SyncForcedMovementStatusVfx(RuntimeHero hero, HeroViewState view, int heroSortingOrder)
        {
            if (hero == null || view == null || view.StatusEffectRoot == null)
            {
                return;
            }

            var shouldDisplayKnockbackVfx = hero.IsUnderForcedMovement
                && view.LastForcedMovementHorizontalDistance > directionalTrailMinDistance;

            if (!shouldDisplayKnockbackVfx)
            {
                if (view.ForcedMovementStatusView != null)
                {
                    DestroyStatusEffectView(view.ForcedMovementStatusView);
                    view.ForcedMovementStatusView = null;
                }

                return;
            }

            if (view.ForcedMovementStatusView == null)
            {
                view.ForcedMovementStatusView = CreateStatusEffectView("Knockback", view, KnockbackStatusVfxConfig);
            }

            ApplyStatusEffectView(view.ForcedMovementStatusView, KnockbackStatusVfxConfig, heroSortingOrder, view.LastForcedMovementDirection);
        }

        private void SyncReactiveGuardVfx(RuntimeHero hero, HeroViewState view, int heroSortingOrder)
        {
            if (hero == null || view == null || view.StatusEffectRoot == null)
            {
                return;
            }

            List<string> activeReactiveGuardKeys = null;
            var context = battleManager != null ? battleManager.Context : null;
            var reactiveGuards = context != null ? context.ReactiveGuards : null;
            if (reactiveGuards != null)
            {
                for (var i = 0; i < reactiveGuards.Count; i++)
                {
                    var guard = reactiveGuards[i];
                    if (guard == null
                        || guard.ProtectedHero != hero
                        || guard.IsExpired
                        || guard.TriggersRemaining <= 0
                        || guard.SourceSkill?.reactiveGuard?.guardLoopVfxPrefab == null
                        || !TryBuildReactiveGuardViewKey(guard.Caster, guard.SourceSkill, out var reactiveGuardKey))
                    {
                        continue;
                    }

                    if (!view.ReactiveGuardViews.TryGetValue(reactiveGuardKey, out var guardView))
                    {
                        guardView = CreateAttachedHeroVfxView($"{reactiveGuardKey}_Loop", view, guard.SourceSkill.reactiveGuard.guardLoopVfxPrefab);
                        if (guardView == null)
                        {
                            continue;
                        }

                        view.ReactiveGuardViews.Add(reactiveGuardKey, guardView);
                    }

                    ApplyAttachedHeroVfxView(
                        guardView,
                        guard.SourceSkill.reactiveGuard.guardLoopVfxLocalOffset,
                        guard.SourceSkill.reactiveGuard.guardLoopVfxLocalScale,
                        guard.SourceSkill.reactiveGuard.guardLoopVfxEulerAngles,
                        heroSortingOrder,
                        ReactiveGuardLoopSortOrderOffset);

                    activeReactiveGuardKeys ??= new List<string>();
                    activeReactiveGuardKeys.Add(reactiveGuardKey);
                }
            }

            List<string> staleReactiveGuardKeys = null;
            foreach (var pair in view.ReactiveGuardViews)
            {
                if (activeReactiveGuardKeys != null && activeReactiveGuardKeys.Contains(pair.Key))
                {
                    continue;
                }

                staleReactiveGuardKeys ??= new List<string>();
                staleReactiveGuardKeys.Add(pair.Key);
            }

            if (staleReactiveGuardKeys == null)
            {
                return;
            }

            for (var i = 0; i < staleReactiveGuardKeys.Count; i++)
            {
                var key = staleReactiveGuardKeys[i];
                DestroyStatusEffectView(view.ReactiveGuardViews[key]);
                view.ReactiveGuardViews.Remove(key);
            }
        }

        private StatusEffectViewState CreateStatusEffectView(StatusEffectType effectType, HeroViewState view, StatusEffectVfxConfig config)
        {
            return CreateStatusEffectView(effectType.ToString(), view?.StatusEffectRoot, config);
        }

        private StatusEffectViewState CreateStatusEffectView(string effectName, HeroViewState view, StatusEffectVfxConfig config)
        {
            return CreateStatusEffectView(effectName, view?.StatusEffectRoot, config);
        }

        private StatusEffectViewState CreateStatusEffectView(string effectName, Transform parent, StatusEffectVfxConfig config)
        {
            if (parent == null || config == null)
            {
                return null;
            }

            var prefab = config.LoadLoopPrefab();
            if (prefab == null)
            {
                Debug.LogWarning($"BattleView could not load status VFX prefab for {effectName} from Resources/{config.LoopPrefabResourcesPath}.");
                return null;
            }

            var instance = Instantiate(prefab, parent);
            instance.name = $"{effectName}_StatusVfx";
            RemovePrefabPhysics(instance);

            var state = new StatusEffectViewState
            {
                Root = instance,
                SortingGroup = instance.GetComponent<SortingGroup>(),
                OrbitController = instance.GetComponent<OrbitingStatusIconVfx>(),
                LoadedPrefabResourcesPath = config.LoopPrefabResourcesPath,
                Renderers = instance.GetComponentsInChildren<Renderer>(true),
                ParticleSystems = instance.GetComponentsInChildren<ParticleSystem>(true),
                BaseLocalPosition = instance.transform.localPosition,
                BaseLocalScale = instance.transform.localScale,
                BaseLocalRotation = instance.transform.localRotation,
            };

            if (state.SortingGroup == null)
            {
                state.SortingGroup = instance.AddComponent<SortingGroup>();
            }

            RestartStatusEffectView(state);
            return state;
        }

        private StatusEffectViewState CreateAttachedHeroVfxView(string effectName, HeroViewState view, GameObject prefab)
        {
            if (view?.StatusEffectRoot == null || prefab == null)
            {
                return null;
            }

            var instance = Instantiate(prefab, view.StatusEffectRoot);
            instance.name = $"{effectName}_AttachedVfx";
            RemovePrefabPhysics(instance);

            var state = new StatusEffectViewState
            {
                Root = instance,
                SortingGroup = instance.GetComponent<SortingGroup>(),
                OrbitController = instance.GetComponent<OrbitingStatusIconVfx>(),
                LoadedPrefabResourcesPath = string.Empty,
                Renderers = instance.GetComponentsInChildren<Renderer>(true),
                ParticleSystems = instance.GetComponentsInChildren<ParticleSystem>(true),
                BaseLocalPosition = instance.transform.localPosition,
                BaseLocalScale = instance.transform.localScale,
                BaseLocalRotation = instance.transform.localRotation,
            };

            if (state.SortingGroup == null)
            {
                state.SortingGroup = instance.AddComponent<SortingGroup>();
            }

            RestartStatusEffectView(state);
            return state;
        }

        private void ApplyStatusEffectView(StatusEffectViewState viewState, StatusEffectVfxConfig config, int heroSortingOrder, Vector2 directionalHint = default)
        {
            if (viewState?.Root == null || config == null)
            {
                return;
            }

            var statusTransform = viewState.Root.transform;
            statusTransform.localPosition = viewState.BaseLocalPosition + config.LocalOffset;
            var appliedRotation = Quaternion.Euler(config.LocalEulerAngles);
            if (config.AlignToDirection && directionalHint.sqrMagnitude > Mathf.Epsilon)
            {
                var angle = Mathf.Atan2(directionalHint.y, directionalHint.x) * Mathf.Rad2Deg;
                appliedRotation = Quaternion.Euler(0f, 0f, angle - 90f) * appliedRotation;
            }

            statusTransform.localRotation = appliedRotation * viewState.BaseLocalRotation;
            statusTransform.localScale = new Vector3(
                viewState.BaseLocalScale.x * config.LocalScale.x,
                viewState.BaseLocalScale.y * config.LocalScale.y,
                viewState.BaseLocalScale.z * config.LocalScale.z);

            var sortingOrder = heroSortingOrder + config.SortingOrderOffset;
            if (viewState.OrbitController != null)
            {
                viewState.OrbitController.SetBaseSortingOrder(sortingOrder);
            }
            else if (viewState.SortingGroup != null)
            {
                viewState.SortingGroup.sortingOrder = sortingOrder;
            }
            else
            {
                SetRendererSorting(viewState.Renderers, sortingOrder);
            }
        }

        private static void ApplyWorldStatusEffectView(
            StatusEffectViewState viewState,
            StatusEffectVfxConfig config,
            Vector3 worldPosition,
            int sortingOrder,
            Vector2 directionalHint = default)
        {
            if (viewState?.Root == null || config == null)
            {
                return;
            }

            var statusTransform = viewState.Root.transform;
            statusTransform.position = worldPosition + viewState.BaseLocalPosition + config.LocalOffset;
            var appliedRotation = Quaternion.Euler(config.LocalEulerAngles);
            if (config.AlignToDirection && directionalHint.sqrMagnitude > Mathf.Epsilon)
            {
                var angle = Mathf.Atan2(directionalHint.y, directionalHint.x) * Mathf.Rad2Deg;
                appliedRotation = Quaternion.Euler(0f, 0f, angle - 90f) * appliedRotation;
            }

            statusTransform.rotation = appliedRotation * viewState.BaseLocalRotation;
            statusTransform.localScale = new Vector3(
                viewState.BaseLocalScale.x * config.LocalScale.x,
                viewState.BaseLocalScale.y * config.LocalScale.y,
                viewState.BaseLocalScale.z * config.LocalScale.z);

            if (viewState.OrbitController != null)
            {
                viewState.OrbitController.SetBaseSortingOrder(sortingOrder);
            }
            else if (viewState.SortingGroup != null)
            {
                viewState.SortingGroup.sortingOrder = sortingOrder;
            }
            else
            {
                SetRendererSorting(viewState.Renderers, sortingOrder);
            }
        }

        private static void ApplyAttachedHeroVfxView(
            StatusEffectViewState viewState,
            Vector3 localOffset,
            Vector3 localScale,
            Vector3 localEulerAngles,
            int heroSortingOrder,
            int sortingOrderOffset)
        {
            if (viewState?.Root == null)
            {
                return;
            }

            var attachedTransform = viewState.Root.transform;
            attachedTransform.localPosition = viewState.BaseLocalPosition + localOffset;
            attachedTransform.localRotation = Quaternion.Euler(localEulerAngles) * viewState.BaseLocalRotation;
            attachedTransform.localScale = new Vector3(
                viewState.BaseLocalScale.x * localScale.x,
                viewState.BaseLocalScale.y * localScale.y,
                viewState.BaseLocalScale.z * localScale.z);

            var sortingOrder = heroSortingOrder + sortingOrderOffset;
            if (viewState.OrbitController != null)
            {
                viewState.OrbitController.SetBaseSortingOrder(sortingOrder);
            }
            else if (viewState.SortingGroup != null)
            {
                viewState.SortingGroup.sortingOrder = sortingOrder;
            }
            else
            {
                SetRendererSorting(viewState.Renderers, sortingOrder);
            }
        }

        private static bool TryBuildReactiveGuardViewKey(RuntimeHero caster, SkillData sourceSkill, out string reactiveGuardKey)
        {
            reactiveGuardKey = null;
            if (caster == null || sourceSkill == null)
            {
                return false;
            }

            var skillId = !string.IsNullOrWhiteSpace(sourceSkill.skillId)
                ? sourceSkill.skillId
                : sourceSkill.name;
            reactiveGuardKey = $"reactive_guard_{caster.RuntimeId}_{skillId}";
            return !string.IsNullOrWhiteSpace(reactiveGuardKey);
        }

        private static void ApplyOrbitingStatusEffectLayout(List<StatusEffectViewState> orbitViews)
        {
            if (orbitViews == null || orbitViews.Count == 0)
            {
                return;
            }

            orbitViews.Sort((left, right) => string.CompareOrdinal(
                left?.LoadedPrefabResourcesPath ?? string.Empty,
                right?.LoadedPrefabResourcesPath ?? string.Empty));

            for (var i = 0; i < orbitViews.Count; i++)
            {
                var orbitView = orbitViews[i];
                if (orbitView?.OrbitController == null)
                {
                    continue;
                }

                orbitView.OrbitController.SetOrbitLayout(i, orbitViews.Count);
            }
        }

        private static void RestartStatusEffectView(StatusEffectViewState viewState)
        {
            if (viewState?.ParticleSystems == null)
            {
                return;
            }

            for (var i = 0; i < viewState.ParticleSystems.Length; i++)
            {
                var particleSystem = viewState.ParticleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private static void DestroyStatusEffectView(StatusEffectViewState viewState)
        {
            if (viewState?.Root != null)
            {
                Destroy(viewState.Root);
            }
        }

        private static bool TryGetStatusEffectVfxConfig(StatusEffectType effectType, out StatusEffectVfxConfig config)
        {
            return StatusEffectVfxConfigs.TryGetValue(effectType, out config) && config != null;
        }

        private static bool TryResolveStatusEffectVfxConfig(RuntimeHero hero, RuntimeStatusEffect status, out StatusEffectVfxConfig config)
        {
            config = null;
            if (status == null)
            {
                return false;
            }

            if (SignedStatusEffectVfxConfigs.TryGetValue(status.EffectType, out var signedConfig))
            {
                var combinedMagnitude = GetCombinedStatusMagnitude(hero, status.EffectType);
                if (combinedMagnitude > Mathf.Epsilon)
                {
                    config = signedConfig.PositiveConfig;
                    return config != null;
                }

                if (combinedMagnitude < -Mathf.Epsilon)
                {
                    config = signedConfig.NegativeConfig;
                    return config != null;
                }

                return false;
            }

            return TryGetStatusEffectVfxConfig(status.EffectType, out config);
        }

        private static float GetCombinedStatusMagnitude(RuntimeHero hero, StatusEffectType effectType)
        {
            if (hero == null)
            {
                return 0f;
            }

            var combinedMagnitude = 0f;
            var activeStatuses = hero.ActiveStatusEffects;
            for (var i = 0; i < activeStatuses.Count; i++)
            {
                var status = activeStatuses[i];
                if (status == null || status.EffectType != effectType)
                {
                    continue;
                }

                combinedMagnitude += status.Magnitude;
            }

            return combinedMagnitude;
        }

        private static StatusEffectVfxConfig CreateStatModifierStatusVfxConfig(string resourcesPath)
        {
            return new StatusEffectVfxConfig(
                resourcesPath,
                new Vector3(0f, 0.64f, 0f),
                Vector3.one,
                Vector3.zero,
                176);
        }

        private static void SetRendererVisibility(Renderer[] renderers, bool visible)
        {
            if (renderers == null)
            {
                return;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        private static void DisablePrefabBehaviours(GameObject instance)
        {
            var behaviours = instance.GetComponentsInChildren<Behaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour is Animator || behaviour is SortingGroup)
                {
                    continue;
                }

                var namespaceName = behaviour.GetType().Namespace ?? string.Empty;
                if (!namespaceName.StartsWith("Assets.HeroEditor4D.Common.Scripts"))
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static void RemovePrefabPhysics(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            foreach (var collider2D in instance.GetComponentsInChildren<Collider2D>(true))
            {
                Destroy(collider2D);
            }

            foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                Destroy(rigidbody);
            }

            foreach (var rigidbody2D in instance.GetComponentsInChildren<Rigidbody2D>(true))
            {
                Destroy(rigidbody2D);
            }
        }

        private void SyncProjectiles(BattleContext context)
        {
            for (var i = 0; i < context.Projectiles.Count; i++)
            {
                var projectile = context.Projectiles[i];
                if (!projectileViews.TryGetValue(projectile.ProjectileId, out var view))
                {
                    view = CreateProjectileView(projectile);
                    projectileViews.Add(projectile.ProjectileId, view);
                }

                var pos = Map(projectile.CurrentPosition) + new Vector3(0f, 0.12f, 0f);
                var sortingOrder = Sort(pos.y, 60);
                view.Root.transform.position = pos;
                AlignProjectileView(projectile, view, pos);
                if (view.SortingGroup != null)
                {
                    view.SortingGroup.sortingOrder = sortingOrder;
                }
                else
                {
                    SetRendererSorting(view.Renderers, sortingOrder);
                }
            }

            CleanupMissingProjectiles(context.Projectiles);
        }

        private ProjectileViewState CreateProjectileView(RuntimeBasicAttackProjectile projectile)
        {
            var state = new ProjectileViewState();
            var projectilePrefab = projectile?.Attacker?.Definition?.visualConfig?.projectilePrefab;

            if (projectilePrefab != null)
            {
                var instance = Instantiate(projectilePrefab, projectileRoot);
                instance.name = projectile.ProjectileId;
                RemovePrefabPhysics(instance);

                state.Root = instance;
                state.SortingGroup = instance.GetComponent<SortingGroup>();
                if (state.SortingGroup == null)
                {
                    state.SortingGroup = instance.AddComponent<SortingGroup>();
                }

                state.Renderers = instance.GetComponentsInChildren<Renderer>(true);
                return state;
            }

            var fallbackRenderer = MakeSprite(
                projectile.ProjectileId,
                projectileRoot,
                circleSprite,
                Color.Lerp(Team(projectile.Attacker.Side), projectileTint, 0.6f),
                0,
                Vector3.zero,
                Vector3.one * 0.22f);

            state.Root = fallbackRenderer.gameObject;
            state.FallbackRenderer = fallbackRenderer;
            state.Renderers = new Renderer[] { fallbackRenderer };
            return state;
        }

        private void SyncSkillAreas(BattleContext context)
        {
            for (var i = 0; i < context.SkillAreas.Count; i++)
            {
                var area = context.SkillAreas[i];
                if (!skillAreaViews.TryGetValue(area.AreaId, out var viewState))
                {
                    viewState = new SkillAreaViewState
                    {
                        Renderer = MakeSprite(area.AreaId, skillAreaRoot, circleSprite, skillAreaTint, 0, Vector3.zero, Vector3.one),
                        LastPulseAtSeconds = context.Clock != null ? context.Clock.ElapsedTimeSeconds : 0f,
                    };
                    CreateSkillAreaEffect(area, viewState);
                    skillAreaViews.Add(area.AreaId, viewState);
                }

                var pos = Map(area.CurrentCenter);
                var renderer = viewState.Renderer;
                renderer.transform.position = pos;
                renderer.transform.localScale = GetSkillAreaScale(area);
                renderer.color = GetSkillAreaColor(context, area, viewState);
                renderer.sortingOrder = SkillAreaCircleSortOrder;
                SyncSkillAreaEffect(area, viewState, pos);
            }

            CleanupMissingSkillAreas(context.SkillAreas);
        }

        private void EnsureArena()
        {
            var camera = FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                camera = new GameObject("BattleCamera").AddComponent<Camera>();
            }

            var arenaBackground = TryLoadArenaBackgroundSprite();
            camera.orthographic = true;
            camera.orthographicSize = Stage01ArenaSpec.CameraOrthographicSize;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = arenaBackground != null
                ? new Color(0.08f, 0.1f, 0.14f)
                : new Color(0.39f, 0.67f, 0.95f);

            if (GameObject.Find(ArenaRootName) != null)
            {
                return;
            }

            var arenaRoot = new GameObject(ArenaRootName).transform;
            arenaRoot.SetParent(transform, false);
            if (arenaBackground != null)
            {
                MakeSprite("Backdrop", arenaRoot, arenaBackground, Color.white, -404, new Vector3(0f, 0.4f, 0f), GetArenaBackgroundScale(arenaBackground));
                MakeSprite("BackdropShade", arenaRoot, squareSprite, new Color(0.05f, 0.07f, 0.09f, 0.2f), -403, new Vector3(0f, 0.25f, 0f), new Vector3(Stage01ArenaSpec.BackdropShadeWidthWorldUnits, Stage01ArenaSpec.BackdropShadeHeightWorldUnits, 1f));
                MakeSprite("FloorTint", arenaRoot, squareSprite, new Color(0.16f, 0.12f, 0.08f, 0.32f), -398, new Vector3(0f, -0.15f, 0f), new Vector3(Stage01ArenaSpec.FloorWidthWorldUnits, Stage01ArenaSpec.FloorHeightWorldUnits, 1f));
                MakeSprite("Dust", arenaRoot, circleSprite, new Color(0.94f, 0.77f, 0.48f, 0.16f), -397, new Vector3(0f, -0.25f, 0f), new Vector3(Stage01ArenaSpec.DustWidthWorldUnits, Stage01ArenaSpec.DustHeightWorldUnits, 1f));
                MakeSprite("Ring", arenaRoot, circleSprite, new Color(0.98f, 0.83f, 0.57f, 0.12f), -396, new Vector3(0f, -0.15f, 0f), new Vector3(Stage01ArenaSpec.RingWidthWorldUnits, Stage01ArenaSpec.RingHeightWorldUnits, 1f));
            }
            else
            {
                MakeSprite("Sky", arenaRoot, squareSprite, new Color(0.45f, 0.72f, 0.96f), -400, Vector3.zero, new Vector3(Stage01ArenaSpec.SkyWidthWorldUnits, Stage01ArenaSpec.SkyHeightWorldUnits, 1f));
                MakeSprite("Frame", arenaRoot, squareSprite, new Color(0.76f, 0.5f, 0.31f), -399, Vector3.zero, new Vector3(Stage01ArenaSpec.WidthWorldUnits, Stage01ArenaSpec.HeightWorldUnits, 1f));
                MakeSprite("Floor", arenaRoot, squareSprite, new Color(0.86f, 0.67f, 0.44f), -398, new Vector3(0f, -0.15f, 0f), new Vector3(Stage01ArenaSpec.FloorWidthWorldUnits, Stage01ArenaSpec.FloorHeightWorldUnits, 1f));
                MakeSprite("Dust", arenaRoot, circleSprite, new Color(0.8f, 0.59f, 0.35f, 0.26f), -397, new Vector3(0f, -0.25f, 0f), new Vector3(Stage01ArenaSpec.DustWidthWorldUnits, Stage01ArenaSpec.DustHeightWorldUnits, 1f));
                MakeSprite("Ring", arenaRoot, circleSprite, new Color(0.76f, 0.55f, 0.33f, 0.18f), -396, new Vector3(0f, -0.15f, 0f), new Vector3(Stage01ArenaSpec.RingWidthWorldUnits, Stage01ArenaSpec.RingHeightWorldUnits, 1f));
            }
        }

        private Sprite TryLoadArenaBackgroundSprite()
        {
            if (!string.IsNullOrWhiteSpace(arenaBackgroundResourcesPath))
            {
                var resourceSprite = Resources.Load<Sprite>(arenaBackgroundResourcesPath);
                if (resourceSprite != null)
                {
                    return resourceSprite;
                }
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                return null;
            }

            var fullPath = ResolveArenaBackgroundFilePath(projectRoot.FullName);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            try
            {
                return LoadArenaBackgroundSpriteFromFile(fullPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"BattleView could not load arena background from '{fullPath}': {exception.Message}");
                return null;
            }
        }

        private string ResolveArenaBackgroundFilePath(string projectRoot)
        {
            var candidates = new[]
            {
                arenaBackgroundProjectRelativePath,
                "Assets/Resources/Battle/jjc_background.png",
                "jjc_background.png"
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                var fullPath = Path.Combine(projectRoot, candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static Sprite LoadArenaBackgroundSpriteFromFile(string fullPath)
        {
            if (customArenaBackgroundSprite != null
                && string.Equals(customArenaBackgroundSourcePath, fullPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return customArenaBackgroundSprite;
            }

            var imageBytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            if (!texture.LoadImage(imageBytes, false))
            {
                Destroy(texture);
                return null;
            }

            customArenaBackgroundSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                Stage01ArenaSpec.ImportedSpritePixelsPerUnit);
            customArenaBackgroundSprite.hideFlags = HideFlags.HideAndDontSave;
            customArenaBackgroundSourcePath = fullPath;
            return customArenaBackgroundSprite;
        }

        private Vector3 GetArenaBackgroundScale(Sprite sprite)
        {
            if (sprite == null || sprite.rect.height <= 0f)
            {
                return new Vector3(Stage01ArenaSpec.WidthWorldUnits, ArenaBackgroundHeight, 1f);
            }

            var spriteWorldSize = sprite.bounds.size;
            if (spriteWorldSize.x <= 0f || spriteWorldSize.y <= 0f)
            {
                return new Vector3(Stage01ArenaSpec.WidthWorldUnits, ArenaBackgroundHeight, 1f);
            }

            var aspect = sprite.rect.width / sprite.rect.height;
            var targetWorldWidth = ArenaBackgroundHeight * aspect;
            return new Vector3(
                targetWorldWidth / spriteWorldSize.x,
                ArenaBackgroundHeight / spriteWorldSize.y,
                1f);
        }

        private static void EnsureSprites()
        {
            if (squareSprite == null)
            {
                squareSprite = MakeSpriteAsset(16, false);
            }

            if (circleSprite == null)
            {
                circleSprite = MakeSpriteAsset(64, true);
            }
        }

        private static Sprite MakeSpriteAsset(int size, bool circle)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            var c = (size - 1) * 0.5f;
            var r = size * 0.5f - 2f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var a = 1f;
                    if (circle)
                    {
                        var dx = x - c;
                        var dy = y - c;
                        var d = Mathf.Sqrt(dx * dx + dy * dy);
                        a = d <= r ? 1f - Mathf.Clamp01((d - (r * 0.78f)) / Mathf.Max(1f, r * 0.22f)) : 0f;
                    }

                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Color color, int order, Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Vector3 Map(Vector3 battlePos)
        {
            return new Vector3(battlePos.x, battlePos.z, 0f);
        }

        private Vector3 GetSkillAreaScale(RuntimeSkillArea area)
        {
            var radius = Mathf.Max(0f, area != null ? area.Radius : 0f);
            return new Vector3(radius * 2f, radius * 2f, 1f);
        }

        private Color GetSkillAreaColor(BattleContext context, RuntimeSkillArea area, SkillAreaViewState viewState)
        {
            if (IsThrownProjectilePresentation(area))
            {
                return Color.clear;
            }

            var color = skillAreaTint;
            if (HasSkillAreaEffectPrefab(area))
            {
                color.a *= 0.28f;
            }

            var elapsedSeconds = context?.Clock != null ? context.Clock.ElapsedTimeSeconds : 0f;
            var pulseInterval = Mathf.Max(0.1f, area != null ? area.TickIntervalSeconds : 1f);
            var pulseWindow = Mathf.Min(skillAreaPulseWindowSeconds, pulseInterval);
            var pulseAge = Mathf.Max(0f, elapsedSeconds - viewState.LastPulseAtSeconds);
            var pulseProgress = pulseWindow > 0f ? 1f - Mathf.Clamp01(pulseAge / pulseWindow) : 0f;
            var expiryWindow = Mathf.Min(skillAreaExpiryFadeSeconds, area != null ? area.TotalDurationSeconds : 0f);
            var expiryFade = expiryWindow > 0f
                ? Mathf.Clamp01(area.RemainingDurationSeconds / expiryWindow)
                : 1f;

            color.a *= Mathf.Lerp(0.65f, 1f + skillAreaPulseStrength, pulseProgress) * expiryFade;
            return color;
        }

        private int Sort(float y, int offset)
        {
            return HeroSortBase - Mathf.RoundToInt(y * 100f) + offset;
        }

        private Color Team(TeamSide side)
        {
            return side == TeamSide.Blue ? blueColor : redColor;
        }

        private void CleanupMissingProjectiles(IReadOnlyList<RuntimeBasicAttackProjectile> activeItems)
        {
            var stale = new List<string>();
            foreach (var pair in projectileViews)
            {
                var alive = false;
                for (var i = 0; i < activeItems.Count; i++)
                {
                    if (activeItems[i].ProjectileId == pair.Key)
                    {
                        alive = true;
                        break;
                    }
                }

                if (!alive)
                {
                    stale.Add(pair.Key);
                }
            }

            for (var i = 0; i < stale.Count; i++)
            {
                var viewState = projectileViews[stale[i]];
                Destroy(viewState.Root);
                projectileViews.Remove(stale[i]);
            }
        }

        private void CleanupMissingSkillAreas(IReadOnlyList<RuntimeSkillArea> activeItems)
        {
            var stale = new List<string>();
            foreach (var pair in skillAreaViews)
            {
                var alive = false;
                for (var i = 0; i < activeItems.Count; i++)
                {
                    if (activeItems[i].AreaId == pair.Key)
                    {
                        alive = true;
                        break;
                    }
                }

                if (!alive)
                {
                    stale.Add(pair.Key);
                }
            }

            for (var i = 0; i < stale.Count; i++)
            {
                var viewState = skillAreaViews[stale[i]];
                KillSkillAreaTweens(viewState);
                Destroy(viewState.Renderer.gameObject);
                if (viewState.EffectInstance != null)
                {
                    Destroy(viewState.EffectInstance);
                }

                skillAreaViews.Remove(stale[i]);
            }
        }

        private Color Accent(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Warrior:
                    return new Color(0.94f, 0.73f, 0.25f);
                case HeroClass.Mage:
                    return new Color(0.63f, 0.42f, 0.97f);
                case HeroClass.Assassin:
                    return new Color(0.22f, 0.24f, 0.29f);
                case HeroClass.Tank:
                    return new Color(0.49f, 0.74f, 0.41f);
                case HeroClass.Support:
                    return new Color(0.98f, 0.88f, 0.42f);
                case HeroClass.Marksman:
                    return new Color(0.91f, 0.53f, 0.28f);
                default:
                    return Color.white;
            }
        }

        private void EnsureEventSubscription(BattleContext context)
        {
            if (context == null || context.EventBus == null || subscribedEventBus == context.EventBus)
            {
                return;
            }

            if (subscribedEventBus != null)
            {
                subscribedEventBus.Published -= HandleBattleEvent;
            }

            subscribedEventBus = context.EventBus;
            subscribedEventBus.Published += HandleBattleEvent;
        }

        protected virtual void OnDisable()
        {
            if (subscribedEventBus != null)
            {
                subscribedEventBus.Published -= HandleBattleEvent;
            }

            subscribedEventBus = null;

            foreach (var pair in skillAreaViews)
            {
                KillSkillAreaTweens(pair.Value);
            }
        }

        protected virtual void HandleBattleEvent(IBattleEvent battleEvent)
        {
            if (battleEvent is SkillAreaPulseEvent skillAreaPulseEvent
                && skillAreaViews.TryGetValue(skillAreaPulseEvent.Area.AreaId, out var skillAreaView))
            {
                skillAreaView.LastPulseAtSeconds = battleManager != null && battleManager.Context != null && battleManager.Context.Clock != null
                    ? battleManager.Context.Clock.ElapsedTimeSeconds
                    : skillAreaView.LastPulseAtSeconds;
                RestartSkillAreaEffect(skillAreaView);
                SpawnThrownProjectileImpact(skillAreaPulseEvent.Area);
            }

            if (battleEvent is StatusAppliedEvent statusAppliedEvent)
            {
                if (statusAppliedEvent.EffectType == StatusEffectType.DamageShare
                    && statusAppliedEvent.Source != null
                    && heroViews.TryGetValue(statusAppliedEvent.Source.RuntimeId, out var damageShareSourceView)
                    && damageShareSourceView.DamageShareSourceView != null)
                {
                    RestartStatusEffectView(damageShareSourceView.DamageShareSourceView);
                }
                else if (statusAppliedEvent.Target != null
                    && heroViews.TryGetValue(statusAppliedEvent.Target.RuntimeId, out var heroViewState)
                    && heroViewState.StatusEffectViews.TryGetValue(statusAppliedEvent.EffectType, out var statusEffectView))
                {
                    RestartStatusEffectView(statusEffectView);
                }
            }

            if (battleEvent is HealAppliedEvent healAppliedEvent
                && healAppliedEvent.Target != null
                && healAppliedEvent.HealAmount > 0f)
            {
                PlayHealImpactVfx(healAppliedEvent);
            }

            if (battleEvent is SkillCastEvent skillCastImpactEvent
                && skillCastImpactEvent.PrimaryTarget != null
                && skillCastImpactEvent.Skill != null
                && skillCastImpactEvent.Skill.castImpactVfxPrefab != null)
            {
                PlaySkillCastImpactVfx(skillCastImpactEvent);
            }

            if (battleEvent is SkillCastEvent skillTargetIndicatorEvent
                && skillTargetIndicatorEvent.PrimaryTarget != null
                && skillTargetIndicatorEvent.Skill != null
                && skillTargetIndicatorEvent.Skill.targetIndicatorVfxPrefab != null)
            {
                PlaySkillTargetIndicatorVfx(skillTargetIndicatorEvent);
            }

            if (battleEvent is ReactiveGuardTriggeredEvent reactiveGuardTriggeredEvent)
            {
                PlayReactiveGuardTriggerVfx(reactiveGuardTriggeredEvent);
            }

            foreach (var driver in heroAnimationDrivers.Values)
            {
                if (driver != null)
                {
                    driver.OnBattleEvent(battleEvent);
                }
            }

            if (battleEvent is DamageAppliedEvent damageAppliedEvent
                && damageAppliedEvent.Target != null
                && damageAppliedEvent.DamageAmount > 0f
                && damageAppliedEvent.Target.CurrentHealth > 0f
                && heroViews.TryGetValue(damageAppliedEvent.Target.RuntimeId, out var heroView))
            {
                heroView.HitFlashUntilSeconds = GetElapsedTimeSeconds() + Mathf.Max(0.01f, prefabHitFlashDuration);
            }

            if (battleEvent is ForcedMovementAppliedEvent forcedMovementAppliedEvent)
            {
                if (forcedMovementAppliedEvent.Target != null
                    && heroViews.TryGetValue(forcedMovementAppliedEvent.Target.RuntimeId, out var displacedHeroView))
                {
                    RegisterForcedMovementPresentation(displacedHeroView, forcedMovementAppliedEvent);
                }

                if (ShouldPlayBlinkPhaseVfx(forcedMovementAppliedEvent))
                {
                    PlayBlinkPhaseVfx(forcedMovementAppliedEvent);
                }

                if (ShouldPlayDashChargeVfx(forcedMovementAppliedEvent))
                {
                    PlayDashChargeVfx(forcedMovementAppliedEvent);
                }
            }
        }

        private void PlayReactiveGuardTriggerVfx(ReactiveGuardTriggeredEvent reactiveGuardTriggeredEvent)
        {
            if (reactiveGuardTriggeredEvent?.ProtectedHero == null
                || reactiveGuardTriggeredEvent.SourceSkill?.reactiveGuard == null
                || reactiveGuardTriggeredEvent.SourceSkill.reactiveGuard.triggerVfxPrefab == null
                || !heroViews.TryGetValue(reactiveGuardTriggeredEvent.ProtectedHero.RuntimeId, out var protectedHeroView)
                || protectedHeroView == null
                || reactiveGuardTriggeredEvent.ProtectedHero.IsDead
                || ShouldHideCorpse(protectedHeroView))
            {
                return;
            }

            if (TryBuildReactiveGuardViewKey(
                    reactiveGuardTriggeredEvent.Caster,
                    reactiveGuardTriggeredEvent.SourceSkill,
                    out var reactiveGuardKey)
                && protectedHeroView.ReactiveGuardViews.TryGetValue(reactiveGuardKey, out var reactiveGuardView))
            {
                DestroyStatusEffectView(reactiveGuardView);
                protectedHeroView.ReactiveGuardViews.Remove(reactiveGuardKey);
            }

            var guardPresentation = reactiveGuardTriggeredEvent.SourceSkill.reactiveGuard;
            SpawnTransientHeroVfx(
                protectedHeroView,
                guardPresentation.triggerVfxPrefab,
                guardPresentation.triggerVfxLocalOffset,
                ReactiveGuardTriggerSortOrderOffset,
                localRotation: Quaternion.Euler(guardPresentation.triggerVfxEulerAngles),
                localScaleMultiplier: guardPresentation.triggerVfxLocalScale);
        }

        private static void DisableSnapshotBehaviours(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var behaviours = instance.GetComponentsInChildren<Behaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour is SortingGroup)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private void RegisterForcedMovementPresentation(HeroViewState view, ForcedMovementAppliedEvent forcedMovementAppliedEvent)
        {
            if (view == null || forcedMovementAppliedEvent == null)
            {
                return;
            }

            var horizontalOffset = forcedMovementAppliedEvent.Destination - forcedMovementAppliedEvent.StartPosition;
            horizontalOffset.y = 0f;
            var mappedDirection = Map(horizontalOffset);
            var direction2D = new Vector2(mappedDirection.x, mappedDirection.y);
            view.LastForcedMovementDirection = direction2D.sqrMagnitude > Mathf.Epsilon
                ? direction2D.normalized
                : Vector2.zero;
            view.LastForcedMovementHorizontalDistance = horizontalOffset.magnitude;
            view.LastForcedMovementPeakHeight = Mathf.Max(view.LastForcedMovementPeakHeight, forcedMovementAppliedEvent.PeakHeight);
            view.DirectionalTrailUntilSeconds = GetElapsedTimeSeconds()
                + Mathf.Max(directionalTrailFadeOutSeconds, forcedMovementAppliedEvent.DurationSeconds + directionalTrailFadeOutSeconds);

            if (forcedMovementAppliedEvent.PeakHeight > MinAirborneEffectHeight || view.LastForcedMovementHorizontalDistance > directionalTrailMinDistance)
            {
                StartImpactPulse(view, launchPulseColor, launchPulseDuration);
            }
        }

        private void PlayHealImpactVfx(HealAppliedEvent healAppliedEvent)
        {
            if (healAppliedEvent?.Target == null
                || !heroViews.TryGetValue(healAppliedEvent.Target.RuntimeId, out var targetView))
            {
                return;
            }

            if (healAppliedEvent.Target.IsDead || ShouldHideCorpse(targetView))
            {
                return;
            }

            var healImpactPrefab = GetSharedHealImpactPrefab();
            if (healImpactPrefab == null)
            {
                healImpactPrefab = healAppliedEvent.Caster?.Definition?.visualConfig?.hitVfxPrefab;
                if (healImpactPrefab == null)
                {
                    return;
                }
            }

            SpawnTransientHeroVfx(targetView, healImpactPrefab, Vector3.zero, HealEventVfxSortOrderOffset, HealImpactTransientKey);
        }

        private void PlaySkillTargetIndicatorVfx(SkillCastEvent skillCastEvent)
        {
            if (skillCastEvent?.PrimaryTarget == null
                || skillCastEvent.Skill == null
                || skillCastEvent.Skill.targetIndicatorVfxPrefab == null)
            {
                return;
            }

            if (!heroViews.TryGetValue(skillCastEvent.PrimaryTarget.RuntimeId, out var targetView))
            {
                targetView = CreateHeroView(skillCastEvent.PrimaryTarget);
                heroViews[skillCastEvent.PrimaryTarget.RuntimeId] = targetView;
            }

            if (skillCastEvent.PrimaryTarget.IsDead || ShouldHideCorpse(targetView))
            {
                return;
            }

            var uniqueKey = GetSkillTargetIndicatorTransientKey(skillCastEvent.Caster, skillCastEvent.Skill);
            var expiresAtSeconds = GetElapsedTimeSeconds() + GetSkillTargetIndicatorLifetime(skillCastEvent.Skill);
            RemoveTransientVfxWithKeyFromAllHeroes(uniqueKey, targetView);
            if (TryRefreshTransientVfx(targetView, uniqueKey, expiresAtSeconds))
            {
                return;
            }

            SpawnTransientHeroVfx(
                targetView,
                skillCastEvent.Skill.targetIndicatorVfxPrefab,
                Vector3.zero,
                SkillTargetIndicatorSortOrderOffset,
                uniqueKey,
                lifetimeOverrideSeconds: expiresAtSeconds - GetElapsedTimeSeconds());
        }

        private void PlaySkillCastImpactVfx(SkillCastEvent skillCastEvent)
        {
            if (skillCastEvent?.PrimaryTarget == null
                || skillCastEvent.Skill == null
                || skillCastEvent.Skill.castImpactVfxPrefab == null)
            {
                return;
            }

            var worldPosition = Map(skillCastEvent.PrimaryTarget.CurrentPosition) + skillCastEvent.Skill.castImpactVfxLocalOffset;
            var sortingOrder = Sort(worldPosition.y, SkillCastImpactVfxSortOrderOffset);
            if (heroViews.TryGetValue(skillCastEvent.PrimaryTarget.RuntimeId, out var targetView)
                && targetView?.SortingGroup != null)
            {
                sortingOrder = targetView.SortingGroup.sortingOrder + SkillCastImpactVfxSortOrderOffset;
            }

            var rotation = ResolveSkillCastImpactRotation(skillCastEvent);
            var scale = ResolveSkillCastImpactScale(skillCastEvent);
            SpawnTransientWorldVfx(
                skillCastEvent.Skill.castImpactVfxPrefab,
                worldPosition,
                sortingOrder,
                rotation,
                scale);
        }

        private Quaternion ResolveSkillCastImpactRotation(SkillCastEvent skillCastEvent)
        {
            var eulerAngles = skillCastEvent?.Skill != null
                ? skillCastEvent.Skill.castImpactVfxEulerAngles
                : Vector3.zero;

            if (skillCastEvent?.Skill == null
                || !skillCastEvent.Skill.castImpactVfxAlignToTargetDirection
                || skillCastEvent.Caster == null
                || skillCastEvent.PrimaryTarget == null)
            {
                return Quaternion.Euler(eulerAngles);
            }

            var direction = Map(skillCastEvent.PrimaryTarget.CurrentPosition) - Map(skillCastEvent.Caster.CurrentPosition);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Quaternion.Euler(eulerAngles);
            }

            var zAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(eulerAngles + new Vector3(0f, 0f, zAngle));
        }

        private Vector3 ResolveSkillCastImpactScale(SkillCastEvent skillCastEvent)
        {
            var skill = skillCastEvent?.Skill;
            var scale = skill != null ? skill.castImpactVfxScaleMultiplier : Vector3.one;
            if (skill == null
                || !skill.castImpactVfxScaleWithSkillArea
                || skill.areaRadius <= 0f)
            {
                return scale;
            }

            var areaDiameter = Mathf.Max(0.1f, skill.areaRadius * 2f);
            var areaScaleMultiplier = Mathf.Max(0.1f, skill.castImpactVfxAreaDiameterScaleMultiplier);
            var dynamicScale = areaDiameter * areaScaleMultiplier;
            return new Vector3(
                scale.x * dynamicScale,
                scale.y * dynamicScale,
                scale.z);
        }

        private static string GetSkillTargetIndicatorTransientKey(RuntimeHero caster, SkillData skill)
        {
            var casterId = caster != null ? caster.RuntimeId : "unknown";
            var skillId = skill != null && !string.IsNullOrWhiteSpace(skill.skillId)
                ? skill.skillId
                : "skill";
            return $"target_indicator_{casterId}_{skillId}";
        }

        private static float GetSkillTargetIndicatorLifetime(SkillData skill)
        {
            if (skill?.actionSequence == null || !skill.actionSequence.enabled)
            {
                return CombatActionTiming.DefaultWindupSeconds
                    + CombatActionTiming.DefaultRecoverySeconds
                    + 0.12f;
            }

            var sequence = skill.actionSequence;
            var sequenceDurationSeconds = sequence.repeatMode == CombatActionSequenceRepeatMode.FixedDuration
                ? Mathf.Max(0.01f, sequence.durationSeconds)
                : Mathf.Max(0.01f, sequence.repeatCount * Mathf.Max(0f, sequence.intervalSeconds));

            return CombatActionTiming.DefaultWindupSeconds
                + CombatActionTiming.DefaultRecoverySeconds
                + sequenceDurationSeconds
                + 0.12f;
        }

        private static GameObject GetSharedHealImpactPrefab()
        {
            if (sharedHealImpactPrefab == null)
            {
                sharedHealImpactPrefab = Resources.Load<GameObject>(HealReceivedImpactVfxResourcesPath);
            }

            return sharedHealImpactPrefab;
        }

        private static GameObject GetSharedDashChargePrefab()
        {
            if (sharedDashChargePrefab == null)
            {
                sharedDashChargePrefab = Resources.Load<GameObject>(DashChargeTrailVfxResourcesPath);
            }

            return sharedDashChargePrefab;
        }

        private static bool ShouldPlayBlinkPhaseVfx(ForcedMovementAppliedEvent forcedMovementAppliedEvent)
        {
            if (forcedMovementAppliedEvent?.SourceSkill == null
                || forcedMovementAppliedEvent.Source == null
                || forcedMovementAppliedEvent.Target == null
                || forcedMovementAppliedEvent.SourceSkill.skillType != SkillType.Dash
                || forcedMovementAppliedEvent.DurationSeconds > InstantBlinkDurationThreshold
                || HasAirborneArc(forcedMovementAppliedEvent)
                || !string.Equals(
                    forcedMovementAppliedEvent.Source.RuntimeId,
                    forcedMovementAppliedEvent.Target.RuntimeId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var offset = forcedMovementAppliedEvent.Destination - forcedMovementAppliedEvent.StartPosition;
            offset.y = 0f;
            return offset.sqrMagnitude >= InstantBlinkMinDistance * InstantBlinkMinDistance;
        }

        private static bool ShouldPlayDashChargeVfx(ForcedMovementAppliedEvent forcedMovementAppliedEvent)
        {
            if (forcedMovementAppliedEvent?.SourceSkill == null
                || forcedMovementAppliedEvent.Source == null
                || forcedMovementAppliedEvent.Target == null
                || forcedMovementAppliedEvent.SourceSkill.skillType != SkillType.Dash
                || forcedMovementAppliedEvent.DurationSeconds <= InstantBlinkDurationThreshold
                || HasAirborneArc(forcedMovementAppliedEvent)
                || !string.Equals(
                    forcedMovementAppliedEvent.Source.RuntimeId,
                    forcedMovementAppliedEvent.Target.RuntimeId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var offset = forcedMovementAppliedEvent.Destination - forcedMovementAppliedEvent.StartPosition;
            offset.y = 0f;
            return offset.sqrMagnitude >= DashChargeMinDistance * DashChargeMinDistance;
        }

        private static bool HasAirborneArc(ForcedMovementAppliedEvent forcedMovementAppliedEvent)
        {
            return forcedMovementAppliedEvent != null
                && forcedMovementAppliedEvent.DurationSeconds > InstantBlinkDurationThreshold
                && forcedMovementAppliedEvent.PeakHeight > MinAirborneEffectHeight;
        }

        private void PlayBlinkPhaseVfx(ForcedMovementAppliedEvent forcedMovementAppliedEvent)
        {
            if (forcedMovementAppliedEvent?.Target == null
                || !heroViews.TryGetValue(forcedMovementAppliedEvent.Target.RuntimeId, out var heroView)
                || heroView?.VisualRoot == null
                || forcedMovementAppliedEvent.Target.IsDead
                || ShouldHideCorpse(heroView))
            {
                return;
            }

            var startPosition = Map(forcedMovementAppliedEvent.StartPosition);
            var destinationPosition = Map(forcedMovementAppliedEvent.Destination);
            SpawnBlinkGhostSnapshot(heroView, startPosition, "BlinkDepartGhost", blinkGhostStartAlpha);

            if ((destinationPosition - startPosition).sqrMagnitude > 0.0001f)
            {
                SpawnBlinkGhostSnapshot(heroView, destinationPosition, "BlinkArriveGhost", blinkGhostStartAlpha * 0.72f);
            }

            heroView.BlinkRevealStartedAtSeconds = GetElapsedTimeSeconds();
        }

        private void PlayDashChargeVfx(ForcedMovementAppliedEvent forcedMovementAppliedEvent)
        {
            if (forcedMovementAppliedEvent?.Target == null
                || !heroViews.TryGetValue(forcedMovementAppliedEvent.Target.RuntimeId, out var heroView)
                || forcedMovementAppliedEvent.Target.IsDead
                || ShouldHideCorpse(heroView))
            {
                return;
            }

            var skill = forcedMovementAppliedEvent.SourceSkill;
            var dashChargePrefab = GetSharedDashChargePrefab();
            var customDashPrefab = skill != null ? skill.dashTravelVfxPrefab : null;
            if (dashChargePrefab == null && customDashPrefab == null)
            {
                return;
            }

            var horizontalOffset = forcedMovementAppliedEvent.Destination - forcedMovementAppliedEvent.StartPosition;
            horizontalOffset.y = 0f;
            var mappedDirection = Map(horizontalOffset);
            var direction2D = new Vector2(mappedDirection.x, mappedDirection.y);
            if (direction2D.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var normalizedDirection = direction2D.normalized;
            var angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            var distance = horizontalOffset.magnitude;
            var scaleMultiplier = Mathf.Clamp(0.92f + (distance * 0.15f), 0.92f, 1.35f);
            var lifetimeSeconds = Mathf.Max(
                DashChargeMinLifetimeSeconds,
                forcedMovementAppliedEvent.DurationSeconds + DashChargeLifetimePaddingSeconds);

            if (customDashPrefab != null)
            {
                // Keep the wave attached to the dashing hero so the leading edge reads as part of the charge.
                var localOffset = skill.dashTravelVfxLocalOffset
                    + new Vector3(normalizedDirection.x, normalizedDirection.y, 0f) * skill.dashTravelVfxForwardOffset;
                var localScale = skill.dashTravelVfxScaleMultiplier;
                var dashPathWidth = GetDashPathWidth(skill);
                if (dashPathWidth > 0f)
                {
                    localScale = Vector3.Scale(
                        localScale,
                        new Vector3(
                            dashPathWidth * Mathf.Max(0.01f, skill.dashTravelVfxPathWidthScaleMultiplier),
                            1f,
                            1f));
                }

                SpawnTransientHeroVfx(
                    heroView,
                    customDashPrefab,
                    localOffset,
                    DashChargeSortOrderOffset,
                    DashChargeTransientKey,
                    Quaternion.Euler(skill.dashTravelVfxEulerAngles + new Vector3(0f, 0f, angle - 90f)),
                    localScale,
                    lifetimeSeconds);
                return;
            }

            SpawnTransientHeroVfx(
                heroView,
                dashChargePrefab,
                Vector3.zero,
                DashChargeSortOrderOffset,
                DashChargeTransientKey,
                Quaternion.Euler(0f, 0f, angle),
                new Vector3(scaleMultiplier, scaleMultiplier, 1f),
                lifetimeSeconds);
        }

        private static float GetDashPathWidth(SkillData skill)
        {
            if (skill?.effects == null)
            {
                return 0f;
            }

            var maxRadius = 0f;
            for (var i = 0; i < skill.effects.Count; i++)
            {
                var effect = skill.effects[i];
                if (effect == null || effect.targetMode != SkillEffectTargetMode.DashPathEnemies)
                {
                    continue;
                }

                var radius = effect.radiusOverride > 0f ? effect.radiusOverride : skill.areaRadius;
                if (radius > maxRadius)
                {
                    maxRadius = radius;
                }
            }

            return maxRadius * 2f;
        }

        private void SpawnBlinkGhostSnapshot(HeroViewState sourceView, Vector3 worldPosition, string ghostName, float startAlpha)
        {
            if (sourceView?.VisualRoot == null || transientWorldVfxRoot == null)
            {
                return;
            }

            var instance = Instantiate(sourceView.VisualRoot.gameObject, transientWorldVfxRoot, true);
            instance.name = ghostName;
            instance.transform.position += worldPosition - sourceView.Root.transform.position;
            var statusVfxRoot = instance.transform.Find("StatusVfx");
            if (statusVfxRoot != null)
            {
                Destroy(statusVfxRoot.gameObject);
            }

            var directionalTrail = instance.transform.Find("DirectionalTrail");
            if (directionalTrail != null)
            {
                Destroy(directionalTrail.gameObject);
            }

            DisableSnapshotBehaviours(instance);
            RemovePrefabPhysics(instance);

            var sortingOrder = Sort(worldPosition.y, 0);
            var sortingGroup = instance.GetComponent<SortingGroup>();
            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = sortingOrder;
            }
            else
            {
                SetRendererSorting(instance.GetComponentsInChildren<Renderer>(true), sortingOrder);
            }

            var spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                Destroy(instance);
                return;
            }

            var ghostBaseColors = new Color[spriteRenderers.Length];
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                var ghostColor = Color.Lerp(spriteRenderer.color, blinkGhostTintColor, blinkGhostTintStrength);
                ghostColor.a = spriteRenderer.color.a * startAlpha;
                spriteRenderer.color = ghostColor;
                ghostBaseColors[i] = ghostColor;
            }

            StartCoroutine(FadeBlinkGhostSnapshot(instance.transform, instance, spriteRenderers, ghostBaseColors));
        }

        private void SpawnTransientHeroVfx(
            HeroViewState heroView,
            GameObject prefab,
            Vector3 localOffset,
            int sortingOrderOffset,
            string uniqueKey = null,
            Quaternion? localRotation = null,
            Vector3? localScaleMultiplier = null,
            float? lifetimeOverrideSeconds = null)
        {
            if (heroView?.Root == null || prefab == null)
            {
                return;
            }

            var parent = heroView.StatusEffectRoot != null ? heroView.StatusEffectRoot : heroView.VisualRoot;
            if (parent == null)
            {
                return;
            }

            RemoveTransientVfxWithKey(heroView, uniqueKey);

            var instance = Instantiate(prefab, parent);
            instance.name = $"{prefab.name}_Transient";
            instance.transform.localPosition += localOffset;
            if (localRotation.HasValue)
            {
                instance.transform.localRotation = localRotation.Value * instance.transform.localRotation;
            }

            if (localScaleMultiplier.HasValue)
            {
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, localScaleMultiplier.Value);
            }

            RemovePrefabPhysics(instance);
            ConfigureTransientParticleSystems(instance);

            var sortingGroup = instance.GetComponent<SortingGroup>();
            var transientState = new TransientHeroVfxState
            {
                Root = instance,
                SortingGroup = sortingGroup,
                Renderers = instance.GetComponentsInChildren<Renderer>(true),
                ParticleSystems = instance.GetComponentsInChildren<ParticleSystem>(true),
                UniqueKey = uniqueKey,
                SortingOrderOffset = sortingOrderOffset,
                ExpiresAtSeconds = GetElapsedTimeSeconds() + Mathf.Max(0.05f, lifetimeOverrideSeconds ?? GetTransientVfxLifetime(instance)),
            };

            heroView.TransientVfx.Add(transientState);
            ApplyTransientVfxState(transientState, heroView.SortingGroup != null ? heroView.SortingGroup.sortingOrder : HeroSortBase);
        }

        private void SpawnTransientWorldVfx(
            GameObject prefab,
            Vector3 worldPosition,
            int sortingOrder,
            Quaternion? worldRotation = null,
            Vector3? scaleMultiplier = null)
        {
            if (prefab == null || transientWorldVfxRoot == null)
            {
                return;
            }

            var instance = Instantiate(prefab, transientWorldVfxRoot, false);
            instance.name = $"{prefab.name}_Transient";
            instance.transform.position = worldPosition;
            if (worldRotation.HasValue)
            {
                instance.transform.rotation = worldRotation.Value * instance.transform.rotation;
            }

            if (scaleMultiplier.HasValue)
            {
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scaleMultiplier.Value);
            }

            RemovePrefabPhysics(instance);
            ConfigureTransientParticleSystems(instance, forceOneShotEmission: true);

            var sortingGroup = instance.GetComponent<SortingGroup>();
            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = sortingOrder;
            }
            else
            {
                SetRendererSorting(instance.GetComponentsInChildren<Renderer>(true), sortingOrder);
            }

            StartCoroutine(DestroyTransientWorldVfxAfter(instance, Mathf.Max(0.05f, GetTransientVfxLifetime(instance))));
        }

        private IEnumerator DestroyTransientWorldVfxAfter(GameObject instance, float lifetimeSeconds)
        {
            if (instance == null)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(lifetimeSeconds);
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private void SpawnThrownProjectileImpact(RuntimeSkillArea area)
        {
            if (!IsThrownProjectilePresentation(area) || area?.Skill?.persistentAreaVfxPrefab == null)
            {
                return;
            }

            var areaScale = GetSkillAreaScale(area);
            var multiplier = Mathf.Max(0.1f, area.Skill.persistentAreaVfxScaleMultiplier);
            SpawnTransientWorldVfx(
                area.Skill.persistentAreaVfxPrefab,
                Map(area.CurrentCenter),
                SkillAreaEffectSortOrder,
                Quaternion.Euler(area.Skill.persistentAreaVfxEulerAngles),
                new Vector3(areaScale.x * multiplier, areaScale.y * multiplier, 1f));
        }

        private IEnumerator FadeBlinkGhostSnapshot(
            Transform ghostTransform,
            GameObject ghostRoot,
            SpriteRenderer[] spriteRenderers,
            Color[] ghostBaseColors)
        {
            if (ghostTransform == null || ghostRoot == null || spriteRenderers == null || ghostBaseColors == null)
            {
                yield break;
            }

            var initialScale = ghostTransform.localScale;
            var endScale = initialScale * blinkGhostEndScale;
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, blinkGhostLifetimeSeconds);
            while (elapsed < duration && ghostRoot != null)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                if (ghostTransform != null)
                {
                    ghostTransform.localScale = Vector3.Lerp(initialScale, endScale, progress);
                }

                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    var spriteRenderer = spriteRenderers[i];
                    if (spriteRenderer == null)
                    {
                        continue;
                    }

                    var baseColor = i < ghostBaseColors.Length ? ghostBaseColors[i] : spriteRenderer.color;
                    baseColor.a *= 1f - progress;
                    spriteRenderer.color = baseColor;
                }

                yield return null;
            }

            if (ghostRoot != null)
            {
                Destroy(ghostRoot);
            }
        }

        private static void RemoveTransientVfxWithKey(HeroViewState heroView, string uniqueKey)
        {
            if (heroView?.TransientVfx == null || string.IsNullOrWhiteSpace(uniqueKey))
            {
                return;
            }

            for (var i = heroView.TransientVfx.Count - 1; i >= 0; i--)
            {
                var transient = heroView.TransientVfx[i];
                if (transient == null || !string.Equals(transient.UniqueKey, uniqueKey, StringComparison.Ordinal))
                {
                    continue;
                }

                DestroyTransientVfx(transient);
                heroView.TransientVfx.RemoveAt(i);
            }
        }

        private void RemoveTransientVfxWithKeyFromAllHeroes(string uniqueKey, HeroViewState excludedHeroView = null)
        {
            if (string.IsNullOrWhiteSpace(uniqueKey))
            {
                return;
            }

            foreach (var pair in heroViews)
            {
                if (pair.Value == null || pair.Value == excludedHeroView)
                {
                    continue;
                }

                RemoveTransientVfxWithKey(pair.Value, uniqueKey);
            }
        }

        private static bool TryRefreshTransientVfx(HeroViewState heroView, string uniqueKey, float expiresAtSeconds)
        {
            if (heroView?.TransientVfx == null || string.IsNullOrWhiteSpace(uniqueKey))
            {
                return false;
            }

            for (var i = heroView.TransientVfx.Count - 1; i >= 0; i--)
            {
                var transient = heroView.TransientVfx[i];
                if (transient == null
                    || !string.Equals(transient.UniqueKey, uniqueKey, StringComparison.Ordinal)
                    || transient.Root == null)
                {
                    continue;
                }

                transient.ExpiresAtSeconds = Mathf.Max(transient.ExpiresAtSeconds, expiresAtSeconds);
                return true;
            }

            return false;
        }

        private void SyncTransientVfx(RuntimeHero hero, HeroViewState heroView, int heroSortingOrder)
        {
            if (heroView?.TransientVfx == null || heroView.TransientVfx.Count == 0)
            {
                return;
            }

            var elapsedSeconds = GetElapsedTimeSeconds();
            var clearAll = hero == null || hero.IsDead || ShouldHideCorpse(heroView);
            for (var i = heroView.TransientVfx.Count - 1; i >= 0; i--)
            {
                var transient = heroView.TransientVfx[i];
                if (transient == null || transient.Root == null || clearAll || elapsedSeconds >= transient.ExpiresAtSeconds)
                {
                    DestroyTransientVfx(transient);
                    heroView.TransientVfx.RemoveAt(i);
                    continue;
                }

                ApplyTransientVfxState(transient, heroSortingOrder);
            }
        }

        private static void ApplyTransientVfxState(TransientHeroVfxState transientState, int heroSortingOrder)
        {
            if (transientState == null)
            {
                return;
            }

            var sortingOrder = heroSortingOrder + transientState.SortingOrderOffset;
            if (transientState.SortingGroup != null)
            {
                transientState.SortingGroup.sortingOrder = sortingOrder;
            }
            else
            {
                SetRendererSorting(transientState.Renderers, sortingOrder);
            }
        }

        private static void DestroyTransientVfx(TransientHeroVfxState transientState)
        {
            if (transientState?.Root != null)
            {
                if (transientState.ParticleSystems != null)
                {
                    for (var i = 0; i < transientState.ParticleSystems.Length; i++)
                    {
                        var particleSystem = transientState.ParticleSystems[i];
                        if (particleSystem == null)
                        {
                            continue;
                        }

                        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        particleSystem.Clear(true);
                    }
                }

                transientState.Root.SetActive(false);
                Destroy(transientState.Root);
            }
        }

        private static void ConfigureTransientParticleSystems(GameObject instance, bool forceOneShotEmission = false)
        {
            if (instance == null)
            {
                return;
            }

            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                var main = particleSystem.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (forceOneShotEmission)
                {
                    main.loop = false;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private static float GetTransientVfxLifetime(GameObject instance)
        {
            if (instance == null)
            {
                return DefaultTransientVfxLifetime;
            }

            var lifetime = DefaultTransientVfxLifetime;
            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                var main = particleSystem.main;
                lifetime = Mathf.Max(lifetime, main.duration + GetParticleCurveMax(main.startLifetime));
            }

            var animators = instance.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                var controller = animator != null ? animator.runtimeAnimatorController : null;
                if (controller == null)
                {
                    continue;
                }

                var clips = controller.animationClips;
                for (var clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    if (clips[clipIndex] != null)
                    {
                        lifetime = Mathf.Max(lifetime, clips[clipIndex].length);
                    }
                }
            }

            return Mathf.Clamp(lifetime + 0.08f, 0.15f, 4f);
        }

        private static float GetParticleCurveMax(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant => curve.constant,
                ParticleSystemCurveMode.TwoConstants => Mathf.Max(curve.constantMin, curve.constantMax),
                ParticleSystemCurveMode.Curve => curve.curve != null && curve.curve.length > 0 ? curve.curve.keys[curve.curve.length - 1].value : 0f,
                ParticleSystemCurveMode.TwoCurves => Mathf.Max(
                    curve.curveMin != null && curve.curveMin.length > 0 ? curve.curveMin.keys[curve.curveMin.length - 1].value : 0f,
                    curve.curveMax != null && curve.curveMax.length > 0 ? curve.curveMax.keys[curve.curveMax.length - 1].value : 0f),
                _ => 0f,
            };
        }

        private void CreateSkillAreaEffect(RuntimeSkillArea area, SkillAreaViewState viewState)
        {
            if (viewState == null)
            {
                return;
            }

            if (HasCustomSkillAreaPresentation(area))
            {
                CreateCustomSkillAreaEffect(area, viewState);
                return;
            }

            if (!HasSkillAreaEffectPrefab(area))
            {
                return;
            }

            var instance = Instantiate(area.Skill.persistentAreaVfxPrefab, skillAreaRoot);
            instance.name = $"{area.Skill.displayName}_{area.AreaId}_Vfx";
            instance.transform.localRotation = Quaternion.Euler(area.Skill.persistentAreaVfxEulerAngles) * instance.transform.localRotation;
            RemovePrefabPhysics(instance);
            viewState.EffectInstance = instance;
            viewState.EffectSortingGroup = instance.GetComponent<SortingGroup>();
            if (viewState.EffectSortingGroup == null)
            {
                viewState.EffectSortingGroup = instance.AddComponent<SortingGroup>();
            }

            viewState.EffectAnimators = instance.GetComponentsInChildren<Animator>(true);
            viewState.EffectParticleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            viewState.EffectRenderers = instance.GetComponentsInChildren<Renderer>(true);
            viewState.BaseEffectScale = instance.transform.localScale;
            RestartSkillAreaEffect(viewState);
        }

        private void SyncSkillAreaEffect(RuntimeSkillArea area, SkillAreaViewState viewState, Vector3 position)
        {
            if (viewState?.EffectInstance == null)
            {
                return;
            }

            viewState.EffectInstance.transform.position = position;
            viewState.EffectInstance.transform.localScale = GetSkillAreaEffectScale(area, viewState);
            var effectSortingOrder = SkillAreaEffectSortOrder;
            if (viewState.CustomController != null)
            {
                viewState.CustomController.Sync(area, position, effectSortingOrder, skillAreaExpiryFadeSeconds);
                if (viewState.CustomController.UsesWorldSorting)
                {
                    effectSortingOrder = Sort(viewState.CustomController.GetWorldSortingPosition(position).y, ThrownProjectileSortOrderOffset);
                    if (viewState.EffectSortingGroup != null)
                    {
                        viewState.EffectSortingGroup.sortingOrder = effectSortingOrder;
                    }
                    else
                    {
                        SetRendererSorting(viewState.EffectRenderers, effectSortingOrder);
                    }
                }
            }
            else if (viewState.EffectSortingGroup != null)
            {
                viewState.EffectSortingGroup.sortingOrder = effectSortingOrder;
            }
            else
            {
                SetRendererSorting(viewState.EffectRenderers, effectSortingOrder);
            }
        }

        private Vector3 GetSkillAreaEffectScale(RuntimeSkillArea area, SkillAreaViewState viewState)
        {
            if (viewState?.CustomController != null)
            {
                return viewState.CustomController.GetScaledSize(area, GetSkillAreaScale(area));
            }

            var baseScale = viewState != null ? viewState.BaseEffectScale : Vector3.one;
            var multiplier = area?.Skill != null ? Mathf.Max(0.1f, area.Skill.persistentAreaVfxScaleMultiplier) : 1f;
            var areaScale = GetSkillAreaScale(area);
            return new Vector3(
                baseScale.x * areaScale.x * multiplier,
                baseScale.y * areaScale.y * multiplier,
                baseScale.z);
        }

        private static bool HasSkillAreaEffectPrefab(RuntimeSkillArea area)
        {
            return area != null
                && area.Skill != null
                && area.Skill.persistentAreaVfxPrefab != null;
        }

        private static void RestartSkillAreaEffect(SkillAreaViewState viewState)
        {
            if (viewState?.CustomController != null)
            {
                viewState.CustomController.RestartPulse();
            }

            if (viewState?.EffectAnimators != null)
            {
                for (var i = 0; i < viewState.EffectAnimators.Length; i++)
                {
                    var animator = viewState.EffectAnimators[i];
                    if (animator == null)
                    {
                        continue;
                    }

                    animator.Rebind();
                    animator.Update(0f);
                    animator.Play(0, 0, 0f);
                }
            }

            if (viewState?.EffectParticleSystems == null)
            {
                return;
            }

            for (var i = 0; i < viewState.EffectParticleSystems.Length; i++)
            {
                var particleSystem = viewState.EffectParticleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private static bool HasCustomSkillAreaPresentation(RuntimeSkillArea area)
        {
            return area != null
                && area.Skill != null
                && area.Skill.skillAreaPresentationType != SkillAreaPresentationType.None;
        }

        private void CreateCustomSkillAreaEffect(RuntimeSkillArea area, SkillAreaViewState viewState)
        {
            var root = new GameObject($"{area.Skill.displayName}_{area.AreaId}_Presentation");
            root.transform.SetParent(skillAreaRoot, false);
            viewState.EffectInstance = root;
            viewState.CustomController = CreateSkillAreaPresentationController(area, root);
            if (viewState.CustomController == null)
            {
                Destroy(root);
                viewState.EffectInstance = null;
                return;
            }

            viewState.EffectSortingGroup = root.GetComponent<SortingGroup>();
            viewState.EffectRenderers = viewState.CustomController.Renderers;
            viewState.BaseEffectScale = Vector3.one;
            RestartSkillAreaEffect(viewState);
        }

        private SkillAreaPresentationController CreateSkillAreaPresentationController(RuntimeSkillArea area, GameObject root)
        {
            if (area?.Skill == null || root == null)
            {
                return null;
            }

            switch (area.Skill.skillAreaPresentationType)
            {
                case SkillAreaPresentationType.FireSea:
                    var controller = root.AddComponent<FireSeaSkillAreaPresentationController>();
                    controller.Initialize();
                    return controller;
                case SkillAreaPresentationType.ThrownProjectile:
                    var thrownProjectileController = root.AddComponent<ThrownProjectileSkillAreaPresentationController>();
                    thrownProjectileController.Initialize(
                        area.Skill.castProjectileVfxPrefab,
                        ResolveSkillProjectileLaunchPosition(area.Caster),
                        area.TotalDurationSeconds);
                    return thrownProjectileController;
                default:
                    return null;
            }
        }

        private void AlignProjectileView(RuntimeBasicAttackProjectile projectile, ProjectileViewState view, Vector3 position)
        {
            if (projectile?.Attacker?.Definition?.visualConfig == null
                || view?.Root == null
                || !projectile.Attacker.Definition.visualConfig.projectileAlignToMovement)
            {
                return;
            }

            Vector3 direction;
            if (view.HasLastPosition)
            {
                direction = position - view.LastPosition;
            }
            else if (projectile.Target != null)
            {
                var targetPosition = Map(projectile.Target.CurrentPosition) + new Vector3(0f, 0.12f, 0f);
                direction = targetPosition - position;
            }
            else
            {
                direction = Vector3.right;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var offset = Quaternion.Euler(projectile.Attacker.Definition.visualConfig.projectileEulerAngles);
            view.Root.transform.rotation = Quaternion.Euler(0f, 0f, angle) * offset;
            view.LastPosition = position;
            view.HasLastPosition = true;
        }

        private Vector3 ResolveSkillProjectileLaunchPosition(RuntimeHero caster)
        {
            if (caster != null
                && heroViews.TryGetValue(caster.RuntimeId, out var heroView)
                && heroView?.ProjectileLaunchAnchor != null)
            {
                return heroView.ProjectileLaunchAnchor.position;
            }

            if (caster != null
                && heroViews.TryGetValue(caster.RuntimeId, out heroView)
                && heroView?.VisualRoot != null)
            {
                return heroView.VisualRoot.position + new Vector3(0f, 0.32f, 0f);
            }

            return caster != null
                ? Map(caster.CurrentPosition) + new Vector3(0f, 0.32f, 0f)
                : Vector3.zero;
        }

        private static bool IsThrownProjectilePresentation(RuntimeSkillArea area)
        {
            return area != null
                && area.Skill != null
                && area.Skill.skillAreaPresentationType == SkillAreaPresentationType.ThrownProjectile;
        }

        private static Transform FindPreferredVisualAnchor(Transform root, params string[] preferredNames)
        {
            if (root == null || preferredNames == null || preferredNames.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < preferredNames.Length; i++)
            {
                var anchor = FindChildRecursive(root, preferredNames[i]);
                if (anchor != null)
                {
                    return anchor;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (string.Equals(root.name, childName, StringComparison.Ordinal))
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var result = FindChildRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void KillSkillAreaTweens(SkillAreaViewState viewState)
        {
            if (viewState == null)
            {
                return;
            }

            viewState.CustomController?.Cleanup();
        }

        private static void SetRendererSorting(Renderer[] renderers, int sortingOrder)
        {
            if (renderers == null)
            {
                return;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sortingOrder = sortingOrder;
                }
            }
        }

        private float GetElapsedTimeSeconds()
        {
            return battleManager != null && battleManager.Context != null && battleManager.Context.Clock != null
                ? battleManager.Context.Clock.ElapsedTimeSeconds
                : Time.time;
        }
    }
}
