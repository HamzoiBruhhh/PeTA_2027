using System;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reshape.ReFramework
{
    [HideMonoScript]    
    public partial class LevelManager : BaseBehaviour
    {
        private static LevelManager me;

        public bool crashReportHandling;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void Exception ()
        {
            me.LogException();
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

        protected void Awake ()
        {
            me = this;
            PlanPreInit();
        }

        protected void OnDestroy ()
        {
            me = null;
            ClearArea();
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        [SpecialName]
        public override void PreInit ()
        {
            if (crashReportHandling)
                UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata("Level", SceneManager.GetActiveScene().name);
            DonePreInit();
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

#if REGRAPH_DEV_DEBUG
        [Button, HideInEditorMode]
#endif
        private void LogException ()
        {
            if (Application.isPlaying)
                Debug.LogException(new Exception("Intended Exception Log at Level Manager!"));
        }
        
        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}