using System;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class LayoutController : BaseBehaviour
    {
        private bool running;
        private int updateTime;
        private LayoutGroup layoutGroup;
        
        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void RefreshLayout (LayoutGroup layout)
        {
            layoutGroup = layout;
            running = true;
            layoutGroup.enabled = false;
            updateTime = ReTime.frameCount;
        }
        
        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Update ()
        {
            if (running)
            {
                if (updateTime != ReTime.frameCount && updateTime < ReTime.frameCount)
                {
                    layoutGroup.enabled = true;
                    running = false;
                }
            }
        }
        
        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}