using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Reshape.Unity.Editor;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class StatTypeList : ScriptableObject
    {
        [Serializable]
        public class StatTypeListItem
        {
            [HideLabel]
            public string statName;

            [HideInInspector]
            public StatType statObj;
        }
        
        [PropertySpace(SpaceBefore = 5, SpaceAfter = 5)]
        [ListDrawerSettings(ShowPaging = false)]
        [LabelText("Stats")]
        public StatTypeListItem[] statType;

        public bool GenerateStatType ()
        {
            var assets = AssetDatabase.FindAssets("t:StatTypeList");
            for (var i = 0; i < assets.Length; i++)
            {
                bool processed = false;
                var listPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(listPath))
                {
                    if (this.GetInstanceID() != obj.GetInstanceID()) continue;

                    var me = (StatTypeList) obj;
                    var listFolderPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(assets[i]));
#if UNITY_EDITOR_WIN
                    var choicesPath = listFolderPath + "\\"+me.name+"Stats\\";
#elif UNITY_EDITOR_OSX
                    var choicesPath = listFolderPath + "/" + me.name + "Stats/";
#endif

                    var availableStat = new List<StatTypeListItem>();
                    for (var j = 0; j < statType.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(statType[j].statName))
                        {
                            EditorUtility.DisplayDialog("Generate All Stat Type", "Please make sure all stat is not empty!", "OK");
                            return false;
                        }

                        for (var k = 0; k < availableStat.Count; k++)
                        {
                            if (availableStat[k].statName == statType[j].statName)
                            {
                                EditorUtility.DisplayDialog("Generate All Stat Type", "Please make sure all stat is not duplicated!", "OK");
                                return false;
                            }
                        }

                        availableStat.Add(statType[j]);
                    }

                    var abandonStat = new List<string>();
                    var choiceAssets = AssetDatabase.FindAssets("t:StatType", new[] {choicesPath});
                    for (var j = 0; j < choiceAssets.Length; j++)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(choiceAssets[j]);
                        var loaded = AssetDatabase.LoadAssetAtPath<StatType>(path);
                        bool contains = false;
                        for (var k = 0; k < availableStat.Count; k++)
                        {
                            if (availableStat[k].statObj == loaded)
                            {
                                if (loaded.name != availableStat[k].statName)
                                    AssetDatabase.RenameAsset(path, availableStat[k].statName);
                                loaded.statName = availableStat[k].statName;
                                EditorUtility.SetDirty(loaded);
                                AssetDatabase.SaveAssets();
                                availableStat.RemoveAt(k);
                                contains = true;
                                break;
                            }
                        }

                        if (!contains)
                            abandonStat.Add(path);
                    }

                    if (availableStat.Count > 0)
                    {
                        if (!Directory.Exists(choicesPath))
                            Directory.CreateDirectory(choicesPath);
                    }

                    for (var j = 0; j < abandonStat.Count; j++)
                    {
                        var directoryPath = Path.GetDirectoryName(abandonStat[j]);
                        if (directoryPath is {Length: > 6})
                            directoryPath = Path.GetDirectoryName(directoryPath);
                        if (listFolderPath == directoryPath)
                            AssetDatabase.DeleteAsset(abandonStat[j]);
                    }

                    for (var j = 0; j < availableStat.Count; j++)
                    {
                        var statTypeInst = ScriptableObject.CreateInstance<StatType>();
                        statTypeInst.statName = availableStat[j].statName;
                        AssetDatabase.CreateAsset(statTypeInst, choicesPath + availableStat[j].statName + ".asset");
                        AssetDatabase.SaveAssets();
                        EditorUtility.FocusProjectWindow();
                        availableStat[j].statObj = statTypeInst;
                    }

                    if (availableStat.Count > 0)
                        EditorUtility.SetDirty(obj);
                    processed = true;
                    break;
                }

                if (processed)
                    break;
            }

            return true;
        }

#if UNITY_EDITOR
        public static StatTypeList CreateNew ()
        {
            var path = EditorUtility.SaveFilePanelInProject("Graph Variable", "New Number Variable", "asset", "Select a location to create variable");
            if (path.Length == 0 || !Directory.Exists(Path.GetDirectoryName(path)))
                return null;
            return ReEditorHelper.CreateScriptableObject<StatTypeList>((asset, assetPath) => { }, true, true, filePath: path);
        }
#endif
    }
}