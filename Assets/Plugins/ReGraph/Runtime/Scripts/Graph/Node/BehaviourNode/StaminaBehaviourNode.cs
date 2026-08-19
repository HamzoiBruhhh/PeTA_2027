using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class StaminaBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            GetCost = 10,
            HaveCost = 100,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(character)")]
        [InlineButton("@character.SetObjectValue(AssignComponent<CharacterOperator>())", "♺", ShowIf = "@character.IsObjectValueType()")]
        [InfoBox("@character.GetMismatchWarningMessage()", InfoMessageType.Error, "@character.IsShowMismatchWarning()")]
        private SceneObjectProperty character = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");
        
        [ValueDropdown("@Stamina.DrawTypeChoiceDropdown()")]
        [OnValueChanged("MarkDirty")]
        public Stamina.Type staminaType;
        
        [LabelText("Variable")]
        [ShowIf("@executionType == ExecutionType.GetCost")]
        [OnInspectorGUI("@MarkPropertyDirty(number1)")]
        [InlineProperty]
        public FloatProperty number1;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Stamina Behaviour node in " + context.objectName);
            }
            else if (character == null || character.IsEmpty || !character.IsMatchType())
            {
                LogWarning("Found an empty Stamina Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.GetCost)
            {
                if (staminaType == Stamina.Type.None || !number1.IsVariable() || number1.IsNull())
                {
                    LogWarning("Found an empty Stamina Behaviour node in " + context.objectName);
                }
                else
                {
                    var charOperator = (CharacterOperator) character;
                    number1.SetVariableValue(charOperator.GetStaminaConsume(staminaType));
                }
            }
            else if (executionType is ExecutionType.HaveCost)
            {
                if (staminaType == Stamina.Type.None)
                {
                    LogWarning("Found an empty Stamina Behaviour node in " + context.objectName);
                }
                else
                {
                    var charOperator = (CharacterOperator) character;
                    var cost= charOperator.GetStaminaConsume(staminaType);
                    var current = charOperator.currentStamina;
                    var had = current >= cost;
                    UpdateConditionNode(execution, updateId, had);
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public void OnChangeType ()
        {
            if (executionType == ExecutionType.GetCost)
                number1.AllowVariableOnly();
            else
                number1.AllowAll();
            MarkDirty();
            MarkRepaint();
        }
        
        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Get Cost", ExecutionType.GetCost},
            {"Have Cost", ExecutionType.HaveCost},
        };

        public static string displayName = "Stamina Behaviour Node";
        public static string nodeName = "Stamina";

        public override bool IsPortReachable (GraphNode node)
        {
            if (node is YesConditionNode or NoConditionNode)
                return AcceptConditionNode();
            if (node is ChoiceConditionNode)
                return false;
            return true;
        }

        public bool AcceptConditionNode ()
        {
            return executionType is ExecutionType.HaveCost;
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
            if (executionType != ExecutionType.None && !character.IsNull && character.IsMatchType() && staminaType != Stamina.Type.None)
            {
                if (executionType == ExecutionType.GetCost)
                {
                    if (number1.IsVariable() && !number1.IsNull())
                        return $"Get {character.objectName}'s {staminaType} cost into {number1.GetVariableName()}";
                }
                else if (executionType == ExecutionType.HaveCost)
                {
                    return $"Check {character.objectName} have enough for {staminaType}";
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.GetCost)
                tip += "This will get the cost of the stamina type.\n\n";
            else if (executionType == ExecutionType.HaveCost)
                tip += "This will check current stamina compare with the require cost of the stamina type.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}