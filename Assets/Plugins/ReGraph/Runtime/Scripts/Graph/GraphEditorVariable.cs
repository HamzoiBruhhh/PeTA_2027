#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    public static class GraphEditorVariable
    {
#if UNITY_EDITOR
        private const string GRAPH_PATH = "serializedGraph.graphPath";

        public static string GetPath ()
        {
            return EditorPrefs.GetString(GRAPH_PATH, string.Empty);
        }
        
        public static void SetPath (string value)
        {
            EditorPrefs.SetString(GRAPH_PATH, value);
        }
        
        public static string GetString (string id, string variable)
        {
            return EditorPrefs.GetString(id+variable, string.Empty);
        }
        
        public static void SetString (string id, string variable, string value)
        {
            EditorPrefs.SetString(id+variable, value);
        }
        
        public static bool GetBool (string id, string variable)
        {
            return EditorPrefs.GetBool(id+variable, false);
        }
        
        public static void SetBool (string id, string variable, bool value)
        {
            EditorPrefs.SetBool(id+variable, value);
        }
#endif
    }
}