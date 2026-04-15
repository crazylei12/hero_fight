using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fight.UI.Flow
{
    [DisallowMultipleComponent]
    public class MainMenuSceneController : MonoBehaviour
    {
        [SerializeField] private string heroSelectSceneName = "HeroSelect";
        [SerializeField] private string developmentBattleSceneName = "BattleBasicAttackOnly";

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle devButtonStyle;

        private void Awake()
        {
            GameFlowState.ClearBattleResult();
            GameFlowState.ResetSelectionsToDefault();
        }

        private void OnGUI()
        {
            EnsureStyles();

            var panel = new Rect((Screen.width - 720f) * 0.5f, 64f, 720f, 520f);
            GUI.Box(panel, string.Empty);

            GUI.Label(new Rect(panel.x, panel.y + 36f, panel.width, 54f), "Fight Stage 01", titleStyle);
            GUI.Label(new Rect(panel.x + 48f, panel.y + 106f, panel.width - 96f, 60f), "正式主通路已经切到 Game 窗口。先选阵容，再进入自动战斗，然后直接看结果页。", subtitleStyle);

            if (!GameFlowState.HasBattleTemplate)
            {
                GUI.Label(new Rect(panel.x + 48f, panel.y + 188f, panel.width - 96f, 80f), "没有找到默认示例战斗配置。请先在 Unity 菜单里执行 Fight/Stage 01/Generate Demo Content。", bodyStyle);
                DrawQuitButton(panel);
                return;
            }

            if (GUI.Button(new Rect(panel.x + 240f, panel.y + 220f, 240f, 54f), "Start Match"))
            {
                GameFlowState.ClearBattleResult();
                SceneManager.LoadScene(heroSelectSceneName);
            }

            GUI.Label(new Rect(panel.x + 48f, panel.y + 306f, panel.width - 96f, 34f), "开发入口", subtitleStyle);
            GUI.Label(new Rect(panel.x + 48f, panel.y + 344f, panel.width - 96f, 44f), "下面的入口会直接进入开发验证场景，保留调试 HUD 和日志输出。", bodyStyle);

            if (GUI.Button(new Rect(panel.x + 220f, panel.y + 396f, 280f, 42f), "Open Development Battle", devButtonStyle))
            {
                SceneManager.LoadScene(developmentBattleSceneName);
            }

            DrawQuitButton(panel);
        }

        private void DrawQuitButton(Rect panel)
        {
            if (!GUI.Button(new Rect(panel.x + 280f, panel.y + 458f, 160f, 36f), "Quit"))
            {
                return;
            }

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.93f, 1f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.86f, 0.93f) }
            };

            devButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
