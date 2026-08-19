using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if REGRAPH_PATHFIND
using Pathfinding;
#endif

namespace Reshape.ReFramework
{
#if REGRAPH_PATHFIND
    [Serializable]
    public class PathFindGraphSelect
    {
        [InlineProperty]
        public StringProperty name;
        public GraphMask graphMask = GraphMask.everything;
    }
    
    [RequireComponent(typeof(Seeker))]
    public class PathFindGraphSelection : BaseBehaviour
    {
        public Seeker seeker;
        public List<PathFindGraphSelect> selection;

        private GraphMask defaultGraphMask;
        
        public void SetGraph (string selectType)
        {
            if (seeker == null)
                return;
            for (int i = 0; i < selection.Count; i++)
            {
                string tempName = selection[i].name;
                if (tempName.Equals(selectType, StringComparison.InvariantCulture))
                {
                    seeker.graphMask = selection[i].graphMask;
                    break;
                }
            }
        }
        
        public void SetGraphToEverything ()
        {
            if (seeker == null)
                return;
            seeker.graphMask = GraphMask.everything;
        }
        
        public void SetGraphToDefault ()
        {
            if (seeker == null)
                return;
            seeker.graphMask = defaultGraphMask;
        }

        protected void Awake ()
        {
            defaultGraphMask = seeker.graphMask;
        }
    }
#else
    public class PathFindGraphSelection : MonoBehaviour { }
#endif
}