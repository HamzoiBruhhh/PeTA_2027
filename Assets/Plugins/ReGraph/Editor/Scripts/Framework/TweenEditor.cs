using System.IO;
using Reshape.ReGraph;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Reshape.ReFramework
{
    public static class TweenEditor
    {
        [MenuItem("Tools/Reshape/Delete Unused TweenData", priority = 15107)]
        public static void DeleteUnusedTweenData ()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            CleanTweenData(SceneManager.GetActiveScene(), activeScene.path);
        }
        
        public static void CleanTweenData (Scene scene, string path)
        {
            var settings = FrameworkSettings.GetSettings();
            if (settings == null)
                return;
            if (!settings.tweenDataRemoveAtSaveScene)
                return;

            string sceneFolder = Path.GetDirectoryName(path);
            string folderName = Path.GetFileNameWithoutExtension(path);
#if UNITY_EDITOR_WIN
            string dataFolder = sceneFolder + $"\\{folderName}\\";
#elif UNITY_EDITOR_OSX
            string dataFolder = sceneFolder + $"/{folderName}/";
#endif
            if (!Directory.Exists(dataFolder))
                return;
            EditorUtility.DisplayProgressBar("Graph Saving", "Searching tween data files", 0);
            var foundAllGraphRunner = UnityEngine.Object.FindObjectsOfType(typeof(GraphRunner), true);
            var graphRunnerCount = foundAllGraphRunner.Length;
            var folderInfo = new DirectoryInfo(dataFolder);
            var folderFilesInfo = folderInfo.GetFiles("*.asset");
            var folderFilesInfoCount = folderFilesInfo.Length;
            var currentProcessing = 0f;
            foreach (var fileInfo in folderFilesInfo)
            {
                currentProcessing++;
                EditorUtility.DisplayProgressBar("Graph Saving", "Processing tween data files", currentProcessing/folderFilesInfoCount);
                var graphId = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));
                if (string.IsNullOrEmpty(graphId))
                {
                    var found = false;
                    for (var i = 0; i < graphRunnerCount; i++)
                    {
                        var runner = (GraphRunner) foundAllGraphRunner[i];
                        if (runner.graph.id.Equals(graphId))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;
                    string filePath = dataFolder + fileInfo.Name;
                    if (!File.Exists(filePath))
                        continue;
                    AssetDatabase.DeleteAsset(filePath);
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }
    }
}