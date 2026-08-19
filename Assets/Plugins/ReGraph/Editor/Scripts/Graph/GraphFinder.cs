using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Reshape.ReGraph
{
    public class GraphFinder : OdinEditorWindow
    {
        [InfoBox("This tool is used to find specific node type that using at Graph Runner in the scene")]
        [PropertyOrder(-10)]
        [ValueDropdown("TypeChoice")]
        [OnValueChanged("OnChangeType")]
        [InlineButton("SearchType", "↺")]
        public Type searchType;

        [PropertyOrder(-9)]
        [ShowIf("ShowTriggerType")]
        [OnValueChanged("SearchType")]
        [ValueDropdown("TriggerTypeChoice")]
        public TriggerNode.Type triggerType;

        [PropertyOrder(-9)]
        [Space(5)]
        [LabelText("Found GameObject")]
        [OnInspectorGUI("DisableGUIAfter")]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, IsReadOnly = true, ShowPaging = false)]
        public List<GameObject> matchGo;

        [MenuItem("Tools/Reshape/Graph Finder", priority = 11002)]
        public static void OpenWindow ()
        {
            var window = GetWindow<GraphFinder>();
            window.Show();
        }

        public void Reset ()
        {
            if (matchGo != null)
                matchGo.Clear();
            searchType = default;
        }

        private void OnChangeType ()
        {
            triggerType = TriggerNode.Type.None;
            SearchType();
        }

        private void SearchType ()
        {
            if (searchType == null)
                return;
            matchGo = new List<GameObject>();
            EditorUtility.DisplayProgressBar("Graph Finder", "Search " + searchType.Name, 0);
            var found = FindObjectsOfType(typeof(GraphRunner), true);
            var searchLength = found.Length;
            for (var i = 0; i < searchLength; i++)
            {
                EditorUtility.DisplayProgressBar("Graph Finder", $"Search {searchType.Name} ({(i + 1).ToString()}/{searchLength.ToString()})", (i + 1f) / searchLength);
                var runner = (GraphRunner) found[i];
                if (runner.ContainNode(searchType))
                {
                    if (ShowTriggerType())
                    {
                        if (triggerType != TriggerNode.Type.None)
                        {
                            if (runner.ContainTriggerNode(triggerType))
                                matchGo.Add(runner.gameObject);
                        }
                        else
                        {
                            matchGo.Add(runner.gameObject);
                        }
                    }
                    else
                    {
                        matchGo.Add(runner.gameObject);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private bool ShowTriggerType ()
        {
            return searchType != null && searchType.IsSubclassOf(typeof(TriggerNode));
        }

        public ValueDropdownList<Type> TypeChoice ()
        {
            var listDropdown = new ValueDropdownList<Type>();
            var types = TypeCache.GetTypesDerivedFrom<GraphNode>();
            foreach (var type in types)
            {
                if (type == typeof(BehaviourNode) || type == typeof(ConditionNode) || type == typeof(TriggerNode) || type == typeof(RootNode))
                    continue;
                listDropdown.Add($"{type.Name.Substring(0, type.Name.LastIndexOf("Node", StringComparison.Ordinal))}", type);
            }

            return listDropdown;
        }
        
        public ValueDropdownList<TriggerNode.Type> TriggerTypeChoice ()
        {
            var listDropdown = new ValueDropdownList<TriggerNode.Type>();
            foreach (TriggerNode.Type value in Enum.GetValues(typeof(TriggerNode.Type)))
                if (value != TriggerNode.Type.All && value != TriggerNode.Type.None)
                    listDropdown.Add(value.ToString(), value);
            return listDropdown;
        }

        private void DisableGUIAfter ()
        {
            GUI.enabled = false;
        }
        
#if UNITY_EDITOR
        [InitializeOnLoad]
        public static class GraphFinderResetOnPlay
        {
            static GraphFinderResetOnPlay ()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }

            private static void OnPlayModeChanged (PlayModeStateChange state)
            {
                bool update = false;
                if ( state == PlayModeStateChange.EnteredEditMode )
                {
                    if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                        update = true;
                }

                if (update)
                {
                    if (HasOpenInstances<GraphFinder>())
                    {
                        var window = GetWindow<GraphFinder>();
                        if (window != null)
                            window.Reset();
                    }
                }
            }
        }
#endif
    }
}