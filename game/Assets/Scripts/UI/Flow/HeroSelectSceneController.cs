using System.Collections.Generic;
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

        private TeamSide activeSide = TeamSide.Blue;
        private int activeSlotIndex;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle slotStyle;
        private GUIStyle focusedSlotStyle;
        private GUIStyle heroButtonStyle;
        private Vector2 catalogScroll;

        private void Awake()
        {
            GameFlowState.EnsureSelectionsInitialized();
            GameFlowState.ClearBattleResult();
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(18f, 18f, Screen.width - 36f, Screen.height - 36f), string.Empty);
            GUI.Label(new Rect(24f, 26f, Screen.width - 48f, 44f), "Hero Select", titleStyle);
            GUI.Label(new Rect(24f, 66f, Screen.width - 48f, 24f), "先选左侧或右侧的阵容槽位，再点击中间英雄卡把英雄放进去。", bodyStyle);

            DrawTopButtons();
            DrawTeamPanel(new Rect(30f, 120f, 330f, Screen.height - 180f), TeamSide.Blue, new Color(0.22f, 0.42f, 0.9f, 0.85f));
            DrawCatalogPanel(new Rect(380f, 120f, Screen.width - 760f, Screen.height - 180f));
            DrawTeamPanel(new Rect(Screen.width - 360f, 120f, 330f, Screen.height - 180f), TeamSide.Red, new Color(0.88f, 0.28f, 0.28f, 0.85f));
        }

        private void DrawTopButtons()
        {
            if (GUI.Button(new Rect(28f, 88f, 130f, 28f), "Back"))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }

            if (GUI.Button(new Rect(170f, 88f, 150f, 28f), "Reset To Demo"))
            {
                GameFlowState.ResetSelectionsToDefault();
                activeSide = TeamSide.Blue;
                activeSlotIndex = 0;
            }

            if (GUI.Button(new Rect(332f, 88f, 150f, 28f), "Clear Focused Slot"))
            {
                GameFlowState.ClearSelectedHero(activeSide, activeSlotIndex);
            }

            var canStart = GameFlowState.HasValidSelections();
            GUI.enabled = canStart;
            if (GUI.Button(new Rect(Screen.width - 210f, 88f, 180f, 28f), "Start Battle"))
            {
                if (GameFlowState.TryPrepareBattleInput(out _))
                {
                    SceneManager.LoadScene(battleSceneName);
                }
            }

            GUI.enabled = true;
        }

        private void DrawTeamPanel(Rect rect, TeamSide side, Color accentColor)
        {
            GUI.Box(rect, string.Empty);

            var title = side == TeamSide.Blue ? "Blue Team" : "Red Team";
            GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 28f), title, sectionStyle);

            var selection = side == TeamSide.Red ? GameFlowState.RedSelection : GameFlowState.BlueSelection;
            for (var i = 0; i < BattleInputConfig.DefaultTeamSize; i++)
            {
                var slotRect = new Rect(rect.x + 16f, rect.y + 52f + (i * 88f), rect.width - 32f, 74f);
                var isFocused = activeSide == side && activeSlotIndex == i;
                var previousColor = GUI.color;
                GUI.color = Color.Lerp(Color.white, accentColor, isFocused ? 0.75f : 0.35f);

                var hero = selection.Count > i ? selection[i] : null;
                var buttonText = hero != null
                    ? $"{i + 1}. {hero.displayName}\n{GetHeroClassLabel(hero.heroClass)}"
                    : $"{i + 1}. Empty Slot";
                var style = isFocused ? focusedSlotStyle : slotStyle;

                if (GUI.Button(slotRect, buttonText, style))
                {
                    activeSide = side;
                    activeSlotIndex = i;
                }

                GUI.color = previousColor;
            }

            GUI.Label(
                new Rect(rect.x + 16f, rect.y + rect.height - 40f, rect.width - 32f, 28f),
                activeSide == side ? $"Focused slot: {activeSlotIndex + 1}" : "Click a slot to assign heroes",
                bodyStyle);
        }

        private void DrawCatalogPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 28f), "Hero Catalog", sectionStyle);

            var visibleRect = new Rect(rect.x + 12f, rect.y + 48f, rect.width - 24f, rect.height - 128f);
            var contentHeight = Mathf.Max(visibleRect.height, (GameFlowState.HeroCatalog.Count * 82f) + 8f);
            var viewRect = new Rect(0f, 0f, visibleRect.width - 18f, contentHeight);

            catalogScroll = GUI.BeginScrollView(visibleRect, catalogScroll, viewRect);

            var y = 0f;
            for (var i = 0; i < GameFlowState.HeroCatalog.Count; i++)
            {
                var hero = GameFlowState.HeroCatalog[i];
                if (hero == null)
                {
                    continue;
                }

                var cardRect = new Rect(0f, y, viewRect.width, 72f);
                GUI.Box(cardRect, string.Empty);

                var buttonText = $"{hero.displayName}  |  {GetHeroClassLabel(hero.heroClass)}\n{BuildHeroTagLine(hero.tags)}";
                if (GUI.Button(new Rect(cardRect.x + 8f, cardRect.y + 8f, cardRect.width - 16f, 56f), buttonText, heroButtonStyle))
                {
                    GameFlowState.SetSelectedHero(activeSide, activeSlotIndex, hero);
                }

                y += 82f;
            }

            GUI.EndScrollView();

            var focusLabel = $"{activeSide} slot {activeSlotIndex + 1}";
            GUI.Label(new Rect(rect.x + 16f, rect.y + rect.height - 70f, rect.width - 32f, 24f), $"Assigning to: {focusLabel}", bodyStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + rect.height - 44f, rect.width - 32f, 24f), GameFlowState.HasValidSelections() ? "Both teams are ready." : "All 10 slots must be filled before battle start.", bodyStyle);
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.86f, 0.93f) }
            };

            slotStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            focusedSlotStyle = new GUIStyle(slotStyle)
            {
                fontSize = 15
            };

            heroButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }
    }
}
