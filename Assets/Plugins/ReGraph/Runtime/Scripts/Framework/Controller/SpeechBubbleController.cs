using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
using Reshape.Unity;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class SpeechBubbleController : VisualEffectController
    {
        public const int TYPE_FACE_CAMERA = 1;
        public const int TYPE_FOLLOW_CAMERA = 2;
        private const string POOL_TYPE = "SpeechBubble";

        [BoxGroup("Speech Bubble")]
        public GameObject panel;

        [BoxGroup("Speech Bubble")]
        public TMP_Text label;

        private int updateType;
        private Camera updateCamera;
        private Transform followTransform;
        
        public delegate void UpdateDelegate ();
        public event UpdateDelegate OnStartUsage;
        public event UpdateDelegate OnFinishUsage;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static SpeechBubbleController Spawn (string msg, Transform loc, string name = "", Transform parent = null, float size = -1, Color color = default, int type = 0, Camera cam = null,
            Transform followTrans = null, GameObject effectPrefab = null)
        {
            var vfx = effectPrefab;
            if (!vfx)
                vfx = GraphManager.instance.runtimeSettings.speechBubble;
            if (vfx)
            {
                var bubble = ReMonoBehaviour.TakeFromPool(POOL_TYPE, vfx, loc, true, parent);
                if (bubble)
                {
                    bubble.name = string.IsNullOrWhiteSpace(name) ? GraphManager.instance.runtimeSettings.speechBubble.name : name;
                    if (bubble.TryGetComponent<SpeechBubbleController>(out var controller))
                    {
                        controller.Setup(msg, size, color, type, cam, followTrans);
                        return controller;
                    }
                }
            }

            return null;
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public override void StartUsage ()
        {
            OnStartUsage?.Invoke();
        }

        public override void FinishUsage ()
        {
            BackToPool();
            OnFinishUsage?.Invoke();
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        protected virtual void Setup (string message, float size, Color color, int type = 0, Camera cam = null, Transform followTrans = null)
        {
            updateType = type;
            updateCamera = cam;
            followTransform = followTrans;
            label.text = message;
            if (color != default)
                label.color = color;
            if (size >= 0)
                label.fontSize = size;
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void LateUpdate ()
        {
            if (updateType == 0) return;
            if (updateType == TYPE_FACE_CAMERA)
            {
                if (!updateCamera) return;
                var rotation = updateCamera.transform.rotation;
                transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
            }
            else if (updateType == TYPE_FOLLOW_CAMERA)
            {
                if (!updateCamera || !followTransform) return;
                if (panel)
                {
                    var screenPos = updateCamera.WorldToScreenPoint(followTransform.position, Camera.MonoOrStereoscopicEye.Mono);
                    var pos = panel.transform.position;
                    pos.x = screenPos.x;
                    pos.y = screenPos.y;
                    panel.transform.position = pos;
                }
            }
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void BackToPool ()
        {
            var me = gameObject;
            me.SetActiveOpt(false);
            InsertIntoPool(POOL_TYPE, me, true);
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}