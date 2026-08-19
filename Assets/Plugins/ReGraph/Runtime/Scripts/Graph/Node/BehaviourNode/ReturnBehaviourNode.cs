using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class ReturnBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            ReturnFalse = 0,
            ReturnTrue = 1,
            ReturnFloat = 10,
            ReturnInt = 11,
            ReturnCharacter = 2147470000,
            ReturnBackstab = 2147480000,
            ReturnMiss = 2147483000,
            ReturnDodge = 2147483001,
            ReturnBlock = 2147483002,
            ReturnCritical = 2147483003,
            ReturnParry = 2147483004,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [InlineProperty]
        [OnInspectorGUI("@MarkPropertyDirty(numberParam)")]
        [ShowIf("@executionType == ExecutionType.ReturnFloat || executionType == ExecutionType.ReturnInt")]
        [LabelText("Variable")]
        private FloatProperty numberParam;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.ReturnCharacter")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        [LabelText("Variable")]
        private SceneObjectVariable characterVariable;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.ReturnFloat or ExecutionType.ReturnInt)
            {
                if (numberParam.IsNull())
                {
                    LogWarning("Found an empty Return Stat Behaviour node in " + context.objectName);
                }
                else
                {
                    if (executionType == ExecutionType.ReturnFloat)
                    {
                        execution.variables.SetFloat(execution.parameters.actionName, numberParam, GraphVariables.PREFIX_RETURN);
                    }
                    else if (executionType == ExecutionType.ReturnInt)
                    {
                        execution.variables.SetInt(execution.parameters.actionName, numberParam, GraphVariables.PREFIX_RETURN);
                    }
                }
            }
            else if (executionType is ExecutionType.ReturnTrue or ExecutionType.ReturnFalse)
            {
                execution.variables.SetInt(execution.parameters.actionName, (int) executionType, GraphVariables.PREFIX_RETURN);
            }
            else if (executionType is ExecutionType.ReturnMiss or ExecutionType.ReturnDodge or ExecutionType.ReturnBlock or ExecutionType.ReturnBackstab or ExecutionType.ReturnCritical or ExecutionType.ReturnParry)
            {
                execution.variables.SetInt(execution.parameters.actionName, (int) executionType, GraphVariables.PREFIX_RETURN);
            }
            else if (executionType == ExecutionType.ReturnCharacter)
            {
                if (characterVariable == null || characterVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                {
                    LogWarning("Found an empty Return Stat Behaviour node in " + context.objectName);
                }
                else
                {
                    var character = (CharacterOperator) characterVariable.GetComponent();
                    if (character == null)
                    {
                        LogWarning("Found an invalid Return Character Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        execution.variables.SetCharacter(execution.parameters.actionName, character, GraphVariables.PREFIX_RETURN);
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private bool ShowObjectVariableWarning ()
        {
            if (characterVariable != null)
                if (characterVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                    return true;
            return false;
        }

        public ValueDropdownList<ExecutionType> TypeChoice ()
        {
            var typeListDropdown = new ValueDropdownList<ExecutionType>();
            var graph = GetGraph();
            if (graph is {isAttackDamagePack: true})
            {
                typeListDropdown.Add("Miss", ExecutionType.ReturnMiss);
                typeListDropdown.Add("Dodge", ExecutionType.ReturnDodge);
                typeListDropdown.Add("Block", ExecutionType.ReturnBlock);
                typeListDropdown.Add("Critical", ExecutionType.ReturnCritical);
                typeListDropdown.Add("Backstab", ExecutionType.ReturnBackstab);
                typeListDropdown.Add("Parry", ExecutionType.ReturnParry);
                typeListDropdown.Add("Float", ExecutionType.ReturnFloat);
                typeListDropdown.Add("Integer", ExecutionType.ReturnInt);
            }
            else if (graph is {isTargetAimPack: true})
            {
                typeListDropdown.Add("Character", ExecutionType.ReturnCharacter);
            }
            else if (graph is {isAttackSkillPack: true})
            {
                typeListDropdown.Add("True", ExecutionType.ReturnTrue);
                typeListDropdown.Add("False", ExecutionType.ReturnFalse);
            }
            else
            {
                typeListDropdown.Add("Float", ExecutionType.ReturnFloat);
                typeListDropdown.Add("Integer", ExecutionType.ReturnInt);
            }

            return typeListDropdown;
        }

        public static string displayName = "Return Behaviour Node";
        public static string nodeName = "Return";

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
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.ReturnFloat || executionType == ExecutionType.ReturnInt)
            {
                if (!numberParam.IsNull())
                    return $"Return {numberParam.GetDisplayName()}";
            }
            else if (executionType is ExecutionType.ReturnMiss or ExecutionType.ReturnDodge or ExecutionType.ReturnBlock or ExecutionType.ReturnBackstab or ExecutionType.ReturnCritical or ExecutionType.ReturnParry)
            {
                return executionType.ToString().SplitCamelCase();
            }
            else if (executionType == ExecutionType.ReturnCharacter)
            {
                if (characterVariable != null && characterVariable.sceneObject.type == SceneObject.ObjectType.CharacterOperator)
                    return $"Return {characterVariable.name}";
            }
            else if (executionType == ExecutionType.ReturnTrue)
            {
                return $"Return True";
            }
            else if (executionType == ExecutionType.ReturnFalse)
            {
                return $"Return False";
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            var graph = GetGraph();
            if (graph is {isAttackDamagePack: true})
                tip += "This will return the damage result back to Attack Damage pack.\n\n";
            else if (graph is {isTargetAimPack: true})
                tip += "This will return the character value back to Target Aim pack.\n\n";
            else if (graph is {isAttackSkillPack: true})
                tip += "This will return the detect value back to Attack Skill pack.\n\n";
            else
                tip += "This will return the number value back to BrainStat Trigger node.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}