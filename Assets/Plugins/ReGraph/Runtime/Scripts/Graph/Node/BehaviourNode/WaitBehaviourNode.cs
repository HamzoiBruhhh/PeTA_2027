using Sirenix.OdinInspector;
using Reshape.ReFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class WaitBehaviourNode : BehaviourNode
    {
        public const string VAR_CALLBACK = "_callback";

        [ValidateInput("ValidateWaitTime", "time must be more than or equal 0!", InfoMessageType.Warning)]
        [OnInspectorGUI("OnTimeGUI")]
        [InlineProperty]
        public FloatProperty time = new FloatProperty(1);

        private string callbackKey;

        private void InitVariables ()
        {
            if (string.IsNullOrEmpty(callbackKey))
                callbackKey = guid + VAR_CALLBACK;
        }

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            InitVariables();
            execution.variables.SetInt(callbackKey, 0);

            if (time < 0f)
            {
                LogWarning("Found an empty Wait Behaviour node in " + context.objectName);
            }
            else if (time > 0f)
            {
                context.runner.Wait(guid + execution.id, time, OnWaitComplete);
            }

            base.OnStart(execution, updateId);

            void OnWaitComplete ()
            {
                execution.variables.SetInt(callbackKey, updateId);
                context.runner.ResumeTrigger(execution.id, updateId);
            }
        }

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            int key = execution.variables.GetInt(callbackKey);
            if (time <= 0f && key == 0)
                execution.variables.SetInt(callbackKey, updateId);
            if (key > 0 && key < updateId)
                return base.OnUpdate(execution, updateId);
            return State.Running;
        }

        protected override void OnStop (GraphExecution execution, int updateId)
        {
            var started = execution.variables.GetStarted(guid, false);
            if (started)
            {
                if (!string.IsNullOrEmpty(callbackKey))
                {
                    var callback = execution.variables.GetInt(callbackKey);
                    if (callback == 0)
                        context.runner.CancelWait(guid + execution.id);
                }
            }

            base.OnStop(execution, updateId);
        }

        protected override void OnPause (GraphExecution execution)
        {
            var started = execution.variables.GetStarted(guid, false);
            if (started)
            {
                var callback = execution.variables.GetInt(callbackKey);
                if (callback == 0)
                    context.runner.StopWait(guid + execution.id);
            }

            base.OnPause(execution);
        }

        protected override void OnUnpause (GraphExecution execution)
        {
            var started = execution.variables.GetStarted(guid, false);
            if (started)
            {
                var callback = execution.variables.GetInt(callbackKey);
                if (callback == 0)
                    context.runner.ResumeWait(guid + execution.id);
            }

            base.OnUnpause(execution);
        }

        public override bool IsRequireUpdate ()
        {
            return enabled;
        }

#if UNITY_EDITOR
        private void OnTimeGUI ()
        {
            MarkPropertyDirty(time);
            ShowTimeTip();
        }

        private void ShowTimeTip ()
        {
            if (time == 0f)
            {
                EditorGUILayout.HelpBox("Value 0 means 1 frame", MessageType.Info);
            }
        }

        private bool ValidateWaitTime (float value)
        {
            return value >= 0f;
        }

        public static string displayName = "Wait Behaviour Node";
        public static string nodeName = "Wait";

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
            return time.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (time >= 0f)
            {
                if (time == 0f)
                    return "<color=#FFF600>Wait 1 frame</color>";
                return "<color=#FFF600>Wait " + time + " seconds</color>";
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will execute a stop and wait at this node for this branch, it will continue after timed up.\n\nGraph will continue on following branches during the wait.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}