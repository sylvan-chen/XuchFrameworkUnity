using DigiEden.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    [CustomPropertyDrawer(typeof(AxisMask))]
    public class AxisMaskDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                var boldLabel = new GUIContent(label.text, label.tooltip);
                var originalFontStyle = EditorStyles.label.fontStyle;
                EditorStyles.label.fontStyle = FontStyle.Bold;
                var contentRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), boldLabel);
                EditorStyles.label.fontStyle = originalFontStyle;

                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                float toggleWidth = 15f;
                float labelWidth = 15f;
                float toggleLabelGap = 2f; // Gap between toggle and its label
                float spacing = 0f;        // Gap between different axis groups

                float startX = contentRect.x;

                var xToggleRect = new Rect(startX, contentRect.y, toggleWidth, contentRect.height);
                var xLabelRect = new Rect(startX + toggleWidth + toggleLabelGap, contentRect.y, labelWidth, contentRect.height);

                float yStart = startX + toggleWidth + toggleLabelGap + labelWidth + spacing;
                var yToggleRect = new Rect(yStart, contentRect.y, toggleWidth, contentRect.height);
                var yLabelRect = new Rect(yStart + toggleWidth + toggleLabelGap, contentRect.y, labelWidth, contentRect.height);

                float zStart = yStart + toggleWidth + toggleLabelGap + labelWidth + spacing;
                var zToggleRect = new Rect(zStart, contentRect.y, toggleWidth, contentRect.height);
                var zLabelRect = new Rect(zStart + toggleWidth + toggleLabelGap, contentRect.y, labelWidth, contentRect.height);

                SerializedProperty xProp = property.FindPropertyRelative("X");
                SerializedProperty yProp = property.FindPropertyRelative("Y");
                SerializedProperty zProp = property.FindPropertyRelative("Z");

                EditorGUI.PropertyField(xToggleRect, xProp, GUIContent.none);
                EditorGUI.LabelField(xLabelRect, "X");

                EditorGUI.PropertyField(yToggleRect, yProp, GUIContent.none);
                EditorGUI.LabelField(yLabelRect, "Y");

                EditorGUI.PropertyField(zToggleRect, zProp, GUIContent.none);
                EditorGUI.LabelField(zLabelRect, "Z");

                EditorGUI.indentLevel = indent;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}