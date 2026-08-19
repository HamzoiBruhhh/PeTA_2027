using System.Collections.Generic;
using Reshape.Unity;
using Reshape.Unity.Editor;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
#if REGRAPH_DEV_DEBUG
    [CreateAssetMenu(menuName = "Reshape/Framework Settings", order = 201)]
#endif
    [HideMonoScript]
    public class FrameworkSettings : ScriptableObject
    {
        [BoxGroup("Tween"), LabelWidth(180)]
        [LabelText("Remove At Save Scene")]
        public bool tweenDataRemoveAtSaveScene;

        [BoxGroup("Character Control"), LabelWidth(180)]
        [LabelText("1st Person Controller")]
        public GameObject fpPlayerController;

        [BoxGroup("Character Control"), LabelWidth(180)]
        [LabelText("3rd Person Controller")]
        public GameObject tpPlayerController;
        
        [BoxGroup("Character Control"), LabelWidth(180)]
        [LabelText("Top Down Person Controller")]
        public GameObject tdPlayerController;

        private static FrameworkSettings FindSettings ()
        {
            var guids = AssetDatabase.FindAssets("t:FrameworkSettings");
            if (guids.Length > 1)
            {
                ReDebug.LogWarning("Graph Editor", $"Found multiple settings files, currently is using the first found settings file.", false);
            }

            switch (guids.Length)
            {
                case 0:
                    return null;
                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<FrameworkSettings>(path);
            }
        }

        public static FrameworkSettings GetSettings ()
        {
            var settings = FindSettings();
            return settings;
        }

        internal static SerializedObject GetSerializedSettings ()
        {
            return new SerializedObject(GetSettings());
        }
        
        [MenuItem("GameObject/Reshape/Top Down Person Controller", false, -100)]
        public static void AddTdPlayerController()
        {
            if ( ReEditorHelper.IsInPrefabStage() )
            {
                ReDebug.LogWarning("Not able to do add Top Down Person Controller when you are editing a prefab!");
                EditorUtility.DisplayDialog("Top Down Person Controller", "Not able to do add controller when you are editing a prefab!", "OK");
                return;
            }
            
            var controller = FindObjectOfType<TopDownPersonController>();
            if (controller != null)
            {
                ReDebug.LogWarning("Not able to do add Top Down Person Controller since there is already have one in the scene!");
                EditorUtility.DisplayDialog("Top Down Person Controller", "Not able to do add controller since there is already have one in the scene!", "OK");
                return;
            }

            if (Camera.main != null)
            {
                ReDebug.LogWarning("Please aware there is a camera in Top Down Person Controller while your scene have another MainCamera!");
                EditorUtility.DisplayDialog("Top Down Person Controller", "Please aware there is a camera in Top Down Person Controller while your scene have another MainCamera!", "OK");
            }

            var settings = GetSettings();
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(settings.tdPlayerController);
            go.name = "Top Down Person Controller";
            ReDebug.Log("Created Top Down Person Controller GameObject!");
            EditorSceneManager.MarkAllScenesDirty();
        }
        
        [MenuItem("GameObject/Reshape/1st Person Controller", false, -102)]
        public static void AddFpPlayerController()
        {
            if ( ReEditorHelper.IsInPrefabStage() )
            {
                ReDebug.LogWarning("Not able to do add 1st Person Controller when you are editing a prefab!");
                EditorUtility.DisplayDialog("1st Person Controller", "Not able to do add controller when you are editing a prefab!", "OK");
                return;
            }
            
            var controller = FindObjectOfType<FirstPersonController>();
            if (controller != null)
            {
                ReDebug.LogWarning("Not able to do add 1st Person Controller since there is already have one in the scene!");
                EditorUtility.DisplayDialog("1st Person Controller", "Not able to do add controller since there is already have one in the scene!", "OK");
                return;
            }

            if (Camera.main != null)
            {
                ReDebug.LogWarning("Please aware there is a camera in 1st Person Controller while your scene have another MainCamera!");
                EditorUtility.DisplayDialog("1st Person Controller", "Please aware there is a camera in 1st Person Controller while your scene have another MainCamera!", "OK");
            }

            var settings = GetSettings();
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(settings.fpPlayerController);
            go.name = "First Person Controller";
            ReDebug.Log("Created First Person Controller GameObject!");
            EditorSceneManager.MarkAllScenesDirty();
        }
        
        [MenuItem("GameObject/Reshape/3rd Person Controller", false, -101)]
        public static void AddTpPlayerController()
        {
            if ( ReEditorHelper.IsInPrefabStage() )
            {
                ReDebug.LogWarning("Not able to do add 3rd Person Controller when you are editing a prefab!");
                EditorUtility.DisplayDialog("3rd Person Controller", "Not able to do add controller when you are editing a prefab!", "OK");
                return;
            }
            
            var controller = FindObjectOfType<ThirdPersonController>();
            if (controller != null)
            {
                ReDebug.LogWarning("Not able to do add 3rd Person Controller since there is already have one in the scene!");
                EditorUtility.DisplayDialog("3rd Person Controller", "Not able to do add controller since there is already have one in the scene!", "OK");
                return;
            }
            
            if (Camera.main != null)
            {
                ReDebug.LogWarning("Please aware there is a camera in 3rd Person Controller while your scene have another MainCamera!");
                EditorUtility.DisplayDialog("3rd Person Controller", "Please aware there is a camera in 3rd Person Controller while your scene have another MainCamera!", "OK");
            }

            var settings = GetSettings();
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(settings.tpPlayerController);
            go.name = "Third Person Controller";
            ReDebug.Log("Created Third Person Controller GameObject!");
            EditorSceneManager.MarkAllScenesDirty();
        }
    }
}