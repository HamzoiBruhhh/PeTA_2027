using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [Serializable]
    public class GraphExecution
    {
        [LabelText("@lastExecutedUpdateId")]
        public Node.State state;

        [HideLabel, FoldoutGroup("Variables")]
        public GraphVariables variables;

        [HideLabel, FoldoutGroup("Parameters")]
        public GraphParameters parameters;

        [HideInInspector]
        public int lastExecutedUpdateId;
        
        [ShowInInspector]
        [PropertyOrder(-1)]
        [LabelText("@executionId")]
        public TriggerNode.Type type => triggerType;

        private long executionId;
        private TriggerNode.Type triggerType;
        private bool reserved;

        public delegate void UpdateDelegate ();

        public event UpdateDelegate OnComplete;

        public long id => executionId;
        public bool isFailed => state == Node.State.Failure;
        public bool isSucceed => state == Node.State.Success;
        public bool isRunning => state == Node.State.Running;
        public bool isCompleted => state is Node.State.Failure or Node.State.Success;
        public bool isReserved => reserved;

        public GraphExecution ()
        {
            variables = new GraphVariables();
            parameters = new GraphParameters();
        }

        public void CollectReturnVariableData (GraphExecution input, string id, string collectorId)
        {
            if (input != null)
                variables.CollectReturnData(input.variables, id, collectorId);
        }

        public void SetState (Node.State s)
        {
            state = s;
            if (isCompleted)
                OnComplete?.Invoke();
        }

        public void MarkAsReverse ()
        {
            reserved = true;
        }
        
        public void ReleaseReverse ()
        {
            reserved = false;
        }

        public void Stop ()
        {
            SetState(Node.State.Stop);
        }

        public void Reset ()
        {
            executionId = 0;
            triggerType = TriggerNode.Type.None;
            reserved = false;
            variables?.Reset();
            parameters?.Reset();
            SetState(Node.State.Running);
            lastExecutedUpdateId = 0;
            OnComplete = null;
        }

        public void Init (long id, TriggerNode.Type type)
        {
            executionId = id;
            triggerType = type;
        }
    }
}