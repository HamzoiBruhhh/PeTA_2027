using System;
using System.Collections;
using System.Collections.Generic;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using Reshape.Unity.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [Serializable]
    public class Graph
    {
        public enum GraphType
        {
            None,
            BehaviourGraph = 10,
            StatGraph = 11,
            GraphScriptable = 100,
            AttackDamagePack = 101,
            TargetAimPack = 102,
            AttackStatusPack = 103,
            MoralePack = 501,
            LootPack = 1001,
            StaminaPack = 1501,
            AttackSkillPack = 5001,
            BehaviourPack = 6001,
        }
        
        [FoldoutGroup("Parameters")]
        [LabelText("Input")]
        [ShowIf("ShowInOutParameters")]
        public VariableScriptableObject[] inParameters;
        
        [FoldoutGroup("Parameters")]
        [LabelText("Output")]
        [ShowIf("ShowInOutParameters")]
        public VariableScriptableObject[] outParameters;

        [SerializeField]
        [ValueDropdown("TypeChoice")]
        [DisableIf("@isCreated || IsApplicationPlaying()")]
        [HideIf("HavePreviewNode")]
        private GraphType type;

        [SerializeReference]
        [HideInInspector]
        private RootNode rootNode;

        [SerializeReference]
        [HideIf("HideNodesList")]
        [DisableIf("@HavePreviewNode() == false")]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, Expanded = false, ShowIndexLabels = true, ListElementLabelName = "GetNodeInspectorTitle")]
        public List<GraphNode> nodes = new List<GraphNode>();

        [SerializeField, ReadOnly, HideLabel]
        [ShowIf("@IsApplicationPlaying() && !HavePreviewNode()")]
        private GraphExecutes executes;

        [HideInInspector]
        public string id;

        private GraphContext context;
        private bool terminated;

#if UNITY_EDITOR
        [HideInInspector]
        public Vector3 viewPosition = new Vector3(300, 200);

        [HideInInspector]
        public Vector3 viewScale = Vector3.one;

        [HideInInspector]
        public bool haveValidGraphId = true;

        [SerializeReference]
        [HideIf("HidePreviewNode")]
        [HideDuplicateReferenceBox]
        [HideLabel]
        [BoxGroup("PreviewNode", GroupName = "@GetPreviewNodeName()")]
        public GraphNode previewNode;

        [HideInInspector]
        public bool previewSelected;

        [ShowIfGroup("SelectionWarning", Condition = "ShowSelectionWarning")]
        [BoxGroup("SelectionWarning/Hide", ShowLabel = false)]
        [HideLabel]
        [DisplayAsString]
        public string multipleSelectionWarning = "Multiple Dialog Tree selected!";

        [ShowIfGroup("PrefabWarning", Condition = "ShowPrefabWarning")]
        [BoxGroup("PrefabWarning/Hide", ShowLabel = false)]
        [HideLabel]
        [DisplayAsString]
        [PropertyOrder(-1)]
        [InfoBox("Editing in Prefab Mode Warning", InfoMessageType.Warning)]
        public string editInPrefabModeWarning;

        [HideInInspector]
        public List<ISelectable> selectedViewNode;
