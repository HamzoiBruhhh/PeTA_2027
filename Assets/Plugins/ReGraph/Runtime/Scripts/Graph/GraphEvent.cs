using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [AddComponentMenu("ReGraph/Graph Event", 1)]
    [HideMonoScript]
    [DisallowMultipleComponent]
    public class GraphEvent : BaseBehaviour
    {
        public GraphEventListener[] events;

        public GraphExecution Execute (GraphEventListener.EventType type)
        {
            return GraphEventListener.ExecuteList(type, events);
        }
    }
}