using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
#if REGRAPH_DEV_DEBUG
    [CreateAssetMenu(menuName = "Reshape/Item Manager", fileName = "ReshapeItemManager", order = 501)]
#endif
    [Serializable]
    [HideMonoScript]
    public class ItemManager : ScriptableObject
    {
        [ShowIf("ShowItemDB")]
        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden, Expanded = true)]
        [ListDrawerSettings(CustomAddFunction = "GenerateItemList", DraggableItems = false, Expanded = true, CustomRemoveElementFunction = "RemoveExisting")]
        [LabelText("Item DB")]
        public List<ItemList> itemDb;

        public ItemData GetItemData (string itemID)
        {
            if (itemDb != null)
            {
                for (var i = 0; i < itemDb.Count; i++)
                {
                    if (itemDb[i].FindItem(itemID, out var item))
                        return item;
                }
            }

            return null;
        }
        
#if UNITY_EDITOR
        [Button("Generate Item List")]
        [ShowIf("@ShowItemDB() == false")]
        [PropertyOrder(-2)]
        public void GenerateItemList ()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Item List", "ItemList.asset", "asset", "Please choose the a folder to save the item list");
            if (path.Length != 0)
            {
                int pathIndex = path.IndexOf("Assets", StringComparison.Ordinal);
                if (pathIndex < 0)
                {
                    ReDebug.LogError("Save Item List", "Please select a path that relative to the project Assets folder!");
                    return;
                }

                path = path.Substring(pathIndex);
                ItemList asset = ScriptableObject.CreateInstance<ItemList>();
                AssetDatabase.CreateAsset(asset, path);
                itemDb.Add(asset);
                AssetDatabase.SaveAssets();
                Selection.activeObject = asset;
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
            }
        }
        
        public void RemoveExisting (List<ItemList> db, ItemList elementToBeRemoved)
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog("Delete Item List",
                        $"Are you sure you wants to delete {elementToBeRemoved.name} ? \n\n This action is not undoable, the list will be removed permanently once confirmed.", "Confirm",
                        "Cancel"))
                {
                    itemDb.Remove(elementToBeRemoved);
                    for (int i = 0; i < elementToBeRemoved.items.Count; i++)
                    {
                        var data = elementToBeRemoved.items[i];
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(data));
                    }
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(elementToBeRemoved));
                    AssetDatabase.Refresh();
                }
            };
        }
        
        private bool ShowItemDB ()
        {
            if (itemDb != null && itemDb.Count > 0)
                return true;
            return false;
        }
#endif
    }
}