using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using XuchFramework.Core;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    [CustomEditor(typeof(GameProcedure))]
    internal sealed class GameProcedureInspector : InspectorBase
    {
        private SerializedProperty _availableProcedureTypeNames;
        private SerializedProperty _startupProcedureTypeName;

        private string[] _allProcedureTypeNames;
        private List<string> _currentAvailableProcedureTypeNames = new();
        private int _currentStartupProcedureTypeNameIndex = -1;

        private void OnEnable()
        {
            _availableProcedureTypeNames = serializedObject.FindProperty("_availableProcedureTypeNames");
            _startupProcedureTypeName = serializedObject.FindProperty("_startupProcedureTypeName");

            UpdateSubtypeNames();
        }

        protected override void OnCompileFinish()
        {
            base.OnCompileFinish();

            UpdateSubtypeNames();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var targetComponent = target as GameProcedure;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Current Procedure",
                    targetComponent?.CurrentProcedure == null ? "None" : targetComponent.CurrentProcedure.GetType().Name);
                EditorGUILayout.LabelField("Current Procedure Time", FormatProcedureTime(targetComponent?.CurrentProcedureSeconds ?? 0));
                EditorGUILayout.Separator();
            }
            else if (string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("First procedure invalid.", MessageType.Error);
                EditorGUILayout.Separator();
            }

            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.LabelField("Available Procedures", EditorStyles.boldLabel);
                if (_allProcedureTypeNames.Length > 0)
                {
                    // Display checkboxes for all available procedure types
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        foreach (string typeName in _allProcedureTypeNames)
                        {
                            bool selectStatus = _currentAvailableProcedureTypeNames.Contains(typeName);
                            bool isSelectedByUser = EditorGUILayout.ToggleLeft(SimplifyTypeName(typeName), selectStatus);

                            if (isSelectedByUser != selectStatus)
                            {
                                if (isSelectedByUser)
                                {
                                    _currentAvailableProcedureTypeNames.Add(typeName);
                                    WritePropertyAvailableProcedureTypeNames();
                                }
                                else if (typeName != _startupProcedureTypeName.stringValue)
                                {
                                    _currentAvailableProcedureTypeNames.Remove(typeName);
                                    WritePropertyAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No available procedures.", MessageType.Warning);
                }

                if (_currentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();
                    if (string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
                    {
                        EditorGUILayout.HelpBox("Select a startup procedure.", MessageType.Warning);
                    }

                    var popupNames = new string[_currentAvailableProcedureTypeNames.Count];
                    for (int i = 0; i < _currentAvailableProcedureTypeNames.Count; i++)
                    {
                        popupNames[i] = SimplifyTypeName(_currentAvailableProcedureTypeNames[i]);
                    }

                    int selectedIndexByUser = EditorGUILayout.Popup("Startup Procedure", _currentStartupProcedureTypeNameIndex, popupNames.ToArray());
                    if (selectedIndexByUser != _currentStartupProcedureTypeNameIndex)
                    {
                        _currentStartupProcedureTypeNameIndex = selectedIndexByUser;
                        WritePropertyStartupProcedure(_currentAvailableProcedureTypeNames[_currentStartupProcedureTypeNameIndex]);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        /// <summary>
        /// Update all subtype names of procedures, and update available procedure type names property
        /// </summary>
        private void UpdateSubtypeNames()
        {
            _allProcedureTypeNames = TypeHelper.GetDerivedTypeNames(typeof(ProcedureBase));
            // Read the old _availableProcedureTypeNames, for comparing with new _allProcedureTypeNames
            _currentAvailableProcedureTypeNames.Clear();
            for (int i = 0; i < _availableProcedureTypeNames.arraySize; i++)
            {
                _currentAvailableProcedureTypeNames.Add(_availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }

            int countBeforeFilter = _currentAvailableProcedureTypeNames.Count;
            _currentAvailableProcedureTypeNames = _currentAvailableProcedureTypeNames.Where(x => _allProcedureTypeNames.Contains(x)).ToList();
            if (countBeforeFilter != _currentAvailableProcedureTypeNames.Count)
            {
                WritePropertyAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
            {
                _currentStartupProcedureTypeNameIndex = _currentAvailableProcedureTypeNames.IndexOf(_startupProcedureTypeName.stringValue);
                if (_currentStartupProcedureTypeNameIndex < 0)
                {
                    WritePropertyStartupProcedure(null);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void WritePropertyAvailableProcedureTypeNames()
        {
            _availableProcedureTypeNames.ClearArray();
            if (_currentAvailableProcedureTypeNames == null)
            {
                return;
            }

            _currentAvailableProcedureTypeNames.Sort();
            for (int i = 0; i < _currentAvailableProcedureTypeNames.Count; i++)
            {
                _availableProcedureTypeNames.InsertArrayElementAtIndex(i);
                _availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = _currentAvailableProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
            {
                _currentStartupProcedureTypeNameIndex = _currentAvailableProcedureTypeNames.IndexOf(_startupProcedureTypeName.stringValue);
                if (_currentStartupProcedureTypeNameIndex < 0)
                {
                    WritePropertyStartupProcedure(null);
                }
            }
        }

        private void WritePropertyStartupProcedure(string typeName)
        {
            _startupProcedureTypeName.stringValue = typeName;
        }

        private string SimplifyTypeName(string typeName)
        {
            var splited = typeName.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return splited[splited.Length - 1];
        }

        private string FormatProcedureTime(float totalSeconds)
        {
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            int seconds = (int)(totalSeconds % 60);

            if (hours > 24)
            {
                return "> 24h";
            }

            if (hours >= 1)
            {
                return $"{hours:00}h{minutes:00}m{seconds:00}s";
            }

            if (minutes >= 1)
            {
                return $"{minutes:00}m{seconds:00}s";
            }

            return $"{seconds:00}s";
        }
    }
}