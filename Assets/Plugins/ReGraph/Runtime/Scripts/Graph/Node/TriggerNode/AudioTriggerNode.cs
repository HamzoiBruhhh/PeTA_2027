using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class AudioTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;
        
        private AudioController audioController;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type == triggerType && execution.type is Type.AudioFinished)
                {
                    execution.variables.SetState(guid, State.Success);
                    state = State.Success;
                }
                else if (execution.type == Type.All)
                {
                    if (execution.parameters.actionName != null && execution.parameters.actionName.Equals(TriggerId))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
                    }
                }

                if (state != State.Success)
                {
                    execution.variables.SetState(guid, State.Failure);
                    state = State.Failure;
                }
                else
                    OnSuccess();
            }

            if (state == State.Success)
                return base.OnUpdate(execution, updateId);
            return State.Failure;
        }
        
        private void OnAudioPlayerFinished ()
        {
            context.runner.TriggerAudio(Type.AudioFinished, TriggerId);
        }
        
        protected override void OnInit ()
        {
            if (context.runner.TryGetComponent<AudioController>(out audioController))
                audioController.endPointReached += OnAudioPlayerFinished;
        }

        protected override void OnReset ()
        {
            if (audioController)
                audioController.endPointReached -= OnAudioPlayerFinished;
        }
        
        public override bool IsRequireInit ()
        {
            if (triggerType == Type.None)
                return false;
            return true;
        }

        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == triggerType;
        }

#if UNITY_EDITOR
        private IEnumerable TriggerTypeChoice ()
        {
            var menu = new ValueDropdownList<Type>();
            menu.Add("Finish", Type.AudioFinished);
            return menu;
        }

        public static string displayName = "Audio Trigger Node";
        public static string nodeName = "Audio";
        
        public override Type GetNodeType ()
        {
            return triggerType;
        }

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }

        public override string GetNodeIdentityName ()
        {
            return triggerType.ToString();
        }

        public override string GetNodeViewDescription ()
        {
            var desc = string.Empty;
            if (triggerType == Type.AudioFinished)
                desc = "Finished Playing";
            return desc;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.AudioFinished)
                tip += "This will get trigger when the audio have been finished playing.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}