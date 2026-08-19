using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class SceneObjectDrawer : OdinValueDrawer<SceneObject>
    {
        protected override void DrawPropertyLayout (GUIContent label)
        {
            var value = this.ValueEntry.SmartValue;
            if (!value.showType && !value.ShowGo() && !value.ShowMaterial() && !value.ShowSprite() && !value.ShowMesh() && !value.ShowAudioMixer() && !value.ShowScriptableObject() && value.showAsNodeProperty)
            {
                var temp = value.component;
                var fieldLabel = value.ComponentName();
                if (fieldLabel.Equals("HIDE", StringComparison.InvariantCulture))
                    value.component = (Component) EditorGUILayout.ObjectField(value.component, value.ComponentType(), true);
                else
                    value.component = (Component) EditorGUILayout.ObjectField(fieldLabel, value.component, value.ComponentType(), true);
                if (temp != value.component)
                    value.dirty = true;
            }
            else if (!value.showType && value.ShowGo() && value.showAsNodeProperty)
            {
                var temp = value.gameObject;
                var fieldLabel = value.GameObjectName();
                if (fieldLabel.Equals("HIDE", StringComparison.InvariantCulture))
                    value.gameObject = (GameObject) EditorGUILayout.ObjectField(value.gameObject, typeof(GameObject), true);
                else
                    value.gameObject = (GameObject) EditorGUILayout.ObjectField(fieldLabel, value.gameObject, typeof(GameObject), true);
                if (temp != value.gameObject)
                    value.dirty = true;
            }

            this.ValueEntry.SmartValue = value;

            CallNextDrawer(label);
        }
    }
}