using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class CameraManager : BaseBehaviour
    {
        protected static CameraManager instance;
        
        [Hint("showHints", "Define the camera that render display to player. \n\n" +
                           "Position of Transform component at this gameobject in Prefab always uses default value.\nRotation Y & Z of Transform component at this gameobject in Prefab always uses default value. \n\n" +
                           "This gameobject is always be a child of its confiner.")]
        [PropertyOrder(-9999)]
        public Camera viewCamera;
        
        [Hint("showHints", "Define the camera that detect input from player.")]
        [PropertyOrder(-9998)]
        public Camera inputCamera;
        
        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static Camera GetViewCamera ()
        {
            return !instance ? null : instance.viewCamera;
        }
        
        public static Camera GetInputCamera ()
        {
            if (!instance)
                return null;
            return instance.inputCamera ? instance.inputCamera : GetViewCamera();
        }
        
        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        protected virtual void Awake ()
        {
            instance = this;
        }

        protected virtual void OnDestroy ()
        {
            instance = null;
        }
        
        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}