using System;
using System.Globalization;
using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    [Serializable]
    public class FloatProperty : ReProperty, IClone<FloatProperty>
    {
        [SerializeField]
        [HideLabel]
        [ShowIf("@type == 0")]
        [InlineButton("SwitchToVariable", "▼")]
        [OnValueChanged("MarkDirty")]
        private float floatValue;

        [SerializeField]
        [HideLabel]
        [ShowIf("@type == 1 || type == 2")]
        [InlineButton("SwitchToFloat", "▼", ShowIf = "ShowSwitchButton")]
        [InlineButton("CreateNumberVariable", "✚")]
        [OnValueChanged("MarkDirty")]
        private NumberVariable variableValue;

        [HideInInspector]
        public int type = 0;

        public FloatProperty ShallowCopy ()
        {
            return (FloatProperty) this.MemberwiseClone();
        }

        public static implicit operator float (FloatProperty f)
        {
            if (f == null)
                return 0f;
            if (f.type == 0)
                return f.floatValue;
            if (f.variableValue == null)
                return 0f;
            return f.variableValue;
        }

        public static implicit operator int (FloatProperty f)
        {
            if (f == null)
                return 0;
            if (f.type == 0)
                return (int) f.floatValue;
            if (f.variableValue == null)
                return 0;
            return (int) f.variableValue;
        }


        public static explicit operator FloatProperty (float f) => new FloatProperty(f);

        public FloatProperty (float f)
        {
            type = 0;
            variableValue = null;
            floatValue = f;
        }

        public override string ToString ()
        {
            if (type == 0)
                return floatValue.DisplayString();
            if (variableValue == null)
                return "0";
            return variableValue.ToString();
        }
        
        public bool Equals (float compare)
        {
            return Math.Abs(value - compare) < 0.00001f;
        }
        
        public bool Equals (NumberVariable compare)
        {
            if (type is 0)
                return false;
            return variableValue == compare;
        }

        public float value => (float) this;

        public void Reset ()
        {
            type = 0;
            variableValue = null;
            floatValue = 0;
        }
        
        public bool IsValidPositive ()
        {
            if (type is 1 or 2 && variableValue)
                return true;
            if (type == 0 && value > 0)
                return true;
            return false;
        }
        
        public bool IsValid ()
        {
            if (type is 1 or 2 && variableValue)
                return true;
            if (type == 0 && value != 0)
                return true;
            return false;
        }

        public bool IsNull ()
        {
            if (type is 1 or 2 && !variableValue)
                return true;
            return false;
        }

        public bool IsVariable ()
        {
            return type is 1 or 2;
        }
        
        public bool IsAssigned ()
        {
            if (IsVariable())
                return variableValue != null;
            return true;
        }

        public void SetVariableValue (float value)
        {
            variableValue.SetValue(value);
        }
        
        public string GetVariableName ()
        {
            if (variableValue)
                return variableValue.name;
            return string.Empty;
        }

#if UNITY_EDITOR
        public bool ShowSwitchButton ()
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

        public void AllowAll ()
        {
            if (type == 2)
            {
                dirty = true;
                type = 1;
            }
        }

        public string GetDisplayName ()
        {
            if (IsVariable())
                return GetVariableName();
            return ToString();
        }

        private void CreateNumberVariable ()
        {
            variableValue = NumberVariable.CreateNew(variableValue);
            dirty = true;
        }

        private void MarkDirty ()
        {
            dirty = true;
        }

        public void SwitchToVariable ()
        {
            dirty = true;
            type = 1;
        }

        public void SwitchToFloat ()
        {
            dirty = true;
            type = 0;
        }
#endif
    }
}