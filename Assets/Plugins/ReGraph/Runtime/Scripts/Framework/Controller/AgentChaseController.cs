using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Reshape.ReFramework
{
    [AddComponentMenu("")]
    [HideMonoScript]
    public class AgentChaseController : BaseBehaviour
    {
        private Transform targetTransform;
        private NavMeshAgent nmAgent;
#if REGRAPH_PATHFIND
        private PathFindAgent pfAgent;
#endif
        private float updateInterval;
        private float updateTimer;

        public void Initial (NavMeshAgent agent, Transform trans, float interval = 0.1f)
        {
            targetTransform = trans;
            nmAgent = agent;
            updateInterval = interval;
            updateTimer = 0;
        }

#if REGRAPH_PATHFIND
        public void Initial (PathFindAgent agent, Transform trans, float interval = 0.1f)
        {
            targetTransform = trans;
            pfAgent = agent;
            updateInterval = interval;
            updateTimer = 0;
        }
#endif

        public void Terminate ()
        {
            Destroy(this);
        }

        protected void Update ()
        {
            if (targetTransform == null)
                Terminate();
#if REGRAPH_PATHFIND
            if (nmAgent == null && pfAgent == null)
                Terminate();
#else
            if (nmAgent == null)
                Terminate();
#endif
            updateTimer += ReTime.deltaTime;
            if (updateTimer >= updateInterval)
            {
                if (nmAgent != null)
                    nmAgent.destination = targetTransform.position;
#if REGRAPH_PATHFIND
                if (pfAgent != null)
                    pfAgent.SetDestination(targetTransform.position);
#endif
                updateTimer -= updateInterval;
            }
        }
    }
}