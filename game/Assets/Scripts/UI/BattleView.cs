using System;
using System.Collections.Generic;
using System.IO;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using Fight.UI.Presentation.Skills;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleView : MonoBehaviour
    {
        private const int HeroSortBase = 1200;
        private const string ArenaRootName = "BattleArena2D";
        private const float CorpseVisibleSeconds = 1f;
        [SerializeField] private float horizontalScale = 1.35f;
        [SerializeField] private float verticalScale = 1.05f;
        [SerializeField] private float heroMarkerScale = 1f;
        [SerializeField] private float prefabVisualScale = 0.9f;
        [SerializeField] private Vector3 footUiOffset = new Vector3(0f, -0.36f, 0f);
        [SerializeField] private Color blueColor = new Color(0.19f, 0.58f, 0.95f);
        [SerializeField] private Color redColor = new Color(0.9f, 0.33f, 0.29f);
        [SerializeField] private Color deadColor = new Color(0.45f, 0.45f, 0.48f);
        [SerializeField] private Color projectileTint = new Color(1f, 0.91f, 0.47f);
        [SerializeField] private Color skillAreaTint = new Color(0.98f, 0.42f, 0.24f, 0.22f);
        [SerializeField] private float skillAreaPulseStrength = 0.12f;
        [SerializeField] private float skillAreaPulseWindowSeconds = 0.28f;
        [SerializeField] private float skillAreaExpiryFadeSeconds = 0.3f;
        [SerializeField] private bool autoCreateArena = true;
        [SerializeField] private string arenaBackgroundResourcesPath = "Battle/jjc_background";
        [SerializeField] private string arenaBackgroundProjectRelativePath = "Assets/Resources/Battle/jjc_background.png";
        [SerializeField] private float arenaBackgroundHeight = 16f;

        private BattleManager battleManager;
        private readonly Dictionary<string, HeroViewState> heroViews = new Dictionary<string, HeroViewState>();
        private readonly Dictionary<string, HeroEditor4DBattleAnimationDriver> heroAnimationDrivers = new Dictionary<string, HeroEditor4DBattleAnimationDriver>();
        private readonly Dictionary<string, ProjectileViewState> projectileViews = new Dictionary<string, ProjectileViewState>();
        private readonly Dictionary<string, SkillAreaViewState> skillAreaViews = new Dictionary<string, SkillAreaViewState>();
        private Transform heroRoot;
        private Transform projectileRoot;
        private Transform skillAreaRoot;
        private BattleEventBus subscribedEventBus;

        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite customArenaBackgroundSprite;
        private static string customArenaBackgroundSourcePath;

        protected BattleManager BattleManager => battleManager;

        private sealed class HeroViewState
        {
            public GameObject Root;
            public SortingGroup SortingGroup;
            public Transform VisualRoot;
            public Renderer[] VisualRenderers;
            public SpriteRenderer Shadow;
            public SpriteRenderer Halo;
            public SpriteRenderer Body;
            public SpriteRenderer Accent;
            public Transform FootUiRoot;
            public SpriteRenderer HealthBack;
            public SpriteRenderer HealthFill;
            public SpriteRenderer UltimateIcon;
            public HeroEditor4DBattleAnimationDriver AnimationDriver;
            public bool LastDeadState;
            public float DeathStartedAtSeconds = -1f;
        }

        private sealed class SkillAreaViewState
        {
            public SpriteRenderer Renderer;
            public float LastPulseAtSeconds;
            public GameObject EffectInstance;
            public SortingGroup EffectSortingGroup;
            public Animator[] EffectAnimators;
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
            view.SortingGroup.sortingOrder = Sort(pos.y, 0);
            UpdateDeathVisibility(hero, view);
            view.VisualRoot.localScale = Vector3.one * (hero.IsDead ? 0.82f : 1f);

            if (view.Body != null)
            {
                view.Body.color = hero.IsDead ? deadColor : Team(hero.Side);
            }

            if (view.Halo != null)
            {
                var halo = Team(hero.Side);
                halo.a = hero.IsDead
                    ? (ShouldHideCorpse(view) ? 0f : 0.18f)
                    : 0.78f;
                view.Halo.color = halo;
            }

            if (view.Shadow != null)
            {
                var shadow = view.Shadow.color;
                shadow.a = hero.IsDead
                    ? (ShouldHideCorpse(view) ? 0f : 0.12f)
                    : 0.24f;
                view.Shadow.color = shadow;
            }

            if (view.HealthFill != null)
            {
                var ratio = hero.MaxHealth > 0f ? Mathf.Clamp01(hero.CurrentHealth / hero.MaxHealth) : 0f;
                var healthColor = hero.Side == TeamSide.Blue
                    ? new Color(0.44f, 0.86f, 0.34f)
                    : new Color(0.92f, 0.29f, 0.24f);
                view.HealthFill.transform.localScale = new Vector3(Mathf.Max(0.02f, ratio), 0.07f, 1f);
                view.HealthFill.color = healthColor;
            }

            if (view.UltimateIcon != null)
            {
                var ultimateColor = hero.HasCastUltimate
                    ? new Color(0.26f, 0.26f, 0.29f, 0.55f)
                    : new Color(1f, 0.9f, 0.36f, 0.98f);
                view.UltimateIcon.color = ultimateColor;
            }

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
            view.VisualRoot = new GameObject("Visual").transform;
            view.VisualRoot.SetParent(view.Root.transform, false);

            if (hero.Definition.visualConfig.battlePrefab != null)
            {
                var visual = Instantiate(hero.Definition.visualConfig.battlePrefab, view.VisualRoot);
                visual.name = $"{hero.Definition.displayName}_Visual";
                visual.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                visual.transform.localScale = Vector3.one * prefabVisualScale;
                DisablePrefabBehaviours(visual);
                RemovePrefabPhysics(visual);

                var driver = view.Root.AddComponent<HeroEditor4DBattleAnimationDriver>();
                driver.Initialize(hero, visual);
                if (driver.IsReady)
                {
                    view.AnimationDriver = driver;
                    heroAnimationDrivers[hero.RuntimeId] = driver;
                }
                else
                {
                    Destroy(driver);
                }
            }
            else
            {
                view.Body = MakeSprite("Body", view.VisualRoot, circleSprite, Team(hero.Side), 20, Vector3.zero, Vector3.one * (0.82f * heroMarkerScale));
                view.Accent = MakeSprite("Accent", view.VisualRoot, squareSprite, Accent(hero.Definition.heroClass), 21, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.22f, 1f));
                view.Accent.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            view.VisualRenderers = view.VisualRoot.GetComponentsInChildren<Renderer>(true);

            view.FootUiRoot = new GameObject("FootUi").transform;
            view.FootUiRoot.SetParent(view.Root.transform, false);
            view.FootUiRoot.localPosition = footUiOffset;
            view.HealthBack = MakeSprite("HealthBack", view.FootUiRoot, squareSprite, new Color(0.08f, 0.1f, 0.12f, 0.92f), 300, Vector3.zero, new Vector3(0.9f, 0.11f, 1f));

            var fillRoot = new GameObject("FillRoot").transform;
            fillRoot.SetParent(view.FootUiRoot, false);
            fillRoot.localPosition = new Vector3(-0.44f, 0f, 0f);
            view.HealthFill = MakeSprite("HealthFill", fillRoot, squareSprite, Color.green, 301, new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.07f, 1f));
            view.UltimateIcon = MakeSprite("UltimateIcon", view.FootUiRoot, squareSprite, new Color(1f, 0.9f, 0.36f, 0.98f), 302, new Vector3(-0.58f, 0f, 0f), new Vector3(0.16f, 0.16f, 1f));
            view.UltimateIcon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return view;
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

            foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                Destroy(rigidbody);
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
                view.Root.transform.position = pos;

                var sortingOrder = Sort(pos.y, 60);
                if (view.SortingGroup != null)
                {
                    view.SortingGroup.sortingOrder = sortingOrder;
                }

                SetRendererSorting(view.Renderers, sortingOrder);
            }

            CleanupMissing(context.Projectiles, projectileViews, p => p.ProjectileId);
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
                renderer.sortingOrder = Sort(pos.y, -80);
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
            camera.orthographicSize = 8.8f;
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
                MakeSprite("BackdropShade", arenaRoot, squareSprite, new Color(0.05f, 0.07f, 0.09f, 0.2f), -403, new Vector3(0f, 0.25f, 0f), new Vector3(28.5f, 16.4f, 1f));
                MakeSprite("FloorTint", arenaRoot, squareSprite, new Color(0.16f, 0.12f, 0.08f, 0.32f), -398, new Vector3(0f, -0.15f, 0f), new Vector3(26.2f, 14.2f, 1f));
                MakeSprite("Dust", arenaRoot, circleSprite, new Color(0.94f, 0.77f, 0.48f, 0.16f), -397, new Vector3(0f, -0.25f, 0f), new Vector3(19f, 10.5f, 1f));
                MakeSprite("Ring", arenaRoot, circleSprite, new Color(0.98f, 0.83f, 0.57f, 0.12f), -396, new Vector3(0f, -0.15f, 0f), new Vector3(21.2f, 11.1f, 1f));
            }
            else
            {
                MakeSprite("Sky", arenaRoot, squareSprite, new Color(0.45f, 0.72f, 0.96f), -400, Vector3.zero, new Vector3(38f, 24f, 1f));
                MakeSprite("Frame", arenaRoot, squareSprite, new Color(0.76f, 0.5f, 0.31f), -399, Vector3.zero, new Vector3(28f, 16f, 1f));
                MakeSprite("Floor", arenaRoot, squareSprite, new Color(0.86f, 0.67f, 0.44f), -398, new Vector3(0f, -0.15f, 0f), new Vector3(26.2f, 14.2f, 1f));
                MakeSprite("Dust", arenaRoot, circleSprite, new Color(0.8f, 0.59f, 0.35f, 0.26f), -397, new Vector3(0f, -0.25f, 0f), new Vector3(19f, 10.5f, 1f));
                MakeSprite("Ring", arenaRoot, circleSprite, new Color(0.76f, 0.55f, 0.33f, 0.18f), -396, new Vector3(0f, -0.15f, 0f), new Vector3(21.2f, 11.1f, 1f));
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
                100f);
            customArenaBackgroundSprite.hideFlags = HideFlags.HideAndDontSave;
            customArenaBackgroundSourcePath = fullPath;
            return customArenaBackgroundSprite;
        }

        private Vector3 GetArenaBackgroundScale(Sprite sprite)
        {
            if (sprite == null || sprite.rect.height <= 0f)
            {
                return new Vector3(28f, arenaBackgroundHeight, 1f);
            }

            var spriteWorldSize = sprite.bounds.size;
            if (spriteWorldSize.x <= 0f || spriteWorldSize.y <= 0f)
            {
                return new Vector3(28f, arenaBackgroundHeight, 1f);
            }

            var aspect = sprite.rect.width / sprite.rect.height;
            var targetWorldWidth = arenaBackgroundHeight * aspect;
            return new Vector3(
                targetWorldWidth / spriteWorldSize.x,
                arenaBackgroundHeight / spriteWorldSize.y,
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
            return new Vector3(battlePos.x * horizontalScale, battlePos.z * verticalScale, 0f);
        }

        private Vector3 GetSkillAreaScale(RuntimeSkillArea area)
        {
            var radius = Mathf.Max(0f, area != null ? area.Radius : 0f);
            return new Vector3(radius * horizontalScale * 2f, radius * verticalScale * 2f, 1f);
        }

        private Color GetSkillAreaColor(BattleContext context, RuntimeSkillArea area, SkillAreaViewState viewState)
        {
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

        private static void CleanupMissing<T>(IReadOnlyList<T> activeItems, Dictionary<string, ProjectileViewState> views, System.Func<T, string> keySelector)
        {
            var stale = new List<string>();
            foreach (var pair in views)
            {
                var alive = false;
                for (var i = 0; i < activeItems.Count; i++)
                {
                    if (keySelector(activeItems[i]) == pair.Key)
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
                Destroy(views[stale[i]].Root);
                views.Remove(stale[i]);
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
            }

            foreach (var driver in heroAnimationDrivers.Values)
            {
                if (driver != null)
                {
                    driver.OnBattleEvent(battleEvent);
                }
            }
        }

        private void CreateSkillAreaEffect(RuntimeSkillArea area, SkillAreaViewState viewState)
        {
            if (!HasSkillAreaEffectPrefab(area) || viewState == null)
            {
                return;
            }

            if (HasCustomSkillAreaPresentation(area))
            {
                CreateCustomSkillAreaEffect(area, viewState);
                return;
            }

            var instance = Instantiate(area.Skill.persistentAreaVfxPrefab, skillAreaRoot);
            instance.name = $"{area.Skill.displayName}_{area.AreaId}_Vfx";
            RemovePrefabPhysics(instance);
            viewState.EffectInstance = instance;
            viewState.EffectSortingGroup = instance.GetComponent<SortingGroup>();
            if (viewState.EffectSortingGroup == null)
            {
                viewState.EffectSortingGroup = instance.AddComponent<SortingGroup>();
            }

            viewState.EffectAnimators = instance.GetComponentsInChildren<Animator>(true);
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
            var effectSortingOrder = Sort(position.y, -140);
            if (viewState.CustomController != null)
            {
                viewState.CustomController.Sync(area, position, effectSortingOrder, skillAreaExpiryFadeSeconds);
            }
            else if (viewState.EffectSortingGroup != null)
            {
                viewState.EffectSortingGroup.sortingOrder = effectSortingOrder;
            }

            SetRendererSorting(viewState.EffectRenderers, effectSortingOrder);
        }

        private Vector3 GetSkillAreaEffectScale(RuntimeSkillArea area, SkillAreaViewState viewState)
        {
            if (viewState?.CustomController != null)
            {
                return viewState.CustomController.GetScaledSize(area, GetSkillAreaScale(area));
            }

            var baseScale = viewState != null ? viewState.BaseEffectScale : Vector3.one;
            var radiusScale = Mathf.Max(1f, (area != null ? area.Radius : 0f) * 0.35f);
            var multiplier = area?.Skill != null ? Mathf.Max(0.1f, area.Skill.persistentAreaVfxScaleMultiplier) : 1f;
            return baseScale * (radiusScale * multiplier);
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
                return;
            }

            if (viewState?.EffectAnimators == null)
            {
                return;
            }

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

        private static SkillAreaPresentationController CreateSkillAreaPresentationController(RuntimeSkillArea area, GameObject root)
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
                default:
                    return null;
            }
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
    }
}
