using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections;
using Reshape.ReFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class AnimatorBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            SetBoolTrue = 10,
            SetBoolFalse = 11,
            SetTrigger = 20,
            SetFloat = 30,
            SetInt = 31,
            GetBool = 510,
            GetFloat = 530,
            GetInt = 531,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideIf("HideAnimator")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(animator)")]
        [InlineButton("@animator.SetObjectValue(AssignComponent<Animator>())", "♺", ShowIf = "@animator.IsObjectValueType()")]
        [InfoBox("@animator.GetMismatchWarningMessage()", InfoMessageType.Error, "@animator.IsShowMismatchWarning()")]
        private SceneObjectProperty animator = new SceneObjectProperty(SceneObject.ObjectType.Animator);

        [SerializeField]
        [ShowIf("ShowCharacter")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(character)")]
        [InfoBox("@character.GetMismatchWarningMessage()", InfoMessageType.Error, "@character.IsShowMismatchWarning()")]
        private SceneObjectProperty character = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [HideIf("HideAnimator")]
        [ValueDropdown("ParameterChoice")]
        [OnInspectorGUI("DrawParameter", false)]
        [DisableIf("DisableParameter")]
        private int parameter;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(parameterName)")]
        [ShowIf("ShowCharacter")]
        [InlineProperty]
        private StringProperty parameterName;

        [SerializeField]
        [LabelText("Value")]
        [ShowIf("ShowParamValue")]
        [InlineProperty]
        [OnInspectorGUI("@MarkPropertyDirty(paramValue)")]
        private FloatProperty paramValue;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Animator Behaviour node in " + context.objectName);
            }
            else
            {
                Animator anim = null;
                if (context.graph.isAttackSkillPack)
                {
                    if (character == null || character.IsEmpty || !character.IsMatchType() || string.IsNullOrEmpty(parameterName))
                    {
                        LogWarning("Found an empty Animator Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        var charOperator = (CharacterOperator) character;
                        if (charOperator != null)
                        {
                            anim = charOperator.GetAnimator();
                            if (anim != null)
                            {
                                if (executionType == ExecutionType.SetBoolTrue)
                                    anim.SetBool(parameterName, true);
                                else if (executionType == ExecutionType.SetBoolFalse)
                                    anim.SetBool(parameterName, false);
                                else if (executionType == ExecutionType.SetTrigger)
                                    anim.SetTrigger(parameterName);
                                else if (executionType == ExecutionType.SetInt)
                                    anim.SetInteger(parameterName, paramValue);
                                else if (executionType == ExecutionType.SetFloat)
                                    anim.SetFloat(parameterName, paramValue);
                                else if (executionType is ExecutionType.GetBool or ExecutionType.GetFloat or ExecutionType.GetInt)
                                {
                                    if (!paramValue.IsVariable() && paramValue.IsNull())
                                    {
                                        LogWarning("Found an empty Animator Behaviour node in " + context.objectName);
                                    }
                                    else
                                    {
                                        if (executionType == ExecutionType.GetBool)
                                            paramValue.SetVariableValue(anim.GetBool(parameterName) ? 1 : 0);
                                        else if (executionType == ExecutionType.GetInt)
                                            paramValue.SetVariableValue(anim.GetInteger(parameterName));
                                        else if (executionType == ExecutionType.GetFloat)
                                            paramValue.SetVariableValue(anim.GetFloat(parameterName));
                                    }
                                }
                            }
                            else
                            {
                                LogWarning("Found an invalid Animator Behaviour node in " + context.objectName);
                            }
                        }
                    }
                }
                else
                {
                    if (animator.IsEmpty || !animator.IsMatchType() || parameter == 0)
                    {
                        LogWarning("Found an empty Animator Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        anim = (Animator) animator;
                        if (anim != null)
                        {
                            if (executionType == ExecutionType.SetBoolTrue)
                                anim.SetBool(parameter, true);
                            else if (executionType == ExecutionType.SetBoolFalse)
                                anim.SetBool(parameter, false);
                            else if (executionType == ExecutionType.SetTrigger)
                                anim.SetTrigger(parameter);
                            else if (executionType == ExecutionType.SetInt)
                                anim.SetInteger(parameter, paramValue);
                            else if (executionType == ExecutionType.SetFloat)
                                anim.SetFloat(parameter, paramValue);
                            else if (executionType is ExecutionType.GetBool or ExecutionType.GetFloat or ExecutionType.GetInt)
                            {
                                if (!paramValue.IsVariable() && paramValue.IsNull())
                                {
                                    LogWarning("Found an empty Animator Behaviour node in " + context.objectName);
                                }
                                else
                                {
                                    if (executionType == ExecutionType.GetBool)
                                        paramValue.SetVariableValue(anim.GetBool(parameter) ? 1 : 0);
                                    else if (executionType == ExecutionType.GetInt)
                                        paramValue.SetVariableValue(anim.GetInteger(parameter));
                                    else if (executionType == ExecutionType.GetFloat)
                                        paramValue.SetVariableValue(anim.GetFloat(parameter));
                                }
                            }
                        }
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public void OnChangeType ()
        {
            if (executionType is ExecutionType.GetBool or ExecutionType.GetFloat or ExecutionType.GetInt)
                paramValue.AllowVariableOnly();
            else
                paramValue.AllowAll();
            MarkDirty();
        }
        
        private bool HideAnimator ()
        {
            var graph = GetGraph();
            if (graph is {isAttackSkillPack: true})
                return true;
            if (executionType == ExecutionType.None)
                return true;
            return false;
        }

        private bool ShowParamValue ()
        {
            return executionType is ExecutionType.SetFloat or ExecutionType.SetInt or ExecutionType.GetBool or ExecutionType.GetFloat or ExecutionType.GetInt;
        }

        private bool ShowCharacter ()
        {
            if (executionType == ExecutionType.None)
                return false;
            var graph = GetGraph();
            if (graph is {isAttackSkillPack: true})
                return true;
            return false;
        }

        private void DrawParameter ()
        {
            GUI.enabled = true;
            if (DisableParameter())
            {
                EditorGUILayout.HelpBox("Parameter editing require gameObject to be active.", MessageType.Warning);
                GUI.enabled = false;
            }
        }

        private bool DisableParameter ()
        {
            if (!animator.IsEmpty)
            {
                var anim = ((Animator) animator);
                if (!anim.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        public ValueDropdownList<int> ParameterChoice ()
        {
            var listDropdown = new ValueDropdownList<int>();
            listDropdown.Add("Yet Select", 0);
            if (!animator.IsEmpty && animator.IsMatchType())
            {
                var anim = ((Animator) animator);
                anim.enabled = !anim.enabled;
                anim.enabled = !anim.enabled;
                var paramList = anim.parameters;
                for (var i = 0; i < paramList.Length; i++)
                {
                    AnimatorControllerParameter param = paramList[i];
                    if (executionType is ExecutionType.SetBoolTrue or ExecutionType.SetBoolFalse or ExecutionType.GetBool)
                    {
                        if (param.type == AnimatorControllerParameterType.Bool)
                            listDropdown.Add(param.name, param.nameHash);
                    }
                    else if (executionType == ExecutionType.SetTrigger)
                    {
                        if (param.type == AnimatorControllerParameterType.Trigger)
                            listDropdown.Add(param.name, param.nameHash);
                    }
                    else if (executionType is ExecutionType.SetInt or ExecutionType.GetInt)
                    {
                        if (param.type == AnimatorControllerParameterType.Int)
                            listDropdown.Add(param.name, param.nameHash);
                    }
                    else if (executionType is ExecutionType.SetFloat or ExecutionType.GetFloat)
                    {
                        if (param.type == AnimatorControllerParameterType.Float)
                            listDropdown.Add(param.name, param.nameHash);
                    }
                }
            }

            return listDropdown;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Tick Bool", ExecutionType.SetBoolTrue},
            {"Untick Bool", ExecutionType.SetBoolFalse},
            {"Call Trigger", ExecutionType.SetTrigger},
            {"Set Int", ExecutionType.SetInt},
            {"Set Float", ExecutionType.SetFloat},
            {"Get Bool", ExecutionType.GetBool},
            {"Get Int", ExecutionType.GetInt},
            {"Get Float", ExecutionType.GetFloat},
        };

        public static string displayName = "Animator Behaviour Node";
        public static string nodeName = "Animator";

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
            return $"Animation/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType != ExecutionType.None)
            {
                var paramName = string.Empty;
                var animName = string.Empty;
                var graph = GetGraph();
                if (graph is {isAttackSkillPack: true})
                {
                    paramName = parameterName;
                    if (!character.IsNull && character.IsMatchType())
                        animName = character.objectName;
                }
                else
                {
                    if (!animator.IsEmpty && animator.IsMatchType() && parameter != 0)
                    {
                        animName = animator.name;
                        var paramList = ((Animator) animator).parameters;
                        for (var i = 0; i < paramList.Length; i++)
                        {
                            AnimatorControllerParameter param = paramList[i];
                            if (param.nameHash == parameter)
                            {
                                paramName = param.name;
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(animName))
                {
                    if (executionType is ExecutionType.SetBoolTrue)
                        return "Tick " + paramName + " Bool on " + animName;
                    if (executionType is ExecutionType.SetBoolFalse)
                        return "Untick " + paramName + " Bool on " + animName;
                    if (executionType is ExecutionType.SetTrigger)
                        return "Call " + paramName + " Trigger on " + animName;
                    if (executionType is ExecutionType.SetFloat)
                        return "Set " + paramValue + " to " + paramName + " Float on " + animName;
                    if (executionType is ExecutionType.SetInt)
                        return "Set " + paramValue + " to " + paramName + " Int on " + animName;
                    if (executionType is ExecutionType.GetBool && paramValue.IsVariable() && !paramValue.IsNull())
                        return "Get " + paramName + " Bool on " + animName + " into " + paramValue.GetVariableName();
                    if (executionType is ExecutionType.GetInt && paramValue.IsVariable() && !paramValue.IsNull())
                        return "Get " + paramName + " Int on " + animName + " into " + paramValue.GetVariableName();
                    if (executionType is ExecutionType.GetFloat && paramValue.IsVariable() && !paramValue.IsNull())
                        return "Get " + paramName + " Float on " + animName + " into " + paramValue.GetVariableName();
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will provide several controls to a specific Animator.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}