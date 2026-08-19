using System.Collections.Generic;
using Reshape.Unity;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public abstract class BehaviourNode : GraphNode
    {
        protected override void OnStart (GraphExecution execution, int updateId)
        {
            ExecuteStart(execution, updateId);
        }

        private void ExecuteStart (GraphExecution execution, int updateId)
        {
            if (children == null) return;
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] != null)
                    execution.variables.SetInt(children[i].guid, (int) State.Running);
            }
        }

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            return ExecuteUpdate(execution, updateId);
        }

        private State ExecuteUpdate (GraphExecution execution, int updateId)
        {
            if (children == null) return State.Success;

            var stillRunning = false;
            var containFailure = false;
            for (var i = 0; i < children.Count; ++i)
            {
                if (children[i] != null)
                {
                    var state = execution.variables.GetInt(children[i].guid);
                    if (state == (int) State.Running)
                    {
                        var status = children[i].Update(execution, updateId);
                        execution.variables.SetInt(children[i].guid, (int) status);
                        if (status == State.Failure)
                            containFailure = true;
                        else if (status == State.Running)
                            stillRunning = true;
                        else if (status == State.Break)
                            return State.Break;
                    }
                    else if (state == (int) State.Failure)
                    {
                        containFailure = true;
                    }
                }
            }

            if (stillRunning)
                return State.Running;
            if (containFailure)
                return State.Failure;
            return State.Success;
        }

        protected override void OnStop (GraphExecution execution, int updateId) { }
        protected override void OnInit () { }
        protected override void OnReset () { }
        
        protected override void OnPause (GraphExecution execution)
        {
            if (children != null)
                for (int i = 0; i < children.Count; ++i)
                    children[i].Pause(execution);
        }
        
        protected override void OnUnpause (GraphExecution execution)
        {
            if (children != null)
                for (int i = 0; i < children.Count; ++i)
                    children[i].Unpause(execution);
        }

        protected override State OnDisabled (GraphExecution execution, int updateId)
        {
            bool started = execution.variables.GetStarted(guid, false);
            if (!started)
            {
                ExecuteStart(execution, updateId);
                execution.variables.SetStarted(guid, true);
            }

            return ExecuteUpdate(execution, updateId);
        }
        
        public string BehaviourId => guid;

        public override ChildrenType GetChildrenType ()
        {
            return ChildrenType.Multiple;
        }

        public override void GetChildren (ref List<GraphNode> list)
        {
            if (children != null)
                for (var i = 0; i < children.Count; i++)
                    list.Add(children[i]);
        }
        
        public override void GetParents (ref List<GraphNode> list)
        {
            if (parents != null)
                for (var i = 0; i < parents.Count; i++)
                    list.Add(parents[i]);
        }

        public override bool IsRequireUpdate ()
        {
            return false;
        }
        
        public override bool IsRequireInit ()
        {
            return false;
        }
        
        public override bool IsRequireBegin ()
        {
            return false;
        }
        
        public override bool IsRequirePreUninit ()
        {
            return false;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return false;
        }

        protected void UpdateConditionNode (GraphExecution execution, int updateId, bool result)
        {
            for (var i = 0; i < children.Count; ++i)
            {
                if (children[i] is YesConditionNode yesNode)
                    yesNode.MarkExecute(execution, updateId, result);
                else if (children[i] is NoConditionNode noNode)
                    noNode.MarkExecute(execution, updateId, result);
            }
        }

#if UNITY_EDITOR
        public override void OnPrintFlow (int state)
        {
            if (state == 1 && context.runner && context.runner.printFlowLog)
                ReDebug.Log($"{context.runner.gameObject.name} GraphFlow", $@"Start {GetNodeInspectorTitle().Replace(" ","")}.{GetNodeIdentityName()} [{guid}]");
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "Behaviour node act as execution node, it will command specific functions to execute base on the node configurations.";
        }
        
        public override string GetNodeIdentityName ()
        {
            return string.Empty;
        }
#endif
    }
}