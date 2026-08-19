using System.Collections.Generic;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class AnimateController : BaseBehaviour
    {
        protected static readonly int Speed = Animator.StringToHash("Speed");
        
        private static List<AnimateController> list;

        [Hint("showHints", "Define animator that use for animation.")]
        [PropertyOrder(-100)]
        [BoxGroup("Animate")]
        public Animator animator;

        [Hint("showHints", "Define particle effects that use for animation.")]
        [PropertyOrder(-99)]
        [BoxGroup("Animate")]
        public ParticleSystem[] particles;
        
        [Hint("showHints", "If tick, the animation will pause when this component is disable, while animation will running when component is enable.")]
        [PropertyOrder(-50)]
        [BoxGroup("Control")]
        public bool bindEnable;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void UpdateAllAnimSpeedMtp ()
        {
            for (var i = 0; i < list.Count; i++)
            {
                list[i].UpdateAnimSpeedMtp();
            }
        }

        public static void PauseAll ()
        {
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Pause();
            }
        }

        public static void UnpauseAll ()
        {
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Unpause();
            }
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public virtual void Pause ()
        {
            if (animator != null)
                animator.enabled = false;
            if (particles is {Length: > 0})
                for (int i = 0; i < particles.Length; i++)
                    if (particles[i] != null)
                        particles[i].Pause();
        }

        public virtual void Unpause ()
        {
            if (animator != null)
                animator.enabled = true;
            if (particles is {Length: > 0})
                for (int i = 0; i < particles.Length; i++)
                    if (particles[i] != null)
                        particles[i].Play();
        }
        
        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        protected virtual void UpdateAnimSpeed ()
        {
            animator.SetFloat(Speed, ReTime.deltaModifier);
        }
        
        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected virtual void Awake ()
        {
            list ??= new List<AnimateController>();
            list.Add(this);
            if (animator != null)
                animator.SetFloat(Speed, ReTime.deltaModifier);
        }

        protected virtual void OnDestroy ()
        {
            list?.Remove(this);
        }

        protected void OnEnable ()
        {
            if (bindEnable)
            {
                Unpause();
            }
        }
        
        protected void OnDisable ()
        {
            if (bindEnable)
            {
                Pause();
            }
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void UpdateAnimSpeedMtp ()
        {
            if (animator != null)
                UpdateAnimSpeed();
            if (particles is {Length: > 0})
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i] != null)
                    {
                        var partMain = particles[i].main; 
                        partMain.simulationSpeed = ReTime.deltaModifier;
                    }
                }
            }
        }
        
        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}