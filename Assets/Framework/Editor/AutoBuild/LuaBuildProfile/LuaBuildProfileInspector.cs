using System.IO;
using DigiEden.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    [CustomEditor(typeof(LuaBuildProfile))]
    public class LuaBuildProfileInspector : InspectorBase
    {
        private SerializedProperty _luaScriptsDirectory;
        private SerializedProperty _encryptedLuaSciptsOutputDirectory;
        private SerializedProperty _ignoredDirectoryNames;
        private SerializedProperty _addressableGroupNames;
        private SerializedProperty _addressableLabels;

        private void OnEnable()
        {
            _luaScriptsDirectory = serializedObject.FindProperty(nameof(LuaBuildProfile.LuaScriptsDirectory));
            _encryptedLuaSciptsOutputDirectory = serializedObject.FindProperty(nameof(LuaBuildProfile.EncryptedLuaSciptsOutputDirectory));
            _ignoredDirectoryNames = serializedObject.FindProperty(nameof(LuaBuildProfile.IgnoredDirectoryNames));
            _addressableGroupNames = serializedObject.FindProperty(nameof(LuaBuildProfile.AddressableGroupName));
            _addressableLabels = serializedObject.FindProperty(nameof(LuaBuildProfile.AddressableLabel));

            UpdatePathProperties();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            UpdatePathProperties();

            EditorGUILayout.PropertyField(_luaScriptsDirectory);
            EditorGUILayout.PropertyField(_encryptedLuaSciptsOutputDirectory);
            EditorGUILayout.PropertyField(_ignoredDirectoryNames);
            EditorGUILayout.PropertyField(_addressableGroupNames);
            EditorGUILayout.PropertyField(_addressableLabels);

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void UpdatePathProperties()
        {
            _luaScriptsDirectory.stringValue = Path.GetFullPath(_luaScriptsDirectory.stringValue, Application.dataPath);
            _encryptedLuaSciptsOutputDirectory.stringValue = Path.GetFullPath(_encryptedLuaSciptsOutputDirectory.stringValue, Application.dataPath);

            _luaScriptsDirectory.stringValue = Framework.Utils.PathHelper.GetRegularPath(_luaScriptsDirectory.stringValue);
            _encryptedLuaSciptsOutputDirectory.stringValue = Framework.Utils.PathHelper.GetRegularPath(_encryptedLuaSciptsOutputDirectory.stringValue);
        }
    }
}