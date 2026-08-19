using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Action Name List", fileName = "ReshapeActionNameList", order = 105)]
    public class ActionNameList : ScriptableObject
    {
        [Serializable]
        public class ActionNameListItem
        {
            [HideLabel]
            public string actionName;

            [HideInInspector]
            public ActionNameChoice actionChoice;
            
            public string ValueToString()
            {
                return actionName;
            }
        }

        [PropertySpace(SpaceBefore = 0, SpaceAfter = 10)]
        [ListDrawerSettings(OnTitleBarGUI = "DrawSortButton", ShowPaging = true, NumberOfItemsPerPage = 20)]
        public ActionNameListItem[] actionNames;

        [Button("Generate Action Name")]
        [HideIf("@this.GetInstanceID() != 23462")]
        public bool GenerateActionNameChoice ()
        {
            var assets = AssetDatabase.FindAssets("t:ActionNameList");
            for (var i = 0; i < assets.Length; i++)
            {
                var processed = false;
                var listPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                foreach (var nameList in AssetDatabase.LoadAllAssetsAtPath(listPath))
                {
                    if (this.GetInstanceID() != nameList.GetInstanceID()) continue;

                    var listFolderPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(assets[i]));
#if UNITY_EDITOR_WIN
                    var choicesPath = listFolderPath + "\\ActionName\\";
#elif UNITY_EDITOR_OSX
                    var choicesPath = listFolderPath + "/ActionName/";
#endif
                    var availableAction = new List<ActionNameListItem>();
                    for (var j = 0; j < actionNames.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(actionNames[j].actionName))
                        {
                            EditorUtility.DisplayDialog("Generate All Action Name", "Please make sure all action name is not empty!", "OK");
                            return false;
                        }

                        for (var k = 0; k < availableAction.Count; k++)
                        {
                            if (availableAction[k].actionName == actionNames[j].actionName)
                            {
                                EditorUtility.DisplayDialog("Generate All Action Name", "Please make sure all action name is not duplicated!", "OK");
                                return false;
                            }
                        }

                        availableAction.Add(actionNames[j]);
                    }

                    var abandonAction = new List<string>();
                    var choiceAssets = AssetDatabase.FindAssets("t:ActionNameChoice", new[] {choicesPath});
                    for (var j = 0; j < choiceAssets.Length; j++)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(choiceAssets[j]);
                        var loaded = AssetDatabase.LoadAssetAtPath<ActionNameChoice>(path);
                        bool contains = false;
                        for (var k = 0; k < availableAction.Count; k++)
                        {
                            if (availableAction[k].actionChoice == loaded)
                            {
                                if (loaded.name != availableAction[k].actionName)
                                    AssetDatabase.RenameAsset(path, availableAction[k].actionName);
                                loaded.actionName = availableAction[k].actionName;
                                EditorUtility.SetDirty(loaded);
                                AssetDatabase.SaveAssets();
                                availableAction.RemoveAt(k);
                                contains = true;
                                break;
                            }
                        }

                        if (!contains)
                            abandonAction.Add(path);
                    }

                    if (availableAction.Count > 0)
                        if (!Directory.Exists(choicesPath))
                            Directory.CreateDirectory(choicesPath);

                    for (var j = 0; j < abandonAction.Count; j++)
                    {
                        var directoryPath = Path.GetDirectoryName(abandonAction[j]);
                        if (directoryPath is {Length: > 6})
                            directoryPath = Path.GetDirectoryName(directoryPath);
                        if (listFolderPath == directoryPath)
                            AssetDatabase.DeleteAsset(abandonAction[j]);
                    }

                    for (var j = 0; j < availableAction.Count; j++)
                    {
                        var actionNameChoice = ScriptableObject.CreateInstance<ActionNameChoice>();
                        actionNameChoice.actionName = availableAction[j].actionName;
                        AssetDatabase.CreateAsset(actionNameChoice, choicesPath + availableAction[j].actionName + ".asset");
                        AssetDatabase.SaveAssets();
                        EditorUtility.FocusProjectWindow();
                        availableAction[j].actionChoice = actionNameChoice;
                    }
                    
                    if (availableAction.Count > 0)
                        EditorUtility.SetDirty(nameList);
                    processed = true;
                    break;
                }

                if (processed)
                    break;
            }

            return true;
        }
        
        private void DrawSortButton ()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.ArrowUp))
            {
                var temp = actionNames.ToList();
                temp.Sort(ComparisonAscending);
                actionNames = temp.ToArray();
                EditorUtility.SetDirty(this);
            }
            
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.ArrowDown))
            {
                var temp = actionNames.ToList();
                temp.Sort(ComparisonDescending);
                actionNames = temp.ToArray();
                EditorUtility.SetDirty(this);
            }
        }
        
        private int ComparisonAscending (ActionNameListItem x, ActionNameListItem y)
        {
            string xName = x.actionName;
            string yName = y.actionName;
            return string.Compare(xName, yName, StringComparison.Ordinal);
        }
        
        private int ComparisonDescending (ActionNameListItem x, ActionNameListItem y)
        {
            string xName = x.actionName;
            string yName = y.actionName;
            return string.Compare(yName, xName, StringComparison.Ordinal);
        }
    }
}