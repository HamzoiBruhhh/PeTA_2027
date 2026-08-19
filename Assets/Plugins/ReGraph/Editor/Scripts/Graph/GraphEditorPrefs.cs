using UnityEditor;

namespace Reshape.ReGraph
{
    public static class GraphEditorPrefs
    {
        private const string EditorPrefAutoSaveModification = "GraphEditorPrefs.AutoSaveModification";

        public static bool autoSaveModification
        {
            get => EditorPrefs.GetBool(EditorPrefAutoSaveModification, true);
            set => EditorPrefs.SetBool(EditorPrefAutoSaveModification, value);
        }
    }
}