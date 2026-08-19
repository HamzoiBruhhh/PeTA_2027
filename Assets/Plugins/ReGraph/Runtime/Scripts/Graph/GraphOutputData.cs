using System;
using Reshape.ReFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReGraph
{
    [Serializable]
    public class GraphOutputData : IClone<GraphOutputData>
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
        public string name;

        [HideInInspector]
        public int type;

        [HorizontalGroup(0.6f)]
        [HideLabel]
        public VariableScriptableObject variable;

        public void Reset ()
        {
            if (!variable)
                return;
            variable.Rebase();
        }
        
        public void Return (VariableScriptableObject target)
        {
            if (!target || !variable)
                return;
            if (type == (int) Type.Number)
            {
                if (variable is NumberVariable mine && target is NumberVariable num)
                    mine.SetValue(num);
                
            }else if (type == (int) Type.Word)
            {
                if (variable is WordVariable mine && target is WordVariable word)
                    mine.SetValue(word);
            }
            else if (type == (int) Type.Object)
            {
                if (variable is SceneObjectVariable mine && target is SceneObjectVariable word)
                {
                    if (mine.sceneObject.type == word.sceneObject.type)
                        mine.SetValue(word.sceneObject);
                }
            }
        }
        
        public string variableName => variable ? variable.name : string.Empty;

        public GraphOutputData ShallowCopy ()
        {
            var info = new GraphOutputData(null);
            info.name = name;
            info.type = type;
            info.variable = variable;
            return info;
        }
        
        public GraphOutputData (VariableScriptableObject so)
        {
            if (so)
            {
                name = so.name;
                if (so is NumberVariable)
                    type = (int) Type.Number;
                else if (so is WordVariable)
                    type = (int) Type.Word;
                else if (so is SceneObjectVariable)
                    type = (int) Type.Object;
            }
        }

#if UNITY_EDITOR
        private Color GetVariableFieldColor ()
        {
            if (variable != null)
            {
                if (variable is NumberVariable && type != (int) Type.Number)
                    return Color.red;
                if (variable is WordVariable && type != (int) Type.Word)
                    return Color.red;
                if (variable is SceneObjectVariable && type != (int) Type.Object)
                    return Color.red;
            }

            return Color.white;
        }
#endif
    }
}