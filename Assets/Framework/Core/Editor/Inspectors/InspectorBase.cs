using UnityEditor;
using UnityEngine;

namespace Framework.Editor
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

        protected virtual void OnCompileStart() { }

        protected virtual void OnCompileFinish() { }

        private static void DrawScriptField(Object target)
        {
            if (target == null)
                return;

            MonoScript script = target switch
            {
                MonoBehaviour mono => MonoScript.FromMonoBehaviour(mono),
                ScriptableObject so => MonoScript.FromScriptableObject(so),
                // Try to find by type name for other types (EditorWindow, custom objects, etc.) 
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