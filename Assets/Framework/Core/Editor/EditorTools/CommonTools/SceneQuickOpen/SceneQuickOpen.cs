using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace XuchFramework.Editor
{
    /// <summary>
    /// 在菜单栏快速打开场景的编辑器工具
    /// </summary>
    public static class SceneQuickOpen
    {


        static SceneQuickOpen()
        {
            EditorApplication.update += OnUpdate;
        }
        [MenuItem("Scenes/Scene Browser", priority = 0)]
        public static void OpenSceneBrowser()
        {
            SceneQuickOpenWindow.ShowWindow();
        }

        [MenuItem("Scenes/Boot", priority = 50)]
        public static void OpenBoot()
        {
            OpenScene("Assets/Res/Scenes/Boot.unity");
        }

        [MenuItem("Scenes/Game001", priority = 51)]
        public static void OpenGame001()
        {
            OpenScene("Assets/MonkeyLike/Scenes/Game001.unity");
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

#region Toolbar_Button
        private static readonly Type kToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject sCurrentToolbar;
        private static void OnUpdate()
        {
            if (sCurrentToolbar == null)
            {
                UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(kToolbarType);
                sCurrentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (sCurrentToolbar != null)
                {
                    FieldInfo root = sCurrentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    VisualElement concreteRoot = root.GetValue(sCurrentToolbar) as VisualElement;

                    VisualElement toolbarZone = concreteRoot.Q("ToolbarZoneRightAlign");
                    VisualElement parent = new VisualElement()
                    {
                        style = {
                                flexGrow = 1,
                                flexDirection = FlexDirection.Row,
                            }
                    };
                    IMGUIContainer container = new IMGUIContainer();
                    container.onGUIHandler += OnGuiBody;
                    parent.Add(container);
                    toolbarZone.Add(parent);
                }
            }
        }

        private static void OnGuiBody()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Launch", EditorGUIUtility.FindTexture("PlayButton"))))
            {
                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Boot"))
                {
                    EditorSceneManager.OpenScene("Assets/Res/Scenes/Boot.unity");
                }
                EditorApplication.ExecuteMenuItem("Edit/Play");

            }
            GUILayout.EndHorizontal();
        }
#endregion
    }
}