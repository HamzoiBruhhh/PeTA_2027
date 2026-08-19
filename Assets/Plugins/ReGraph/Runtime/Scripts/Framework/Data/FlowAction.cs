using Reshape.ReGraph;
using System;
using UnityEngine;

namespace Reshape.ReFramework
{
    [Serializable]
    public struct FlowAction
    {
        [HideInInspector]
        public GraphRunner runner;
        [HideInInspector]
        public ActionNameChoice actionName;

        public static void ExecuteList (FlowAction[] actions)
        {
            for (var i = 0; i < actions.Length; i++)
                if (actions[i].runner && actions[i].actionName)
                    actions[i].Execute();
        }

        public void Execute ()
        {
            runner.CacheExecute(runner.TriggerAction(actionName));
        }
    }
}