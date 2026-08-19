using UnityEngine;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class FloatTextOwner : BaseBehaviour
    {
        private const string GO_NAME = "CharacterFloatText";

        [BoxGroup("Float Text Effect")]
        public GameObject vfxPrefab;

        [BoxGroup("Float Text Effect")]
        public Color fontColor;

        [BoxGroup("Float Text Effect")]
        public float fontSize;

        [BoxGroup("Float Text Effect")]
        public Transform spawnLocation; //-- modelController.hudPoint

        [BoxGroup("Float Text Effect")]
        public Transform parent; //-- unit.agentTransform

        private Camera cam;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void SetVfx (GameObject go)
        {
            vfxPrefab = go;
        }

        public void SetFontColor (string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out fontColor);
        }

        public void SetFontSize (float s)
        {
            fontSize = s;
        }

#if REGRAPH_DEV_DEBUG
        [Button]
#endif
        public void Show (string message)
        {
            FloatTextController.Spawn(message, spawnLocation, GO_NAME, parent, fontSize, fontColor, FloatTextController.TYPE_FOLLOW_CAMERA, cam, spawnLocation, vfxPrefab);
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Awake ()
        {
            PlanBegin();
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        public override void Begin ()
        {
            cam = CameraManager.GetViewCamera();
            DoneBegin();
        }

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}