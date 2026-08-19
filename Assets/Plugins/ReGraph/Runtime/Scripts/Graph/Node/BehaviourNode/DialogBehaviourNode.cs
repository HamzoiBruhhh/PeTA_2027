using System.Collections;
using System.Collections.Generic;
using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class DialogBehaviourNode : BehaviourNode
    {
        public const string VAR_PROCEED = "_proceed";
        public const string VAR_PAUSED = "_paused";

        public enum ExecutionType
        {
            None,
            ShowDialog = 10,
            ShowChoices = 11,
            ShowSpeech = 200,
            ClearSpeech = 201,
            HideSpeech = 202,
            HideCanvas = 1000,
            DialogExcept = 10000
        }
        
        public enum ActorType
        {
            Custom,
            Character,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [OnValueChanged("OnChangeActorType")]
        [ShowIf("@executionType == ExecutionType.ShowDialog")]
        private ActorType actorType;
        
        [SerializeField]
        [ShowIf("ShowParamActor")]
        [OnInspectorGUI("@MarkPropertyDirty(actor)")]
        [InlineProperty]
        private StringProperty actor;

        [SerializeField]
        [ShowIf("ShowParamSprite")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(sceneObject)")]
        [InfoBox("@sceneObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@sceneObject.IsShowMismatchWarning()")]
        private SceneObjectProperty sceneObject = new SceneObjectProperty(SceneObject.ObjectType.Sprite, "Avatar");

        [SerializeField]
        [ShowIf("ShowParamDialog")]
        [OnInspectorGUI("@MarkPropertyDirty(dialog)")]
        [InlineProperty]
        [LabelText("Message")]
        private StringProperty dialog;

        [SerializeField]
        [ShowIf("ShowParamGameObject")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(gameObject)")]
        [InlineButton("@gameObject.SetObjectValue(AssignGameObject())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@GetParamGameObjectMismatchWarningMessage()", InfoMessageType.Error, "@IsShowParamGameObjectMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.ShowSpeech")]
        [LabelText("Immediate")]
        private bool paramBool;
        
        private string proceedKey;
        private string pausedKey;

        private void InitVariables ()
        {
            if (string.IsNullOrEmpty(proceedKey))
                proceedKey = guid + VAR_PROCEED;
            if (string.IsNullOrEmpty(pausedKey))
                pausedKey = guid + VAR_PAUSED;
        }

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (execution == null && updateId == int.MaxValue)
            {
                if (DialogCanvas.instance)
                    DialogCanvas.instance.onKeyDialogProceed -= OnKeyEnter;
                return;
            }

            if (executionType == ExecutionType.None)
            {
                LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.ShowDialog)
            {
                if (string.IsNullOrEmpty(dialog))
                {
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    InitVariables();
                    execution.variables.SetInt(proceedKey, -updateId);

                    if (DialogCanvas.instance == null)
                    {
                        GameObject go = (GameObject) Object.Instantiate(GraphManager.instance.runtimeSettings.dialogCanvas);
                        go.name = GraphManager.instance.runtimeSettings.dialogCanvas.name;
                    }

                    var dialogNodes = new List<BehaviourNode>();
                    for (int i = 0; i < children.Count; ++i)
                    {
                        if (children[i] is DialogBehaviourNode)
                        {
                            var behave = (DialogBehaviourNode) children[i];
                            if (behave.executionType is ExecutionType.ShowDialog or ExecutionType.ShowChoices)
                                dialogNodes.Add(behave);
                        }
                    }

                    if (actorType == ActorType.Custom)
                    {
                        DialogCanvas.ShowMessagePanel(actor, dialog, sceneObject, dialogNodes.Count > 0);
                    }
                    else if (actorType == ActorType.Character)
                    {
                        DialogCanvas.ShowMessagePanel((CharacterOperator) sceneObject, dialog, dialogNodes.Count > 0);
                    }

                    if (DialogCanvas.instance != null)
                    {
                        DialogCanvas.instance.onKeyDialogProceed += OnKeyEnter;
                    }
                }
            }
            else if (executionType is ExecutionType.ShowChoices)
            {
                var choices = new List<string>();
                for (int i = 0; i < children.Count; ++i)
                {
                    if (children[i] is ChoiceConditionNode)
                    {
                        var child = (ChoiceConditionNode) children[i];
                        if (!string.IsNullOrEmpty(child.choice))
                            if (child.enabled)
                                choices.Add(child.choice);
                    }
                }

                if (choices.Count <= 0)
                {
                    InitVariables();
                    execution.variables.SetInt(proceedKey, 1);
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    InitVariables();
                    execution.variables.SetInt(proceedKey, 0);

                    if (DialogCanvas.instance == null)
                    {
                        GameObject go = (GameObject) Object.Instantiate(GraphManager.instance.runtimeSettings.dialogCanvas);
                        go.name = GraphManager.instance.runtimeSettings.dialogCanvas.name;
                    }

                    DialogCanvas.ShowChoicePanel(choices);
                }
            }
            else if (executionType == ExecutionType.HideCanvas)
            {
                if (DialogCanvas.instance == null)
                {
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    DialogCanvas.HidePanel();
                }
            }
            else if (executionType == ExecutionType.DialogExcept)
            {
                if (gameObject.IsEmpty || !gameObject.IsMatchType())
                {
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    DialogCanvas.AddDialogExceptObject(((GameObject) gameObject).GetInstanceID());
                }
            }
            else if (executionType == ExecutionType.ShowSpeech)
            {
                if (gameObject.IsEmpty || !IsParamGameObjectMatchType() || string.IsNullOrEmpty(dialog))
                {
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    var go = (GameObject) gameObject;
                    var trans = (Transform) sceneObject;
                    if (go)
                    {
                        SpeechOwnerController.Show(go, trans, dialog, paramBool);
                    }
                    else
                    {
                        var unit = (CharacterOperator) gameObject;
                        if (unit)
                        {
                            SpeechOwnerController.Show(unit, trans, dialog, paramBool);
                        }
                    }
                }
            }
            else if (executionType == ExecutionType.ClearSpeech)
            {
                if (gameObject.IsEmpty || !IsParamGameObjectMatchType())
                {
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    SpeechOwnerController speaker = null;
                    var go = (GameObject) gameObject;
                    if (go)
                        go.TryGetComponent<SpeechOwnerController>(out speaker);
                    else
                    {
                        var unit = (CharacterOperator) gameObject;
                        if (unit)
                            speaker = unit.GetComponentInChildren<SpeechOwnerController>();
                    }
                    
                    if (speaker)
                        speaker.ClearPending();
                }
            }
            else if (executionType == ExecutionType.HideSpeech)
            {
                if (gameObject.IsEmpty || !IsParamGameObjectMatchType())
                {
                    LogWarning("Found an empty Dialog Behaviour node in " + context.objectName);
                }
                else
                {
                    SpeechOwnerController speaker = null;
                    var go = (GameObject) gameObject;
                    if (go)
                        go.TryGetComponent<SpeechOwnerController>(out speaker);
                    else
                    {
                        var unit = (CharacterOperator) gameObject;
                        if (unit)
                            speaker = unit.GetComponentInChildren<SpeechOwnerController>();
                    }
                    
                    if (speaker)
                        speaker.Hide();
                }
            }
            
            base.OnStart(execution, updateId);

            void OnKeyEnter ()
            {
                if (execution != null)
                {
                    var key = execution.variables.GetInt(proceedKey);
                    var currentKey = ReTime.frameCount;
                    var frameRate = Application.targetFrameRate;
                    if (frameRate <= 0)
                        frameRate = 60;
                    var showMinFrame = (int) (frameRate * DialogCanvas.proceedInputDelay);
                    if (-currentKey < key - showMinFrame)
                    {
                        var paused = execution.variables.GetInt(pausedKey);
                        if (paused == 0)
                        {
                            if (!DialogCanvas.IsMouseOnDialogExceptionUI())
                            {
                                if (DialogCanvas.IsPanelShowReady())
                                {
                                    DialogCanvas.instance.onKeyDialogProceed -= OnKeyEnter;
                                    if (key <= 0)
                                    {
                                        DialogCanvas.MarkDialogProceed();
                                        execution.variables.SetInt(proceedKey, 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.HideCanvas)
            {
                if (!DialogCanvas.IsPanelHide())
                    return State.Running;
            }

            if (executionType is ExecutionType.ShowDialog)
            {
                var key = execution.variables.GetInt(proceedKey);
                if (key == 1)
                {
                    if (DialogCanvas.IsPanelHide())
                        return State.Failure;
                    execution.variables.SetInt(proceedKey, 2);
                    return base.OnUpdate(execution, updateId);
                }
                else if (key == 2)
                {
                    return base.OnUpdate(execution, updateId);
                }

                return State.Running;
            }

            if (executionType is ExecutionType.ShowChoices)
            {
                int key = execution.variables.GetInt(proceedKey);
                if (key == 0)
                {
                    string selectedChoice = DialogCanvas.GetChosenChoice();
                    bool foundChoice = false;
                    for (var i = 0; i < children.Count; ++i)
                    {
                        if (children[i] is ChoiceConditionNode)
                        {
                            var cNode = children[i] as ChoiceConditionNode;
                            if (string.Equals(cNode.choice, selectedChoice))
                            {
                                cNode.MarkExecute(execution, updateId, true);
                                foundChoice = true;
                            }
                        }
                    }

                    if (!foundChoice)
                    {
                        return State.Running;
                    }
                    else
                    {
                        DialogCanvas.HideChoicePanel();
                        execution.variables.SetInt(proceedKey, 1);
                    }
                }
            }

            return base.OnUpdate(execution, updateId);
        }

        protected override void OnStop (GraphExecution execution, int updateId)
        {
            var started = execution.variables.GetStarted(guid, false);
            if (started)
            {
                if (executionType is ExecutionType.ShowDialog)
                {
                    OnStart(null, int.MaxValue);
                }
            }

            base.OnStop(execution, updateId);
        }

        protected override void OnPause (GraphExecution execution)
        {
            var started = execution.variables.GetStarted(guid, false);
            if (started)
            {
                if (executionType is ExecutionType.ShowDialog)
                {
                    var key = execution.variables.GetInt(proceedKey);
                    if (key < 0)
                        execution.variables.SetInt(pausedKey, 1);
                }
            }

            base.OnPause(execution);
        }

        protected override void OnUnpause (GraphExecution execution)
        {
            var started = execution.variables.GetStarted(guid, false);
            if (started)
            {
                if (executionType is ExecutionType.ShowDialog)
                {
                    var key = execution.variables.GetInt(proceedKey);
                    if (key < 0)
                        execution.variables.SetInt(pausedKey, 0);
                }
            }

            base.OnUnpause(execution);
        }

        public override bool IsRequireUpdate ()
        {
            return enabled;
        }

        public bool IsParamGameObjectMatchType ()
        {
            if (gameObject.IsVariableValueType())
            {
                if (gameObject.variableValue != null)
                    return gameObject.variableValue.sceneObject.type is SceneObject.ObjectType.GameObject or SceneObject.ObjectType.CharacterOperator;
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        public bool IsShowParamGameObjectMismatchWarning ()
        {
            return !IsParamGameObjectMatchType();
        }

        public string GetParamGameObjectMismatchWarningMessage ()
        {
            return "The assigned variable require GameObject / Character type";
        }

        private bool ShowParamGameObject ()
        {
            if (executionType is ExecutionType.DialogExcept or ExecutionType.ShowSpeech or ExecutionType.ClearSpeech or ExecutionType.HideSpeech)
                return true;
            return false;
        }

        private bool ShowParamActor ()
        {
            if (executionType is ExecutionType.ShowDialog && actorType == ActorType.Custom)
                return true;
            return false;
        }
        
        private bool ShowParamSprite ()
        {
            if (executionType is ExecutionType.ShowSpeech)
                return true;
            if (executionType is ExecutionType.ShowDialog)
                return true;
            return false;
        }

        private bool ShowParamDialog ()
        {
            if (executionType is ExecutionType.ShowDialog or ExecutionType.ShowSpeech)
                return true;
            return false;
        }
        
        private void OnChangeActorType ()
        {
            if (executionType is ExecutionType.ShowDialog)
            {
                if (actorType == ActorType.Custom)
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.Sprite, "Avatar");
                else if (actorType == ActorType.Character)
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");
            }
                
            MarkDirty();
            MarkRepaint();
        }

        private void OnChangeType ()
        {
            if (executionType is ExecutionType.ShowSpeech)
                sceneObject = new SceneObjectProperty(SceneObject.ObjectType.Transform, "Spawn Location");
            else if (executionType is ExecutionType.ShowDialog)
            {
                if (actorType == ActorType.Custom)
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.Sprite, "Avatar");
                else if (actorType == ActorType.Character)
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");
            }
                
            MarkDirty();
            MarkRepaint();
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Show Dialog", ExecutionType.ShowDialog},
            {"Show Choices", ExecutionType.ShowChoices},
            {"Hide Canvas", ExecutionType.HideCanvas},
            {"Show Speech", ExecutionType.ShowSpeech},
            {"Hide Speech", ExecutionType.HideSpeech},
            {"Clear Speech", ExecutionType.ClearSpeech},
            {"Except Proceed Dialog", ExecutionType.DialogExcept}
        };

        public static string displayName = "Dialog Behaviour Node";
        public static string nodeName = "Dialog";

        public override bool IsPortReachable (GraphNode node)
        {
            if (node is ChoiceConditionNode)
            {
                if (executionType != ExecutionType.ShowChoices)
                    return false;
            }
            else if (node is YesConditionNode or NoConditionNode)
            {
                return false;
            }

            return true;
        }

        public bool AcceptConditionNode ()
        {
            if (executionType == ExecutionType.ShowChoices)
                return true;
            return false;
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
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Gameplay/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.ShowDialog && !string.IsNullOrEmpty(dialog))
            {
                if (actorType == ActorType.Custom)
                {
                    if (!string.IsNullOrEmpty(actor))
                        return actor + " : " + dialog + "\n<color=#FFF600>Continue at dialog end";
                    return dialog + "\n<color=#FFF600>Continue at dialog end";
                }
                else if (actorType == ActorType.Character)
                {
                    if (!sceneObject.IsNull && sceneObject.IsMatchType())
                        return sceneObject.objectName + " : " + dialog + "\n<color=#FFF600>Continue at dialog end";
                    return dialog + "\n<color=#FFF600>Continue at dialog end";
                }
            }

            if (executionType == ExecutionType.DialogExcept)
            {
                if (!gameObject.IsNull && !gameObject.IsShowMismatchWarning())
                    return "Set " + gameObject.name + "not proceed dialog";
            }

            if (executionType == ExecutionType.ShowSpeech && !string.IsNullOrEmpty(dialog))
            {
                if (!gameObject.IsNull && !IsShowParamGameObjectMismatchWarning())
                    return "Speak \"" + dialog + "\" on " + gameObject.objectName;
            }
            
            if (executionType == ExecutionType.ClearSpeech)
            {
                if (!gameObject.IsNull && !IsShowParamGameObjectMismatchWarning())
                    return "Clear all speech on " + gameObject.objectName;
            }
            
            if (executionType == ExecutionType.HideSpeech)
            {
                if (!gameObject.IsNull && !IsShowParamGameObjectMismatchWarning())
                    return "Hide speech on " + gameObject.objectName;
            }

            if (executionType == ExecutionType.ShowChoices)
                return "Show Choices";
            if (executionType == ExecutionType.HideCanvas)
                return "Hide Canvas\n<color=#FFF600>Continue at complete hide";
            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.ShowChoices)
                tip += "This will show all connected choices on the Dialog UI Choice Section.\n\n";
            else if (executionType == ExecutionType.HideCanvas)
                tip += "This will hide the UI Choice.\n\n";
            else if (executionType == ExecutionType.ShowDialog)
                tip += "This will show the dialog message on the Dialog UI Dialog Section.\n\n";
            else if (executionType == ExecutionType.ShowSpeech)
                tip += "This will show the speech bubble message on the gameObject.\n\n";
            else if (executionType == ExecutionType.ClearSpeech)
                tip += "This will clear all speech message on the gameObject.\n\n";
            else if (executionType == ExecutionType.HideSpeech)
                tip += "This will hide current speech bubble message on the gameObject.\n\n";
            else if (executionType == ExecutionType.DialogExcept)
                tip += "Set specific UI click will not make the dialog proceed.\n\n";
            else
                tip += "This will execute all Dialog related behaviour.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}