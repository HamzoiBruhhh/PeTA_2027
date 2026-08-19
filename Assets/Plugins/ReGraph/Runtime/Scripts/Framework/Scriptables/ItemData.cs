using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [Serializable]
    [HideMonoScript]
    public class ItemData : InventoryItemData, ISearchFilterable
    {
        [Hint("showHints", "Define an icon that represent the item.")]
        [PreviewField(36, ObjectFieldAlignment.Left)]
        public Sprite icon;

        [Hint("showHints", "Define name of the item.")]
        [InlineProperty]
        [LabelText("Name")]
        public StringProperty displayName;
        
        [Hint("showHints", "Define a 3D model that represent the item.")]
        public GameObject model;

        [Hint("showHints", "Define item description of the item.")]
        [InlineProperty]
        public StringProperty description;

        [Hint("showHints", "Define item categories of the item.")]
        [OnInspectorGUI("OnUpdateMultiTags")]
        [LabelText("Tag")]
        public MultiTag multiTags = new MultiTag("Tags", typeof(MultiTagInv));

        [Hint("showHints", "Define buy and sell value of the item.")]
        public int cost;

        [Hint("showHints", "Define item can be stack at an inventory slot.")]
        [ShowInInspector]
        public bool Stackable
        {
            get => stack;
            set => stack = value;
        }

        [Hint("showHints", "Define attack status that apply to the equipped unit.")]
        [LabelText("Attack Status")]
        public AttackStatusPack[] attackStatusPack;

        [Hint("showHints", "Define attack skill that apply to the equipped unit.")]
        [LabelText("Attack Skill")]
        public AttackSkillPack[] attackSkillPack;

        [Hint("showHints", "Define sub inventory data that apply on the item.")]
        [LabelText("Inventory")]
        public InventoryBehaviour invBehaviour;

        [Hint("showHints", "Define durability of the item. Attack status of the item will get removed when item durability < 0.")]
        [LabelText("Durability")]
        [ShowInInspector]
        public int Decay
        {
            get => decay;
            set => decay = value;
        }

        [Hint("showHints", "Define the number of slots will be occupy by the item.")]
        [ShowInInspector]
        public Vector2Int Size
        {
            get => size;
            set => size = value;
        }
        
        [Hint("showHints", "Define the weight of the item.")]
        [ShowInInspector]
        public int Weight    
        {
            get => weight;
            set => weight = value;
        }
        
        [Hint("showHints", "Define the unique ID of the item.")]
        [ShowInInspector, ReadOnly, HideInEditorMode]
        public string Id => id;

        [InlineProperty]
        [PropertyOrder(999)]
        [HideLabel]
        [OnInspectorInit("InitAddonInfo")]
        public ItemAddonInfo addonInfo;

        public bool isEmptyStatus => attackStatusPack is not {Length: > 0};
        public bool isEmptySkill => attackSkillPack is not {Length: > 0};

        public bool IsMatch (string searchString)
        {
            return string.Equals(searchString, displayName);
        }

#if UNITY_EDITOR
        [Button]
        [PropertySpace(16)]
        [DisableIf("DisableUpdateFileName")]
        [PropertyOrder(10000)]
        private void UpdateFileNameToItemName ()
        {
            var listPath = AssetDatabase.GetAssetPath(this);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(listPath))
            {
                if (this.GetInstanceID() != obj.GetInstanceID()) continue;
                AssetDatabase.RenameAsset(listPath, displayName);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                break;
            }
        }

        public static ValueDropdownList<ScriptableObject> GetListDropdown ()
        {
            var itemDataListDropdown = new ValueDropdownList<ScriptableObject>();
            var guids = AssetDatabase.FindAssets("t:ItemList");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var itemList = AssetDatabase.LoadAssetAtPath<ItemList>(path);
                for (var j = 0; j < itemList.items.Count; j++)
                    if (itemList.items[j])
                        itemDataListDropdown.Add(itemList.name + "/" + itemList.items[j].displayName, itemList.items[j]);
            }

            return itemDataListDropdown;
        }

        private bool DisableUpdateFileName ()
        {
            return Selection.count > 1;
        }

        private void InitAddonInfo ()
        {
            addonInfo.Init(this);
        }

        private void OnUpdateMultiTags ()
        {
            tags = multiTags;
        }

        [HideInInspector]
        public bool showHints;

        [MenuItem("CONTEXT/ItemData/Hints Display/Show", false)]
        public static void ShowHints (MenuCommand command)
        {
            var comp = (ItemData) command.context;
            comp.showHints = true;
        }

        [MenuItem("CONTEXT/ItemData/Hints Display/Show", true)]
        public static bool IsShowHints (MenuCommand command)
        {
            var comp = (ItemData) command.context;
            if (comp.showHints)
                return false;
            return true;
        }

        [MenuItem("CONTEXT/ItemData/Hints Display/Hide", false)]
        public static void HideHints (MenuCommand command)
        {
            var comp = (ItemData) command.context;
            comp.showHints = false;
        }

        [MenuItem("CONTEXT/ItemData/Hints Display/Hide", true)]
        public static bool IsHideHints (MenuCommand command)
        {
            var comp = (ItemData) command.context;
            if (!comp.showHints)
                return false;
            return true;
        }
#endif
    }
}