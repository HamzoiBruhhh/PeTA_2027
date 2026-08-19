using UnityEngine;
using Sirenix.Utilities;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
#endif

namespace Reshape.ReFramework
{
#if UNITY_EDITOR
    public sealed class HintAttributeDrawer : OdinAttributeDrawer<HintAttribute>
    {
        private ValueResolver<bool> formattedDateResolver;
        //private ActionResolver actionResolver;
        
        protected override void Initialize()
        {
            //actionResolver = ActionResolver.Get(Property, Attribute.message);
            formattedDateResolver = ValueResolver.Get<bool>(Property, Attribute.showInfoBoxCondition);
        }
        
        protected override void DrawPropertyLayout (GUIContent label)
        {
            if (label != null)
            {
                label.tooltip = Attribute.message;
            }

            //this.actionResolver.DoActionForAllSelectionIndices();
            if (formattedDateResolver.HasError)
            {
                formattedDateResolver.DrawError();
            }
            else
            {
                bool show = (bool)formattedDateResolver.GetValue();
                if (show)
                {
                    bool guiEnabled = GUI.enabled; 
                    GUI.enabled = true;
                    SirenixEditorGUI.InfoMessageBox(Attribute.message);
                    GUI.enabled = guiEnabled;
                }
            }
            
            CallNextDrawer(label);
        }
    }
#endif
}