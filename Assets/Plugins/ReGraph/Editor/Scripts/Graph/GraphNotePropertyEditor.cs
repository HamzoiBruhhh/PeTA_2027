using UnityEngine;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
using Reshape.ReFramework;
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(GraphNoteProperty))]
    public class GraphNotePropertyEditor : PropertyDrawer
    {
        private float taHeight;
        private string message;
        private GraphSettings settings;

        public override void OnGUI (Rect pos, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(pos, label, property);
            pos = EditorGUI.PrefixLabel(pos, label);
            var taStyle = new GUIStyle(EditorStyles.textArea);

            if (settings == null)
                settings = GraphSettings.GetSettings();
            if (settings == null || settings.graphNoteDb == null)
            {
                var style = new GUIStyle {normal = {textColor = Color.red}};
                EditorGUI.LabelField(pos, "Please Define Graph Node DB", style);
            }
            else
            {
                string graphId = SerializedGraph.GetCurrentGraphId();
                if (graphId.Equals("0"))
                {
                    EditorGUILayout.HelpBox("Please save the scene before inserting note message.", MessageType.Warning);
                    GUI.enabled = false;
                    EditorGUI.TextArea(pos, message, taStyle);
                    GUI.enabled = true;
                }
                else
                {
                    var uid = $"{graphId}_{property.FindPropertyRelative("reid").stringValue}";
                    var previousMessage = settings.graphNoteDb.GetNote(uid);
                    message = previousMessage;
                    message = EditorGUI.TextArea(pos, message, taStyle);
                    if (!string.Equals(message, previousMessage))
                    {
                        message = Regex.Replace(message, @"[\p{C}-[\t\r\n]]+", "");
                        settings.graphNoteDb.SetNote(uid, message);
                        EditorUtility.SetDirty(settings.graphNoteDb);
                        property.FindPropertyRelative("dirty").boolValue = true;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            EditorGUI.EndProperty();

            if (pos.width > 1)
            {
                var guiContent = new GUIContent(message);
                taHeight = taStyle.CalcHeight(guiContent, pos.width);
            }
        }

        public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
        {
            var minHeight = EditorGUIUtility.singleLineHeight;
            return taHeight < minHeight ? minHeight : taHeight;
        }
    }
#endif
}