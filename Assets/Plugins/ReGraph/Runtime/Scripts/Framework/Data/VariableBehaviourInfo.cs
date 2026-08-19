using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [System.Serializable]
    public struct VariableBehaviourInfo : IClone<VariableBehaviourInfo>
    {
        public enum Type
        {
            None = 0,
            SetValue = 1,
            AddValue = 2,
            MinusValue = 3,
            MultiplyValue = 4,
            DivideValue = 5,
            RandomValue = 6,
            RoundValue = 7,
            CeilValue = 8,
            FloorValue = 9,
            MinValue = 10,
            MaxValue = 11,
            CheckCondition = 20,
            ClearValue = 100,
            GetId = 200,
            AppendValue = 300,
        }

        public enum Condition
        {
            None = 0,
            Equal = 1,
            NotEqual = 2,
            LessThan = 31,
            MoreThan = 32,
            LessThanAndEqual = 33,
            MoreThanAndEqual = 34,
            Contains = 101,
            EqualNull = 1001,
            EqualId = 1002,
        }

        [OnValueChanged("OnChangeVariable")]
        [InlineButton("@VariableScriptableObject.OpenCreateVariableMenu(variable)", "✚")]
        public VariableScriptableObject variable;

        [InfoBox("ID is unique per runtime session, only AttackStatusPack ID is persistent.", InfoMessageType.Info, "@type == Type.GetId")]
        [ValueDropdown("TypeChoice")]
        [OnInspectorGUI("ShowTip")]
        [OnValueChanged("OnChangeType")]
        [ShowIf("ShowParamType")]
        public Type type;

        [ValueDropdown("ConditionChoice")]
        [ShowIf("ShowParamCondition")]
        public Condition condition;

        [LabelText("@CheckBoxLabel()")]
        [ShowIf("ShowParamCheck")]
        public bool check;

        [LabelText("@NumberLabel()")]
        [ShowIf("ShowParamNumber")]
        [InlineProperty]
        public FloatProperty number;

        [LabelText("@Number2Label()")]
        [ShowIf("ShowParamNumber2")]
        [InlineProperty]
        public FloatProperty number2;

        [LabelText("Value")]
        [ShowIf("ShowParamMessage")]
        [InlineProperty]
        public StringProperty message;

        [HideLabel, InlineProperty]
        [ShowIf("ShowParamSceneObject")]
        [InfoBox("@sceneObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@sceneObject.IsShowMismatchWarning()")]
        public SceneObjectProperty sceneObject;

        [LabelText("Character Value")]
        [ShowIf("ShowParamCharacter")]
        [InfoBox("Character Value property is deprecated, please make change to use Character property once you see it have value in it.", InfoMessageType.Error, "@character != null")]
        [ReadOnly]
        public CharacterOperator character;

        public VariableBehaviourInfo ShallowCopy ()
        {
            var info = new VariableBehaviourInfo();
            info.variable = variable;
            info.type = type;
            info.condition = condition;
            info.check = check;
            info.number = number.ShallowCopy();
            info.number2 = number2.ShallowCopy();
            info.message = message.ShallowCopy();
            info.sceneObject = sceneObject.ShallowCopy();
            info.character = character;
            return info;
        }

        public bool Activate (VariableBehaviourNode node, GraphContext context)
        {
            if (!variable)
                return false;
            if (variable is NumberVariable numVar)
            {
                if (type == Type.SetValue)
                    numVar.SetValue(number);
                else if (type == Type.AddValue)
                    numVar.AddValue(number);
                else if (type == Type.MinusValue)
                    numVar.MinusValue(number);
                else if (type == Type.DivideValue)
                    numVar.DivideValue(number);
                else if (type == Type.MultiplyValue)
                    numVar.MultiplyValue(number);
                else if (type == Type.RandomValue)
                {
                    if (!check)
                        numVar.RandomValue();
                    else
                        numVar.RandomValue(number, number2);
                }
                else if (type == Type.RoundValue)
                    numVar.RoundValue(number);
                else if (type == Type.CeilValue)
                    numVar.CeilValue();
                else if (type == Type.FloorValue)
                    numVar.FloorValue();
                else if (type == Type.MinValue)
                    numVar.MinValue(number);
                else if (type == Type.MaxValue)
                    numVar.MaxValue(number);
                else if (type == Type.CheckCondition)
                {
                    if (condition is Condition.Equal or Condition.LessThanAndEqual or Condition.MoreThanAndEqual)
                        if (numVar.IsEqual(number))
                            return true;
                    if (condition == Condition.NotEqual)
                        if (!numVar.IsEqual(number))
                            return true;
                    if (condition is Condition.LessThan or Condition.LessThanAndEqual)
                        if ((float) numVar < (float) number)
                            return true;
                    if (condition is Condition.MoreThan or Condition.MoreThanAndEqual)
                        if ((float) numVar > (float) number)
                            return true;
                }
            }
            else if (variable is WordVariable wordVar)
            {
                if (type == Type.SetValue)
                    wordVar.SetValue(message);
                else if (type == Type.AppendValue)
                    wordVar.SetValue(wordVar + message);
                else if (type == Type.CheckCondition)
                {
                    if (condition == Condition.Equal)
                    {
                        if (wordVar.IsEqual(message))
                            return true;
                    }
                    else if (condition == Condition.Contains)
                    {
                        if (wordVar.Contains(message))
                            return true;
                    }
                }
            }
            else if (variable is SceneObjectVariable soVar)
            {
                if (type == Type.ClearValue)
                    soVar.Rebase();
                else if (type == Type.GetId)
                {
                    number.SetVariableValue(soVar.GetSceneObjectUniqueID());
                }
                else if (type == Type.CheckCondition)
                {
                    if (condition is Condition.Equal or Condition.EqualNull)
                        return soVar.IsUnassigned();
                    if (condition is Condition.Contains or Condition.EqualId)
                        return soVar.GetSceneObjectUniqueID() == number;
                }
                else if (type == Type.SetValue)
                {
                    if (soVar.sceneObject.IsSprite())
                        soVar.SetValue((Sprite) sceneObject);
                    else if (soVar.sceneObject.IsAttackStatusPack())
                        soVar.SetValue((AttackStatusPack) sceneObject);
                    else if (soVar.sceneObject.IsCharacterOperator())
                    {
                        soVar.SetValue(character);
                        if (character != null)
                            ReDebug.LogWarning("Graph Warning", "<Color='Red'>Please inform developer</Color> : Character Value property is being deprecated and being use in " + context.objectName);
                        soVar.SetValue((CharacterOperator) sceneObject);
                    }
                }
            }

            return false;
        }

        [HideInInspector]
        public int typeChanged;

#if UNITY_EDITOR
        private void OnChangeType ()
        {
            if (variable is NumberVariable)
            {
                typeChanged = 1001;
            }
            else if (variable is WordVariable)
            {
                typeChanged = 2001;
            }
            else if (variable is SceneObjectVariable)
            {
                if (type == Type.GetId)
                    number.AllowVariableOnly();
                else
                    number.AllowAll();
                typeChanged = 3001;
            }
        }

        private void OnChangeVariable ()
        {
            var reset = false;
            if (variable is NumberVariable)
            {
                if (typeChanged is not 1000)
                {
                    typeChanged = 1001;
                    reset = true;
                }
            }
            else if (variable is WordVariable)
            {
                if (typeChanged is not 2000)
                {
                    typeChanged = 2001;
                    reset = true;
                }
            }
            else if (variable is SceneObjectVariable so)
            {
                if (so.sceneObject.IsSprite())
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.Sprite, "Sprite");
                else if (so.sceneObject.IsCharacterOperator())
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator, "Character");
                else if (so.sceneObject.IsAttackStatusPack())
                    sceneObject = new SceneObjectProperty(SceneObject.ObjectType.AttackStatusPack, "Attack Status");
                else if (type == Type.SetValue)
                    reset = true;
                if (typeChanged is not 3000)
                {
                    typeChanged = 3001;
                    reset = true;
                }
            }

            if (reset)
            {
                type = Type.None;
                condition = Condition.None;
                number.Reset();
                number2.Reset();
                message.Reset();
                check = false;
            }
        }

        private bool ShowParamSceneObject ()
        {
            if (type == Type.SetValue && variable is SceneObjectVariable so)
                if (so.sceneObject.IsSprite() || so.sceneObject.IsCharacterOperator() || so.sceneObject.IsAttackStatusPack())
                    return true;
            return false;
        }

        private bool ShowParamType ()
        {
            if (variable != null)
                return true;
            return false;
        }

        private bool ShowParamNumber ()
        {
            if (variable != null && variable is NumberVariable)
            {
                if (type == Type.RandomValue)
                {
                    if (check)
                        return true;
                }
                else if (type != Type.CeilValue && type != Type.FloorValue)
                    return true;
            }
            else if (variable != null && variable is SceneObjectVariable)
            {
                if (type == Type.GetId)
                    return true;
                if (type == Type.CheckCondition && condition is Condition.Contains or Condition.EqualId)
                    return true;
            }

            return false;
        }

        private bool ShowParamNumber2 ()
        {
            if (variable != null && variable is NumberVariable)
            {
                if (type == Type.RandomValue)
                {
                    if (check)
                        return true;
                }
            }

            return false;
        }

        private bool ShowParamCharacter ()
        {
#if REGRAPH_DESCENT
            if (type == Type.SetValue && variable != null && variable is SceneObjectVariable objectVariable)
                if (objectVariable.sceneObject.IsCharacterOperator())
                    return character;
            return false;
#else
            return false;
#endif
        }

        private bool ShowParamMessage ()
        {
            if (variable != null && variable is WordVariable)
                return true;
            return false;
        }

        private bool ShowParamCheck ()
        {
            if (variable != null && variable is NumberVariable)
            {
                if (type == Type.RandomValue)
                    return true;
            }

            return false;
        }

        private bool ShowParamCondition ()
        {
            if (variable != null && type == Type.CheckCondition)
                return true;
            return false;
        }

        private string CheckBoxLabel ()
        {
            if (type == Type.RandomValue)
                return "Custom Range";
            return string.Empty;
        }

        private string NumberLabel ()
        {
            if (type == Type.RandomValue)
                return "Min Value";
            if (type == Type.RoundValue)
                return "Fractional";
            return "Value";
        }

        private string Number2Label ()
        {
            if (type == Type.RandomValue)
                return "Max Value";
            return string.Empty;
        }

        public ValueDropdownList<Condition> ConditionChoice ()
        {
            var listDropdown = new ValueDropdownList<Condition>();
            if (variable is NumberVariable)
            {
                listDropdown.Add("Equal", Condition.Equal);
                listDropdown.Add("Not Equal", Condition.NotEqual);
                listDropdown.Add("Less Than", Condition.LessThan);
                listDropdown.Add("More Than", Condition.MoreThan);
                listDropdown.Add("Less Than And Equal", Condition.LessThanAndEqual);
                listDropdown.Add("More Than And Equal", Condition.MoreThanAndEqual);
            }
            else if (variable is WordVariable)
            {
                listDropdown.Add("Equal", Condition.Equal);
                listDropdown.Add("Contains", Condition.Contains);
            }
            else if (variable is SceneObjectVariable)
            {
                listDropdown.Add("Equal Null", Condition.EqualNull);
                listDropdown.Add("Equal Id", Condition.EqualId);
            }

            return listDropdown;
        }

        public ValueDropdownList<Type> TypeChoice ()
        {
            var listDropdown = new ValueDropdownList<Type>();
            if (variable is NumberVariable)
            {
                listDropdown.Add("Set Value", Type.SetValue);
                listDropdown.Add("Add Value", Type.AddValue);
                listDropdown.Add("Minus Value", Type.MinusValue);
                listDropdown.Add("Multiply Value", Type.MultiplyValue);
                listDropdown.Add("Divide Value", Type.DivideValue);
                listDropdown.Add("Random Value", Type.RandomValue);
                listDropdown.Add("Round Value", Type.RoundValue);
                listDropdown.Add("Ceil Value", Type.CeilValue);
                listDropdown.Add("Floor Value", Type.FloorValue);
                listDropdown.Add("Min Value", Type.MinValue);
                listDropdown.Add("Max Value", Type.MaxValue);
                listDropdown.Add("Check Condition", Type.CheckCondition);
            }
            else if (variable is WordVariable)
            {
                listDropdown.Add("Set Value", Type.SetValue);
                listDropdown.Add("Append Value", Type.AppendValue);
                listDropdown.Add("Check Condition", Type.CheckCondition);
            }
            else if (variable is SceneObjectVariable objectVariable)
            {
                if (objectVariable.sceneObject.IsCharacterOperator() || objectVariable.sceneObject.IsSprite() || objectVariable.sceneObject.IsAttackStatusPack())
                    listDropdown.Add("Set Value", Type.SetValue);
                listDropdown.Add("Get Id", Type.GetId);
                listDropdown.Add("Check Condition", Type.CheckCondition);
                listDropdown.Add("Clear Value", Type.ClearValue);
            }

            return listDropdown;
        }

        private void ShowTip ()
        {
            if (type == Type.RandomValue && !check)
            {
                EditorGUILayout.HelpBox("Random between 1 to 100", MessageType.Info);
            }
        }
#endif
    }
}