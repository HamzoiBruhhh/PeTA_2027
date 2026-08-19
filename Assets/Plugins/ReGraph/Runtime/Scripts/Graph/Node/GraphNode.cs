using System.Collections.Generic;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reshape.ReGraph
{
    [System.Serializable]
    [HideReferenceObjectPicker]
    public abstract class GraphNode : Node
    {
        public const string VAR_CHILD = "_child";
        public const string ID_SEPERATOR = ".";

        public enum ChildrenType
        {
            Single,
            Multiple,
            None
        }

        [SerializeReference]
        [ShowIf("ShowChildren"), BoxGroup("Debug Info")]
        [ReadOnly]
        [ListDrawerSettings(ListElementLabelName = "guid")]
        public List<GraphNode> children = new List<GraphNode>();

        [SerializeReference]
        [HideInInspector]
        public GraphNode parent;

        [SerializeReference]
        [ShowIf("ShowParents"), BoxGroup("Debug Info")]
        [ReadOnly]
        [ListDrawerSettings(ListElementLabelName = "guid")]
        public List<GraphNode> parents = new List<GraphNode>();
        
        [HideInInspector]
        public GraphContext context;

        public void Abort (GraphExecution execution)
        {
            Graph.Traverse(this, (node) => { node.OnStop(execution, 0); });
        }

        public void Stop (GraphExecution execution)
        {
            Graph.Traverse(this, (node) => { node.OnStop(execution, 0); });
        }

        [HideInInspector]
        public bool drawGizmos = false;

#if UNITY_EDITOR
        private Object graphSelectionObject;

        public void SetGraphEditorContext (Object obj)
        {
            graphSelectionObject = obj;
        }

        public Graph GetGraph ()
        {
            return Graph.GetFromSelectionObject(graphSelectionObject);
        }
        
        public GraphRunner GetRunner ()
        {
            return GraphRunner.GetFromSelectionObject(graphSelectionObject);
        } 

        public string GetGraphSelectionInstanceID ()
        {
            return graphSelectionObject != null ? graphSelectionObject.GetInstanceID().ToString() : string.Empty;
        }
        
        public string GetGraphSelectionName ()
        {
            return graphSelectionObject != null ? graphSelectionObject.name : string.Empty;
        }

        public bool HaveGraphSelectionObject ()
        {
            return graphSelectionObject != null;
        }

        public string id
        {
            get
            {
                var graph = GetGraph();
                if (graph != null && !string.IsNullOrEmpty(graph.id))
                    return GenerateId(graph.id);
                LogWarning("Found empty graph id in " + GetGraphSelectionName());
                return string.Empty;
            }
        }

        public string GenerateId (string graphId)
        {
            return graphId + ID_SEPERATOR + ReUniqueId.GenerateId(guid);
        }

        private bool ShowChildren ()
        {
            if (GetType().ToString().Contains("RootNode"))
                return true;
            return showAdvanceSettings;
        }
        
        public bool FindParents (string findId, bool behaviourOnly)
        {
            if (behaviourOnly)
                if (GetType().ToString().Contains("RootNode") || GetType().ToString().Contains("TriggerNode"))
                    return false;
            if (parents is {Count: > 0})
            {
                for (var i = 0; i < parents.Count; i++)
                {
                    if (parents[i] != null)
                    {
                        if (parents[i].guid == findId)
                            return true;
                        if (parents[i].FindParents(findId, behaviourOnly))
                            return true;
                    }
                }
            }
            
            return false;
        }
        
        private bool ShowParents ()
        {
            if (GetType().ToString().Contains("RootNode") || GetType().ToString().Contains("TriggerNode"))
                return false;
            return showAdvanceSettings;
        }

        public void RepairParents ()
        {
            if (parents == null || parents.Count == 0)
            {
                if (parent != null)
                {
                    parents = new List<GraphNode> {parent};
                }
            }
        }
        
        public void AddParent (GraphNode node)
        {
            parents ??= new List<GraphNode>();
            parents.Add(node);
        }

        public virtual bool IsPortReachable (GraphNode node)
        {
            return true;
        }

        protected T AssignComponent<T> ()
        {
            MarkDirty();
            return PropertyLinking.GetComponent<T>((GameObject)graphSelectionObject);
        }

        protected GameObject AssignGameObject ()
        {
            MarkDirty();
            return (GameObject)graphSelectionObject;
        }

        protected Camera AssignCamera ()
        {
            MarkDirty();
            return PropertyLinking.GetCamera();
        }

        public abstract string GetNodeInspectorTitle ();
        public abstract string GetNodeViewTitle ();
        public abstract string GetNodeViewDescription ();
        public abstract string GetNodeViewTooltip ();
        public abstract string GetNodeMenuDisplayName ();
        public abstract string GetNodeIdentityName ();
#endif

        public abstract ChildrenType GetChildrenType ();
        public abstract void GetChildren (ref List<GraphNode> list);
        public abstract void GetParents (ref List<GraphNode> list);
        public abstract bool IsRequireUpdate ();
        public abstract bool IsRequireInit ();
        public abstract bool IsRequireBegin ();
        public abstract bool IsRequirePreUninit ();
        public abstract bool IsTrigger (TriggerNode.Type type, int paramInt = 0);
    }
}