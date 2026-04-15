using System.Collections.Generic;
using Fight.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fight.UI.Flow
{
    [DisallowMultipleComponent]
    public class ResultSceneController : MonoBehaviour
    {
        [SerializeField] private string battleSceneName = "Battle";
        [SerializeField] private string heroSelectSceneName = "HeroSelect";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle rowStyle;
        private Vector2 statsScroll;

        private void OnGUI()
        {
            EnsureStyles();

            var panel = new Rect((Screen.width - 860f) * 0.5f, 28f, 860f, Screen.height - 56f);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x, panel.y + 18f, panel.width, 44f), "Match Result", titleStyle);

            var result = GameFlowState.LastBattleResult;
            if (result == null)
            {
                GUI.Label(new Rect(panel.x + 40f, panel.y + 96f, panel.width - 80f, 44f), "当前没有可展示的战斗结果。", subtitleStyle);
                DrawBottomButtons(panel, hasReplay: false);
                return;
            }

            GUI.Label(new Rect(panel.x + 40f, panel.y + 78f, panel.width - 80f, 36f), $"Winner: {result.winner}", subtitleStyle);
            GUI.Label(new Rect(panel.x + 40f, panel.y + 114f, panel.width - 80f, 26f), $"Score  Blue {result.blueKills} - {result.redKills} Red", bodyStyle);
            GUI.Label(new Rect(panel.x + 40f, panel.y + 140f, panel.width - 80f, 26f), $"End Reason: {result.endReason}  |  Overtime: {(result.enteredOvertime ? "Yes" : "No")}  |  Time: {result.elapsedTimeSeconds:0.0}s", bodyStyle);

            DrawStatsTable(new Rect(panel.x + 32f, panel.y + 184f, panel.width - 64f, panel.height - 276f), result.heroStats);
            DrawBottomButtons(panel, hasReplay: true);
        }

        private void DrawStatsTable(Rect rect, List<HeroBattleStatLine> heroStats)
        {
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 24f), "Hero Stats", subtitleStyle);

            var headerRect = new Rect(rect.x + 12f, rect.y + 40f, rect.width - 24f, 24f);
            GUI.Label(headerRect, "Hero / Side / K / D / Damage / Healing", bodyStyle);

            var visibleRect = new Rect(rect.x + 12f, rect.y + 68f, rect.width - 24f, rect.height - 80f);
            var sortedStats = BuildSortedStats(heroStats);
            var contentHeight = Mathf.Max(visibleRect.height, (sortedStats.Count * 34f) + 8f);
            var viewRect = new Rect(0f, 0f, visibleRect.width - 18f, contentHeight);
            statsScroll = GUI.BeginScrollView(visibleRect, statsScroll, viewRect);

            for (var i = 0; i < sortedStats.Count; i++)
            {
                var line = sortedStats[i];
                var heroName = string.IsNullOrWhiteSpace(line.heroId) ? "Unknown" : line.heroId;
                var rowText = $"{heroName}  |  {line.side}  |  {line.kills}  |  {line.deaths}  |  {line.damageDealt:0.0}  |  {line.healingDone:0.0}";
                GUI.Label(new Rect(0f, i * 34f, viewRect.width, 28f), rowText, rowStyle);
            }

            GUI.EndScrollView();
        }

        private void DrawBottomButtons(Rect panel, bool hasReplay)
        {
            if (hasReplay && GUI.Button(new Rect(panel.x + 120f, panel.yMax - 62f, 170f, 36f), "Rematch"))
            {
                if (GameFlowState.TryPrepareBattleInput(out _))
                {
                    SceneManager.LoadScene(battleSceneName);
                }
                else
                {
                    SceneManager.LoadScene(heroSelectSceneName);
                }
            }

            if (GUI.Button(new Rect(panel.x + 346f, panel.yMax - 62f, 170f, 36f), "Adjust Heroes"))
            {
                SceneManager.LoadScene(heroSelectSceneName);
            }

            if (GUI.Button(new Rect(panel.x + 572f, panel.yMax - 62f, 170f, 36f), "Main Menu"))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        private static List<HeroBattleStatLine> BuildSortedStats(List<HeroBattleStatLine> heroStats)
        {
            var sorted = new List<HeroBattleStatLine>();
            if (heroStats != null)
            {
                sorted.AddRange(heroStats);
            }

            sorted.Sort((left, right) =>
            {
                var sideComparison = left.side.CompareTo(right.side);
                if (sideComparison != 0)
                {
                    return sideComparison;
                }

                var killComparison = right.kills.CompareTo(left.kills);
                if (killComparison != 0)
                {
                    return killComparison;
                }

                return string.Compare(left.heroId, right.heroId, System.StringComparison.OrdinalIgnoreCase);
            });

            return sorted;
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
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
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
                normal = { textColor = new Color(0.83f, 0.87f, 0.93f) }
            };

            rowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                wordWrap = false,
                normal = { textColor = Color.white }
            };
        }
    }
}
