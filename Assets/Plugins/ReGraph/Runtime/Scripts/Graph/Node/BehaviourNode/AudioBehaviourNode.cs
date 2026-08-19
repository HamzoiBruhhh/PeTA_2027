using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class AudioBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            PlayClip = 100,
            PlayOneShot = 101,
            Play = 102,
            Stop = 200,
            Pause = 300,
            Unpause = 400,
            FadeVolume = 500,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideIf("@executionType == ExecutionType.None")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(audioController)")]
        [InlineButton("@audioController.SetObjectValue(AssignComponent<AudioController>())", "♺", ShowIf = "@audioController.IsObjectValueType()")]
        [InfoBox("@audioController.GetMismatchWarningMessage()", InfoMessageType.Error, "@audioController.IsShowMismatchWarning()")]
        private SceneObjectProperty audioController = new SceneObjectProperty(SceneObject.ObjectType.AudioController);
        
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.PlayOneShot || executionType == ExecutionType.PlayClip")]
        private AudioClip clip;
        
        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.PlayClip")]
        private bool loop;
        
        [LabelText("@Number1Label()")]
        [ShowIf("@executionType == ExecutionType.FadeVolume")]
        [OnInspectorGUI("@MarkPropertyDirty(number1)")]
        [InlineProperty]
        public FloatProperty number1;
        
        [LabelText("@Number2Label()")]
        [ShowIf("@executionType == ExecutionType.FadeVolume")]
        [OnInspectorGUI("@MarkPropertyDirty(number2)")]
        [InlineProperty]
        public FloatProperty number2;
        
        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (audioController.IsEmpty || !audioController.IsMatchType() || executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Audio Behaviour node in " + context.objectName);
            }
            else
            {
                var audio = (AudioController) audioController;
                if (executionType == ExecutionType.Stop)
                {
                    audio.Stop();
                }
                else if (executionType == ExecutionType.Pause)
                {
                    audio.Pause();
                }
                else if (executionType == ExecutionType.Unpause)
                {
                    audio.Unpause();
                }
                else if (executionType == ExecutionType.Play)
                {
                    audio.Play();
                }
                else if (executionType == ExecutionType.PlayOneShot)
                {
                    audio.PlayOneShot(clip);
                }
                else if (executionType == ExecutionType.PlayClip)
                {
                    audio.PlayClip(clip, loop);
                }
                else if (executionType == ExecutionType.FadeVolume)
                {
                    audio.FadeVolume(number1, number2);
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private string Number1Label ()
        {
            if (executionType is ExecutionType.FadeVolume)
                return "Volume";
            return string.Empty;
        }
        
        private string Number2Label ()
        {
            if (executionType is ExecutionType.FadeVolume)
                return "Duration";
            return string.Empty;
        }
        
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Play", ExecutionType.Play},
            {"Stop", ExecutionType.Stop},
            {"Pause", ExecutionType.Pause},
            {"Resume", ExecutionType.Unpause},
            {"Fade Volume", ExecutionType.FadeVolume},
            {"Play One Shot", ExecutionType.PlayOneShot},
            {"Play Clip", ExecutionType.PlayClip},
        };

        public static string displayName = "Audio Behaviour Node";
        public static string nodeName = "Audio";

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
            if (!audioController.IsNull && audioController.IsMatchType() && executionType is ExecutionType.None == false)
            {
                if (executionType is ExecutionType.Stop)
                    return "Stop " + audioController.name;
                if (executionType is ExecutionType.Pause)
                    return "Pause " + audioController.name;
                if (executionType is ExecutionType.Unpause)
                    return "Resume " + audioController.name;
                if (executionType is ExecutionType.Play)
                    return "Play " + audioController.name;
                if (executionType is ExecutionType.PlayOneShot)
                    return "Shot " + clip.name + " on " + audioController.name;
                if (executionType is ExecutionType.PlayClip)
                {
                    var message = loop ? "Loop play " : "Play ";
                    return message + clip.name + " on " + audioController.name;
                }
                if (executionType is ExecutionType.FadeVolume)
                    return "Fade " + audioController.name + "'s volume to " + number1 + " for " + number2 + "s";
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Audio Controller.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}