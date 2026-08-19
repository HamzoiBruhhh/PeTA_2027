using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Reshape.Unity;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif


namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Item List", fileName = "ReshapeItemList", order = 104)]
    [Serializable]
    [HideMonoScript]
    public class ItemList : ScriptableObject
    {
        [LabelText("@DisplayName()")]
        [ListDrawerSettings(CustomAddFunction = "CreateNew", DraggableItems = true, CustomRemoveElementFunction = "RemoveExisting", OnBeginListElementGUI = "BeginDrawListElement",
            OnEndListElementGUI = "EndDrawListElement", OnTitleBarGUI = "DrawSortButton", NumberOfItemsPerPage = 20)]
        //[Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<ItemData> items;

        public bool FindItem (string itemID, out ItemData item)
        {
            item = null;
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] && items[i].id == itemID)
                    {
                        item = items[i];
                        return true;
                    }
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void DrawSortButton ()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.ArrowUp))
            {
                items.Sort(ComparisonAscending);
                EditorUtility.SetDirty(this);
            }
            
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.ArrowDown))
            {
                items.Sort(ComparisonDescending);
                EditorUtility.SetDirty(this);
            }
        }

        private int ComparisonAscending (ItemData x, ItemData y)
        {
            string xName = x.displayName;
            string yName = y.displayName;
            return string.Compare(xName, yName, StringComparison.Ordinal);
        }
        
        private int ComparisonDescending (ItemData x, ItemData y)
        {
            string xName = x.displayName;
            string yName = y.displayName;
            return string.Compare(yName, xName, StringComparison.Ordinal);
        }

        private void BeginDrawListElement (int index)
        {
            GUILayout.BeginHorizontal();
            var displayName = string.Empty;
            if (index >= 0 && index < items.Count && items[index] != null)
                displayName = items[index].displayName;
            GUILayout.Label(displayName, GUILayout.MinWidth(100), GUILayout.MaxWidth(200));
            GUI.enabled = false;
        }

        private void EndDrawListElement (int index)
        {
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        public void CreateNew ()
        {
            var savePath = string.Empty;
            var settings = RuntimeSettings.GetSettings();
            if (settings != null)
            {
                savePath = settings.itemDataSaveFolder;
#if UNITY_EDITOR_WIN
                savePath += "\\";
#elif UNITY_EDITOR_OSX
                savePath += "/";
#endif
            }

            if (string.IsNullOrEmpty(savePath))
            {
                var listPath = AssetDatabase.GetAssetPath(this);
                var listFolderPath = Path.GetDirectoryName(listPath);
#if UNITY_EDITOR_WIN
                savePath = listFolderPath + "\\ItemData\\";
#elif UNITY_EDITOR_OSX
                savePath = listFolderPath + "/ItemData/";
#endif
            }

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.id = ReUniqueId.GenerateId();
            AssetDatabase.CreateAsset(itemData, savePath + itemData.id + ".asset");
            items.Add(itemData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void RemoveExisting (List<ItemData> list, ItemData elementToBeRemoved)
        {
            items.Remove(elementToBeRemoved);
            var path = AssetDatabase.GetAssetPath(elementToBeRemoved);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(elementToBeRemoved));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public string DisplayName ()
        {
            return ReExtensions.SplitCamelCase(this.name);
        }
#endif
    }
}