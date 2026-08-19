using System;
using System.Collections.Generic;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class AudioController : BaseBehaviour
    {
        private static List<AudioController> list;

        [Hint("showHints", "Define audio source that use for audio control.")]
        [InlineButton("@audioSource = GetComponent<AudioSource>()", "♺", ShowIf = "@audioSource == null")]
        public AudioSource audioSource;

        [LabelText("Tags")]
        public MultiTag audioTags = new MultiTag("Unit Flags", typeof(MultiTagAudio));

        private bool paused;
        private float fadeVolume;
        private float fadeDuration;
        private float fadeSpeed;
        private bool fading;
        private bool playing;

        public delegate void EventHandler ();

        public event EventHandler endPointReached;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void PauseAll (MultiTag tags)
        {
            if (list == null) return;
            for (var i = 0; i < list.Count; i++)
                if (list[i].MatchTags(tags))
                    list[i].Pause();
        }

        public static void UnpauseAll (MultiTag tags)
        {
            if (list == null) return;
            for (var i = 0; i < list.Count; i++)
                if (list[i].MatchTags(tags))
                    list[i].Unpause();
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public bool inited => audioSource;

        public void FadeVolume (float volume, float duration)
        {
            if (!inited) return;
            if (Math.Abs(audioSource.volume - fadeVolume) < 0.01f) return;
            if (duration <= 0f)
            {
                audioSource.volume = fadeVolume;
            }
            else
            {
                fading = true;
                fadeVolume = volume;
                fadeDuration = duration;
                fadeSpeed = (audioSource.volume - fadeVolume) / duration;
            }
        }

        public void PlayClip (AudioClip clip, bool loop)
        {
            if (!inited) return;
            if (audioSource.clip != clip)
                audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
            paused = false;
            playing = true;
        }

        public void PlayOneShot (AudioClip clip)
        {
            audioSource.PlayOneShot(clip);
        }

        public void Play ()
        {
            if (!inited) return;
            audioSource.time = 0;
            audioSource.Play();
            paused = false;
            playing = true;
        }

        public void Stop ()
        {
            if (!inited) return;
            audioSource.Stop();
            paused = false;
            fading = false;
            playing = false;
        }

        public void Pause ()
        {
            if (!inited) return;
            paused = true;
            audioSource.Pause();
            playing = false;
        }

        public void Unpause ()
        {
            if (!inited) return;
            if (paused)
            {
                paused = false;
                audioSource.UnPause();
                playing = true;
            }
        }

        public bool MatchTags (MultiTag tags)
        {
            return audioTags.ContainAll(tags);
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        public override void Tick ()
        {
            if (playing)
            {
                if (audioSource.isPlaying)
                {
                    if (!audioSource.loop && audioSource.time >= audioSource.clip.length)
                    {
                        playing = false;
                        endPointReached?.Invoke();
                    }
                }
                else
                {
                    playing = false;
                    endPointReached?.Invoke();
                }
            }
            else if (audioSource.isPlaying)
            {
#if DEVELOPMENT_BUILD || (UNITY_EDITOR && REGRAPH_DEV_DEBUG)
                Debug.LogWarning("AudioController have invalid sync playing state.");
#endif
                if (audioSource.clip)
                    playing = true;
                /*else
                    audioSource.Stop();*/
            }

            if (paused) return;
            if (!fading) return;
            var rate = fadeSpeed * ReTime.deltaTime;
            var diff = Mathf.Abs(audioSource.volume - fadeVolume);
            if (rate == 0f || Mathf.Abs(rate) > diff)
            {
                audioSource.volume = fadeVolume;
                fading = false;
            }
            else
            {
                audioSource.volume -= rate;
            }
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected virtual void Awake ()
        {
            list ??= new List<AudioController>();
            list.Add(this);
            playing = audioSource.playOnAwake;
            PlanTick();
        }

        protected virtual void OnDestroy ()
        {
            OmitTick();
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