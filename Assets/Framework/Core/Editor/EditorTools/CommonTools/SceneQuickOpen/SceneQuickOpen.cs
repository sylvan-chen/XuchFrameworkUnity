using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace XuchFramework.Editor
{
    [InitializeOnLoad]
    public static class SceneQuickOpen
    {
        private static readonly Type _toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject _currentToolbar;
        private static bool _isInitialized = false;
        private static string _bootScenePath = "Assets/Res/core/scenes/boot.unity";
        private static string _bootSceneName = "boot";

        [MenuItem("Scenes/Scene Browser", priority = 0)]
        public static void OpenSceneBrowser()
        {
            SceneQuickOpenWindow.ShowWindow();
        }

        [MenuItem("Scenes/Set Boot/Current")]
        public static void SetCurrentSceneAsBoot()
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(currentScene.path))
            {
                Debug.LogWarning("[SceneQuickOpen] Current scene is not saved. Please save the scene first.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                _bootScenePath = currentScene.path;
                _bootSceneName = currentScene.name;

                Debug.Log($"[SceneQuickOpen] Boot scene set to: {_bootSceneName} ({_bootScenePath})");
            }
        }

        [MenuItem("Scenes/Set Boot/First Enabled in Build Profiles")]
        public static void SetFirstEnabledInBuildProfilesAsBoot()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogWarning("[SceneQuickOpen] No scenes in Build Profiles.");
                return;
            }

            foreach (var scene in scenes)
            {
                if (scene.enabled)
                {
                    _bootScenePath = scene.path;
                    _bootSceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                    Debug.Log($"[SceneQuickOpen] Boot scene set to: {_bootSceneName} ({_bootScenePath})");
                    return;
                }
            }

            Debug.LogWarning("[SceneQuickOpen] No enabled scenes in Build Settings.");
        }

        // Add a Boot button to toolbar on right of Play button
        static SceneQuickOpen()
        {
            SetFirstEnabledInBuildProfilesAsBoot();
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_isInitialized)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (_currentToolbar == null)
                return;

            FieldInfo root = _currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (root == null)
                return;

            if (root.GetValue(_currentToolbar) is not VisualElement concreteRoot)
                return;

            // The area of original Play button
            // This is based on Unity 6000.0.59f2, may not work in other versions, you should change the name accordingly
            VisualElement toolbarZone = concreteRoot.Q("ToolbarZonePlayMode");
            if (toolbarZone == null)
            {
                Debug.LogWarning("[SceneQuickOpen] Add BootButton failed. Original PlayButton area 'ToolbarZonePlayMode' not found!");
                return;
            }

            var button = new Button(() =>
            {
                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals(_bootSceneName, StringComparison.OrdinalIgnoreCase))
                {
                    EditorSceneManager.OpenScene(_bootScenePath);
                }
                EditorApplication.ExecuteMenuItem("Edit/Play");
            })
            {
                text = "▶ Boot",
                name = "BootButton"
            };

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