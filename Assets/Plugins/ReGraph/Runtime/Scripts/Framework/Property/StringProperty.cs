using System;
using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    [Serializable]
    public class StringProperty : ReProperty, IClone<StringProperty>
    {
        [SerializeField]
        [HideLabel]
        [ShowIf("@type == 0 || type == 3")]
        [InlineButton("SwitchToVariable", "▼", ShowIf = "ShowStringSwitchButton")]
        [MultiLineResizable]
        [OnValueChanged("MarkDirty")]
        private string stringValue;

        [SerializeField]
        [HideLabel]
        [ShowIf("@type == 1 || type == 2")]
        [InlineButton("SwitchToString", "▼", ShowIf = "ShowVariableSwitchButton")]
        [InlineButton("CreateWordVariable", "✚")]
        [OnValueChanged("MarkDirty")]
        private WordVariable variableValue;

        [HideInInspector]
        public int type = 0;

        public StringProperty ShallowCopy ()
        {
            return (StringProperty) this.MemberwiseClone();
        }

        public static implicit operator string (StringProperty s)
        {
            return s == null ? string.Empty : s.ToString();
        }

        public override string ToString ()
        {
            if (type is 0 or 3)
                return stringValue;
            if (variableValue == null)
                return string.Empty;
            return variableValue.ToString();
        }

        public bool Equals (string compare)
        {
            return string.Equals(ToString(), compare);
        }

        public bool Equals (WordVariable compare)
        {
            if (type is 0 or 3)
                return false;
            return variableValue == compare;
        }

        public string value => (string) this;

        public void Reset ()
        {
            type = 0;
            variableValue = null;
            stringValue = string.Empty;
        }

        public bool IsAssigned ()
        {
            if (IsVariable())
            {
                if (variableValue == null)
                    return false;
                return true;
            }

            return !string.IsNullOrEmpty(stringValue);
        }

        public bool IsNull ()
        {
            if (IsVariable() && variableValue == null)
                return true;
            return false;
        }

        public bool IsVariable ()
        {
            return type is 1 or 2;
        }

        public void SetValue (string v)
        {
            if (IsVariable())
            {
                if (variableValue != null)
                    SetVariableValue(v);
            }
            else
                SetStringValue(v);
        }

        public void SetStringValue (string v)
        {
            stringValue = v;
        }

        public void SetVariableValue (string v)
        {
            variableValue.SetValue(v);
        }

        public string GetVariableName ()
        {
            if (variableValue)
                return variableValue.name;
            return string.Empty;
        }

#if UNITY_EDITOR
        public bool ShowStringSwitchButton ()
        {
            if (type == 3)
                return false;
            return true;
        }

        public bool ShowVariableSwitchButton ()
        {
            if (type == 2)
                return false;
            return true;
        }

        public void AllowVariableOnly ()
        {
            dirty = true;
            type = 2;
        }

        public void AllowStringOnly ()
        {
            dirty = true;
            type = 3;
        }

        public void AllowAll ()
        {
            if (type == 2)
            {
                dirty = true;
                type = 1;
            }
            else if (type == 3)
            {
                dirty = true;
                type = 0;
            }
        }

        public string GetDisplayName ()
        {
            if (IsVariable())
                return GetVariableName();
            return ToString();
        }

        private void CreateWordVariable ()
        {
            variableValue = WordVariable.CreateNew(variableValue);
            dirty = true;
        }

        private void MarkDirty ()
        {
            dirty = true;
        }

        private void SwitchToVariable ()
        {
            dirty = true;
            type = 1;
        }

        private void SwitchToString ()
        {
            dirty = true;
            type = 0;
        }
#endif
    }
}