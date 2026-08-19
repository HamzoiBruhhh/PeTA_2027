using Reshape.ReGraph;
using UnityEngine;

namespace Reshape.ReFramework
{
    public static class PropertyLinking
    {
        public static T GetComponent<T>(GameObject go)
        {
            return go.GetComponent<T>();
        }
        
        public static GameObject GetGameObject(Component comp)
        {
            return comp.gameObject;
        }

        public static Camera GetCamera ()
        {
            return Camera.main;
        }
    }
}