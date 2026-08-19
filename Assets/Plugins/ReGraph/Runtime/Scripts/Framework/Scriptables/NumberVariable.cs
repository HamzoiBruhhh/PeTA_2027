using System;
using System.Globalization;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using Reshape.Unity.Editor;
#endif

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Number Variable", order = 11)]
    [Serializable]
    public class NumberVariable : VariableScriptableObject
    {
        [OnValueChanged("OnChangeValue")]
        public float value;

        [ReadOnly]
        [HideInEditorMode]
        public float runtimeValue;

        public float GetValue ()
        {
            return runtimeValue;
        }

        public override object GetObject ()
        {
            return GetValue();
        }

        public override void SetObject (object obj)
        {
            if (obj is int o)
                SetValue(o);
            else if (obj is long l)
                SetValue(l);
            else if (obj is double d)
                SetValue((float) d);
            else if (obj is float f)
                SetValue(f);
        }

        public void SetValue (float input)
        {
            if (!IsEqual(input))
            {
                runtimeValue = input;
                OnChanged();
            }
        }

        public void AddValue (float input)
        {
            runtimeValue += input;
            OnChanged();
        }

        public void MinusValue (float input)
        {
            runtimeValue -= input;
            OnChanged();
        }

        public void MultiplyValue (float input)
        {
            runtimeValue *= input;
            OnChanged();
        }

        public void DivideValue (float input)
        {
            runtimeValue /= input;
            OnChanged();
        }

        public void RandomValue ()
        {
            runtimeValue = ReRandom.Range(1, 101);
            OnChanged();
        }

        public void RandomValue (float min, float max)
        {
            if (int.TryParse(min.ToString(CultureInfo.InvariantCulture), out var a))
            {
                if (int.TryParse(max.ToString(CultureInfo.InvariantCulture), out var b))
                    runtimeValue = ReRandom.Range(a, b);
                else
                    runtimeValue = ReRandom.Range(min, max);
            }
            else
                runtimeValue = ReRandom.Range(min, max);
            OnChanged();
        }

        public void RoundValue (float input)
        {
            runtimeValue = runtimeValue.Round((int) input * -1);
            OnChanged();
        }

        public void CeilValue ()
        {
            runtimeValue = Mathf.Ceil(runtimeValue);
            OnChanged();
        }

        public void FloorValue ()
        {
            runtimeValue = Mathf.Floor(runtimeValue);
            OnChanged();
        }

        public void MaxValue (float input)
        {
            if (runtimeValue > input)
            {
                runtimeValue = input;
                OnChanged();
            }
        }

        public void MinValue (float input)
        {
            if (runtimeValue < input)
            {
                runtimeValue = input;
                OnChanged();
            }
        }

        public bool IsEqual (float input)
        {
            return input.Equals(runtimeValue);
        }

        public static implicit operator float (NumberVariable f)
        {
            return f ? f.runtimeValue : default;
        }

        public static implicit operator int (NumberVariable f)
        {
            if (f)
                return (int) f.runtimeValue;
            return default;
        }

        public static implicit operator string (NumberVariable f)
        {
            return f ? f.ToString() : default;
        }

        public override string ToString ()
        {
            return runtimeValue.DisplayString();
        }

        protected override void OnChanged ()
        {
            if (!resetLinked)
            {
                onReset -= OnReset;
                onReset += OnReset;
                resetLinked = true;
            }
            
            base.OnEarlyChanged();
            base.OnChanged();
        }
        
        public override void Rebase ()
        {
            SetValue(value);
        }

        public override void OnReset ()
        {
            Rebase();
            base.OnReset();
        }

#if UNITY_EDITOR
        public void EditorReset ()
        {
            runtimeValue = value;
            resetLinked = false;
            sceneUnloadLinked = false;
        }
        
        private void OnChangeValue ()
        {
            if (!IsEqual(value))
                runtimeValue = value;
        }

        public static VariableScriptableObject CreateNew (VariableScriptableObject variable)
        {
            if (variable == null)
            {
                return CreateNew(null);
            }
            else if (variable.GetType() == typeof(NumberVariable))
            {
                return CreateNew((NumberVariable) variable);
            }
            else
            {
                bool proceed = EditorUtility.DisplayDialog("Graph Variable", "Are you sure you want to create a new variable to replace the existing assigned variable ?", "OK", "Cancel");
                if (proceed)
                {
                    var number = CreateNew(null);
                    if (number != null)
                        return number;
                }
            }

            return variable;
        }

        public static NumberVariable CreateNew (NumberVariable number)
        {
            if (number != null)
            {
                bool proceed = EditorUtility.DisplayDialog("Graph Variable", "Are you sure you want to create a new variable to replace the existing assigned variable ?", "OK", "Cancel");
                if (!proceed)
                    return number;
            }

            var path = EditorUtility.SaveFilePanelInProject("Graph Variable", "New Number Variable", "asset", "Select a location to create variable");
            if (path.Length == 0)
                return number;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                return number;

            NumberVariable variable = ScriptableObject.CreateInstance<NumberVariable>();
            AssetDatabase.CreateAsset(variable, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return variable;
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class NumberVariableResetOnPlay
    {
        static NumberVariableResetOnPlay ()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged (PlayModeStateChange state)
        {
            ReEditorHelper.HavePlayModeStateChange(state, out var enter, out var exit);
            if (exit)
            {
                var guids = AssetDatabase.FindAssets("t:NumberVariable");
                if (guids.Length > 0)
                {
                    for (var i = 0; i < guids.Length; i++)
                    {
                        var variable = (NumberVariable) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object));
                        if (variable != null)
                            variable.EditorReset();
                    }

                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
#endif
}