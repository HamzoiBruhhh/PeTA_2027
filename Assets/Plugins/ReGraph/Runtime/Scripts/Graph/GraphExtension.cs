using Reshape.Unity;
using UnityEngine;

namespace Reshape.ReGraph
{
    public static class GraphExtension
    {
        public static void Deactivate (this GameObject go)
        {
            var success = false;
            if (go.TryGetComponent(out GraphRunner graph))
            {
                if (graph.activated)
                {
                    var result = graph.TriggerDeactivate();
                    if (result is {isSucceed: true})
                        success = true;
                }
                    
            }

            if (!success)
                go.SetActiveOpt(false);
        }

        public static void Activate (this GameObject go)
        {
            var success = false;
            if (go.TryGetComponent(out GraphRunner graph))
            {
                if (graph.activated)
                {
                    var result = graph.TriggerActivate();
                    if (result is {isSucceed: true})
                        success = true;
                }
            }
            
            if (!success)
                go.SetActiveOpt(true);
        }
    }
}