using System;
using System.Collections;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Video;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class VideoTriggerNode : TriggerNode
    {
        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("MarkDirty")]
        public Type triggerType;

        private VideoPlayer videoPlayer;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if (execution.type == triggerType && execution.type is Type.VideoFinished)
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

        private void OnVideoPlayerFinished (VideoPlayer vp)
        {
            context.runner.TriggerVideo(Type.VideoFinished, TriggerId);
        }

        protected override void OnInit ()
        {
            if (context.runner.TryGetComponent<VideoPlayer>(out videoPlayer))
                videoPlayer.loopPointReached += OnVideoPlayerFinished;
        }

        protected override void OnReset ()
        {
            if (videoPlayer)
                videoPlayer.loopPointReached -= OnVideoPlayerFinished;
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
            menu.Add("Finish", Type.VideoFinished);
            return menu;
        }

        public static string displayName = "Video Trigger Node";
        public static string nodeName = "Video";
        
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
            if (triggerType == Type.VideoFinished)
                desc = "Finished Playing";
            return desc;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.VideoFinished)
                tip += "This will get trigger when the video have been finished playing.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}