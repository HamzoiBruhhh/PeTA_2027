using System;
using System.Collections.Generic;
using System.Linq;
using Reshape.ReGraph;
using Reshape.Unity.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class BodyPartMaterialEditor : OdinValueDrawer<BodyPartMaterial>
    {
        protected override void DrawPropertyLayout (GUIContent label)
        {
            var previousType = ValueEntry.SmartValue.materialType;
            CallNextDrawer(label);
            if (ValueEntry.SmartValue.materialType != previousType)
            {
                ValueEntry.ApplyChanges();
                if (!Application.isPlaying)
                {
                    foreach (var target in this.Property.Tree.WeakTargets)
                    {
                        if (target is UnityEngine.Object unityObj)
                        {
                            EditorUtility.SetDirty(unityObj);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(unityObj);
                        }
                    }
                }
            }
        }
    }
}