#endif

        public GraphNode RootNode => rootNode;
        public GraphType Type => type;
        public GraphContext Context => context;
        public bool isCreated => rootNode != null;
        public bool isTerminated => terminated;
        public bool isBehaviourGraph => type == GraphType.BehaviourGraph;
        public bool isStatGraph => type == GraphType.StatGraph;
        public bool isMoralePack => type == GraphType.MoralePack;
        public bool isStaminaPack => type == GraphType.StaminaPack;
        public bool isAttackSkillPack => type == GraphType.AttackSkillPack;
        public bool isAttackStatusPack => type == GraphType.AttackStatusPack;
        public bool isTargetAimPack => type == GraphType.TargetAimPack;
        public bool isAttackDamagePack => type == GraphType.AttackDamagePack;
        public bool isLootPack => type == GraphType.LootPack;
        public bool isBehaviourPack => type == GraphType.BehaviourPack;

        public Graph ()
        {
            executes = new GraphExecutes();
        }

        public void CreateRootNode ()
        {
            rootNode = new RootNode();
            nodes.Add(rootNode);
        }

        public void Create (GraphType graphType)
        {
            type = graphType;
            CreateRootNode();
        }

        public void Bind (GraphContext c)
        {
            context = c;
            Traverse(rootNode, node => { node.context = context; });
        }

        public GraphExecution InitExecute (long d, TriggerNode.Type triggerType)
        {
            if (!isCreated)
                return null;
            var execution = executes.Add(d, triggerType);
            return execution;
        }

        public void RunExecute (GraphExecution execution, int updateId)
        {
            if (!isCreated)
                return;
            if (execution != null)
                StartExecute(execution, updateId);
        }

        public GraphExecution FindExecute (long executionId)
        {
            if (!isCreated)
                return null;
            return executes.Find(executionId);
        }

        public void ResumeExecute (GraphExecution execution, int updateId)
        {
            if (!isCreated)
                return;
            if (execution != null)
                StartExecute(execution, updateId);
        }

        public void StopExecute (GraphExecution execution, int updateId)
        {
            if (!isCreated)
                return;
            if (execution != null)
            {
                execution.Stop();
            }
        }

        public void StopExecutes ()
        {
            if (!isCreated || executes == null)
                return;
            for (int i = 0; i < executes.count; i++)
            {
                var execution = executes.Get(i);
                if (execution.isRunning)
                    rootNode.Stop(execution);
            }

            executes.Stop();
        }

        public void PauseExecutes ()
        {
            if (!isCreated || executes == null)
                return;
            for (int i = 0; i < executes.count; i++)
            {
                var execution = executes.Get(i);
                if (execution.isRunning)
                    rootNode.Pause(execution);
            }
        }

        public void UnpauseExecutes ()
        {
            if (!isCreated || executes == null)
                return;
            for (var i = 0; i < executes.count; i++)
            {
                var execution = executes.Get(i);
                if (execution.isRunning)
                    rootNode.Unpause(execution);
            }
        }

        private bool StartExecute (GraphExecution execution, int updateId)
        {
            if (execution.lastExecutedUpdateId < updateId)
            {
                execution.SetState(rootNode.Update(execution, updateId));
                execution.lastExecutedUpdateId = updateId;
                if (execution.isCompleted)
                    return true;
            }
            else if (execution.lastExecutedUpdateId == updateId)
            {
                //-- NOTE ReDebug.LogWarning("Graph Warning", "Ignore an execution same with previous execution (" + updateId + ") in " + context.objectName);
            }
            else
            {
                ReDebug.LogWarning("Graph Warning", "Ignore an outdated execution in " + context.objectName);
            }

            return false;
        }

        public void Update (int updateId)
        {
            if (!isCreated || executes == null)
                return;
            for (int i = 0; i < executes.count; i++)
            {
                var execution = executes.Get(i);
                if (execution.isRunning)
                    StartExecute(execution, updateId);
            }

            CleanExecutes();
        }

        public void CleanExecutes ()
        {
            for (int i = 0; i < executes.count; i++)
            {
                var execution = executes.Get(i);
                if (execution.isCompleted)
                {
                    executes.Remove(i);
                    i--;
                }
            }
        }

        public void CleanExecute (GraphExecution execute)
        {
            if (execute.isCompleted)
                executes.Remove(execute);
        }

        public void ClearExecutes ()
        {
            executes?.Clear();
        }

        public void Reset ()
        {
            ClearExecutes();
            Traverse(rootNode, node => { node.Reset(); });
        }

        public void Stop ()
        {
            if (executes != null)
                for (int i = 0; i < executes.count; i++)
                    rootNode?.Abort(executes.Get(i));
            Reset();
            terminated = true;
        }

        public void Start ()
        {
            executes.Clear();
            Traverse(rootNode, node => { node.Init(); });
        }

        public bool IsNodeTypeInUse (Type nodeType)
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] != null && nodes[i].GetType() == nodeType)
                        return true;
                }
            }

            return false;
        }

        public bool HaveRequireUpdate ()
        {
            if (!isCreated || executes == null)
                return false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsRequireUpdate())
                    return true;
            }

            return false;
        }

        public bool HaveRequireBegin ()
        {
            if (!isCreated || executes == null)
                return false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsRequireBegin())
                    return true;
            }

            return false;
        }

        public bool HaveRequirePreUninit ()
        {
            if (!isCreated || executes == null)
                return false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsRequirePreUninit())
                    return true;
            }

            return false;
        }

        public bool HaveRequireInit ()
        {
            if (!isCreated || executes == null)
                return false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsRequireInit())
                    return true;
            }

            return false;
        }

        public bool HaveTrigger (TriggerNode.Type t, bool ignoreInactive = false, int paramInt = 0)
        {
            for (var i = 0; i < rootNode.children.Count; i++)
            {
                if (!rootNode.children[i].enabled && ignoreInactive)
                    continue;
                if (rootNode.children[i].IsTrigger(t, paramInt))
                    return true;
            }

            return false;
        }

        public GraphNode GetNode (string nodeId)
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] != null && nodes[i].guid == nodeId)
                        return nodes[i];
                }
            }

            return null;
        }

        public static List<GraphNode> GetChildren (GraphNode parent)
        {
            var children = new List<GraphNode>();
            parent?.GetChildren(ref children);
            return children;
        }

        public static List<GraphNode> GetParents (GraphNode child)
        {
            var parent = new List<GraphNode>();
            child?.GetParents(ref parent);
            return parent;
        }

        public static void Traverse (GraphNode node, Action<GraphNode> visitor)
        {
            if (node != null)
            {
                visitor.Invoke(node);
                var children = GetChildren(node);
                foreach (var n in children)
                    Traverse(n, visitor);
            }
        }

        public static void TraverseReverse (GraphNode node, Action<GraphNode> visitor)
        {
            if (node != null)
            {
                visitor.Invoke(node);
                var parent = GetParents(node);
                foreach (var n in parent)
                    TraverseReverse(n, visitor);
            }
        }

