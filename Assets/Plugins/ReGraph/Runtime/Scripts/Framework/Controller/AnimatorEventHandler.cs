using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class AnimatorEventHandler : MonoBehaviour
    {
        [System.Serializable]
        public struct AnimatorEventAction
        {
            [LabelText("Name")]
            public ActionNameChoice actionChoice;

            public UnityEvent action;

            public void Execute (AnimatorEventHandler handler)
            {
                action?.Invoke();
            }
        }

        public bool readyMarking;
        public AnimatorEventAction[] animatorActions;
        
        private bool isShowReady = true;
        
        public bool showReady => isShowReady;

        public void MarkReady ()
        {
            if (readyMarking)
                isShowReady = true;
        }
        
        public void MarkNotReady ()
        {
            if (readyMarking)
                isShowReady = false;
        }

        public void ExecuteAnimAction (ActionNameChoice type)
        {
            ExecuteAction(type.actionName, true);
        }

        private void ExecuteAction (string actionName, bool hiddenFromEvent)
        {
            if (!enabled)
                return;
            for (var i = 0; i < animatorActions.Length; i++)
            {
                if (animatorActions[i].actionChoice.actionName == actionName)
                {
                    animatorActions[i].Execute(this);
                }
            }
        }
    }
}