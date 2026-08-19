using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Video;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class VideoController : BaseBehaviour
    {
        [Hint("showHints", "Define video player that use for video control.")]
        [InlineButton("@videoPlayer = GetComponent<VideoPlayer>()", "♺", ShowIf = "@videoPlayer == null")]
        public VideoPlayer videoPlayer;

        private static List<VideoController> list;

        private bool paused;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void PauseAll ()
        {
            for (var i = 0; i < list.Count; i++)
                list[i].Pause();
        }

        public static void UnpauseAll ()
        {
            for (var i = 0; i < list.Count; i++)
                list[i].Unpause();
        }
        
        public static void ClearAll ()
        {
            for (var i = 0; i < list.Count; i++)
                list[i].Clear();
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public bool inited => videoPlayer;

        public void Play ()
        {
            if (!inited) return;
            videoPlayer.enabled = true;
            videoPlayer.frame = 0;
            videoPlayer.Play();
            paused = false;
        }
        
        public void Stop ()
        {
            if (!inited) return;
            if (videoPlayer.enabled)
            {
                videoPlayer.Stop();
                paused = false;
            }
        }
        
        public void Clear ()
        {
            if (!inited) return;
            videoPlayer.enabled = false;
            paused = false;
        }
        
        public void Pause ()
        {
            if (!inited) return;
            if (videoPlayer.enabled)
            {
                paused = true;
                videoPlayer.Pause();
            }
        }

        public void Unpause ()
        {
            if (!inited) return;
            if (paused)
            {
                paused = false;
                videoPlayer.Play();
            }
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected virtual void Awake ()
        {
            list ??= new List<VideoController>();
            list.Add(this);
        }

        protected virtual void OnDestroy ()
        {
            list?.Remove(this);
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