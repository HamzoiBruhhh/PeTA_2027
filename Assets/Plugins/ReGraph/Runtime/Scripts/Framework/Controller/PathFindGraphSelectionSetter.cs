using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    public class PathFindGraphSelectionSetter : BaseBehaviour
    {
#if REGRAPH_PATHFIND
        [InlineProperty]
        [SerializeField]
#endif
        private StringProperty selectionName;

        public string GetName ()
        {
            return selectionName;
        }
    }
}