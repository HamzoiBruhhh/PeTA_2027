using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace Reshape.ReFramework
{
    public class LayerAttributeDrawer : OdinAttributeDrawer<LayerAttribute, int>
    {
        protected override void DrawPropertyLayout (GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect();
            if (label != null)
                rect = EditorGUI.PrefixLabel(rect, label);
            var oldValue = ValueEntry.SmartValue;
            var value = EditorGUI.LayerField(rect, oldValue);
            if (value != oldValue)
            {
                ValueEntry.SmartValue = value;
                ValueEntry.Property.MarkSerializationRootDirty();
            }
        }
    }
}