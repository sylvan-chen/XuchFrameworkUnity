using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Xuch.Editor
{
    /// <summary>
    /// 场景快速打开窗口：
    /// 1. 自动扫描项目中所有 Scene 资源 (排除 Packages 内资源)
    /// 2. 支持搜索过滤
    /// 3. 支持单击选中，双击直接打开并关闭窗口，也可以通过底部按钮打开当前选中的场景
    /// 5. 监听工程变更自动刷新
    /// </summary>
    public class SceneQuickOpenWindow : EditorWindow
    {
        private class SceneInfo
        {
            public string Path;
            public string Name;
            public Object SceneAsset;
            public string Folder;
        }

        private List<SceneInfo> _scenes = new List<SceneInfo>();
        private Vector2 _scroll;
        private int _selectedIndex = -1;
        private string _search = string.Empty;
        private double _lastClickTime;                   // 用于防止误判（备用）
        private const double DoubleClickInterval = 0.3f; // 备用阈值 (秒)
        private GUIStyle _rowStyleOdd;
        private GUIStyle _rowStyleEven;
        private GUIStyle _selectedStyle;
        private GUIContent _iconContent;
        private bool _needRescan;

        private static readonly Color _selectedColor = new Color(0.24f, 0.48f, 0.90f, 0.85f);

        public static void ShowWindow()
        {
            var wnd = GetWindow<SceneQuickOpenWindow>(true, "Scene Browser", true);
            wnd.minSize = new Vector2(420, 320);
            wnd.RefreshScenes();
            wnd.Focus();
        }

        private void OnEnable()
        {
            BuildStyles();
            _iconContent = EditorGUIUtility.IconContent("SceneAsset Icon");
            EditorApplication.projectChanged += OnProjectChanged;
            RefreshScenes();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnFocus()
        {
            if (_needRescan)
            {
                RefreshScenes();
            }
        }

        private void OnProjectChanged()
        {
            // 延迟到获得焦点再刷新，避免频繁刷新
            _needRescan = true;
        }

        private void BuildStyles()
        {
            _rowStyleOdd ??= new GUIStyle("Label") { normal = { background = MakeTex(new Color(0, 0, 0, 0)) } };

            _rowStyleEven ??= new GUIStyle("Label") { normal = { background = MakeTex(new Color(0, 0, 0, 0.04f)) } };

            _selectedStyle ??= new GUIStyle("Label")
            {
                normal =
                {
                    textColor = Color.white,
                    background = MakeTex(_selectedColor)
                }
            };
        }

        private Texture2D MakeTex(Color col)
        {
            var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void RefreshScenes()
        {
            _needRescan = false;
            _scenes.Clear();
            var guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/"))
                    continue; // 排除包内场景
                if (!path.EndsWith(".unity"))
                    continue;
                _scenes.Add(
                    new SceneInfo
                    {
                        Path = path,
                        Name = System.IO.Path.GetFileNameWithoutExtension(path),
                        SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path),
                        Folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/')
                    });
            }

            _scenes = _scenes.OrderBy(s => s.Name).ThenBy(s => s.Path).ToList();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSceneList();
            DrawBottomBar();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 搜索框
                var newSearch = GUILayout.TextField(
                    _search,
                    GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField,
                    GUILayout.MinWidth(120));
                if (newSearch != null && newSearch != _search)
                {
                    _search = newSearch;
                }

                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _search = string.Empty;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RefreshScenes();
                }
            }
        }

        private void DrawSceneList()
        {
            var filtered = string.IsNullOrEmpty(_search) ? _scenes : _scenes
                .Where(s => s.Name.ToLower().Contains(_search.ToLower()) || s.Path.ToLower().Contains(_search.ToLower())).ToList();

            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;
                for (int i = 0; i < filtered.Count; i++)
                {
                    var scene = filtered[i];
                    var globalIndex = _scenes.IndexOf(scene); // 还原原始索引，保证选中状态一致
                    DrawSceneRow(scene, globalIndex, i);
                }
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                // 点击空白区域取消选中
                Repaint();
            }
        }

        private void DrawSceneRow(SceneInfo scene, int originalIndex, int visibleRow)
        {
            var rowRect = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight + 4, GUILayout.ExpandWidth(true));
            var isSelected = originalIndex == _selectedIndex;
            var style = isSelected ? _selectedStyle : (visibleRow % 2 == 0 ? _rowStyleEven : _rowStyleOdd);

            // 背景
            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(rowRect, false, false, isSelected, false);
            }

            // 内容区域
            var iconRect = new Rect(rowRect.x + 4, rowRect.y + 2, rowRect.height - 4, rowRect.height - 4);
            var nameRect = new Rect(iconRect.xMax + 4, rowRect.y + 2, rowRect.width * 0.35f, rowRect.height - 4);
            var folderRect = new Rect(nameRect.xMax + 6, rowRect.y + 2, rowRect.width - (nameRect.xMax - rowRect.x) - 10, rowRect.height - 4);

            if (_iconContent != null && _iconContent.image != null)
            {
                GUI.Label(iconRect, _iconContent);
            }

            GUI.Label(nameRect, scene.Name, EditorStyles.boldLabel);
            GUI.Label(folderRect, scene.Folder, EditorStyles.miniLabel);

            HandleRowEvents(rowRect, originalIndex, scene);
        }

        private void HandleRowEvents(Rect rowRect, int index, SceneInfo scene)
        {
            var evt = Event.current;
            if (!rowRect.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (_selectedIndex != index)
                {
                    _selectedIndex = index;
                    Repaint();
                }
                else
                {
                    // 已选中，再点击 -> 检查是否双击
                    if (evt.clickCount == 2 || (EditorApplication.timeSinceStartup - _lastClickTime) < DoubleClickInterval)
                    {
                        OpenSelectedSceneAndClose();
                    }
                }

                _lastClickTime = EditorApplication.timeSinceStartup;
                evt.Use();
            }
            else if (evt.type == EventType.MouseDown && evt.button == 1)
            {
                // 右键弹出菜单
                var menu = new GenericMenu();
                menu.AddItem(
                    new GUIContent("打开"),
                    false,
                    () =>
                    {
                        _selectedIndex = index;
                        OpenSelectedSceneAndClose();
                    });
                menu.AddItem(new GUIContent("在 Project 中定位"), false, () => { EditorGUIUtility.PingObject(scene.SceneAsset); });
                menu.ShowAsContext();
                evt.Use();
            }
        }

        private void DrawBottomBar()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _selectedIndex >= 0 && _selectedIndex < _scenes.Count;
                if (GUILayout.Button("打开所选场景", GUILayout.Height(26)))
                    OpenSelectedSceneAndClose();
                GUI.enabled = true;

                // if (GUILayout.Button("关闭", GUILayout.Width(80), GUILayout.Height(26))) Close();
            }
        }

        private void OpenSelectedSceneAndClose()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _scenes.Count)
                return;
            var path = _scenes[_selectedIndex].Path;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
                Close();
            }
        }
    }
}