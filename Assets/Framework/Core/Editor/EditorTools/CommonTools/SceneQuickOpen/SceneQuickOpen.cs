using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Reflection;
using Framework.Core;
using UnityEngine.UIElements;

namespace Framework.Editor
{
    [InitializeOnLoad]
    public static class SceneQuickOpen
    {
        private static readonly Type _toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject _currentToolbar;
        private static bool _isInitialized = false;
        private static string _previousScenePath = string.Empty;

        [MenuItem("Scenes/Scene Browser", priority = 0)]
        public static void OpenSceneBrowser()
        {
            SceneQuickOpenWindow.ShowWindow();
        }

        static SceneQuickOpen()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_isInitialized) return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (_currentToolbar == null) return;

            FieldInfo root = _currentToolbar.GetType()
                .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (root == null) return;

            if (root.GetValue(_currentToolbar) is not VisualElement concreteRoot) return;

            // The area of original Play button
            // This is based on Unity 6000.0.59f2, may not work in other versions, you should change the name accordingly
            VisualElement toolbarZone = concreteRoot.Q("ToolbarZonePlayMode");
            if (toolbarZone == null)
            {
                Debug.LogWarning(
                    "[SceneQuickOpen] Add BootButton failed. Original PlayButton area 'ToolbarZonePlayMode' not found!"
                );
                return;
            }

            var button = new Button(
                () =>
                {
                    if (EditorBuildSettings.scenes.Length == 0)
                    {
                        Log.Error("[SceneQuickOpen] No scenes exists in Build Settings!");
                        return;
                    }

                    _previousScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                    string firstScenePath = EditorBuildSettings.scenes[0].path;
                    EditorSceneManager.OpenScene(firstScenePath);

                    EditorApplication.ExecuteMenuItem("Edit/Play");
                }
            ) { text = "▶ Boot", name = "BootButton" };

            button.RemoveFromClassList("unity-button");
            button.RemoveFromClassList("unity-text-element");
            // button.style.fontSize = 12;
            button.style.flexShrink = 0;
            button.style.marginLeft = 8;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 8;
            button.style.paddingTop = -1;
            button.style.paddingBottom = 1;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.alignSelf = Align.Center;

            Color normalBgColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            Color hoverBgColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            Color borderColor = new Color(0.15f, 0.15f, 0.15f, 1f);

            button.style.backgroundColor = normalBgColor;
            button.style.borderTopLeftRadius = 5;
            button.style.borderTopRightRadius = 5;
            button.style.borderBottomLeftRadius = 5;
            button.style.borderBottomRightRadius = 5;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopColor = borderColor;
            button.style.borderBottomColor = borderColor;
            button.style.borderLeftColor = borderColor;
            button.style.borderRightColor = borderColor;

            button.RegisterCallback<MouseEnterEvent>(evt => { button.style.backgroundColor = hoverBgColor; });
            button.RegisterCallback<MouseLeaveEvent>(evt => { button.style.backgroundColor = normalBgColor; });

            toolbarZone.Add(button);

            _isInitialized = true;
        }
    }
}