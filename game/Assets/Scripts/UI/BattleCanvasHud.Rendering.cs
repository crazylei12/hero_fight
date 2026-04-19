using System.Collections.Generic;
using DG.Tweening;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Fight.UI
{
    public partial class BattleCanvasHud
    {
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

        private void UpdateTopBar(BattleContext context)
        {
            if (topBar == null || context == null)
            {
                return;
            }

            var blueAlive = CountAlive(blueHeroes);
            var redAlive = CountAlive(redHeroes);

            topBar.LeftTeamText.text = "蓝方";
            topBar.RightTeamText.text = "红方";
            topBar.LeftAliveText.text = $"存活 {blueAlive}/{blueHeroes.Count}";
            topBar.RightAliveText.text = $"存活 {redAlive}/{redHeroes.Count}";
            topBar.BlueScoreText.text = context.ScoreSystem.BlueKills.ToString();
            topBar.RedScoreText.text = context.ScoreSystem.RedKills.ToString();

            var remainingSeconds = Mathf.Max(0f, context.Clock.RegulationDurationSeconds - context.Clock.ElapsedTimeSeconds);
            if (context.Clock.IsOvertime)
            {
                topBar.PhaseText.text = "加时决胜";
                topBar.TimerText.text = $"+{Mathf.Max(0f, context.Clock.ElapsedTimeSeconds - context.Clock.RegulationDurationSeconds):0.0}s";
                topBar.CaptionText.text = "下一次击杀结束比赛";
            }
            else
            {
                topBar.PhaseText.text = "常规时间";
                topBar.TimerText.text = $"{remainingSeconds:0.0}s";
                topBar.CaptionText.text = "60 秒结束比击杀";
            }

            topBar.LeftBackground.color = Tint(blueColor, 0.18f);
            topBar.RightBackground.color = Tint(redColor, 0.18f);
            topBar.LeftAccentLine.color = Tint(blueColor, 0.9f);
            topBar.RightAccentLine.color = Tint(redColor, 0.9f);
            topBar.CenterBlueFill.color = Tint(blueColor, 0.86f);
            topBar.CenterRedFill.color = Tint(redColor, 0.86f);

            if (lastBlueKills < 0)
            {
                lastBlueKills = context.ScoreSystem.BlueKills;
            }

            if (lastRedKills < 0)
            {
                lastRedKills = context.ScoreSystem.RedKills;
            }
        }

        private void UpdateSidebar(TeamSidebarView sidebar, List<RuntimeHero> heroes, TeamSide side, Color accentColor)
        {
            if (sidebar == null)
            {
                return;
            }

            sidebar.HeaderText.text = side == TeamSide.Blue ? "蓝方阵容" : "红方阵容";
            sidebar.HeaderRibbon.color = accentColor;
            sidebar.Frame.color = Tint(accentColor, 0.26f);

            for (var i = 0; i < sidebar.Cards.Count; i++)
            {
                var card = sidebar.Cards[i];
                var hasHero = i < heroes.Count;
                card.Root.gameObject.SetActive(hasHero);

                if (!hasHero)
                {
                    continue;
                }

                UpdateHeroCard(card, heroes[i], accentColor);
            }
        }

        private void UpdateHeroCard(HeroCardView card, RuntimeHero hero, Color accentColor)
        {
            var definition = hero.Definition;
            var isDead = hero.IsDead;
            var shieldAmount = StatusEffectSystem.GetTotalShield(hero);
            var hpRatio = hero.MaxHealth > Mathf.Epsilon ? Mathf.Clamp01(hero.CurrentHealth / hero.MaxHealth) : 0f;
            var shieldRatio = hero.MaxHealth > Mathf.Epsilon ? Mathf.Clamp01(shieldAmount / hero.MaxHealth) : 0f;

            card.Background.color = isDead
                ? new Color(0.12f, 0.12f, 0.15f, 0.98f)
                : Color.Lerp(softPanelBackground, accentColor, 0.18f);
            card.Border.color = isDead ? new Color(1f, 1f, 1f, 0.32f) : Tint(accentColor, 0.88f);
            card.AccentBar.color = accentColor;

            var portrait = definition != null && definition.visualConfig != null ? definition.visualConfig.portrait : null;
            card.PortraitImage.gameObject.SetActive(portrait != null);
            card.PortraitFrame.gameObject.SetActive(theme != null && theme.portraitFrame != null);
            card.PortraitFallbackText.gameObject.SetActive(portrait == null);
            card.PortraitBackdrop.color = isDead ? new Color(0.06f, 0.06f, 0.08f, 0.98f) : new Color(0.03f, 0.04f, 0.07f, 0.98f);
            card.PortraitImage.sprite = portrait;
            card.PortraitImage.color = isDead ? new Color(0.68f, 0.68f, 0.72f, 0.9f) : Color.white;
            card.PortraitFallbackText.text = GetPortraitFallback(definition);

            card.NameText.text = definition != null ? definition.displayName : "未知英雄";
            card.MetaText.text = BuildMetaLabel(definition);
            card.HealthText.text = isDead
                ? $"复活 {Mathf.Max(0f, hero.RespawnRemainingSeconds):0.0}s"
                : $"{hero.CurrentHealth:0}/{hero.MaxHealth:0}";
            card.StatText.text = BuildStatLabel(hero);

            SetFillAmount(card.HealthFillRect, hpRatio);
            card.HealthFill.color = Color.Lerp(lowHealthColor, healthyColor, hpRatio);
            SetFillAmount(card.ShieldFillRect, shieldRatio);
            card.ShieldFill.gameObject.SetActive(!isDead && shieldAmount > 0f);

            var skillReady = !isDead && hero.ActiveSkillCooldownRemainingSeconds <= 0f;
            card.SkillChipBackground.color = Tint(skillReady ? healthyColor : mutedTextColor, skillReady ? 0.2f : 0.12f);
            card.SkillChipText.text = BuildSkillChip(hero);
            card.UltimateChipBackground.color = Tint(!hero.HasCastUltimate ? shieldColor : mutedTextColor, !hero.HasCastUltimate ? 0.22f : 0.12f);
            card.UltimateChipText.text = BuildUltimateChip(hero);

            var controlLabel = BuildControlChip(hero);
            var hasControl = !string.IsNullOrEmpty(controlLabel);
            card.ControlChipBackground.gameObject.SetActive(hasControl);
            card.ControlChipText.gameObject.SetActive(hasControl);
            card.ControlChipText.text = controlLabel;

            var deadPulse = Mathf.Lerp(0.42f, 0.64f, Mathf.PingPong(Time.unscaledTime * 1.6f, 1f));
            card.DeadOverlay.gameObject.SetActive(isDead);
            card.DeadOverlay.color = Tint(Color.black, deadPulse);
            card.DeadIcon.gameObject.SetActive(isDead && theme != null && theme.deadIcon != null);
            card.RespawnText.gameObject.SetActive(isDead);
            card.RespawnText.text = isDead ? $"将在 {Mathf.Max(0f, hero.RespawnRemainingSeconds):0.0}s 后复活" : string.Empty;
        }

        private void UpdateNameplates(BattleContext context)
        {
            if (context == null || nameplateLayer == null || canvasRoot == null || battleCamera == null)
            {
                return;
            }

            var activeRuntimeIds = new HashSet<string>();

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var hero = context.Heroes[i];
                if (hero == null || hero.Definition == null)
                {
                    continue;
                }

                activeRuntimeIds.Add(hero.RuntimeId);
                var view = GetOrCreateNameplate(hero.RuntimeId);

                var worldPosition = new Vector3(hero.CurrentPosition.x, hero.CurrentPosition.z + LabelWorldYOffset + hero.VisualHeightOffset, 0f);
                var screenPosition = battleCamera.WorldToScreenPoint(worldPosition);
                if (screenPosition.z <= 0f)
                {
                    view.Root.gameObject.SetActive(false);
                    continue;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPosition, overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : battleCamera, out var localPosition);
                view.Root.anchoredPosition = localPosition + new Vector2(0f, 12f);
                view.Root.gameObject.SetActive(true);

                var teamColor = hero.Side == TeamSide.Blue ? blueColor : redColor;
                view.Background.color = hero.IsDead
                    ? new Color(0.12f, 0.12f, 0.14f, 0.96f)
                    : new Color(0.09f, 0.11f, 0.16f, 0.92f);
                view.AccentLine.color = Tint(teamColor, 0.94f);
                view.DeadIcon.gameObject.SetActive(hero.IsDead && theme != null && theme.deadIcon != null);
                view.LabelText.text = hero.IsDead
                    ? $"{hero.Definition.displayName}  {Mathf.Max(0f, hero.RespawnRemainingSeconds):0.0}s"
                    : hero.Definition.displayName;
                view.LabelText.color = hero.IsDead ? new Color(1f, 0.92f, 0.84f, 1f) : Color.white;

                var width = Mathf.Clamp(74f + (view.LabelText.text.Length * 13f), 104f, 220f);
                var hasDeadIcon = view.DeadIcon.gameObject.activeSelf;
                var leftPadding = hasDeadIcon ? 32f : 12f;
                SetRect(view.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width, 28f), view.Root.anchoredPosition);
                StretchToParent(view.Background.rectTransform, 0f);
                StretchToParent(view.AccentLine.rectTransform, new Vector4(8f, 20f, 8f, 4f));
                SetRect(view.DeadIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 18f), new Vector2(8f, 0f));
                StretchToParent(view.LabelText.rectTransform, new Vector4(leftPadding, 3f, 12f, 3f));
            }

            foreach (var pair in nameplates)
            {
                if (!activeRuntimeIds.Contains(pair.Key))
                {
                    pair.Value.Root.gameObject.SetActive(false);
                }
            }
        }

        private NameplateView GetOrCreateNameplate(string runtimeId)
        {
            if (nameplates.TryGetValue(runtimeId, out var existing))
            {
                return existing;
            }

            var view = new NameplateView
            {
                Root = CreateRect($"Nameplate_{runtimeId}", nameplateLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 28f), Vector2.zero),
            };

            view.Background = CreateImage("Background", view.Root, theme != null ? theme.nameplateBackground : null, Tint(panelBackground, 0.96f));
            StretchToParent(view.Background.rectTransform, 0f);
            view.AccentLine = CreateImage("AccentLine", view.Root, theme != null ? theme.nameplateLine : null, Tint(Color.white, 0.88f));
            view.DeadIcon = CreateImage("DeadIcon", view.Root, theme != null ? theme.deadIcon : null, Color.white, true);
            view.LabelText = CreateText("LabelText", view.Root, 12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            view.LabelText.resizeTextForBestFit = true;
            view.LabelText.resizeTextMinSize = 10;
            view.LabelText.resizeTextMaxSize = 12;

            nameplates.Add(runtimeId, view);
            return view;
        }

        private void UpdateEndBanner()
        {
            if (endBanner == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(endBannerText))
            {
                if (endBanner.Root.gameObject.activeSelf)
                {
                    endBanner.Root.gameObject.SetActive(false);
                    endBanner.CanvasGroup.alpha = 0f;
                }

                return;
            }

            endBanner.Root.gameObject.SetActive(true);
            endBanner.LabelText.text = endBannerText;
        }

        private void UpdateIdlePresentation()
        {
            if (topBar != null)
            {
                topBar.LeftTeamText.text = "蓝方";
                topBar.RightTeamText.text = "红方";
                topBar.LeftAliveText.text = "等待战斗开始";
                topBar.RightAliveText.text = "等待战斗开始";
                topBar.BlueScoreText.text = "0";
                topBar.RedScoreText.text = "0";
                topBar.PhaseText.text = "准备中";
                topBar.TimerText.text = "--";
                topBar.CaptionText.text = "载入战斗数据";
            }

            SetSidebarCardsVisible(blueSidebar, false);
            SetSidebarCardsVisible(redSidebar, false);

            foreach (var pair in nameplates)
            {
                pair.Value.Root.gameObject.SetActive(false);
            }

            UpdateEndBanner();
        }

        private void SetSidebarCardsVisible(TeamSidebarView sidebar, bool visible)
        {
            if (sidebar == null)
            {
                return;
            }

            for (var i = 0; i < sidebar.Cards.Count; i++)
            {
                sidebar.Cards[i].Root.gameObject.SetActive(visible);
            }
        }

        private void PlayIntroAnimation(bool forceReplay)
        {
            if (!Application.isPlaying || canvasGroup == null || topBar == null || blueSidebar == null || redSidebar == null)
            {
                return;
            }

            if (introAnimationPlayed && !forceReplay)
            {
                return;
            }

            introAnimationPlayed = true;

            var blueTarget = blueSidebar.Root.anchoredPosition;
            var redTarget = redSidebar.Root.anchoredPosition;
            var topTarget = topBar.Root.anchoredPosition;

            canvasGroup.DOKill();
            blueSidebar.Root.DOKill();
            redSidebar.Root.DOKill();
            topBar.Root.DOKill();

            canvasGroup.alpha = 0f;
            blueSidebar.Root.anchoredPosition = blueTarget + new Vector2(-46f, 0f);
            redSidebar.Root.anchoredPosition = redTarget + new Vector2(46f, 0f);
            topBar.Root.anchoredPosition = topTarget + new Vector2(0f, 26f);

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(canvasGroup.DOFade(1f, 0.18f));
            sequence.Join(topBar.Root.DOAnchorPos(topTarget, 0.32f).SetEase(Ease.OutCubic));
            sequence.Join(blueSidebar.Root.DOAnchorPos(blueTarget, 0.34f).SetEase(Ease.OutCubic));
            sequence.Join(redSidebar.Root.DOAnchorPos(redTarget, 0.34f).SetEase(Ease.OutCubic));
        }

        private void PunchScore(Text scoreText)
        {
            if (scoreText == null || !Application.isPlaying)
            {
                return;
            }

            scoreText.rectTransform.DOKill();
            scoreText.rectTransform.localScale = Vector3.one;
            scoreText.rectTransform.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.28f, 7, 0.7f).SetUpdate(true);
        }

        private void ShowEndBanner()
        {
            if (endBanner == null || string.IsNullOrWhiteSpace(endBannerText))
            {
                return;
            }

            endBanner.Root.gameObject.SetActive(true);
            endBanner.CanvasGroup.DOKill();
            endBanner.CanvasGroup.alpha = 0f;
            endBanner.CanvasGroup.DOFade(1f, 0.22f).SetUpdate(true);
            endBanner.Root.DOKill();
            endBanner.Root.localScale = new Vector3(0.94f, 0.94f, 1f);
            endBanner.Root.DOScale(1f, 0.24f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void ClearNameplates()
        {
            foreach (var pair in nameplates)
            {
                if (pair.Value == null || pair.Value.Root == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(pair.Value.Root.gameObject);
                }
                else
                {
                    DestroyImmediate(pair.Value.Root.gameObject);
                }
            }

            nameplates.Clear();
        }

        private string BuildEndBannerText(BattleResultData result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            var winnerLabel = result.winner switch
            {
                TeamSide.Blue => "蓝方获胜",
                TeamSide.Red => "红方获胜",
                _ => "平局"
            };

            var reasonLabel = result.endReason switch
            {
                BattleEndReason.OvertimeKill => "加时赛决胜",
                BattleEndReason.TimeExpired => "时间结束",
                _ => result.endReason.ToString()
            };

            return $"{winnerLabel}  |  {reasonLabel}";
        }

        private static string BuildMetaLabel(HeroDefinition definition)
        {
            if (definition == null)
            {
                return "未知职业";
            }

            return $"{GetHeroClassLabel(definition.heroClass)}  ·  {definition.heroId}";
        }

        private static string BuildStatLabel(RuntimeHero hero)
        {
            if (hero == null)
            {
                return "K0 D0 伤0";
            }

            var healSuffix = hero.HealingDone > 0f ? $"  治{FormatCompact(hero.HealingDone)}" : string.Empty;
            return $"K{hero.Kills}  D{hero.Deaths}  伤{FormatCompact(hero.DamageDealt)}{healSuffix}";
        }

        private static string BuildSkillChip(RuntimeHero hero)
        {
            if (hero == null || hero.Definition == null || hero.Definition.activeSkill == null)
            {
                return "技能 --";
            }

            if (hero.IsDead)
            {
                return "技能保留";
            }

            return hero.ActiveSkillCooldownRemainingSeconds <= 0f
                ? "技能就绪"
                : $"技能 {hero.ActiveSkillCooldownRemainingSeconds:0.0}";
        }

        private static string BuildUltimateChip(RuntimeHero hero)
        {
            if (hero == null || hero.Definition == null || hero.Definition.ultimateSkill == null)
            {
                return "大招 --";
            }

            return hero.HasCastUltimate ? "大招已用" : "大招就绪";
        }

        private static string BuildControlChip(RuntimeHero hero)
        {
            if (hero == null || hero.IsDead)
            {
                return string.Empty;
            }

            if (hero.HasHardControl)
            {
                return "受控";
            }

            if (hero.IsUnderForcedMovement)
            {
                return "位移";
            }

            if (hero.IsTaunted)
            {
                return "嘲讽";
            }

            return string.Empty;
        }

        private static string GetPortraitFallback(HeroDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.displayName))
            {
                return "?";
            }

            return definition.displayName.Substring(0, 1).ToUpperInvariant();
        }

        private static string GetHeroClassLabel(HeroClass heroClass)
        {
            return heroClass switch
            {
                HeroClass.Warrior => "战士",
                HeroClass.Mage => "法师",
                HeroClass.Assassin => "刺客",
                HeroClass.Tank => "坦克",
                HeroClass.Support => "辅助",
                HeroClass.Marksman => "射手",
                _ => heroClass.ToString()
            };
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

        private static string FormatCompact(float value)
        {
            if (value >= 1000f)
            {
                return $"{value / 1000f:0.0}k";
            }

            return value.ToString("0");
        }

        private Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
            outline.effectDistance = new Vector2(2f, -2f);

            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect = false)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            var rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
        }

        private static void StretchToParent(RectTransform rect, float inset)
        {
            StretchToParent(rect, new Vector4(inset, inset, inset, inset));
        }

        private static void StretchToParent(RectTransform rect, Vector4 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset.x, inset.y);
            rect.offsetMax = new Vector2(-inset.z, -inset.w);
            rect.localScale = Vector3.one;
        }

        private static void AnchorLeftFill(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetFillAmount(RectTransform rect, float ratio)
        {
            if (rect == null || rect.parent == null)
            {
                return;
            }

            var parentWidth = ((RectTransform)rect.parent).rect.width;
            rect.sizeDelta = new Vector2(parentWidth * Mathf.Clamp01(ratio), 0f);
        }

        private static Color Tint(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
