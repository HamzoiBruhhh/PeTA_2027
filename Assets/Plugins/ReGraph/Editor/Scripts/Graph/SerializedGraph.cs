using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Reshape.ReFramework;
using Reshape.Unity.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Reshape.ReGraph
{
    public class SerializedGraph
    {
        private static SerializedGraph me;

        public readonly SerializedObject serializedObject;
        public readonly UnityEngine.Object graphSelectionObject;
        public readonly int graphObjectId;
        public readonly Graph graph;
        public string graphPath;
        public bool graphRequireUpdateAndSave;
        public int haveChangeNotSave;

        public const int UpdateGraphId = 10;
        const string sPropRootNode = "graph.rootNode";
        const string sPropNodes = "graph.nodes";
        const string sPropBlackboard = "blackboard";
        const string sPropGuid = "guid";
        const string sPropChild = "child";
        const string sPropChildren = "children";
        const string sPropParent = "parent";
        const string sPropParents = "parents";
        const string sPropPosition = "position";
        const string sViewTransformPosition = "graph.viewPosition";
        const string sViewTransformScale = "graph.viewScale";
        const string sPropGraphId = "graph.id";

        public SerializedProperty RootNode => serializedObject.FindProperty(sPropRootNode);
        public SerializedProperty Nodes => serializedObject.FindProperty(sPropNodes);
        public SerializedProperty Blackboard => serializedObject.FindProperty(sPropBlackboard);

        public delegate void GraphDelegate (int type);

        public event GraphDelegate OnUpdate;

        public static Graph GetGraphFromSelectionObject (Object selectionObject)
        {
            return Graph.GetFromSelectionObject(selectionObject);
        }

        public static Graph GetGraphFromSerializedTargetObject (Object serializedObject)
        {
            var runner = serializedObject as GraphRunner;
            if (runner != null)
                return runner.graph;
            var scriptable = serializedObject as GraphScriptable;
            if (scriptable != null)
                return scriptable.graph;
            return null;
        }

        public static Object GetSelectionFromSerializedObject (SerializedObject serializedObject)
        {
            if (serializedObject.targetObject is GraphRunner runner)
                return runner.gameObject;
            if (serializedObject.targetObject is GraphScriptable scriptable)
                return scriptable;
            return null;
        }
        
        public bool IsSelectedGraphObject (ActionBehaviourNode actionBehaviourNode)
        {
            if (actionBehaviourNode != null)
            {
                if (serializedObject.targetObject is GraphRunner runner)
                    return IsSelectedGraphObject(actionBehaviourNode.Runner);
                else if (serializedObject.targetObject is GraphScriptable scriptable)
                    return IsSelectedGraphObject(actionBehaviourNode.Scriptable);
            }
            return false;
        }
        
        public bool IsSelectedGraphObject (GraphRunner compRunner)
        {
            if (compRunner != null && serializedObject.targetObject is GraphRunner runner) 
                return runner == compRunner;
            return false;
        }
        
        public bool IsSelectedGraphObject (GraphScriptable compScriptable)
        {
            if (compScriptable != null && serializedObject.targetObject is GraphScriptable scriptable)
                return scriptable == compScriptable;
            return false;
        }

        public static string GetCurrentGraphId ()
        {
            return me != null ? me.GetLatestGraphId() : string.Empty;
        }

        public static int GetInstanceIdFromSelectionObject (Object selectionObject)
        {
            if (selectionObject is GameObject go)
            {
                if (go.TryGetComponent(out GraphRunner runner))
                    return runner.GetInstanceID();
            }
            else if (selectionObject is GraphScriptable scriptable)
            {
                return scriptable.GetInstanceID();
            }

            return 0;
        }

        public static string GetPathBySelectionObject (Object selectionObject)
        {
            if (selectionObject is GameObject go)
            {
                if (go.TryGetComponent(out GraphRunner runner))
                    return runner.gameObject.scene.path;
            }
            else if (selectionObject is GraphScriptable gs)
            {
                return AssetDatabase.GetAssetPath(gs);
            }

            return null;
        }

        public static bool IsRunnerGraph (SerializedObject serializedObject)
        {
            if (serializedObject.targetObject is GraphRunner)
                return true;
            return false;
        }

        public static bool IsScriptableGraph (SerializedObject serializedObject)
        {
            if (serializedObject.targetObject is GraphScriptable)
                return true;
            return false;
        }

        // Start is called before the first frame update
        public SerializedGraph (SerializedObject tree)
        {
            me = this;
            serializedObject = tree;
            graphSelectionObject = SerializedGraph.GetSelectionFromSerializedObject(serializedObject);
            graphObjectId = SerializedGraph.GetInstanceIdFromSelectionObject(graphSelectionObject);
            graphPath = SerializedGraph.GetPathBySelectionObject(graphSelectionObject);
            graph = SerializedGraph.GetGraphFromSelectionObject(graphSelectionObject);

            GraphEditorVariable.SetPath(graphPath);

            var latestId = GetLatestGraphId();
            if (!string.IsNullOrEmpty(latestId))
            {
                graph.haveValidGraphId = true;
                var oldId = GetSavedGraphId();
                if (!oldId.Equals(latestId) )
                    SetGraphId(oldId, latestId);
            }
            else
                graph.haveValidGraphId = false;

            if (PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                EditorSceneManager.sceneSaved -= OnSceneSaved;
                EditorSceneManager.sceneSaved += OnSceneSaved;
            }
            else
            {
                PrefabStage.prefabSaved -= OnPrefabSaved;
                PrefabStage.prefabSaved += OnPrefabSaved;
            }
        }

        private void OnPrefabSaved (GameObject prefab)
        {
            HandleGraphIdAfterSaved(false);
        }

        private void OnSceneSaved (Scene scene)
        {
            EditorApplication.delayCall += () =>
            {
                HandleGraphIdAfterSaved(true);
                if (string.IsNullOrEmpty(graphPath))
                    graphPath = GetPathBySelectionObject(graphSelectionObject);
            };
        }

        private void HandleGraphIdAfterSaved (bool scene)
        {
            var latestId = GetLatestGraphId();
            if (!string.IsNullOrEmpty(latestId))
            {
                if (scene && !graph.haveValidGraphId)
                {
                    graph.haveValidGraphId = true;
                    graphRequireUpdateAndSave = true;
                }

                var oldId = GetSavedGraphId();
                if (oldId != latestId)
                    SetGraphId(oldId, latestId);
            }
        }

        public void SaveNode (GraphNode node)
        {
            var nodeProp = FindNode(Nodes, node);
            if (nodeProp != null)
            {
                nodeProp.serializedObject.Update();
                SaveSerializedObject(nodeProp.serializedObject, false);
            }
        }

        public SerializedProperty FindNode (SerializedProperty array, GraphNode node)
        {
            if (node == null) return null;
            if (array?.serializedObject == null) return null;
            for (int i = 0; i < array.arraySize; ++i)
            {
                var current = array.GetArrayElementAtIndex(i);
                if (current.managedReferenceValue != null && current.FindPropertyRelative(sPropGuid).stringValue == node.guid)
                    return current;
            }

            return null;
        }

        public void SetViewTransform (Vector3 position, Vector3 scale)
        {
            serializedObject.Update();
            var sp = serializedObject.FindProperty(sViewTransformPosition);
            if (sp != null)
                sp.vector3Value = position;
            sp = serializedObject.FindProperty(sViewTransformScale);
            if (sp != null)
                sp.vector3Value = scale;
            if (GraphEditorWindow.IsAutoSave())
                SaveSerializedObject(serializedObject, true, false);
        }

        public void SetNodePosition (GraphNode node, Vector2 position)
        {
            var nodeProp = FindNode(Nodes, node);
            if (nodeProp != null)
            {
                nodeProp.serializedObject.Update();
                Vector2 ori = nodeProp.FindPropertyRelative(sPropPosition).vector2Value;
                if (Vector2.Distance(ori, position) > 2f)
                {
                    nodeProp.FindPropertyRelative(sPropPosition).vector2Value = position;
                    if (GraphEditorWindow.IsAutoSave())
                        SaveSerializedObject(serializedObject, false, false);
                }
            }
        }

        private bool SetGraphId (string oldId, string latestId)
        {
            if (!oldId.Equals(latestId))
            {
                serializedObject.FindProperty(sPropGraphId).stringValue = latestId;
                if (!string.IsNullOrEmpty(oldId))
                    graph.UpdateGraphId(oldId, latestId);
                InspectorUtilities.RegisterUnityObjectDirty(serializedObject.targetObject);
                SaveSerializedObject(serializedObject, false);
                OnUpdate?.Invoke(UpdateGraphId);
                return true;
            }

            return false;
        }

        public string GetLatestGraphId ()
        {
            var graphId = string.Empty;
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                serializedObject.Update();
                if (IsRunnerGraph(serializedObject))
                    graphId = GetRunnerFileId();
                else if (IsScriptableGraph(serializedObject))
                    graphId = GetScriptableFileId();
            }

            return graphId;
        }

        private string GetSavedGraphId ()
        {
            var graphId = string.Empty;
            if (serializedObject != null && serializedObject.targetObject != null)
            {
                serializedObject.Update();
                graphId = serializedObject.FindProperty(sPropGraphId).stringValue;
            }

            return graphId;
        }

        private string GetRunnerFileId ()
        {
            var inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            inspectorModeInfo?.SetValue(serializedObject, InspectorMode.Debug, null);
            var localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
            return localIdProp != null ? localIdProp.intValue.ToString() : string.Empty;
        }
        
        private string GetScriptableFileId ()
        {
            return AssetDatabase.AssetPathToGUID(graphPath, AssetPathToGUIDOptions.OnlyExistingAssets);
        }

        public void SetViewPreviewNode (GraphNode node)
        {
            graph.previewNode = node;
            //serializedObject.FindProperty("graph.previewNode").managedReferenceValue = node;
        }

        public void SetViewPreviewSelected (bool selected)
        {
            graph.previewSelected = selected;
            //serializedObject.FindProperty("graph.previewSelected").boolValue = selected;
        }

        public void DeleteNode (SerializedProperty array, GraphNode node)
        {
            if (node == null)
            {
                for (int i = 0; i < array.arraySize; ++i)
                {
                    var current = array.GetArrayElementAtIndex(i);
                    if (current.managedReferenceValue == null)
                    {
                        array.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                return;
            }

            for (int i = 0; i < array.arraySize; ++i)
            {
                var current = array.GetArrayElementAtIndex(i);
                if (current.managedReferenceValue != null)
                {
                    if (current.FindPropertyRelative(sPropGuid).stringValue == node.guid)
                    {
                        array.DeleteArrayElementAtIndex(i);
                        return;
                    }
                }
            }
        }

        public GraphNode CreateNodeInstance (System.Type type)
        {
            var node = System.Activator.CreateInstance(type) as GraphNode;
            node.guid = GUID.Generate().ToString();
            return node;
        }

        SerializedProperty AppendArrayElement (SerializedProperty arrayProperty)
        {
            arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            return arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
        }

        public GraphNode CreateNode (System.Type type, Vector2 position)
        {
            var node = CreateNodeInstance(type);
            node.position = position;
            node.SetGraphEditorContext(graphSelectionObject);

            var newNode = AppendArrayElement(Nodes);
            newNode.managedReferenceValue = node;

            SaveSerializedObject(serializedObject, false);

            return node;
        }

        public GraphNode CloneNode (GraphNode selectedNode, Vector2 position, bool addIntoGraph = true, SerializedProperty selectedNodeProp = null)
        {
            serializedObject.Update();
            var type = selectedNode.GetType();
            var node = CreateNodeInstance(type);
            var cloneNodeId = node.guid;

            var nodeObj = Convert.ChangeType(node, type);
            var selectedNodeObj = Convert.ChangeType(selectedNode, type);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                var isClonable = false;
                foreach (Type cInterface in field.FieldType.GetInterfaces())
                    if (cInterface.ToString().Contains("Reshape.ReFramework.IClone"))
                        isClonable = true;
                if (isClonable)
                {
                    SetField(field, field.FieldType);
                    if (field.FieldType == typeof(GraphNoteProperty))
                    {
                        var graphId = GetLatestGraphId();
                        if (!graphId.Equals("0"))
                        {
                            var settings = GraphSettings.GetSettings();
                            if (settings != null && settings.graphNoteDb != null)
                            {
                                var value = field.GetValue(selectedNodeObj);
                                if (value is GraphNoteProperty noteProperty)
                                {
                                    var uid = $"{graphId}_{noteProperty.reid}";
                                    var previousMessage = settings.graphNoteDb.GetNote(uid);
                                    value = field.GetValue(node);
                                    if (value is GraphNoteProperty newNoteProperty)
                                    {
                                        if (addIntoGraph)
                                        {
                                            uid = $"{graphId}_{newNoteProperty.reid}";
                                            settings.graphNoteDb.SetNote(uid, previousMessage);
                                            EditorUtility.SetDirty(settings.graphNoteDb);
                                        }
                                        else
                                        {
                                            newNoteProperty.reid = uid;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (field.FieldType == typeof(UnityEvent))
                {
                    selectedNodeProp ??= FindNode(Nodes, selectedNode);
                    if (selectedNodeProp != null)
                    {
                        var newEventNode = (EventBehaviourNode) node;
                        newEventNode.unityEvent = new UnityEvent();
                        try
                        {
                            ReEditorHelper.CopyUnityEvents(selectedNodeProp.FindPropertyRelative("unityEvent"), newEventNode.unityEvent);
                        }
                        catch (Exception)
                        {
                            //-- ignored
                        }
                    }
                }
                else
                {
                    var value = field.GetValue(selectedNodeObj);
                    if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var listType = field.FieldType.GetGenericArguments()[0];
                        if (listType == typeof(SceneObjectProperty) && value is List<SceneObjectProperty> sopList)
                        {
                            var arr = new SceneObjectProperty[sopList.Count];
                            sopList.CopyTo(arr, 0);
                            for (var i = 0; i < arr.Length; i++)
                                arr[i] = arr[i].ShallowCopy();
                            field.SetValue(nodeObj, arr.ToList());
                        }
                        else if (listType == typeof(Collider) && value is List<Collider> cList)
                        {
                            var arr = new Collider[cList.Count];
                            cList.CopyTo(arr, 0);
                            field.SetValue(nodeObj, arr.ToList());
                        }
                        else if (listType == typeof(VariableScriptableObject) && value is List<VariableScriptableObject> vsoList)
                        {
                            var arr = new VariableScriptableObject[vsoList.Count];
                            vsoList.CopyTo(arr, 0);
                            field.SetValue(nodeObj, arr.ToList());
                        }
                        else if (listType == typeof(GraphInputData) && value is List<GraphInputData> giList)
                        {
                            var arr = new GraphInputData[giList.Count];
                            giList.CopyTo(arr, 0);
                            for (var i = 0; i < arr.Length; i++)
                                arr[i] = arr[i].ShallowCopy();
                            field.SetValue(nodeObj, arr.ToList());
                        }
                        else if (listType == typeof(GraphOutputData) && value is List<GraphOutputData> goList)
                        {
                            var arr = new GraphOutputData[goList.Count];
                            goList.CopyTo(arr, 0);
                            for (var i = 0; i < arr.Length; i++)
                                arr[i] = arr[i].ShallowCopy();
                            field.SetValue(nodeObj, arr.ToList());
                        }
                        else
                        {
                            field.SetValue(nodeObj, value);
                        }
                    }
                    else
                    {
                        field.SetValue(nodeObj, value);
                    }
                }
            }

            node.parents = new List<GraphNode>();
            node.children = new List<GraphNode>();
            node.guid = cloneNodeId;

            node.position = position;
            node.SetGraphEditorContext(graphSelectionObject);
            node.dirty = false;
            node.forceRepaint = false;

            node.OnClone(selectedNode);

            if (addIntoGraph)
            {
                SerializedProperty serializedNode = AppendArrayElement(Nodes);
                serializedNode.managedReferenceValue = node;
                SaveSerializedObject(serializedObject, false);
            }

            return node;

            void SetField (FieldInfo field, Type fieldType)
            {
                var method = fieldType.GetMethod("ShallowCopy");
                var fieldObj = field.GetValue(selectedNodeObj);
                var fieldInstance = Convert.ChangeType(fieldObj, fieldType);
                field.SetValue(nodeObj, method?.Invoke(fieldInstance, null));
            }
        }

        public bool ConnectStartNode (GraphNodeView selectedNode)
        {
            if (selectedNode.node is TriggerNode trigger)
            {
                trigger.parents ??= new List<GraphNode>();
                if (trigger.parents.Count == 0)
                {
                    AddChild(graph.RootNode, trigger);
                    selectedNode.AddParentElement(graph.RootNode);
                    return true;
                }
            }

            return false;
        }

        public void SetRootNode (RootNode node)
        {
            RootNode.managedReferenceValue = node;
            SaveSerializedObject(serializedObject, false);
        }

        public void DeleteNode (GraphNode node)
        {
            /*SerializedProperty nodesProperty = Nodes;
            for (int i = 0; i < nodesProperty.arraySize; ++i)
            {
                var prop = nodesProperty.GetArrayElementAtIndex(i);
                var guid = prop.FindPropertyRelative(sPropGuid).stringValue;*/
            if (serializedObject.targetObject is GraphRunner runner)
                GraphNoteDatabase.DeleteNode(runner, node);
            node.OnDelete();
            DeleteNode(Nodes, node);
            SaveSerializedObject(serializedObject, false);
            /*}*/
        }

        public bool SortChildren (GraphNode node, List<GraphNode> sorted)
        {
            var nodeProperty = FindNode(Nodes, node);
            var save = false;
            if (nodeProperty != null)
            {
                for (var i = 0; i < sorted.Count; i++)
                {
                    if (sorted[i] == null)
                        continue;
                    var childrenProperty = nodeProperty.FindPropertyRelative(sPropChildren);
                    if (childrenProperty != null)
                    {
                        for (var j = 0; j < childrenProperty.arraySize; ++j)
                        {
                            var current = childrenProperty.GetArrayElementAtIndex(j);
                            if (current.managedReferenceValue != null)
                            {
                                if (current.FindPropertyRelative(sPropGuid).stringValue == sorted[i].guid)
                                {
                                    childrenProperty.MoveArrayElement(j, i);
                                    save = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return save;
        }

        public void AddChild (GraphNode parent, GraphNode child)
        {
            var parentProperty = FindNode(Nodes, parent);
            var childProperty = FindNode(Nodes, child);
            var parentChildrenProperty = parentProperty.FindPropertyRelative(sPropChildren);
            var childParentsProperty = childProperty.FindPropertyRelative(sPropParents);

            //~~ NOTE clean up null in children property
            DeleteNode(parentChildrenProperty, null);

            var newChild = AppendArrayElement(parentChildrenProperty);
            newChild.managedReferenceValue = child;
            var newParent = AppendArrayElement(childParentsProperty);
            newParent.managedReferenceValue = parent;

            SaveSerializedObject(serializedObject, false);
        }

        public void RemoveChild (GraphNode parent, GraphNode child)
        {
            var parentProperty = FindNode(Nodes, parent);
            var childProperty = FindNode(Nodes, child);
            if (parentProperty != null)
            {
                var parentChildrenProperty = parentProperty.FindPropertyRelative(sPropChildren);
                DeleteNode(parentChildrenProperty, child);
            }

            if (childProperty != null)
            {
                var childParentProperty = childProperty.FindPropertyRelative(sPropParents);
                DeleteNode(childParentProperty, parent);
            }

            SaveSerializedObject(serializedObject, false);
        }

        private void SaveSerializedObject (SerializedObject serializedObj, bool withoutUndo, bool saveScriptable = true)
        {
            if (!withoutUndo)
                serializedObj.ApplyModifiedProperties();
            else
                serializedObj.ApplyModifiedPropertiesWithoutUndo();
            if (saveScriptable)
            {
                haveChangeNotSave = int.MaxValue;
                SavePrefab(serializedObj);
            }
            else if (haveChangeNotSave < int.MaxValue)
            {
                haveChangeNotSave++;
            }
        }

        public void SaveSerializedObjectAfterSorted ()
        {
            SaveSerializedObject(serializedObject, false, false);
        }

        public void SaveGraph ()
        {
            serializedObject.ApplyModifiedProperties();
            if (serializedObject.targetObject is GraphScriptable scriptable)
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
            else
            {
                SavePrefab(serializedObject);
            }
        }

        private void SavePrefab (SerializedObject serializedObj)
        {
            if (ReEditorHelper.IsInPrefabStage())
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage)
                    EditorUtility.SetDirty(stage.prefabContentsRoot);
                Undo.RecordObject(serializedObj.targetObject, "Save Prefab");
                PrefabUtility.RecordPrefabInstancePropertyModifications(serializedObj.targetObject);
            }
        }
    }
}