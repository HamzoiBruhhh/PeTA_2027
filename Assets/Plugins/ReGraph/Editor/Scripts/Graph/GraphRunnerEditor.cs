using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Reshape.Unity.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

namespace Reshape.ReGraph
{
    [CustomEditor(typeof(GraphRunner))]
    public class GraphRunnerEditor : OdinEditor
    {
        protected string saveSceneMessage = "All graph data in saved in the scene file.";
        protected string savePrefabMessage = "All graph data in saved in the prefab file.";
        protected string autoSavePrefabMessage = "Turn off Auto Save in Prefab Mode in order to increase the graph editing performance.";
        protected string editPrefabMessage = "Graph keep in prefab must edit in Prefab Isolation Mode.";
        protected string editContextMessage = "Only can edit this graph in Prefab Isolation Mode.";

        private Graph graphCache;
        private List<GameObject> referenceGos;
        private string referenceObjId;

        public void OnDestroy ()
        {
            if (graphCache != null)
            {
                graphCache.InitPreviewNode();
                graphCache = null;
            }
        }

        public override void OnInspectorGUI ()
        {
            if (!EditorApplication.isPlaying)
            {
                if (Tree != null && Tree.UnitySerializedObject != null)
                {
                    if (graphCache == null)
                    {
                        var graph = SerializedGraph.GetGraphFromSerializedTargetObject(Tree.UnitySerializedObject.targetObject);
                        if (graph.isCreated)
                            graphCache = graph;
                    }

                    if (graphCache != null)
                    {
                        if (!graphCache.HavePreviewNode())
                        {
                            var isSelectingPrefabAtHierarchy = false;
                            if (SerializedGraph.IsRunnerGraph(Tree.UnitySerializedObject))
                            {
                                var go = (GameObject) SerializedGraph.GetSelectionFromSerializedObject(serializedObject);
                                if (!go.scene.IsValid())
                                    isSelectingPrefabAtHierarchy = true;
                            }

                            if (!isSelectingPrefabAtHierarchy)
                            {
                                var connectedPrefab = PrefabUtility.GetPrefabInstanceStatus(Tree.UnitySerializedObject.targetObject) == PrefabInstanceStatus.Connected;
                                if (!connectedPrefab)
                                {
                                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                                    if (prefabStage != null)
                                    {
                                        if (prefabStage.mode == PrefabStage.Mode.InContext)
                                        {
                                            EditorGUILayout.HelpBox(editContextMessage, MessageType.Warning);
                                            return;
                                        }
                                        else
                                        {
                                            EditorGUILayout.HelpBox(savePrefabMessage, MessageType.Info);
                                            if (ReEditorHelper.IsPrefabStageAutoSave())
                                                EditorGUILayout.HelpBox(autoSavePrefabMessage, MessageType.Warning);
                                            if (GUILayout.Button("Edit Graph"))
                                            {
                                                GraphEditorWindow.OpenWindow(new SerializedObject(Tree.UnitySerializedObject.targetObject));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        EditorGUILayout.HelpBox(saveSceneMessage, MessageType.Info);
                                        if (GUILayout.Button("Edit Graph"))
                                        {
                                            GraphEditorWindow.OpenWindow(new SerializedObject(Tree.UnitySerializedObject.targetObject));
                                        }
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox(editContextMessage, MessageType.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                var scriptablePath = string.Empty;
                                foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
                                {
                                    var go = (GameObject) obj;
                                    if (go.GetComponent<GraphRunner>())
                                    {
                                        var path = AssetDatabase.GetAssetPath(obj);
                                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                                        {
                                            scriptablePath = path;
                                            break;
                                        }
                                    }
                                }

                                EditorGUILayout.HelpBox(editPrefabMessage, MessageType.Info);
                                if (!string.IsNullOrEmpty(scriptablePath))
                                {
                                    if (GUILayout.Button("Edit Prefab"))
                                    {
                                        PrefabStageUtility.OpenPrefab(scriptablePath);
                                    }
                                }
                            }

                            base.OnInspectorGUI();

                            if (IsShowCleanGraphButton(graphCache.nodes))
                            {
                                var nodes = DisplayCleanGraphButton(graphCache.nodes);
                                if (nodes != null)
                                {
                                    graphCache.nodes = nodes;
                                    InspectorUtilities.RegisterUnityObjectDirty(Tree.UnitySerializedObject.targetObject);
                                    GraphEditorWindow.RefreshCurrentGraph(false);
                                }
                            }

                            return;
                        }

                        if (!GraphEditorWindow.HasFocus())
                        {
                            graphCache.InitPreviewNode();
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Graph will not save during Play Mode", MessageType.Warning);
            }

            base.OnInspectorGUI();

            referenceGos ??= new List<GameObject>();
            if (graphCache != null && graphCache.HavePreviewNode() && graphCache.previewNode is ActionTriggerNode actionTrigger)
            {
                if (GUILayout.Button("Find Action Usages"))
                {
                    referenceObjId = actionTrigger.id;
                    referenceGos.Clear();
                    EditorUtility.DisplayProgressBar("Graph Finder", "Search Action References", 0);
                    UnityEngine.Object[] found = null;
                    if (PrefabStageUtility.GetCurrentPrefabStage() == null)
                        found = UnityEngine.Object.FindObjectsOfType(typeof(GraphRunner));
                    else
                        found = PrefabStageUtility.GetCurrentPrefabStage().FindComponentsOfType<GraphRunner>();
                    var searchLength = found.Length;
                    for (var i = 0; i < searchLength; i++)
                    {
                        EditorUtility.DisplayProgressBar("Graph Finder", $"Search Action References ({(i + 1).ToString()}/{searchLength.ToString()})", (i + 1f) / searchLength);
                        var runner = (GraphRunner) found[i];
                        if (runner.HaveNodeLinkAction(actionTrigger.GetRunner(), actionTrigger.ActionName, actionTrigger.TriggerId))
                        {
                            referenceGos.Add(runner.gameObject);
                        }
                    }

                    EditorUtility.ClearProgressBar();
                }

                if (string.Equals(referenceObjId, graphCache.previewNode.id))
                {
                    for (var i = 0; i < referenceGos.Count; i++)
                    {
                        EditorGUILayout.ObjectField("Action References " + (i + 1), referenceGos[i], typeof(GameObject), true);
                    }
                }
            }
        }

        private bool IsShowCleanGraphButton (List<GraphNode> nodes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    return true;
                var children = new List<GraphNode>();
                nodes[i].GetChildren(ref children);
                for (var j = 0; j < children.Count; j++)
                {
                    if (children[j] == null)
                        return true;
                }
            }

            return false;
        }

        private List<GraphNode> DisplayCleanGraphButton (List<GraphNode> nodes)
        {
            if (GUILayout.Button("Clean Graph"))
            {
                if (EditorUtility.DisplayDialog("Clean Graph", "Are you sure you would like to clean up the graph?", "YES", "NO"))
                {
                    for (var i = 0; i < nodes.Count; i++)
                    {
                        var node = nodes[i];
                        if (node == null)
                        {
                            nodes.RemoveAt(i);
                            i--;
                            continue;
                        }

                        for (var j = 0; j < node.children.Count; j++)
                        {
                            if (node.children[j] == null)
                            {
                                node.children.RemoveAt(j);
                                j--;
                            }
                        }
                    }

                    return nodes;
                }
            }

            return null;
        }
    }
}