using Reshape.ReGraph;
using Sirenix.OdinInspector;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class VisualEffectController : AnimateController
    {
        private GraphEvent graphEvent;
        private GraphExecution graphExecution;
        private bool holdStartUsageTrigger;
        private bool holdFinishUsageTrigger;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public virtual void Show ()
        {
            TriggerStartUsage();
        }

        public virtual void Hide ()
        {
            TriggerFinishUsage();
        }
        
        public virtual void StartUsage () { }
        public virtual void FinishUsage () { }

        public void HoldStartUsageTrigger ()
        {
            holdStartUsageTrigger = true;
        }
        
        public void HoldFinishUsageTrigger ()
        {
            holdFinishUsageTrigger = true;
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        protected void TriggerStartUsage ()
        {
            var haveEvent = false;
            if (graphEvent != null)
            {
                holdStartUsageTrigger = false;
                var execution = graphEvent.Execute(GraphEventListener.EventType.VfxStart);
                if (execution != null)
                {
                    haveEvent = true;
                    graphExecution = execution;
                    if (graphExecution.isCompleted)
                        OnCompleteStartUsageTrigger();
                    else
                    {
                        graphExecution.OnComplete -= OnCompleteStartUsageTrigger;
                        graphExecution.OnComplete += OnCompleteStartUsageTrigger;
                    }
                }
            }

            if (!haveEvent)
                StartUsage();
        }

        protected void TriggerFinishUsage ()
        {
            var haveEvent = false;
            if (graphEvent)
            {
                holdFinishUsageTrigger = false;
                var execution = graphEvent.Execute(GraphEventListener.EventType.VfxFinish);
                if (execution != null)
                {
                    haveEvent = true;
                    graphExecution = execution;
                    if (graphExecution.isCompleted)
                        OnCompleteFinishUsageTrigger();
                    else
                    {
                        graphExecution.OnComplete -= OnCompleteFinishUsageTrigger;
                        graphExecution.OnComplete += OnCompleteFinishUsageTrigger;
                    }
                }
            }

            if (!haveEvent)
                FinishUsage();
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected override void Awake ()
        {
            TryGetComponent<GraphEvent>(out graphEvent);
            base.Awake();
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void OnCompleteFinishUsageTrigger ()
        {
            graphExecution.OnComplete -= OnCompleteFinishUsageTrigger;
            graphExecution = null;
            if (!holdFinishUsageTrigger)
                FinishUsage();
        }

        private void OnCompleteStartUsageTrigger ()
        {
            graphExecution.OnComplete -= OnCompleteStartUsageTrigger;
            graphExecution = null;
            if (!holdStartUsageTrigger)
                StartUsage();
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}