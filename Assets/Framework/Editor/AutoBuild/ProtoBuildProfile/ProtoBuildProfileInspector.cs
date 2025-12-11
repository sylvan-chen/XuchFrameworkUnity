using System.IO;
using Xuch.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace Xuch.Editor
{
    [CustomEditor(typeof(ProtoBuildProfile))]
    public class ProtoBuildProfileInspector : InspectorBase
    {
        private SerializedProperty _protoDirectory;
        private SerializedProperty _encryptedProtoOutputDirectory;
        private SerializedProperty _ignoredDirectoryNames;
        private SerializedProperty _addressableGroupNames;
        private SerializedProperty _addressableLabels;

        private void OnEnable()
        {
            _protoDirectory = serializedObject.FindProperty(nameof(ProtoBuildProfile.ProtosDirectory));
            _encryptedProtoOutputDirectory = serializedObject.FindProperty(nameof(ProtoBuildProfile.EncryptedProtoOutputDirectory));
            _ignoredDirectoryNames = serializedObject.FindProperty(nameof(ProtoBuildProfile.IgnoredDirectoryNames));
            _addressableGroupNames = serializedObject.FindProperty(nameof(ProtoBuildProfile.AddressableGroupName));
            _addressableLabels = serializedObject.FindProperty(nameof(ProtoBuildProfile.AddressableLabel));

            UpdatePathProperties();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            UpdatePathProperties();

            EditorGUILayout.PropertyField(_protoDirectory);
            EditorGUILayout.PropertyField(_encryptedProtoOutputDirectory);
            EditorGUILayout.PropertyField(_ignoredDirectoryNames);
            EditorGUILayout.PropertyField(_addressableGroupNames);
            EditorGUILayout.PropertyField(_addressableLabels);

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void UpdatePathProperties()
        {
            _protoDirectory.stringValue = Path.GetFullPath(_protoDirectory.stringValue, Application.dataPath);
            _encryptedProtoOutputDirectory.stringValue = Path.GetFullPath(_encryptedProtoOutputDirectory.stringValue, Application.dataPath);

            _protoDirectory.stringValue = Framework.Utils.PathHelper.GetRegularPath(_protoDirectory.stringValue);
            _encryptedProtoOutputDirectory.stringValue = Framework.Utils.PathHelper.GetRegularPath(_encryptedProtoOutputDirectory.stringValue);
        }
    }
}