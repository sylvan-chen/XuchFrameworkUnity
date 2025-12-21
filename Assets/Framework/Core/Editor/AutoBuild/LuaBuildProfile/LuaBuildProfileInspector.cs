using System.IO;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    [CustomEditor(typeof(LuaBuildProfile))]
    public class LuaBuildProfileInspector : InspectorBase
    {
        private SerializedProperty _luaScriptsDirectory;
        private SerializedProperty _encryptedLuaScriptsOutputDirectory;
        private SerializedProperty _ignoredDirectoryNames;
        private SerializedProperty _addressableGroupNames;
        private SerializedProperty _addressableLabels;

        private void OnEnable()
        {
            _luaScriptsDirectory = serializedObject.FindProperty(nameof(LuaBuildProfile.LuaScriptsDirectory));
            _encryptedLuaScriptsOutputDirectory = serializedObject.FindProperty(nameof(LuaBuildProfile.EncryptedLuaScriptsOutputDirectory));
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
            EditorGUILayout.PropertyField(_encryptedLuaScriptsOutputDirectory);
            EditorGUILayout.PropertyField(_ignoredDirectoryNames);
            EditorGUILayout.PropertyField(_addressableGroupNames);
            EditorGUILayout.PropertyField(_addressableLabels);

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void UpdatePathProperties()
        {
            _luaScriptsDirectory.stringValue = Path.GetFullPath(_luaScriptsDirectory.stringValue, Application.dataPath);
            _encryptedLuaScriptsOutputDirectory.stringValue = Path.GetFullPath(_encryptedLuaScriptsOutputDirectory.stringValue, Application.dataPath);

            _luaScriptsDirectory.stringValue = GameHelper.GetRegularPath(_luaScriptsDirectory.stringValue);
            _encryptedLuaScriptsOutputDirectory.stringValue = GameHelper.GetRegularPath(_encryptedLuaScriptsOutputDirectory.stringValue);
        }
    }
}