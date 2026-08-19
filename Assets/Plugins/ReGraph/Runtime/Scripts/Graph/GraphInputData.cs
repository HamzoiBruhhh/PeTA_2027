using System;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [Serializable]
    public class GraphInputData : IClone<GraphInputData>
    {
        public enum Type
        {
            Number = 1,
            Word = 2,
            Object = 3,
        }

        [HorizontalGroup(0.4f)]
        [HideLabel]
        [DisplayAsString]
#if UNITY_EDITOR
        [GUIColor(nameof(GetVariableFieldColor))]
#endif
        [SuffixLabel("@NameSuffix()")]
        public string name;

        [HideInInspector]
        public int type;

        [HorizontalGroup(0.6f)]
        [HideLabel]
        [ShowIf("ShowNumber")]
        [OnInspectorGUI("@MarkFloatDirty()")]
        [InlineProperty]
        public FloatProperty numberVar;

        [HorizontalGroup(0.6f)]
        [HideLabel]
        [ShowIf("ShowWord")]
        [OnInspectorGUI("@MarkWordDirty()")]
        [InlineProperty]
        public StringProperty wordVar;

        [HorizontalGroup(0.6f)]
        [HideLabel]
        [ShowIf("ShowObject")]
        [OnInspectorGUI("@MarkObjectDirty()")]
        [InlineProperty]
        [HideReferenceObjectPicker]
        public SceneObjectProperty objectVar;

        [HideInInspector]
        public bool dirty;

        private float backupFloat;
        private string backupString;
        private SceneObject backupObject;

        public void Transfer (VariableScriptableObject target)
        {
            if (!target)
                return;
            if (type == (int) Type.Number)
            {
                var num = (NumberVariable) target;
                num.SetValue(backupFloat);
            }
            else if (type == (int) Type.Word)
            {
                var word = (WordVariable) target;
                word.SetValue(backupString);
            }
            else if (type == (int) Type.Object)
            {
                var obj = (SceneObjectVariable) target;
                obj.SetValue(backupObject);
            }
        }

        public void Backup ()
        {
            if (type == (int) Type.Number)
            {
                backupFloat = numberVar;
            }
            else if (type == (int) Type.Word)
            {
                backupString = wordVar;
            }
            else if (type == (int) Type.Object)
            {
                if (objectVar != null)
                    backupObject = objectVar.GetSceneObject();
            }
        }

        public void Restore ()
        {
            if (type == (int) Type.Number && numberVar.IsVariable() && !numberVar.IsNull())
            {
                numberVar.SetVariableValue(backupFloat);
            }
            else if (type == (int) Type.Word && wordVar.IsVariable() && wordVar.IsAssigned())
            {
                wordVar.SetVariableValue(backupString);
            }
            else if (type == (int) Type.Object && objectVar != null && objectVar.IsVariableValueType() && !objectVar.IsEmpty)
            {
                objectVar.SetVariableValue(backupObject);
            }
        }

        public string variableName
        {
            get
            {
                if (type == (int) Type.Number)
                {
                    if (numberVar.IsVariable())
                        return numberVar.GetVariableName();
                }
                else if (type == (int) Type.Word)
                {
                    if (wordVar.IsVariable())
                        return wordVar.GetVariableName();
                }
                else if (type == (int) Type.Object)
                {
                    if (objectVar != null)
                        if (objectVar.IsVariableValueType())
                            return objectVar.GetVariableName();
                }
                
                return string.Empty;
            }
        }
        
        public GraphInputData ShallowCopy ()
        {
            var info = new GraphInputData(null);
            info.name = name;
            info.type = type;
            info.numberVar = numberVar.ShallowCopy();
            info.wordVar = wordVar.ShallowCopy();
            info.objectVar = objectVar.ShallowCopy();
            return info;
        }

        public GraphInputData (VariableScriptableObject so)
        {
            if (so)
            {
                name = so.name;
                if (so is NumberVariable)
                    type = (int) Type.Number;
                else if (so is WordVariable)
                    type = (int) Type.Word;
                else if (so is SceneObjectVariable)
                {
                    type = (int) Type.Object;
                    var obj = (SceneObjectVariable) so;
                    objectVar = new SceneObjectProperty(obj.sceneObject.type, "HIDE");
                }

                MarkDirty();
            }
        }
        
        private void MarkDirty ()
        {
            dirty = true;
        }

#if UNITY_EDITOR
        private bool ShowNumber ()
        {
            return type == (int) Type.Number;
        }

        private bool ShowWord ()
        {
            return type == (int) Type.Word;
        }

        private bool ShowObject ()
        {
            return type == (int) Type.Object;
        }

        private void MarkFloatDirty ()
        {
            if (numberVar.dirty)
            {
                numberVar.dirty = false;
                MarkDirty();
            }
        }

        private void MarkWordDirty ()
        {
            if (wordVar.dirty)
            {
                wordVar.dirty = false;
                MarkDirty();
            }
        }

        private void MarkObjectDirty ()
        {
            if (objectVar is {dirty: true})
            {
                objectVar.dirty = false;
                MarkDirty();
            }
        }

        private Color GetVariableFieldColor ()
        {
            if (type == (int) Type.Object && objectVar is {IsNull: false} && !objectVar.IsMatchType())
                return new Color(1, 0.7f, 0.7f);
            return Color.white;
        }

        private string NameSuffix ()
        {
            if (type == (int) Type.Object && objectVar != null && objectVar.IsVariableValueType())
                return $"({objectVar.objectValue.type})";
            return string.Empty;
        }
#endif
    }
}