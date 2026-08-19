using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reshape.Unity;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using Reshape.Unity.Editor;
#endif

namespace Reshape.ReGraph
{
    [AddComponentMenu("")]
    [HideMonoScript]
    public class GraphManager : ReSingletonBehaviour<GraphManager>
    {
        [HideIf("@runtimeSettings!=null")]
        public RuntimeSettings runtimeSettings;

        public int cacheSize;
        
        [Hint("showHints", "All actions here will get execute after the scene is fully loaded.")]
        [LabelText("Init Actions")]
        public FlowAction[] initActions;

        [Hint("showHints", "All actions here will get execute after all init actions have executed.")]
        [LabelText("Begin Actions")]
        public FlowAction[] beginActions;

        [Hint("showHints", "After all begin actions have executed, all actions here will get execute every frame.")]
        [LabelText("Tick Actions")]
        public FlowAction[] tickActions;

        [Hint("showHints", "All actions here will get execute before unload the scene and scene unloading will only execute after all actions here have executed.\n\nThese actions will only execute when using GraphManager.Exit to change scene.")]
        [LabelText("Uninit Actions")]
        public FlowAction[] uninitActions;

        private delegate void UpdateDelegate ();

        private UpdateDelegate updateDelegate;

        private string exitSceneName;

        public void Exit (string sceneName)
        {
            exitSceneName = sceneName;
            if (updateDelegate != UpdateTickFlow)
            {
                StartSystemUninitFlow();
                updateDelegate = UpdateUninitFlow;
                UpdateUninitFlow();
            }
        }

        protected override void Awake ()
        {
            base.Awake();
            InitSystemFlow();
        }

        protected void Start ()
        {
            updateDelegate = UpdateSystemInit;
        }

        protected void Update ()
        {
            if (updateDelegate != null)
                updateDelegate();
        }

        protected void OnDestroy ()
        {
            ClearInstance();
        }

        private void UpdateSystemInit ()
        {
            if (IsSystemFlowInited())
            {
                StartSystemInitFlow();
                updateDelegate = UpdateInitFlow;
                UpdateInitFlow();
            }
        }

        private void UpdateInitFlow ()
        {
            UpdateSystemFlow();
            if (IsSystemInitFlowCompleted())
            {
                FlowAction.ExecuteList(initActions);
                ReDebug.Log("GraphManager", "System Init Flow Completed");
                StartSystemBeginFlow();
                updateDelegate = UpdateBeginFlow;
                UpdateBeginFlow();
            }
        }

        private void UpdateBeginFlow ()
        {
            UpdateSystemFlow();
            if (IsSystemBeginFlowCompleted())
            {
                FlowAction.ExecuteList(beginActions);
                ReDebug.Log("GraphManager", "System Begin Flow Completed");
                updateDelegate = UpdateTickFlow;
            }
        }

        private void UpdateTickFlow ()
        {
            StartSystemTickFlow();
            if (!IsSystemTickFlowCompleted())
            {
                ReDebug.LogError("GraphManager", "System Update Flow Not Completed Within A Frame");
            }

            FlowAction.ExecuteList(tickActions);

            if (!string.IsNullOrEmpty(exitSceneName))
            {
                StartSystemUninitFlow();
                updateDelegate = UpdateUninitFlow;
                UpdateUninitFlow();
            }
        }

        private void UpdateUninitFlow ()
        {
            UpdateSystemFlow();
            if (IsSystemUninitFlowCompleted())
            {
                FlowAction.ExecuteList(uninitActions);
                ProjectileController.CleanAll();
                LootController.CleanAll();
                LootManager.Clear();
                InventoryManager.ClearInventoryRuntimeUsageOnly();
                ReDebug.Log("GraphManager", "System Uninit Flow Completed");
                updateDelegate = null;
                ClearInstance();
                ClearSystemFlow();
                StartCoroutine(LoadScene());
            }
        }
        
        public IEnumerator LoadScene()
        {
            yield return null;
            var unload = Resources.UnloadUnusedAssets();
            yield return unload;
            GC.Collect();
            yield return null;
            SceneManager.LoadScene(exitSceneName);
        }

#if UNITY_EDITOR
        public static bool CreateGraphManager ()
        {
            var settings = RuntimeSettings.GetSettings();
            if (settings != null)
            {
                var go = (GameObject) PrefabUtility.InstantiatePrefab(settings.graphManager);
                go.name = "Graph Manager";
                ReDebug.Log("Created Graph Manager GameObject!");
                EditorSceneManager.MarkAllScenesDirty();
                return true;
            }

            return false;
        }
        
        [HideInInspector]
        public bool showHints;

        [MenuItem("CONTEXT/GraphManager/Hints Display/Show", false)]
        public static void ShowHints (MenuCommand command)
        {
            var comp = (GraphManager) command.context;
            comp.showHints = true;
        }

        [MenuItem("CONTEXT/GraphManager/Hints Display/Show", true)]
        public static bool IsShowHints (MenuCommand command)
        {
            var comp = (GraphManager) command.context;
            if (comp.showHints)
                return false;
            return true;
        }

        [MenuItem("CONTEXT/GraphManager/Hints Display/Hide", false)]
        public static void HideHints (MenuCommand command)
        {
            var comp = (GraphManager) command.context;
            comp.showHints = false;
        }

        [MenuItem("CONTEXT/GraphManager/Hints Display/Hide", true)]
        public static bool IsHideHints (MenuCommand command)
        {
            var comp = (GraphManager) command.context;
            if (!comp.showHints)
                return false;
            return true;
        }
#endif
    }
}