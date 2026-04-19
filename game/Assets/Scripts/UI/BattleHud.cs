using System.Collections.Generic;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleHud : MonoBehaviour
    {
        private const float HeaderHeight = 96f;
        private const float EndBannerHeight = 58f;
        private const float SidebarTopPadding = 108f;
        private const float CardSpacing = 10f;
        private const float LabelWorldYOffset = 1.05f;

        [SerializeField] private Color blueColor = new Color(0.2f, 0.47f, 0.92f, 1f);
        [SerializeField] private Color redColor = new Color(0.9f, 0.3f, 0.27f, 1f);
        [SerializeField] private Color overlayBackground = new Color(0.05f, 0.06f, 0.08f, 0.9f);
        [SerializeField] private Color panelBackground = new Color(0.08f, 0.1f, 0.13f, 0.88f);
        [SerializeField] private Color mutedTextColor = new Color(0.77f, 0.82f, 0.89f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.27f, 0.22f, 1f);
        [SerializeField] private Color healthyColor = new Color(0.22f, 0.87f, 0.44f, 1f);
        [SerializeField] private Color shieldColor = new Color(1f, 0.88f, 0.34f, 1f);
        [SerializeField] private Color deadOverlayColor = new Color(0f, 0f, 0f, 0.42f);

        private readonly List<RuntimeHero> blueHeroes = new List<RuntimeHero>(BattleInputConfig.DefaultTeamSize);
        private readonly List<RuntimeHero> redHeroes = new List<RuntimeHero>(BattleInputConfig.DefaultTeamSize);

        private BattleManager battleManager;
        private BattleEventBus boundEventBus;
        private Camera battleCamera;
        private string endBannerText;

        private GUIStyle topTeamStyle;
        private GUIStyle topAliveStyle;
        private GUIStyle scoreStyle;
        private GUIStyle timerStyle;
        private GUIStyle phaseStyle;
        private GUIStyle cardNameStyle;
        private GUIStyle cardMetaStyle;
        private GUIStyle cardStatStyle;
        private GUIStyle chipStyle;
        private GUIStyle endBannerStyle;
        private GUIStyle nameplateStyle;
        private GUIStyle nameplateDeadStyle;
        private GUIStyle fallbackPortraitStyle;
        private GUIStyle healthValueStyle;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            if (battleManager != null)
            {
                battleManager.ContextInitialized += OnContextInitialized;
            }
        }

        private void Update()
        {
            BindToBattleEvents(battleManager != null ? battleManager.Context?.EventBus : null);
            if (battleCamera == null || !battleCamera.isActiveAndEnabled)
            {
                battleCamera = FindFirstObjectByType<Camera>();
            }
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

        private void OnGUI()
        {
            EnsureStyles();

            var context = battleManager != null ? battleManager.Context : null;
            if (context == null)
            {
                return;
            }

            RefreshHeroLists(context);

            DrawTopScoreboard(context);
            DrawSidebar(TeamSide.Blue, blueHeroes, new Rect(12f, SidebarTopPadding, GetSidebarWidth(), GetSidebarHeight()), blueColor);
            DrawSidebar(TeamSide.Red, redHeroes, new Rect(Screen.width - GetSidebarWidth() - 12f, SidebarTopPadding, GetSidebarWidth(), GetSidebarHeight()), redColor);
            DrawHeroNameplates(context);

            if (!string.IsNullOrEmpty(endBannerText))
            {
                DrawEndBanner();
            }
        }

        private void DrawTopScoreboard(BattleContext context)
        {
            var headerRect = new Rect(0f, 0f, Screen.width, HeaderHeight);
            FillRect(headerRect, overlayBackground);

            var centerWidth = Mathf.Clamp(Screen.width * 0.24f, 320f, 460f);
            var centerRect = new Rect((Screen.width - centerWidth) * 0.5f, 8f, centerWidth, HeaderHeight - 16f);
            var leftRect = new Rect(10f, 8f, Mathf.Max(80f, centerRect.x - 20f), HeaderHeight - 20f);
            var rightRect = new Rect(centerRect.xMax + 10f, 8f, Mathf.Max(80f, Screen.width - centerRect.xMax - 20f), HeaderHeight - 20f);

            FillRect(leftRect, Tint(blueColor, 0.33f));
            FillRect(rightRect, Tint(redColor, 0.33f));
            DrawBorder(leftRect, 2f, Tint(blueColor, 0.92f));
            DrawBorder(rightRect, 2f, Tint(redColor, 0.92f));
            DrawBorder(centerRect, 2f, new Color(1f, 1f, 1f, 0.12f));
            FillRect(new Rect(centerRect.x, centerRect.y, centerRect.width * 0.5f, centerRect.height), Tint(blueColor, 0.82f));
            FillRect(new Rect(centerRect.center.x, centerRect.y, centerRect.width * 0.5f, centerRect.height), Tint(redColor, 0.82f));
            FillRect(new Rect(centerRect.center.x - 58f, centerRect.y + 8f, 116f, centerRect.height - 16f), panelBackground);

            var blueAlive = CountAlive(blueHeroes);
            var redAlive = CountAlive(redHeroes);
            GUI.Label(new Rect(leftRect.x + 12f, leftRect.y + 10f, leftRect.width - 24f, 34f), "Blue Team", topTeamStyle);
            GUI.Label(new Rect(leftRect.x + 12f, leftRect.y + 42f, leftRect.width - 24f, 24f), $"Alive {blueAlive} / {blueHeroes.Count}", topAliveStyle);
            GUI.Label(new Rect(rightRect.x + 12f, rightRect.y + 10f, rightRect.width - 24f, 34f), "Red Team", topTeamStyle);
            GUI.Label(new Rect(rightRect.x + 12f, rightRect.y + 42f, rightRect.width - 24f, 24f), $"Alive {redAlive} / {redHeroes.Count}", topAliveStyle);

            GUI.Label(new Rect(centerRect.x + 10f, centerRect.y + 6f, centerRect.width * 0.5f - 20f, 44f), context.ScoreSystem.BlueKills.ToString(), scoreStyle);
            GUI.Label(new Rect(centerRect.center.x + 10f, centerRect.y + 6f, centerRect.width * 0.5f - 20f, 44f), context.ScoreSystem.RedKills.ToString(), scoreStyle);

            var remainingSeconds = Mathf.Max(0f, context.Clock.RegulationDurationSeconds - context.Clock.ElapsedTimeSeconds);
            var phaseText = context.Clock.IsOvertime ? "OVERTIME" : "REGULATION";
            var timerText = context.Clock.IsOvertime
                ? $"+{Mathf.Max(0f, context.Clock.ElapsedTimeSeconds - context.Clock.RegulationDurationSeconds):0.0}s"
                : $"{remainingSeconds:0.0}s";
            var phaseCaption = context.Clock.IsOvertime ? "Next kill wins" : "Kills decide the winner";
            GUI.Label(new Rect(centerRect.center.x - 54f, centerRect.y + 8f, 108f, 20f), phaseText, phaseStyle);
            GUI.Label(new Rect(centerRect.center.x - 54f, centerRect.y + 24f, 108f, 34f), timerText, timerStyle);
            GUI.Label(new Rect(centerRect.center.x - 68f, centerRect.y + 56f, 136f, 18f), phaseCaption, topAliveStyle);
        }

        private void DrawSidebar(TeamSide side, List<RuntimeHero> heroes, Rect rect, Color accentColor)
        {
            FillRect(rect, Tint(panelBackground, 0.96f));
            DrawBorder(rect, 2f, Tint(accentColor, 0.92f));

            var headerRect = new Rect(rect.x, rect.y, rect.width, 34f);
            FillRect(headerRect, Tint(accentColor, 0.84f));
            GUI.Label(headerRect, side == TeamSide.Blue ? "Blue Lineup" : "Red Lineup", topAliveStyle);

            var innerTop = rect.y + 44f;
            var cardHeight = Mathf.Max(94f, (rect.height - 44f - (CardSpacing * (BattleInputConfig.DefaultTeamSize - 1))) / BattleInputConfig.DefaultTeamSize);
            for (var i = 0; i < heroes.Count; i++)
            {
                var cardRect = new Rect(rect.x + 10f, innerTop + (i * (cardHeight + CardSpacing)), rect.width - 20f, cardHeight);
                DrawHeroCard(heroes[i], cardRect, accentColor);
            }
        }

        private void DrawHeroCard(RuntimeHero hero, Rect rect, Color accentColor)
        {
            var heroDefinition = hero != null ? hero.Definition : null;
            var isDead = hero == null || hero.IsDead;
            var shieldAmount = hero != null ? StatusEffectSystem.GetTotalShield(hero) : 0f;
            var backgroundColor = isDead
                ? new Color(0.12f, 0.12f, 0.14f, 0.98f)
                : new Color(0.1f, 0.12f, 0.16f, 0.98f);

            FillRect(rect, backgroundColor);
            DrawBorder(rect, 1.5f, Tint(accentColor, isDead ? 0.3f : 0.85f));

            var portraitSize = Mathf.Min(68f, rect.height - 22f);
            var portraitRect = new Rect(rect.x + 10f, rect.y + 10f, portraitSize, portraitSize);
            FillRect(portraitRect, new Color(0.03f, 0.04f, 0.06f, 0.95f));
            DrawBorder(portraitRect, 1f, new Color(1f, 1f, 1f, 0.1f));
            DrawPortrait(heroDefinition, portraitRect, accentColor);

            if (isDead)
            {
                FillRect(portraitRect, deadOverlayColor);
            }

            var contentX = portraitRect.xMax + 10f;
            var contentWidth = rect.xMax - contentX - 10f;
            var title = heroDefinition != null && !string.IsNullOrWhiteSpace(heroDefinition.displayName)
                ? heroDefinition.displayName
                : "Unknown";
            GUI.Label(new Rect(contentX, rect.y + 8f, contentWidth, 22f), title, cardNameStyle);
            GUI.Label(new Rect(contentX, rect.y + 28f, contentWidth, 18f), GetHeroMetaLabel(heroDefinition), cardMetaStyle);

            var compactLayout = rect.height < 116f;
            var hpRatio = hero != null && hero.MaxHealth > Mathf.Epsilon
                ? Mathf.Clamp01(hero.CurrentHealth / hero.MaxHealth)
                : 0f;
            var healthLabel = hero != null && !isDead
                ? $"{hero.CurrentHealth:0}/{hero.MaxHealth:0}"
                : $"Respawn {Mathf.Max(0f, hero != null ? hero.RespawnRemainingSeconds : 0f):0.0}s";
            GUI.Label(new Rect(contentX, rect.y + 40f, contentWidth, 16f), healthLabel, healthValueStyle);
            DrawProgressBar(new Rect(contentX, rect.y + 54f, contentWidth, 8f), hpRatio, Color.Lerp(lowHealthColor, healthyColor, hpRatio), new Color(1f, 1f, 1f, 0.08f));

            if (!compactLayout && !isDead && shieldAmount > 0f)
            {
                var shieldRatio = hero.MaxHealth > Mathf.Epsilon ? Mathf.Clamp01(shieldAmount / hero.MaxHealth) : 0f;
                DrawProgressBar(new Rect(contentX, rect.y + 66f, contentWidth, 4f), shieldRatio, shieldColor, new Color(1f, 1f, 1f, 0.04f));
            }

            var statY = rect.yMax - 34f;
            GUI.Label(new Rect(rect.x + 10f, statY, rect.width - 20f, 16f), BuildStatLabel(hero), cardStatStyle);

            var chipY = rect.yMax - 18f;
            var chipWidth = Mathf.Min(90f, (rect.width - 36f) * 0.5f);
            DrawStateChip(new Rect(rect.x + 10f, chipY, chipWidth, 16f), BuildSkillChip(hero), hero != null && !hero.IsDead && hero.ActiveSkillCooldownRemainingSeconds <= 0f ? healthyColor : mutedTextColor);
            DrawStateChip(new Rect(rect.xMax - chipWidth - 10f, chipY, chipWidth, 16f), BuildUltimateChip(hero), hero != null && !hero.HasCastUltimate ? shieldColor : mutedTextColor);

            if (hero != null && !hero.IsDead && (hero.HasHardControl || hero.IsTaunted || hero.IsUnderForcedMovement))
            {
                DrawStateChip(new Rect(portraitRect.x, portraitRect.yMax - 18f, portraitRect.width, 14f), BuildControlChip(hero), lowHealthColor);
            }
        }

        private void DrawHeroNameplates(BattleContext context)
        {
            if (battleCamera == null)
            {
                battleCamera = FindFirstObjectByType<Camera>();
            }

            if (battleCamera == null)
            {
                return;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var hero = context.Heroes[i];
                if (hero == null || hero.Definition == null)
                {
                    continue;
                }

                var worldPosition = new Vector3(
                    hero.CurrentPosition.x,
                    hero.CurrentPosition.z + LabelWorldYOffset + hero.VisualHeightOffset,
                    0f);
                var screenPosition = battleCamera.WorldToScreenPoint(worldPosition);
                if (screenPosition.z <= 0f)
                {
                    continue;
                }

                var text = hero.IsDead
                    ? $"{hero.Definition.displayName}  Respawn {Mathf.Max(0f, hero.RespawnRemainingSeconds):0.0}s"
                    : hero.Definition.displayName;
                var style = hero.IsDead ? nameplateDeadStyle : nameplateStyle;
                var size = style.CalcSize(new GUIContent(text));
                var width = Mathf.Clamp(size.x + 20f, 72f, 190f);
                var height = 20f;
                var rect = new Rect(
                    screenPosition.x - (width * 0.5f),
                    Screen.height - screenPosition.y - height,
                    width,
                    height);

                FillRect(rect, hero.Side == TeamSide.Blue ? Tint(blueColor, 0.78f) : Tint(redColor, 0.78f));
                DrawBorder(rect, 1f, new Color(1f, 1f, 1f, 0.12f));
                GUI.Label(rect, text, style);
            }
        }

        private void DrawEndBanner()
        {
            var bannerWidth = Mathf.Clamp(Screen.width * 0.28f, 360f, 560f);
            var rect = new Rect((Screen.width - bannerWidth) * 0.5f, Screen.height - EndBannerHeight - 20f, bannerWidth, EndBannerHeight);
            FillRect(rect, Tint(panelBackground, 0.98f));
            DrawBorder(rect, 2f, new Color(1f, 1f, 1f, 0.12f));
            GUI.Label(rect, endBannerText, endBannerStyle);
        }

        private void RefreshHeroLists(BattleContext context)
        {
            blueHeroes.Clear();
            redHeroes.Clear();
            if (context == null || context.Heroes == null)
            {
                return;
            }

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var hero = context.Heroes[i];
                if (hero == null)
                {
                    continue;
                }

                if (hero.Side == TeamSide.Red)
                {
                    redHeroes.Add(hero);
                }
                else
                {
                    blueHeroes.Add(hero);
                }
            }

            blueHeroes.Sort(CompareHeroesBySlot);
            redHeroes.Sort(CompareHeroesBySlot);
        }

        private static int CompareHeroesBySlot(RuntimeHero left, RuntimeHero right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.SlotIndex.CompareTo(right.SlotIndex);
        }

        private float GetSidebarWidth()
        {
            return Mathf.Clamp(Screen.width * 0.185f, 250f, 320f);
        }

        private float GetSidebarHeight()
        {
            return Mathf.Max(420f, Screen.height - SidebarTopPadding - 12f);
        }

        private static int CountAlive(List<RuntimeHero> heroes)
        {
            var aliveCount = 0;
            if (heroes == null)
            {
                return aliveCount;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null && !heroes[i].IsDead)
                {
                    aliveCount++;
                }
            }

            return aliveCount;
        }

        private string BuildStatLabel(RuntimeHero hero)
        {
            if (hero == null)
            {
                return "No runtime data";
            }

            return $"K {hero.Kills}  D {hero.Deaths}   DMG {FormatCompact(hero.DamageDealt)}   HEAL {FormatCompact(hero.HealingDone)}";
        }

        private static string BuildSkillChip(RuntimeHero hero)
        {
            if (hero == null || hero.Definition == null || hero.Definition.activeSkill == null)
            {
                return "SK N/A";
            }

            if (hero.IsDead)
            {
                return "SK HOLD";
            }

            return hero.ActiveSkillCooldownRemainingSeconds <= 0f
                ? "SK READY"
                : $"SK {hero.ActiveSkillCooldownRemainingSeconds:0.0}";
        }

        private static string BuildUltimateChip(RuntimeHero hero)
        {
            if (hero == null || hero.Definition == null || hero.Definition.ultimateSkill == null)
            {
                return "ULT N/A";
            }

            return hero.HasCastUltimate ? "ULT USED" : "ULT READY";
        }

        private static string BuildControlChip(RuntimeHero hero)
        {
            if (hero == null)
            {
                return string.Empty;
            }

            if (hero.HasHardControl)
            {
                return "CC";
            }

            if (hero.IsUnderForcedMovement)
            {
                return "MOVE";
            }

            if (hero.IsTaunted)
            {
                return "TAUNT";
            }

            return "STATE";
        }

        private void DrawPortrait(HeroDefinition heroDefinition, Rect rect, Color accentColor)
        {
            var portrait = heroDefinition != null && heroDefinition.visualConfig != null ? heroDefinition.visualConfig.portrait : null;
            if (portrait != null)
            {
                DrawSprite(portrait, rect, Color.white);
                return;
            }

            FillRect(rect, Tint(accentColor, 0.82f));
            var fallbackLabel = heroDefinition != null && !string.IsNullOrWhiteSpace(heroDefinition.displayName)
                ? heroDefinition.displayName.Substring(0, 1).ToUpperInvariant()
                : "?";
            GUI.Label(rect, fallbackLabel, fallbackPortraitStyle);
        }

        private static string GetHeroMetaLabel(HeroDefinition heroDefinition)
        {
            if (heroDefinition == null)
            {
                return "Unknown";
            }

            return $"{GetHeroClassLabel(heroDefinition.heroClass)}  |  {heroDefinition.heroId}";
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

        private static string FormatCompact(float value)
        {
            if (value >= 1000f)
            {
                return $"{value / 1000f:0.0}k";
            }

            return value.ToString("0");
        }

        private static void DrawSprite(Sprite sprite, Rect rect, Color color)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var textureRect = sprite.textureRect;
            var uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);

            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.color = previousColor;
        }

        private static void FillRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private static void DrawBorder(Rect rect, float thickness, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            FillRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawProgressBar(Rect rect, float ratio, Color fillColor, Color backgroundColor)
        {
            FillRect(rect, backgroundColor);
            if (ratio <= 0f)
            {
                return;
            }

            FillRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), fillColor);
        }

        private void DrawStateChip(Rect rect, string text, Color accentColor)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            FillRect(rect, Tint(accentColor, 0.22f));
            DrawBorder(rect, 1f, Tint(accentColor, 0.88f));
            GUI.Label(rect, text, chipStyle);
        }

        private static Color Tint(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private void EnsureStyles()
        {
            if (topTeamStyle != null)
            {
                return;
            }

            topTeamStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            topAliveStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = mutedTextColor }
            };

            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            timerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            phaseStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.92f, 0.7f, 1f) }
            };

            cardNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };

            cardMetaStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                clipping = TextClipping.Clip,
                normal = { textColor = mutedTextColor }
            };

            cardStatStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };

            chipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            endBannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            nameplateStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };

            nameplateDeadStyle = new GUIStyle(nameplateStyle)
            {
                fontSize = 10,
                normal = { textColor = new Color(1f, 0.92f, 0.82f, 1f) }
            };

            fallbackPortraitStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            healthValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
        }
    }
}
