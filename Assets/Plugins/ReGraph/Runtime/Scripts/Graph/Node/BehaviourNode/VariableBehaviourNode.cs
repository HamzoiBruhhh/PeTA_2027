using Reshape.ReFramework;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class VariableBehaviourNode : BehaviourNode
    {
        [SerializeField]
        [OnValueChanged("OnChangeInfo")]
        [HideLabel]
        [OnInspectorGUI("CheckVariableDirty")]
        private VariableBehaviourInfo variableBehaviour;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (variableBehaviour.type == VariableBehaviourInfo.Type.None || variableBehaviour.variable == null)
            {
                LogWarning("Found an empty Variable Behaviour node in " + context.objectName);
            }
            else
            {
                var result = variableBehaviour.Activate(this, context);
                UpdateConditionNode(execution, updateId, result);
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        public VariableScriptableObject GetVariable ()
        {
            return variableBehaviour.variable;
        }
        
        private void OnChangeInfo ()
        {
            MarkDirty();
            if (variableBehaviour.typeChanged is 1001 or 2001 or 3001)
            {
                variableBehaviour.typeChanged--;
                MarkRepaint();
            }
        }

        private void CheckVariableDirty ()
        {
            string createVarPath = GraphEditorVariable.GetString(GetGraphSelectionInstanceID(), "createVariable");
            if (!string.IsNullOrEmpty(createVarPath))
            {
                GraphEditorVariable.SetString(GetGraphSelectionInstanceID(), "createVariable", string.Empty);
                var createVar = (VariableScriptableObject) AssetDatabase.LoadAssetAtPath(createVarPath, typeof(VariableScriptableObject));
                var info = variableBehaviour;
                info.variable = createVar;
                variableBehaviour = info;
                MarkDirty();
                MarkRepaint();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            MarkPropertyDirty(variableBehaviour.number);
            MarkPropertyDirty(variableBehaviour.number2);
            MarkPropertyDirty(variableBehaviour.message);
            MarkPropertyDirty(variableBehaviour.sceneObject);
        }

        public static string displayName = "Variable Behaviour Node";
        public static string nodeName = "Variable";

        public override bool IsPortReachable (GraphNode node)
        {
            if (node is YesConditionNode or NoConditionNode)
            {
                if (variableBehaviour.type != VariableBehaviourInfo.Type.CheckCondition)
                    return false;
            }
            else if (node is ChoiceConditionNode)
            {
                return false;
            }

            return true;
        }

        public bool AcceptConditionNode ()
        {
            if (variableBehaviour.type == VariableBehaviourInfo.Type.CheckCondition)
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
            return variableBehaviour.type.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Logic/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (variableBehaviour.type != VariableBehaviourInfo.Type.None && variableBehaviour.variable != null)
            {
                var message = variableBehaviour.variable.name + ReExtensions.STRING_SPACE + variableBehaviour.type.ToString().SplitCamelCase().ToLower();
                if (variableBehaviour.type == VariableBehaviourInfo.Type.CheckCondition)
                {
                    if (variableBehaviour.condition != VariableBehaviourInfo.Condition.None)
                    {
                        var conditionString = variableBehaviour.condition.ToString().SplitCamelCase().ToLower();
                        if (variableBehaviour.condition == VariableBehaviourInfo.Condition.LessThan)
                            conditionString = "<";
                        else if (variableBehaviour.condition == VariableBehaviourInfo.Condition.MoreThan)
                            conditionString = ">";
                        else if (variableBehaviour.condition == VariableBehaviourInfo.Condition.LessThanAndEqual)
                            conditionString = "<=";
                        else if (variableBehaviour.condition == VariableBehaviourInfo.Condition.MoreThanAndEqual)
                            conditionString = ">=";
                        else if (variableBehaviour.condition == VariableBehaviourInfo.Condition.Equal)
                            conditionString = "=";
                        if (variableBehaviour.variable is NumberVariable)
                        {
                            message += " : " + conditionString + ReExtensions.STRING_SPACE + variableBehaviour.number.GetDisplayName();
                        }
                        else if (variableBehaviour.variable is WordVariable)
                        {
                            if (string.IsNullOrEmpty(variableBehaviour.message))
                                message += " : " + conditionString + " Empty";
                            else
                                message += " : " + conditionString + ReExtensions.STRING_SPACE + variableBehaviour.message;
                        }
                        else if (variableBehaviour.variable is SceneObjectVariable)
                        {
                            if (variableBehaviour.condition is VariableBehaviourInfo.Condition.Equal or VariableBehaviourInfo.Condition.EqualNull)
                                message += " : Equal Null";
                            else if (variableBehaviour.condition is VariableBehaviourInfo.Condition.Contains or VariableBehaviourInfo.Condition.EqualId)
                                message += " : Equal Id";
                        }
                    }
                }
                else if (variableBehaviour.type is VariableBehaviourInfo.Type.SetValue or VariableBehaviourInfo.Type.AddValue or VariableBehaviourInfo.Type.MinusValue or
                         VariableBehaviourInfo.Type.MultiplyValue or VariableBehaviourInfo.Type.DivideValue or VariableBehaviourInfo.Type.RoundValue or
                         VariableBehaviourInfo.Type.MinValue or VariableBehaviourInfo.Type.MaxValue)
                {
                    if (variableBehaviour.variable is NumberVariable)
                    {
                        message += " : " + variableBehaviour.number.GetDisplayName();
                    }
                    else if (variableBehaviour.variable is WordVariable)
                    {
                        message += " : " + variableBehaviour.message.GetDisplayName();
                    }
                    else if (variableBehaviour.variable is SceneObjectVariable)
                    {
                        message += " : " + variableBehaviour.sceneObject.objectName;
                    }
                }
                else if (variableBehaviour.type is VariableBehaviourInfo.Type.RandomValue)
                {
                    if (!variableBehaviour.check)
                    {
                        message += " : 1-100";
                    }
                    else
                    {
                        message += " : " + variableBehaviour.number + "-" + variableBehaviour.number2;
                    }
                }

                return message;
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (variableBehaviour.type != VariableBehaviourInfo.Type.None && variableBehaviour.variable != null)
            {
                if (variableBehaviour.type == VariableBehaviourInfo.Type.CheckCondition)
                {
                    tip += "This will check the variable value base on the condition configurations.\n\nIf result is positive, it will execute the Yes Condition node linked to it. Otherwise it will execute the No Condition node linked to it.\n\n";
                }
                else if (variableBehaviour.type is VariableBehaviourInfo.Type.SetValue or VariableBehaviourInfo.Type.AddValue or VariableBehaviourInfo.Type.MinusValue or
                         VariableBehaviourInfo.Type.MultiplyValue or VariableBehaviourInfo.Type.DivideValue or VariableBehaviourInfo.Type.RoundValue or
                         VariableBehaviourInfo.Type.MinValue or VariableBehaviourInfo.Type.MaxValue)
                {
                    tip += "This will change the variable value base on the configurations.\n\n";
                }
            }
            else
                tip += "This will execute all Variable related behaviour.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}