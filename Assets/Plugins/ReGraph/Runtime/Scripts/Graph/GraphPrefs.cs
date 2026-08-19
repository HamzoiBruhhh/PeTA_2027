#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    public static class GraphPrefs
    {
        public const string GraphPrefLabelBehaviourShowText = "GraphEditorPrefs.LabelBehaviourShowText";
        
#if UNITY_EDITOR
        public static bool showLabelBehaviourText
        {
            get => EditorPrefs.GetBool(GraphPrefs.GraphPrefLabelBehaviourShowText, true);
            set => EditorPrefs.SetBool(GraphPrefs.GraphPrefLabelBehaviourShowText, value);
        }
#endif
    }
}