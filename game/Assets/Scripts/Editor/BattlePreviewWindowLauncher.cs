using System;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    public static class BattlePreviewWindowLauncher
    {
        private const string MenuPath = "Fight/Stage 01/Open Floating Battle Preview";
        private static readonly Vector2 DefaultWindowSize = new Vector2(1600f, 900f);
        private static readonly Vector2 MinimumWindowSize = new Vector2(960f, 540f);

        [MenuItem(MenuPath)]
        public static void OpenFloatingBattlePreview()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
            {
                Debug.LogError("Could not find UnityEditor.GameView. Open the regular Game view manually.");
                return;
            }

            var gameViewWindow = EditorWindow.GetWindow(gameViewType);
            if (gameViewWindow == null)
            {
                Debug.LogError("Could not open the Unity Game view window.");
                return;
            }

            gameViewWindow.titleContent = new GUIContent("Battle Preview");
            gameViewWindow.minSize = MinimumWindowSize;
            gameViewWindow.maximized = false;
            gameViewWindow.position = GetCenteredRect(DefaultWindowSize);
            gameViewWindow.ShowAuxWindow();
            gameViewWindow.Focus();
        }

        private static Rect GetCenteredRect(Vector2 size)
        {
            var mainWindowPosition = EditorGUIUtility.GetMainWindowPosition();
            var centeredX = mainWindowPosition.x + ((mainWindowPosition.width - size.x) * 0.5f);
            var centeredY = mainWindowPosition.y + ((mainWindowPosition.height - size.y) * 0.5f);
            return new Rect(centeredX, Mathf.Max(40f, centeredY), size.x, size.y);
        }
    }
}
