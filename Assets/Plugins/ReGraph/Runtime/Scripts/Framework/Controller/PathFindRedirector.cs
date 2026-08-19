using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public class PathFindRedirector : BaseBehaviour
    {
#if REGRAPH_PATHFIND
        [InlineProperty]
        [SerializeField]
#endif
        private Transform redirectLocation;

        public Transform GetRedirectLocation ()
        {
            return redirectLocation;
        }
    }
}