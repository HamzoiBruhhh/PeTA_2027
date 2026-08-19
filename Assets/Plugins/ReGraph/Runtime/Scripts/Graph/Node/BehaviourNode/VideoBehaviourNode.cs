using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class VideoBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Play = 100,
            Stop = 200,
            Clear = 300,
            Pause = 500,
            Unpause = 501
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideIf("@executionType == ExecutionType.None")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(videoController)")]
        [InlineButton("@videoController.SetObjectValue(AssignComponent<VideoController>())", "♺", ShowIf = "@videoController.IsObjectValueType()")]
        [InfoBox("@videoController.GetMismatchWarningMessage()", InfoMessageType.Error, "@videoController.IsShowMismatchWarning()")]
        private SceneObjectProperty videoController = new SceneObjectProperty(SceneObject.ObjectType.VideoController);

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (videoController.IsEmpty || !videoController.IsMatchType() || executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Video Behaviour node in " + context.objectName);
            }
            else
            {
                var controller = (VideoController) videoController;
                if (executionType == ExecutionType.Play)
                {
                    controller.Play();
                }
                else if (executionType == ExecutionType.Stop)
                {
                    controller.Stop();
                }
                else if (executionType == ExecutionType.Clear)
                {
                    controller.Clear();
                }
                else if (executionType == ExecutionType.Pause)
                {
                    controller.Pause();
                }
                else if (executionType == ExecutionType.Unpause)
                {
                    controller.Unpause();
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Play", ExecutionType.Play},
            {"Stop", ExecutionType.Stop},
            {"Clear", ExecutionType.Clear},
            {"Pause", ExecutionType.Pause},
            {"Resume", ExecutionType.Unpause}
        };

        public static string displayName = "Video Behaviour Node";
        public static string nodeName = "Video";

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
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Audio & Visual/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (!videoController.IsNull && videoController.IsMatchType() && executionType is ExecutionType.None == false)
            {
                if (executionType is ExecutionType.Stop)
                    return "Stop " + videoController.name;
                if (executionType is ExecutionType.Pause)
                    return "Pause " + videoController.name;
                if (executionType is ExecutionType.Unpause)
                    return "Resume " + videoController.name;
                if (executionType is ExecutionType.Play)
                    return "Play " + videoController.name;
                if (executionType is ExecutionType.Clear)
                    return "Clear " + videoController.name;
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Video Controller.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}