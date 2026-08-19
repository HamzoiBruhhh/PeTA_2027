using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace Reshape.ReFramework
{
    public class StatTypeListWindow : OdinEditorWindow
    {
        [InlineEditor(Expanded = true)]
        [PropertyOrder(-3)]
        [OnInspectorGUI("DrawGenerateAllButton")]
        [ListDrawerSettings(OnTitleBarGUI = "DrawRefreshButton", HideRemoveButton = true, HideAddButton = true, DraggableItems = false)]
        public List<StatTypeList> statTypeList;

        [MenuItem("Tools/Reshape/Edit Stats", priority = 10901)]
        public static void OpenWindow ()
        {
            var window = GetWindow<StatTypeListWindow>("Stats", true);
            window.statTypeList = new List<StatTypeList>();
            window.AssignStatTypeList();
            window.Show();
        }

        private void DrawGenerateAllButton ()
        {
            if (statTypeList != null && statTypeList.Count > 0)
            {
                if (GUILayout.Button("Generate All Stat Type", GUILayout.Height(30)))
                {
                    var duplicate = false;
                    var allName = new List<string>();
                    for (var j = 0; j < statTypeList.Count; j++)
                    {
                        for (var i = 0; i < statTypeList[j].statType.Length; i++)
                        {
                            if (!allName.Contains(statTypeList[j].statType[i].statName))
                                allName.Add(statTypeList[j].statType[i].statName);
                            else
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (duplicate)
                            break;
                    }

                    if (!duplicate)
                    {
                        var failed = false;
                        for (var i = 0; i < statTypeList.Count; i++)
                        {
                            if (statTypeList[i] != null)
                            {
                                if (!statTypeList[i].GenerateStatType())
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }

                        if (!failed)
                            EditorUtility.DisplayDialog("Generate All Stat Type", "Generate successfully!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Generate All Stat Type", "Please make sure all stat is not duplicated!", "OK");
                    }
                }
            }

            GUI.enabled = false;
        }

        private void DrawRefreshButton ()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
            {
                var created = StatTypeList.CreateNew();
                if (created != null)
                    statTypeList.Add(created);
            }


            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                statTypeList.Clear();
                AssignStatTypeList();
            }
        }

        private void AssignStatTypeList ()
        {
            var assets = AssetDatabase.FindAssets("t:StatTypeList");
            for (var i = 0; i < assets.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[i]);
                var typeList = AssetDatabase.LoadAssetAtPath<StatTypeList>(path);
                statTypeList.Add(typeList);
            }
        }
    }
}