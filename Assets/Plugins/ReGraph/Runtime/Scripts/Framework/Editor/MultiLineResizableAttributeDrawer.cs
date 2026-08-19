using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MultiLineResizableAttribute))]
    public class MultiLineResizableAttributeDrawer : PropertyDrawer
    {
        private MultiLineResizableAttribute MultiLineResizable => (MultiLineResizableAttribute) attribute;

        private float taHeight;

        public override void OnGUI (Rect pos, SerializedProperty property, GUIContent label)
        {
            //SerializedProperty.hasMultipleDifferentValues
            //property.Next(true);
            
            label = EditorGUI.BeginProperty(pos, label, property);
            pos = EditorGUI.PrefixLabel(pos, label);

            GUIStyle taStyle = new GUIStyle(EditorStyles.textArea);

            var nextValue = EditorGUI.TextArea(pos, property.stringValue, taStyle);
            if (!string.Equals(nextValue, property.stringValue))
                property.stringValue = nextValue;
            EditorGUI.EndProperty();

            if (pos.width > 1)
            {
                GUIContent guiContent = new GUIContent(property.stringValue);
                taHeight = taStyle.CalcHeight(guiContent, pos.width);
            }
        }

        public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
        {
            float minHeight = EditorGUIUtility.singleLineHeight * MultiLineResizable.minLine;
            if (taHeight < minHeight)
                return minHeight;
            return taHeight;
        }
    }
#endif
}