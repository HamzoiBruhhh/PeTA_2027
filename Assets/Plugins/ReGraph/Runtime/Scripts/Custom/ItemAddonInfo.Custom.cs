using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace Reshape.ReFramework
{
    [Serializable]
    public class BodyPartMesh
    {
        [ValueDropdown("TypeChoice")]
        public string type;

        public Mesh[] meshes;

#if UNITY_EDITOR
        public ValueDropdownList<string> TypeChoice ()
        {
            return MultiTagBodyPart.GetBodyPartChoice();
        }
#endif
    }

    [Serializable]
    public class BodyPartMaterialList
    {
        [LabelText("@MaterialName()")]
        public Material[] materials;

#if UNITY_EDITOR
        private string MaterialName ()
        {
            return $"For Mesh {index+1}";
        }
        
        [HideInInspector]
        public int index;
#endif
    }

    [Serializable]
    public class BodyPartMaterial
    {
        [ValueDropdown("TypeChoice")]
        public string type;
        
        [ShowIf("ShowMaterialsObject")]
        [ListDrawerSettings(OnTitleBarGUI = "DrawSwitchButton")]
        public SceneObjectList[] materials;

        [LabelText("Materials")]
        [ShowIf("ShowMaterialsRaw")]
        [ListDrawerSettings(OnTitleBarGUI = "DrawSwitchButton", OnBeginListElementGUI = "OnBeginSwitchButton")]
        public BodyPartMaterialList[] materialsRaw;

        [HideInInspector]
        public int materialType = 0;

        public List<Material> GetMaterials (int index)
        {
            var mat = new List<Material>();
            if (materialType == 0 && materials != null)
            {
                if (index < materials.Length)
                {
                    if (materials[index] != null && materials[index].GetCount() > 0)
                    {
                        var count = materials[index].GetCount();
                        for (var k = 0; k < count; k++)
                        {
                            var obj = materials[index].GetByIndex(k);
                            if (obj != null)
                                mat.Add(obj);
                        }
                    }
                }
            }
            else if (materialType == 1 && materialsRaw != null)
            {
                if (index < materialsRaw.Length)
                {
                    if (materialsRaw[index] != null && materialsRaw[index].materials is {Length: > 0})
                    {
                        var count = materialsRaw[index].materials.Length;
                        for (var k = 0; k < count; k++)
                        {
                            var obj = materialsRaw[index].materials[k];
                            if (obj != null)
                                mat.Add(obj);
                        }
                    }
                }
            }

            return mat;
        }

#if UNITY_EDITOR
        private void OnBeginSwitchButton (int index)
        {
            materialsRaw[index].index = index;
        }
        
        private void DrawSwitchButton ()
        {
            if (SirenixEditorGUI.ToolbarButton("▼"))
            {
                materialType = materialType == 0 ? 1 : 0;
            }
        }

        public bool ShowMaterialsRaw ()
        {
            return materialType == 1;
        }
        
        public bool ShowMaterialsObject ()
        {
            return materialType == 0;
        }
        
        public ValueDropdownList<string> TypeChoice ()
        {
            return MultiTagBodyPart.GetBodyPartChoice();
        }
#endif
    }

    public partial class ItemAddonInfo
    {
        [BoxGroup("Equipment Behaviours")]
        [LabelText("Show Body Part")]
        [Hint("ShowHints", "Define which body part being show when equipped this item.")]
        public MultiTag showBodyPartTag = new MultiTag("Body Part Tags", typeof(MultiTagBodyPart));

        [BoxGroup("Equipment Behaviours")]
        [LabelText("Hide Body Part")]
        [Hint("ShowHints", "Define which body part being hide when equipped this item.")]
        public MultiTag hideBodyPartTag = new MultiTag("Body Part Tags", typeof(MultiTagBodyPart));

        [BoxGroup("Equipment Behaviours")]
        [Hint("ShowHints", "Define body part mesh being update when equipped this item.")]
        public BodyPartMesh[] bodyPartMesh;

        [BoxGroup("Equipment Behaviours")]
        [Hint("ShowHints", "Define body part materials being update when equipped this item.")]
        public BodyPartMaterial[] bodyPartMaterial;

#if UNITY_EDITOR
        private bool ShowHints ()
        {
            return data != null && data.showHints;
        }
#endif
    }
}