#if UNITY_EDITOR
        public bool IsTriggerNodeTypeInUse (TriggerNode.Type triggerType)
        {
            if (nodes != null)
            {
                for (var i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] != null && nodes[i] is TriggerNode trigger)
                        if (trigger.GetNodeType() == triggerType)
                            return true;
                }
            }

            return false;
        }

        private bool ShowInOutParameters ()
        {
            if (HavePreviewNode())
                return false;
            return isBehaviourPack;
        }
            
        public static Graph GetFromSelectionObject (Object selectionObject)
        {
            if (selectionObject is GameObject go)
            {
                if (go && go.TryGetComponent<GraphRunner>(out var runner))
                    return runner.graph;
            }
            else if (selectionObject is GraphScriptable scriptable)
            {
                if (scriptable)
                    return scriptable.graph;
            }

            return null;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<GraphType>()
        {
            {"Behaviour Graph", GraphType.BehaviourGraph},
            {"Stat Graph", GraphType.StatGraph},
        };

        [ShowIf("@isCreated == false && !IsApplicationPlaying()")]
        [Button]
        public void CreateGraph ()
        {
            if (type == GraphType.None)
            {
                EditorUtility.DisplayDialog("Create Graph", "Please select a graph type before click on Create Graph button", "OK");
            }
            else
            {
                if (ReEditorHelper.IsInPrefabStage())
                {
                    CreateRootNode();
                }
                else
                {
                    if (Object.FindObjectOfType<GraphManager>() == null)
                    {
                        var result = EditorUtility.DisplayDialog("Create Graph", "Graph Runner require Graph Manager which not find at your scene. Do you want to add it now?", "Yes", "No");
                        if (result)
                        {
                            if (GraphManager.CreateGraphManager())
                                CreateRootNode();
                            else
                                EditorUtility.DisplayDialog("Create Graph", "Please add Graph Manager by right click on the Hierarchy", "OK");
                        }
                        else
                            EditorUtility.DisplayDialog("Create Graph", "Please add Graph Manager to your scene before click on Create Graph button", "OK");
                    }
                    else
                        CreateRootNode();
                }
            }
        }

        public void UpdateGraphId (string previousId, string newId)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                    nodes[i].OnUpdateGraphId(previousId, newId);
            }
        }

        private bool ShowPrefabWarning ()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                if (prefabStage.mode == PrefabStage.Mode.InContext)
                {
                    editInPrefabModeWarning = "Make sure not drag any GameObject / Component from scene!";
                    return true;
                }
            }

            editInPrefabModeWarning = string.Empty;
            return false;
        }

        public bool IsApplicationPlaying ()
        {
            return EditorApplication.isPlaying;
        }

        public bool HavePreviewNode ()
        {
            return previewSelected;
        }

        public bool ShowSelectionWarning ()
        {
            if (selectedViewNode != null)
            {
                if (selectedViewNode.Count > 1)
                {
                    multipleSelectionWarning = "Multiple Graph Nodes Selected!";
                    return true;
                }
                else if (selectedViewNode.Count == 1)
                {
                    if (previewSelected && previewNode == null)
                    {
                        multipleSelectionWarning = "Error Graph Nodes Selected!";
                        return true;
                    }
                }
            }

            return false;
        }

        public bool HidePreviewNode ()
        {
            if (selectedViewNode is {Count: > 1})
                return true;
            if (previewNode == null)
                return true;
            return !previewSelected;
        }

        public bool HideNodesList ()
        {
            if (rootNode == null)
                return true;
            return HavePreviewNode();
        }

        public string GetPreviewNodeName ()
        {
            if (previewNode == null)
                return string.Empty;
            return previewNode.GetNodeInspectorTitle();
        }

        public void InitPreviewNode ()
        {
            previewNode = null;
            previewSelected = false;
        }
#endif
    }
}