using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReGraph;

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Behaviour Pack", fileName = "Behaviour Pack", order = 309)]
    [HideMonoScript]
    [Serializable]
    public class BehaviourPack : GraphScriptable
    {
        private GraphContext context;
        
        public BehaviourData TriggerBehaviour (string type, BehaviourData behaviourData)
        {
            if (behaviourData != null)
            {
                InitContext();
                behaviourData.lastExecuteResult = Activate(TriggerNode.Type.ActionTrigger, actionName: type, behaviourData: behaviourData);
            }

            return behaviourData;
        }
        
        public override GraphExecution TriggerAction (ActionNameChoice type, GraphExecution execution)
        {
            if (type != null)
            {
                InitContext();
                return Activate(TriggerNode.Type.ActionTrigger, actionName: type, behaviourData: execution.parameters.behaviourData);
            }

            return null;
        }

        public override GraphExecution InternalTrigger (string type, GraphExecution execution)
        {
            return Activate(TriggerNode.Type.All, actionName: type, behaviourData: execution.parameters.behaviourData);
        }

        protected override void CreateGraph ()
        {
            graph.Create(Graph.GraphType.BehaviourPack);
        }

        private void InitContext ()
        {
            if (context.isUnassigned)
            {
                context = new GraphContext(this);
                graph.Bind(context);
            }
        }
    }
}