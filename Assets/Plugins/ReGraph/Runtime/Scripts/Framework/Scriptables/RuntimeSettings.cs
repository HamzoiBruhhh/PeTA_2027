using System.Collections.Generic;
using Reshape.Unity;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
#if REGRAPH_DEV_DEBUG
    [CreateAssetMenu(menuName = "Reshape/Runtime Settings", order = 203)]
#endif
    [HideMonoScript]
    public class RuntimeSettings : BaseScriptable
    {
        [BoxGroup("Graph"), LabelWidth(180)]
        [PropertyTooltip("The Graph Manager Prefab")]
        public GameObject graphManager;

        [BoxGroup("Graph"), LabelWidth(180)]
        [PropertyTooltip("Skip execute all debug behaviour node")]
        [LabelText("Skip Debug Node")]
        public bool skipDebugNode;

        [BoxGroup("Graph"), LabelWidth(180)]
        [PropertyTooltip("Display deep graph system message for debugging")]
        public bool deepLogging;

        [BoxGroup("Outline for BRP"), LabelWidth(180)]
        public Shader outlineShader;

        [BoxGroup("Outline for BRP"), LabelWidth(180)]
        public Shader outlineBufferShader;

        [Hint("showHints", "Define the Dialog Canvas use in this project.")]
        [BoxGroup("UI"), LabelWidth(180)]
        public GameObject dialogCanvas;

        [Hint("showHints", "Define the Inventory Canvas use in this project.")]
        [BoxGroup("UI"), LabelWidth(180)]
        public GameObject inventoryCanvas;
        
        [Hint("showHints", "Define the Floating Text use in this project.")]
        [BoxGroup("UI"), LabelWidth(180)]
        public GameObject floatText;
        
        [Hint("showHints", "Define the Speech Bubble use in this project.")]
        [BoxGroup("UI"), LabelWidth(180)]
        public GameObject speechBubble;

        [Hint("showHints", "Define the circle scan value of each step increment for melee attack.")]
        [BoxGroup("Battle"), LabelWidth(180)]
        public int meleeScanCircleAngleStep = 5;

        [Hint("showHints", "Define the circle scan value of each step increment for ranged attack.")]
        [BoxGroup("Battle"), LabelWidth(180)]
        public int rangedScanCircleAngleStep = 15;

        [Hint("showHints", "Define the circle scan value of each step increment for loot drop.")]
        [BoxGroup("Battle"), LabelWidth(180)]
        public int dropScanCircleAngleStep = 5;
        
        [Hint("showHints", "Define the gameObject layers that will be ignore by FogOfWarAgent when doing visibility detection.")]
        [BoxGroup("Battle"), LabelWidth(180)]
        public LayerMask ignoreFOWLayers;

        [Hint("showHints", "Define the Item Manager use in this project.")]
        [BoxGroup("Item"), LabelWidth(180)]
        public ItemManager itemManager;

        [Hint("showHints", "Define the loot drop inventory behaviour.")]
        [BoxGroup("Item"), LabelWidth(180)]
        public InventoryBehaviour dropInvBehaviour;

        [Hint("showHints", "Define the loot drop prefab.")]
        [BoxGroup("Item"), LabelWidth(180)]
        [LabelText("Drop Inv GO")]
        public GameObject dropInvGo;

#if UNITY_EDITOR
        [Hint("showHints", "Define delete the tween data when the tween node delete from graph.")]
        [BoxGroup("Tween Data"), LabelWidth(180)]
        public bool removeAtDeleteNode;

        [Hint("showHints", "Define the save folder for tween data. Create tween data will fail if the path is invalid.")]
        [BoxGroup("Tween Data"), LabelWidth(180)]
        [LabelText("Save Folder")]
        [FolderPath(RequireExistingPath = true)]
        public string tweenDataSaveFolder;

        [Hint("showHints", "Define the default save folder for item data when created new item at Item Explorer. Leave this empty if want to let the item data follow the item list folder.")]
        [BoxGroup("Item Data"), LabelWidth(180)]
        [LabelText("Save Folder")]
        [FolderPath(RequireExistingPath = true)]
        public string itemDataSaveFolder;

        public static RuntimeSettings GetSettings ()
        {
            var settings = FindSettings();
            return settings;
        }

        public static SerializedObject GetSerializedSettings ()
        {
            return new SerializedObject(GetSettings());
        }

        static RuntimeSettings FindSettings ()
        {
            var guids = AssetDatabase.FindAssets("t:RuntimeSettings");
            if (guids.Length > 1)
            {
                ReDebug.LogWarning("Framework Editor", $"Found multiple settings files, currently is using the first found settings file.", false);
            }

            switch (guids.Length)
            {
                case 0:
                    return null;
                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<RuntimeSettings>(path);
            }
        }

        public static List<T> LoadAssets<T> () where T : UnityEngine.Object
        {
            string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new List<T>();
            foreach (var assetId in assetIds)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                assets.Add(asset);
            }

            return assets;
        }

        public static List<string> GetAssetPaths<T> () where T : UnityEngine.Object
        {
            string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<string> paths = new List<string>();
            foreach (var assetId in assetIds)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                paths.Add(path);
            }

            return paths;
        }
#endif
    }
}