using Reshape.ReFramework;
using Reshape.Unity;
using Reshape.Unity.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reshape.ReGraph
{
    public static class GraphManagerEditor
    {
        [MenuItem("GameObject/Reshape/Graph Manager", false, -120)]
        public static void AddGraphManager ()
        {
            if (ReEditorHelper.IsInPrefabStage())
            {
                ReDebug.LogWarning("Not able to do add Graph Manager when you are editing a prefab!");
                EditorUtility.DisplayDialog("Graph Manager", "Not able to do add Graph Manager when you are editing a prefab!", "OK");
                return;
            }

            var manager = Object.FindObjectOfType<GraphManager>();
            if (manager != null)
            {
                ReDebug.LogWarning("Not able to do add Graph Manager since there is already Graph Manager in the scene!");
                EditorUtility.DisplayDialog("Graph Manager", "Not able to do add Graph Manager since there is already Graph Manager in the scene!", "OK");
                return;
            }

            GraphManager.CreateGraphManager();
        }

    }
}