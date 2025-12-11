using UnityEditor;
using UnityEngine;

namespace Xuch.Editor
{
    public abstract class InspectorBase : UnityEditor.Editor
    {
        private bool _isCompileStart = false;

        public override void OnInspectorGUI()
        {
            DrawScriptField(target);

            if (!_isCompileStart && EditorApplication.isCompiling)
            {
                _isCompileStart = true;
                OnCompileStart();
            }
            else if (_isCompileStart && !EditorApplication.isCompiling)
            {
                _isCompileStart = false;
                OnCompileFinish();
            }
        }

        /// <summary>
        /// 编译开始事件
        /// </summary>
        protected virtual void OnCompileStart() { }

        /// <summary>
        /// 编译结束事件
        /// </summary>
        protected virtual void OnCompileFinish() { }

        private static void DrawScriptField(Object target)
        {
            if (target == null)
                return;

            MonoScript script = target switch
            {
                MonoBehaviour mono => MonoScript.FromMonoBehaviour(mono),
                ScriptableObject so => MonoScript.FromScriptableObject(so),
                // 其他类型（EditorWindow、自定义对象等）尝试通过类型名查找
                _ => FindScriptFromType(target.GetType())
            };

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }

        private static MonoScript FindScriptFromType(System.Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (string guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return script;
            }
            return null;
        }
    }
}