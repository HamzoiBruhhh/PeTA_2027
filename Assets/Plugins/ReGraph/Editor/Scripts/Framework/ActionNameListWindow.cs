using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class ActionNameListWindow : OdinEditorWindow
    {
        [InlineEditor(Expanded = true)]
        [ListDrawerSettings(OnTitleBarGUI = "DrawRefreshButton", HideRemoveButton = true, HideAddButton = true, DraggableItems = false)]
        [PropertyOrder(-3)]
        [OnInspectorGUI("DrawGenerateAllButton")]
        public List<ActionNameList> customActionNameList;

        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden, Expanded = true)]
        [PropertyOrder(-1)]
        [BoxGroup("Default Action Name List")]
        public ActionNameList defaultActionNameList;

        [MenuItem("Tools/Reshape/Edit Action Name", priority = 10900)]
        public static void OpenWindow ()
        {
            var window = GetWindow<ActionNameListWindow>();
            window.customActionNameList = new List<ActionNameList>();
            window.AssignActionNameList();
            window.Show();
        }

        private void DrawGenerateAllButton ()
        {
            if (GUILayout.Button("Generate All Action Name", GUILayout.Height(30)))
            {
                var duplicate = false;
                var allName = new List<string>();
                for (var i = 0; i < defaultActionNameList.actionNames.Length; i++)
                {
                    if (!allName.Contains(defaultActionNameList.actionNames[i].actionName))
                        allName.Add(defaultActionNameList.actionNames[i].actionName);
                    else
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    for (var j = 0; j < customActionNameList.Count; j++)
                    {
                        for (var i = 0; i < customActionNameList[j].actionNames.Length; i++)
                        {
                            if (!allName.Contains(customActionNameList[j].actionNames[i].actionName))
                                allName.Add(customActionNameList[j].actionNames[i].actionName);
                            else
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (duplicate)
                            break;
                    }
                }

                if (!duplicate)
                {
                    var failed = false;
                    if (defaultActionNameList != null)
                        failed = defaultActionNameList.GenerateActionNameChoice() == false;
                    if (!failed)
                    {
                        for (var i = 0; i < customActionNameList.Count; i++)
                        {
                            if (customActionNameList[i] != null)
                            {
                                if (!customActionNameList[i].GenerateActionNameChoice())
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!failed)
                        EditorUtility.DisplayDialog("Generate All Action Name", "Generate successfully!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Generate All Action Name", "Please make sure all action name is not duplicated!", "OK");
                }
            }

            GUI.enabled = false;
        }

        private void DrawRefreshButton ()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                defaultActionNameList = null;
                customActionNameList.Clear();
                AssignActionNameList();
            }
        }

        private void AssignActionNameList ()
        {
            var assets = AssetDatabase.FindAssets("t:ActionNameList");
            for (var i = 0; i < assets.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[i]);
                var nameList = AssetDatabase.LoadAssetAtPath<ActionNameList>(path);
                var checkPath = "Runtime/Datas/DefaultActionNameList.asset";
                if (path.Contains(checkPath))
                    defaultActionNameList = nameList;
                else
                    customActionNameList.Add(nameList);
            }
        }
    }
}