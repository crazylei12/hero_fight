using Fight.Battle;
using UnityEngine;

namespace Fight.UI
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleHud : MonoBehaviour
    {
        [SerializeField] private Vector2 headerSize = new Vector2(420f, 84f);
        [SerializeField] private Vector2 bannerSize = new Vector2(420f, 60f);

        private BattleManager battleManager;
        private GUIStyle titleStyle;
        private GUIStyle scoreStyle;
        private GUIStyle stateStyle;
        private GUIStyle bannerStyle;
        private string endBannerText;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
        }

        private void Update()
        {
            var context = battleManager.Context;
            if (context?.EventBus == null)
            {
                return;
            }

            context.EventBus.Published -= OnBattleEvent;
            context.EventBus.Published += OnBattleEvent;
        }

        private void OnGUI()
        {
            EnsureStyles();

            var context = battleManager.Context;
            if (context == null)
            {
                return;
            }

            var headerRect = new Rect((Screen.width - headerSize.x) * 0.5f, 16f, headerSize.x, headerSize.y);
            GUI.Box(headerRect, string.Empty);

            GUI.Label(new Rect(headerRect.x + 16f, headerRect.y + 8f, headerRect.width - 32f, 24f), "Arena Battle", titleStyle);
            GUI.Label(new Rect(headerRect.x + 16f, headerRect.y + 34f, headerRect.width - 32f, 26f), $"Blue {context.ScoreSystem.BlueKills} - {context.ScoreSystem.RedKills} Red", scoreStyle);

            var timeText = $"{context.Clock.ElapsedTimeSeconds:0.0}s / {context.Clock.RegulationDurationSeconds:0.0}s";
            var stateText = context.Clock.IsOvertime ? $"Overtime  {timeText}" : $"Regulation  {timeText}";
            GUI.Label(new Rect(headerRect.x + 16f, headerRect.y + 58f, headerRect.width - 32f, 18f), stateText, stateStyle);

            if (!string.IsNullOrEmpty(endBannerText))
            {
                var bannerRect = new Rect((Screen.width - bannerSize.x) * 0.5f, Screen.height - bannerSize.y - 20f, bannerSize.x, bannerSize.y);
                GUI.Box(bannerRect, string.Empty);
                GUI.Label(new Rect(bannerRect.x + 12f, bannerRect.y + 14f, bannerRect.width - 24f, 32f), endBannerText, bannerStyle);
            }
        }

        private void OnDisable()
        {
            var context = battleManager != null ? battleManager.Context : null;
            if (context?.EventBus != null)
            {
                context.EventBus.Published -= OnBattleEvent;
            }
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            stateStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
