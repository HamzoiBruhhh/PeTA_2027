using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Reshape.ReFramework;
using Reshape.Unity;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class InventoryBehaviourNode : BehaviourNode
    {
        private const string SAVE_ALL_NAME = "names";

        public enum ExecutionType
        {
            None,
            Create = 11,
            Destroy = 21,
            DestroyAll = 22,
            Clear = 31,
            Have = 32,
            IsTag = 33,
            AddItem = 41,
            RemoveItem = 51,
            ArrangeItem = 61,
            DecayItem = 62,
            HaveItem = 63,
            Save = 101,
            SaveAll = 102,
            Load = 111,
            LoadAll = 112,
            Delete = 121,
            DeleteAll = 122,
            Show = 201,
            Prepare = 202,
            Hide = 211,
            IsShowing = 221,
            Refresh = 301,
            LinkItemTotalQuantity = 901,
            GetWeightLimit = 951,
            GetItemQuantity = 1001,
            GetItemsTotalQuantity = 1002,
            GetItemsTotalWeight = 1003,
            GetItemsTotalCount = 1004,
            GetItemName = 2001,
            GetItemDesc = 2002,
            GetItemData = 2003,
            GetItemCost = 2010,
            GetItemIcon = 2011,
            GetSlotCount = 3001,
            GetSlotQuantity = 3002,
            GetSlotItemId = 3003,
            GetSlotItemInstanceId = 3004,
            GetSlotPerRow = 3005,
            GetSlotDurable = 3006,
            ApplyItemEffect = 5001,
            ApplyAllItemEffect = 5002,
            CopyBehaviour = 7001
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(inventoryName)")]
        [InlineProperty]
        [ShowIf("ShowInventoryName")]
        [LabelText("@GetInvNameLabel()")]
        [Tooltip("@GetInvNameTip()")]
        [InlineButton("SwitchInvToItemCache", "▼", ShowIf = "ShowInvNameSwitchToItemCache")]
        private StringProperty inventoryName;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("ItemChoice")]
        [InlineButton("SwitchToItemCache", "▼")]
        [InlineButton("@ItemExplorer.OpenWindow()", "✚")]
        [ShowIf("ShowItemData")]
        private ItemData item;

        [SerializeField]
        [HideInInspector]
        private int itemType = 0;

        [SerializeReference]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("GetCacheChoice")]
        [InlineButton("SwitchFromCache", "▼")]
        [ShowIf("ShowItemCache")]
        [LabelText("@GetCacheLabel()")]
        private string itemCache;

        [SerializeField]
        [ShowIf("ShowItemVariable")]
        [OnInspectorGUI("UpdateItemVariable")]
        [HideLabel, InlineProperty]
        [InfoBox("@itemVariable.GetMismatchWarningMessage()", InfoMessageType.Error, "@itemVariable.IsShowMismatchWarning()")]
        [InlineButton("SwitchFromVariable", "▼", ShowIf = "ShowItemVarSwitch")]
        private SceneObjectProperty itemVariable = new SceneObjectProperty(SceneObject.ObjectType.ItemData);

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.AddItem || executionType == ExecutionType.RemoveItem")]
        [ValidateInput("ValidateMoreThan0", "Value must be more than 0!", InfoMessageType.Warning)]
        [LabelText("@GetSizeLabel()")]
        [OnInspectorGUI("@MarkPropertyDirty(size)")]
        [InlineProperty]
        private FloatProperty size = new FloatProperty(0);

        [SerializeField]
        [ShowIf("ShowNumber1")]
        [ValidateInput("ValidateNotNegative", "Value must not be negative!", InfoMessageType.Warning)]
        [OnInspectorGUI("UpdateNumber1")]
        [InlineProperty]
        [LabelText("@GetNumber1Label()")]
        private FloatProperty paramNumber1 = new FloatProperty(0);

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowParamVariable")]
        [LabelText("Variable")]
        private NumberVariable paramVariable;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowInvBehaviour")]
        private InventoryBehaviour invBehaviour;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowInvBehaviour2")]
        [LabelText("Copy To")]
        private InventoryBehaviour invBehaviour2;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.AddItem || executionType == ExecutionType.Load || executionType == ExecutionType.Create")]
        [LabelText("@GetAutoCreateLabel()")]
        private bool autoCreate;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(paramStr1)")]
        [InlineProperty]
        [ShowIf("ShowParamStr1")]
        [LabelText("@GetParamStr1Label()")]
        private StringProperty paramStr1;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(paramStr2)")]
        [InlineProperty]
        [ShowIf("ShowParamStr2")]
        [LabelText("Password")]
        private StringProperty paramStr2;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(paramStr3)")]
        [InlineProperty]
        [ShowIf("ShowParamStr3")]
        [LabelText("Save Tag")]
        private StringProperty paramStr3;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.GetItemName || executionType == ExecutionType.GetItemDesc || executionType == ExecutionType.GetSlotItemId || executionType == ExecutionType.GetSlotItemInstanceId")]
        [LabelText("Variable")]
        private WordVariable paramWord1;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.AddItem || executionType == ExecutionType.SaveAll || executionType == ExecutionType.GetItemQuantity || executionType == ExecutionType.HaveItem || executionType == ExecutionType.RemoveItem")]
        [LabelText("@GetParamBool1Label()")]
        private bool paramBool1;

        [OnInspectorGUI("MarkInvFlagsDirty")]
        [ShowIf("@executionType == ExecutionType.IsTag")]
        [LabelText("Tags")]
        public MultiTag invTags = new MultiTag("Tags", typeof(MultiTagInv));
        
        [SerializeField]
        [ShowIf("@executionType == ExecutionType.GetItemIcon")]
        [OnInspectorGUI("UpdateObjectVariable")]
        [HideLabel, InlineProperty]
        [InfoBox("@paramObject1.GetMismatchWarningMessage()", InfoMessageType.Error, "@paramObject1.IsShowMismatchWarning()")]
        private SceneObjectProperty paramObject1;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem or ExecutionType.GetItemQuantity or ExecutionType.LinkItemTotalQuantity or ExecutionType.GetItemName
                or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.HaveItem or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
            {
                if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                {
                    item = null;
                    CacheBehaviourNode cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                    if (cacheNode != null)
                    {
                        object cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                        if (cacheObj != null)
                            item = (ItemData) cacheObj;
                    }
                }
            }

            if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem or ExecutionType.HaveItem or ExecutionType.GetItemQuantity or ExecutionType.LinkItemTotalQuantity 
                or ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemIcon)
            {
                if (itemType == 4 && !itemVariable.IsEmpty && itemVariable.IsMatchType())
                {
                    item = null;
                    var so = (ScriptableObject) itemVariable;
                    if (so)
                        item = (ItemData) so;
                }
            }

            if (executionType == ExecutionType.Create)
            {
                if (invBehaviour == null || string.IsNullOrEmpty(invBehaviour.Name) || invBehaviour.Size <= 0 || invBehaviour.Stack <= 0)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    if (!InventoryManager.CreateInventory(invBehaviour.Name, invBehaviour.Size, invBehaviour.Stack, invBehaviour.Rows, invBehaviour.tags, invBehaviour.SizeLimit,
                            invBehaviour.CountLimit, invBehaviour.WeightLimit, autoCreate))
                    {
                        LogWarning($"{invBehaviour.Name} inventory not success create in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.Destroy)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var manager = GraphManager.instance.runtimeSettings.itemManager;
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null && manager)
                    {
                        for (var j = 0; j < inv.Count; j++)
                        {
                            var tempItem = inv.GetItem(j);
                            if (tempItem is {isSolid: true})
                            {
                                var subItemData = manager.GetItemData(tempItem.ItemId);
                                if (subItemData && subItemData.invBehaviour)
                                {
                                    var subInv = InventoryManager.GetInventory(tempItem.InstanceId);
                                    if (subInv != null)
                                        InventoryManager.DestroyInventory(tempItem.InstanceId);
                                }
                            }
                        }

                        InventoryManager.DestroyInventory(inventoryName);
                    }
                }
            }
            else if (executionType == ExecutionType.DestroyAll)
            {
                InventoryManager.DestroyAllInventory();
            }
            else if (executionType == ExecutionType.ApplyItemEffect)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    InventoryManager.ApplyInventoryAttackStatus(inventoryName);
                    InventoryManager.ApplyInventoryAttackSkill(inventoryName);
                }
            }
            else if (executionType == ExecutionType.ApplyAllItemEffect)
            {
                InventoryManager.ApplyAllInventoryAttackStatus();
                InventoryManager.ApplyAllInventoryAttackSkill();
            }
            else if (executionType == ExecutionType.Clear)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    InventoryData inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        inv.ClearItem();
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when doing clear item in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.Have)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var result = false;
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                        result = true;
                    UpdateConditionNode(execution, updateId, result);
                }
            }
            else if (executionType == ExecutionType.IsTag)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var result = false;
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                        result = inv.HaveTag(invTags, false);
                    UpdateConditionNode(execution, updateId, result);
                }
            }
            else if (executionType == ExecutionType.Save)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        var op = ReSave.Save(SavePack.GetSaveFileName(paramStr1 + inventoryName, paramStr3, SavePack.TYPE_INV), ReJson.ObjectToCustomJson(inv), paramStr2);
                        if (!op.success)
                            LogWarning($"{inventoryName} inventory not success save in {context.objectName}.");
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when doing save in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.SaveAll)
            {
                var invNames = InventoryManager.GetInventoriesName();
                if (invNames != null)
                {
                    SaveOperation op;
                    var saved = new List<string>();
                    for (var i = 0; i < invNames.Count; i++)
                    {
                        var name = invNames[i];
                        if (!string.IsNullOrEmpty(name))
                        {
                            var inv = InventoryManager.GetInventory(name);
                            if (inv != null)
                            {
                                if (!paramBool1)
                                {
                                    if (LootController.Contains(name))
                                        continue;
                                    if (InventoryManager.IsInventoryRuntimeUsageOnly(name))
                                        continue;
                                }

                                op = ReSave.Save(SavePack.GetSaveFileName(name, paramStr3, SavePack.TYPE_INV), ReJson.ObjectToCustomJson(inv), paramStr2);
                                if (!op.success)
                                    LogWarning($"{name} inventory not success save in {context.objectName}.");
                                else
                                    saved.Add(name);
                            }
                        }
                    }

                    if (saved.Count > 0)
                    {
                        var sb = new StringBuilder();
                        for (var i = 0; i < saved.Count; i++)
                            sb.Append($"{saved[i]},");
                        op = ReSave.Save(SavePack.GetSaveFileName(SAVE_ALL_NAME, paramStr3, SavePack.TYPE_INV), sb.ToString(), paramStr2);
                        if (!op.success)
                            LogWarning($"Dynamics inventory not success save in {context.objectName}.");
                    }
                    else
                    {
                        LogWarning($"No inventory have success save in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.LoadAll)
            {
                var op = ReSave.Load(SavePack.GetSaveFileName(SAVE_ALL_NAME, paramStr3, SavePack.TYPE_INV), paramStr2, false);
                if (op.success)
                {
                    var invNames = op.savedString.Split(ReExtensions.STRING_COMMA);
                    for (var i = 0; i < invNames.Length; i++)
                    {
                        if (string.IsNullOrEmpty(invNames[i]))
                            continue;
                        op = ReSave.Load(SavePack.GetSaveFileName(invNames[i], paramStr3, SavePack.TYPE_INV), paramStr2, false);
                        if (op.success)
                        {
                            var inv = (InventoryData) ReJson.ObjectFromCustomJson<InventoryData>(op.savedString);
                            InventoryManager.CreateInventory(inv);
                        }
                    }
                }
            }
            else if (executionType == ExecutionType.Load)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var result = true;
                    var op = ReSave.Load(SavePack.GetSaveFileName(paramStr1 + inventoryName, paramStr3, SavePack.TYPE_INV), paramStr2, false);
                    if (!op.success)
                    {
                        LogWarning($"{inventoryName} inventory not success load in {context.objectName}.");
                        result = false;
                    }
                    else
                    {
                        var inv = (InventoryData) ReJson.ObjectFromCustomJson<InventoryData>(op.savedString);
                        if (autoCreate)
                            InventoryManager.DestroyInventory(inventoryName);
                        if (!InventoryManager.CreateInventory(inv))
                        {
                            LogWarning($"{inventoryName} inventory not success create in {context.objectName}.");
                            result = false;
                        }
                    }

                    UpdateConditionNode(execution, updateId, result);
                }
            }
            else if (executionType == ExecutionType.DeleteAll)
            {
                var op = ReSave.Load(SavePack.GetSaveFileName(SAVE_ALL_NAME, paramStr3, SavePack.TYPE_INV), paramStr2, false);
                if (op.success)
                {
                    var invNames = op.savedString.Split(ReExtensions.STRING_COMMA);
                    op = ReSave.Delete(SavePack.GetSaveFileName(SAVE_ALL_NAME, paramStr3, SavePack.TYPE_INV), paramStr2);
                    if (op.success)
                    {
                        for (var i = 0; i < invNames.Length; i++)
                        {
                            if (string.IsNullOrEmpty(invNames[i]))
                                continue;
                            op = ReSave.Delete(SavePack.GetSaveFileName(invNames[i], paramStr3, SavePack.TYPE_INV), paramStr2);
                            if (!op.success)
                                LogWarning($"{invNames[i]} inventory not success delete in {context.objectName}.");
                        }
                    }
                }
            }
            else if (executionType == ExecutionType.Delete)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var op = ReSave.Delete(SavePack.GetSaveFileName(paramStr1 + inventoryName, paramStr3, SavePack.TYPE_INV), paramStr2);
                    if (!op.success)
                        LogWarning($"{inventoryName} inventory not success delete in {context.objectName}.");
                }
            }
            else if (executionType == ExecutionType.Refresh)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    InventoryManager.TriggerUpdate(inventoryName);
                }
            }
            else if (executionType == ExecutionType.Show)
            {
                if (string.IsNullOrEmpty(inventoryName) && string.IsNullOrEmpty(paramStr1))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    InventoryCanvas.ShowCanvas(inventoryName, paramStr1, true);
                }
            }
            else if (executionType == ExecutionType.Prepare)
            {
                InventoryCanvas.PrepareCanvas();
            }
            else if (executionType == ExecutionType.Hide)
            {
                InventoryCanvas.HideCanvasIfCursorClear();
            }
            else if (executionType == ExecutionType.IsShowing)
            {
                var result = InventoryCanvas.isShowingCanvas;
                UpdateConditionNode(execution, updateId, result);
            }
            else if (executionType == ExecutionType.AddItem)
            {
                if (string.IsNullOrEmpty(inventoryName) || item == null || size <= 0)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    InventoryData inv = InventoryManager.GetInventory(inventoryName);
                    if (inv == null)
                    {
                        if (!autoCreate)
                        {
                            LogWarning($"{inventoryName} inventory not found when doing add item in {context.objectName}.");
                        }
                        else
                        {
                            InventoryManager.CreateInventory(inventoryName, (int) InventoryBehaviour.DEFAULT_SIZE, (int) InventoryBehaviour.DEFAULT_STACK);
                            inv = InventoryManager.GetInventory(inventoryName);
                        }
                    }

                    if (inv != null)
                    {
                        var manager = GraphManager.instance.runtimeSettings.itemManager;
                        var itemData = manager.GetItemData(item.id);
                        if (itemData)
                        {
                            inv.AddItem(itemData, size, 0, paramBool1);
                            if (autoCreate)
                            {
                                for (var index = 0; index < inv.Size; ++index)
                                {
                                    if (inv.IsItem(item.id, index))
                                        InventoryManager.CreateSubInventory(itemData, inventoryName, index);
                                }
                            }
                        }
                    }
                }
            }
            else if (executionType == ExecutionType.RemoveItem)
            {
                if (string.IsNullOrEmpty(inventoryName) || !item || size <= 0)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    InventoryData inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        InventoryManager.RemoveInventoryItem(inventoryName, item.id, size, paramBool1);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when doing remove item in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.HaveItem)
            {
                if (string.IsNullOrEmpty(inventoryName) || item == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var result = InventoryManager.HaveInventoryItem(inventoryName, item.id, paramBool1);
                    UpdateConditionNode(execution, updateId, result);
                }
            }
            else if (executionType == ExecutionType.DecayItem)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramNumber1 <= 0)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        inv.DecayAllItem(paramNumber1);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when doing decay item in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.ArrangeItem)
            {
                if (string.IsNullOrEmpty(inventoryName))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        inv.ArrangeItem();
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when doing arrange item in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemQuantity)
            {
                if (string.IsNullOrEmpty(inventoryName) || item == null || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(InventoryManager.GetInventoryItemTotalQuantity(inventoryName, item.id, paramBool1));
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get item quantity in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemsTotalQuantity)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(inv.GetAllItemQuantity());
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get items total quantity in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemsTotalCount)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(inv.GetOccupiedSlotCount());
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get items total count in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemsTotalWeight)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(inv.GetAllItemWeight());
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get items total weight in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetWeightLimit)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(inv.WeightLimit);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get weight limit in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetSlotCount)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(inv.Count);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get slot count in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetSlotPerRow)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        paramVariable.SetValue(inv.PerRow);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get slot count in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemData)
            {
                if (!itemVariable.IsVariableValueType() || itemVariable.IsNull || !itemVariable.IsMatchType())
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    itemVariable.variableValue.Rebase();
                    if (itemType == 3)
                    {
                        var invName = inventoryName.ToString();
                        if (string.IsNullOrEmpty(invName))
                        {
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var manager = GraphManager.instance.runtimeSettings.itemManager;
                            var itemData = manager.GetItemData(invName);
                            if (itemData)
                                itemVariable.variableValue.SetValue(itemData);
                        }
                    }
                    else
                    {
                        if (!item)
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                            itemVariable.variableValue.SetValue(item);
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemName)
            {
                if (!paramWord1)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var invName = inventoryName.ToString();
                    paramWord1.Rebase();
                    if (itemType == 3)
                    {
                        if (string.IsNullOrEmpty(invName))
                        {
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var manager = GraphManager.instance.runtimeSettings.itemManager;
                            var itemData = manager.GetItemData(invName);
                            if (itemData)
                                paramWord1.SetValue(itemData.displayName);
                        }
                    }
                    else
                    {
                        if (!item)
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                            paramWord1.SetValue(item.displayName);
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemDesc)
            {
                if (!paramWord1)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var invName = inventoryName.ToString();
                    paramWord1.Rebase();
                    if (itemType == 3)
                    {
                        if (string.IsNullOrEmpty(invName))
                        {
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            var manager = GraphManager.instance.runtimeSettings.itemManager;
                            var itemData = manager.GetItemData(invName);
                            if (itemData)
                                paramWord1.SetValue(itemData.description);
                        }
                    }
                    else
                    {
                        if (!item)
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                            paramWord1.SetValue(item.description);
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemCost)
            {
                if (!paramVariable)
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                else
                {
                    paramVariable.Rebase();
                    if (itemType == 3)
                    {
                        if (string.IsNullOrEmpty(inventoryName))
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                        {
                            var manager = GraphManager.instance.runtimeSettings.itemManager;
                            var itemData = manager.GetItemData(inventoryName);
                            if (itemData)
                                paramVariable.SetValue(itemData.cost);
                        }
                    }
                    else
                    {
                        if (item == null)
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                            paramVariable.SetValue(item.cost);
                    }
                }
            }
            else if (executionType == ExecutionType.GetItemIcon)
            {
                if (!paramObject1.IsVariableValueType() || paramObject1.IsNull)
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                else
                {
                    paramObject1.variableValue.Rebase();
                    if (itemType == 3)
                    {
                        if (string.IsNullOrEmpty(inventoryName))
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                        {
                            var manager = GraphManager.instance.runtimeSettings.itemManager;
                            var itemData = manager.GetItemData(inventoryName);
                            if (itemData)
                                paramObject1.SetVariableValue(itemData.icon);
                        }
                    }
                    else
                    {
                        if (item == null)
                            LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                        else
                            paramObject1.SetVariableValue(item.icon);
                    }
                }
            }
            else if (executionType == ExecutionType.LinkItemTotalQuantity)
            {
                if (string.IsNullOrEmpty(inventoryName) || item == null || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        inv.OnQuantityChange -= OnInventoryChange;
                        inv.OnQuantityChange += OnInventoryChange;
                        OnInventoryChange(inv.Name, item.id);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when doing link item's quantity in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetSlotQuantity)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType is 0 or 1 && paramNumber1.IsNull())
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType == 2 && string.IsNullOrEmpty(itemCache))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    int slotIndex = paramNumber1;
                    if (itemType == 2)
                    {
                        var cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                        if (cacheNode != null)
                        {
                            object cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                            if (cacheObj != null)
                                slotIndex = (int) ((float) cacheObj);
                        }
                    }

                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        var slotItem = inv.GetItem(slotIndex);
                        paramVariable.SetValue(slotItem?.Quantity ?? 0);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get slot quantity in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetSlotDurable)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramVariable == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType is 0 or 1 && paramNumber1.IsNull())
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType == 2 && string.IsNullOrEmpty(itemCache))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    int slotIndex = paramNumber1;
                    if (itemType == 2)
                    {
                        var cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                        if (cacheNode != null)
                        {
                            object cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                            if (cacheObj != null)
                                slotIndex = (int) ((float) cacheObj);
                        }
                    }

                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        var slotItem = inv.GetItem(slotIndex);
                        paramVariable.SetValue(slotItem?.Decay ?? 0);
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when get slot durable in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetSlotItemId)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramWord1 == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType is 0 or 1 && paramNumber1.IsNull())
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType == 2 && string.IsNullOrEmpty(itemCache))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    int slotIndex = paramNumber1;
                    if (itemType == 2)
                    {
                        var cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                        if (cacheNode != null)
                        {
                            object cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                            if (cacheObj != null)
                                slotIndex = (int) ((float) cacheObj);
                        }
                    }

                    var invName = inventoryName.ToString();
                    paramWord1.Rebase();
                    var inv = InventoryManager.GetInventory(invName);
                    if (inv != null)
                    {
                        var slotItem = inv.GetItem(slotIndex);
                        paramWord1.SetValue(slotItem is {isSolid: true} ? slotItem.ItemId : string.Empty);
                    }
                    else
                    {
                        LogWarning($"{invName} inventory not found when get slot item id in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.GetSlotItemInstanceId)
            {
                if (string.IsNullOrEmpty(inventoryName) || paramWord1 == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType is 0 or 1 && paramNumber1.IsNull())
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else if (itemType == 2 && string.IsNullOrEmpty(itemCache))
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    int slotIndex = paramNumber1;
                    if (itemType == 2)
                    {
                        var cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                        if (cacheNode != null)
                        {
                            object cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                            if (cacheObj != null)
                                slotIndex = (int) ((float) cacheObj);
                        }
                    }

                    var invName = inventoryName.ToString();
                    paramWord1.Rebase();
                    var inv = InventoryManager.GetInventory(invName);
                    if (inv != null)
                    {
                        var slotItem = inv.GetItem(slotIndex);
                        paramWord1.SetValue(slotItem is {isSolid: true} ? slotItem.InstanceId : string.Empty);
                    }
                    else
                    {
                        LogWarning($"{invName} inventory not found when get slot item instance id in {context.objectName}.");
                    }
                }
            }
            else if (executionType == ExecutionType.CopyBehaviour)
            {
                if (invBehaviour == null || invBehaviour2 == null)
                {
                    LogWarning("Found an empty Inventory Behaviour node in " + context.objectName);
                }
                else
                {
                    invBehaviour.CopyTo(invBehaviour2);
                }
            }

            base.OnStart(execution, updateId);
        }

        private void OnInventoryChange (string invName, string itemId)
        {
            if (executionType == ExecutionType.LinkItemTotalQuantity)
            {
                if (string.IsNullOrEmpty(inventoryName) || !item || !paramVariable)
                {
                    LogWarning($"Found invalid item ({item.id}) quantity change event in {context.objectName}");
                }
                else
                {
                    if (itemId == item.id)
                    {
                        InventoryData inv = InventoryManager.GetInventory(inventoryName);
                        if (inv != null)
                        {
                            paramVariable.SetValue(inv.GetItemTotalQuantity(item.id));
                        }
                        else
                        {
                            LogWarning($"{inventoryName} inventory not found at quantity change event in {context.objectName}.");
                        }
                    }
                }
            }
        }

        protected override void OnReset ()
        {
            if (executionType == ExecutionType.LinkItemTotalQuantity)
            {
                if (!string.IsNullOrEmpty(inventoryName) && item && paramVariable)
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                        inv.OnQuantityChange -= OnInventoryChange;
                }
            }

            base.OnReset();
        }

        public override bool IsRequireInit ()
        {
            if (executionType == ExecutionType.LinkItemTotalQuantity)
                if (!string.IsNullOrEmpty(inventoryName) && item != null && paramVariable != null)
                    return enabled;
            return false;
        }

#if UNITY_EDITOR
        public string ItemCache => itemCache;

        public void SetItemCache (string value)
        {
            itemCache = value;
        }

        private void MarkInvFlagsDirty ()
        {
            if (invTags.dirty)
            {
                invTags.dirty = false;
                MarkDirty();
            }
        }

        private bool ShowInventoryName ()
        {
            switch (executionType)
            {
                case ExecutionType.None:
                case ExecutionType.Create:
                case ExecutionType.Hide:
                case ExecutionType.SaveAll:
                case ExecutionType.LoadAll:
                case ExecutionType.DestroyAll:
                case ExecutionType.DeleteAll:
                case ExecutionType.ApplyAllItemEffect:
                case ExecutionType.CopyBehaviour:
                case ExecutionType.IsShowing:
                case ExecutionType.Prepare:
                    return false;
                case ExecutionType.GetItemName:
                case ExecutionType.GetItemDesc:
                case ExecutionType.GetItemCost:
                case ExecutionType.GetItemData:
                case ExecutionType.GetItemIcon:
                    return itemType == 3;
                default:
                    return true;
            }
        }

        private bool ShowInvNameSwitchToItemCache ()
        {
            if (executionType is ExecutionType.GetItemCost or ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                return true;
            return false;
        }

        private string GetInvNameLabel ()
        {
            if (executionType is ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                return "Item ID";
            return "Inv Name";
        }

        private string GetInvNameTip ()
        {
            if (executionType is ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                return "Item ID";
            return "Inventory Name";
        }

        private bool ShowInvBehaviour ()
        {
            if (executionType is ExecutionType.Create or ExecutionType.CopyBehaviour)
                return true;
            return false;
        }

        private bool ShowInvBehaviour2 ()
        {
            if (executionType is ExecutionType.CopyBehaviour)
                return true;
            return false;
        }

        private bool ShowParamVariable ()
        {
            if (executionType is ExecutionType.GetItemQuantity or ExecutionType.LinkItemTotalQuantity or ExecutionType.GetSlotCount or ExecutionType.GetSlotQuantity
                or ExecutionType.GetSlotPerRow or ExecutionType.GetItemsTotalQuantity or ExecutionType.GetItemsTotalCount or ExecutionType.GetSlotDurable or ExecutionType.GetItemCost or ExecutionType.GetItemsTotalWeight
                or ExecutionType.GetWeightLimit)
                return true;
            return false;
        }

        private bool ShowParamStr1 ()
        {
            switch (executionType)
            {
                case ExecutionType.Save:
                case ExecutionType.Load:
                case ExecutionType.Delete:
                case ExecutionType.Show:
                    return true;
                default:
                    return false;
            }
        }

        private bool ShowParamStr2 ()
        {
            switch (executionType)
            {
                case ExecutionType.Save:
                case ExecutionType.Load:
                case ExecutionType.Delete:
                case ExecutionType.SaveAll:
                case ExecutionType.LoadAll:
                case ExecutionType.DeleteAll:
                    return true;
                default:
                    return false;
            }
        }

        private bool ShowParamStr3 ()
        {
            switch (executionType)
            {
                case ExecutionType.Save:
                case ExecutionType.Load:
                case ExecutionType.Delete:
                case ExecutionType.SaveAll:
                case ExecutionType.LoadAll:
                case ExecutionType.DeleteAll:
                    return true;
                default:
                    return false;
            }
        }

        private string GetParamStr1Label ()
        {
            switch (executionType)
            {
                case ExecutionType.Save:
                case ExecutionType.Load:
                case ExecutionType.Delete:
                    return "Save File";
                case ExecutionType.Show:
                    return "Trade Inv";
                default:
                    return string.Empty;
            }
        }

        private string GetParamBool1Label ()
        {
            switch (executionType)
            {
                case ExecutionType.AddItem:
                    return "New Slot";
                case ExecutionType.GetItemQuantity:
                case ExecutionType.RemoveItem:
                case ExecutionType.HaveItem:
                    return "Include Sub Inv";
                case ExecutionType.SaveAll:
                    return "Include Loot";
                default:
                    return string.Empty;
            }
        }

        private bool ShowItemData ()
        {
            switch (executionType)
            {
                case ExecutionType.AddItem:
                case ExecutionType.RemoveItem:
                case ExecutionType.HaveItem:
                case ExecutionType.GetItemQuantity:
                case ExecutionType.LinkItemTotalQuantity:
                case ExecutionType.GetItemName:
                case ExecutionType.GetItemDesc:
                case ExecutionType.GetItemCost:
                case ExecutionType.GetItemData:
                case ExecutionType.GetItemIcon:
                    if (itemType == 0)
                        return true;
                    else
                        return false;
                default:
                    return false;
            }
        }

        private bool ShowItemCache ()
        {
            switch (executionType)
            {
                case ExecutionType.AddItem:
                case ExecutionType.RemoveItem:
                case ExecutionType.HaveItem:
                case ExecutionType.GetItemQuantity:
                case ExecutionType.LinkItemTotalQuantity:
                case ExecutionType.GetItemName:
                case ExecutionType.GetItemDesc:
                case ExecutionType.GetItemCost:
                case ExecutionType.GetItemData:
                case ExecutionType.GetItemIcon:
                    return itemType == 1;
                case ExecutionType.GetSlotQuantity:
                case ExecutionType.GetSlotDurable:
                case ExecutionType.GetSlotItemId:
                case ExecutionType.GetSlotItemInstanceId:
                    return itemType == 2;
                default:
                    return false;
            }
        }

        private bool ShowItemVariable ()
        {
            switch (executionType)
            {
                case ExecutionType.GetItemData:
                    return true;
                case ExecutionType.AddItem:
                case ExecutionType.RemoveItem:
                case ExecutionType.HaveItem:
                case ExecutionType.GetItemQuantity:
                case ExecutionType.LinkItemTotalQuantity:
                case ExecutionType.GetItemName:
                case ExecutionType.GetItemDesc:
                case ExecutionType.GetItemCost:
                case ExecutionType.GetItemIcon:
                    return itemType == 4;
                default:
                    return false;
            }
        }

        private bool ShowItemVarSwitch ()
        {
            switch (executionType)
            {
                case ExecutionType.GetItemData:
                    return false;
                case ExecutionType.AddItem:
                case ExecutionType.RemoveItem:
                case ExecutionType.HaveItem:
                case ExecutionType.GetItemQuantity:
                case ExecutionType.LinkItemTotalQuantity:
                case ExecutionType.GetItemName:
                case ExecutionType.GetItemDesc:
                case ExecutionType.GetItemCost:
                case ExecutionType.GetItemIcon:
                    return true;
                default:
                    return false;
            }
        }

        private bool ShowNumber1 ()
        {
            if (executionType == ExecutionType.DecayItem)
                return true;
            if (executionType is ExecutionType.GetSlotQuantity or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId or ExecutionType.GetSlotDurable)
                if (itemType is 0 or 1)
                    return true;
            return false;
        }

        private void SwitchInvToItemCache ()
        {
            itemType = 0;
            MarkDirty();
        }

        private void SwitchToItemCache ()
        {
            itemType = 1;
            MarkDirty();
        }

        private void SwitchFromCache ()
        {
            if (executionType is ExecutionType.GetItemData)
                itemType = 3;
            else if (executionType is ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                itemType = 4;
            else if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem or ExecutionType.HaveItem or ExecutionType.GetItemQuantity or ExecutionType.LinkItemTotalQuantity)
                itemType = 4;
            else
                itemType = 0;
            MarkDirty();
        }

        private void SwitchFromVariable ()
        {
            itemType = executionType is ExecutionType.GetItemCost or ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemData or ExecutionType.GetItemIcon ? 3 : 0;
            MarkDirty();
        }

        private void OnChangeType ()
        {
            if (executionType is not (ExecutionType.GetSlotQuantity or ExecutionType.GetSlotDurable or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId))
                if (itemType == 2)
                    itemType = 0;
            if (executionType is not (ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon))
                if (itemType == 3)
                    itemType = 0;
            if (executionType is not (ExecutionType.AddItem or ExecutionType.RemoveItem or ExecutionType.HaveItem or ExecutionType.GetItemQuantity or ExecutionType.LinkItemTotalQuantity or ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemIcon))
                if (itemType == 4)
                    itemType = 0;
            if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem or ExecutionType.HaveItem or ExecutionType.GetItemQuantity or ExecutionType.LinkItemTotalQuantity or ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                itemVariable.AllowVariableOnly();
            else
                itemVariable.AllowAll();
            if (executionType is ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                inventoryName.AllowVariableOnly();
            else
                inventoryName.AllowAll();
            if (executionType is ExecutionType.GetItemIcon)
                paramObject1 = new SceneObjectProperty(SceneObject.ObjectType.Sprite, "Variable");
            itemVariable.ChangeDisplayName(executionType is ExecutionType.GetItemData ? "Variable" : "Item");
            MarkRepaint();
            MarkDirty();
        }

        private void UpdateItemVariable ()
        {
            if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem or ExecutionType.HaveItem)
                itemVariable.AllowVariableOnly();
            MarkPropertyDirty(itemVariable);
        }
        
        private void UpdateObjectVariable ()
        {
            if (executionType is ExecutionType.GetItemIcon)
                paramObject1.AllowVariableOnly();
            MarkPropertyDirty(paramObject1);
        }

        private void UpdateNumber1 ()
        {
            if (executionType is ExecutionType.GetSlotQuantity or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId or ExecutionType.GetSlotDurable)
            {
                if (itemType == 0 && paramNumber1.type == 1)
                    itemType = 1;
                if (itemType == 1 && paramNumber1.type == 0)
                    itemType = 2;
                MarkDirty();
            }

            MarkPropertyDirty(paramNumber1);
        }

        private IEnumerable ItemChoice ()
        {
            return ItemData.GetListDropdown();
        }

        private IEnumerable GetCacheChoice ()
        {
            var itemListDropdown = new ValueDropdownList<string>();
            var graph = GetGraph();
            if (graph != null)
            {
                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    if (graph.nodes[i] is CacheBehaviourNode)
                    {
                        var cacheNode = (CacheBehaviourNode) graph.nodes[i];
                        if (executionType is ExecutionType.GetSlotQuantity or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId or ExecutionType.GetSlotDurable)
                        {
                            var cache = cacheNode.GetNumberCacheChoice();
                            if (cache != null)
                            {
                                itemListDropdown.Add(cacheNode.GetCacheName(), cacheNode.guid);
                            }
                        }
                        else
                        {
                            var cache = cacheNode.GetItemCacheChoice();
                            if (cache != null)
                                itemListDropdown.Add(cacheNode.GetCacheName(), cacheNode.guid);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(itemCache))
            {
                bool found = false;
                for (int i = 0; i < itemListDropdown.Count; i++)
                {
                    if (itemListDropdown[i].Value == itemCache)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    itemCache = string.Empty;
                    MarkDirty();
                }
            }

            return itemListDropdown;
        }

        private string GetItemCacheName (string cacheNodeId)
        {
            var graph = GetGraph();
            if (graph is {nodes: { }})
            {
                for (var i = 0; i < graph.nodes.Count; i++)
                {
                    if (graph.nodes[i] is CacheBehaviourNode)
                    {
                        CacheBehaviourNode cacheNode = (CacheBehaviourNode) graph.nodes[i];
                        if (cacheNode.guid == cacheNodeId)
                        {
                            return cacheNode.GetCacheSavedName();
                        }
                    }
                }
            }

            return string.Empty;
        }

        public bool GetCacheItemType ()
        {
            if (itemType == 1)
                return true;
            return false;
        }

        private string GetSizeLabel ()
        {
            if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem)
                return "Amount";
            return string.Empty;
        }

        private string GetCacheLabel ()
        {
            if (executionType is ExecutionType.GetSlotQuantity or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId or ExecutionType.GetSlotDurable)
                return "Slot Index";
            return "Item Cache";
        }

        private string GetNumber1Label ()
        {
            if (executionType is ExecutionType.GetSlotQuantity or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId or ExecutionType.GetSlotDurable)
                return "Slot Index";
            if (executionType == ExecutionType.DecayItem)
                return "Decay Value";
            return string.Empty;
        }

        private string GetAutoCreateLabel ()
        {
            if (executionType == ExecutionType.Load)
                return "Overwrite";
            if (executionType is ExecutionType.AddItem)
                return "Auto Create";
            if (executionType is ExecutionType.Create)
                return "Runtime Usage";
            return string.Empty;
        }

        private bool ValidateMoreThan0 (int value)
        {
            return size.IsValidPositive();
        }

        private bool ValidateNotNegative (int value)
        {
            if (executionType is ExecutionType.DecayItem)
                return value >= 0f;
            return true;
        }

        public ValueDropdownList<ExecutionType> TypeChoice ()
        {
            var typeListDropdown = new ValueDropdownList<ExecutionType>();
            var graph = GetGraph();
            if (graph != null)
            {
                if (graph.isLootPack)
                {
                    typeListDropdown.Add("Add Item", ExecutionType.AddItem);
                    return typeListDropdown;
                }
                else if (graph.isStatGraph)
                {
                    typeListDropdown.Add("Get Slot Item Instance ID", ExecutionType.GetSlotItemInstanceId);
                    typeListDropdown.Add("Get Weight Limit", ExecutionType.GetWeightLimit);
                    typeListDropdown.Add("Get Items Weight", ExecutionType.GetItemsTotalWeight);
                    return typeListDropdown;
                }
            }

            typeListDropdown.Add("Add Item", ExecutionType.AddItem);
            typeListDropdown.Add("Remove Item", ExecutionType.RemoveItem);
            typeListDropdown.Add("Decay Item", ExecutionType.DecayItem);
            typeListDropdown.Add("Arrange Item", ExecutionType.ArrangeItem);
            typeListDropdown.Add("Have Item", ExecutionType.HaveItem);
            typeListDropdown.Add("Link Item Quantity", ExecutionType.LinkItemTotalQuantity);
            typeListDropdown.Add("Create", ExecutionType.Create);
            typeListDropdown.Add("Destroy", ExecutionType.Destroy);
            typeListDropdown.Add("Destroy All", ExecutionType.DestroyAll);
            typeListDropdown.Add("Have", ExecutionType.Have);
            typeListDropdown.Add("Is Tag", ExecutionType.IsTag);
            typeListDropdown.Add("Clear", ExecutionType.Clear);
            typeListDropdown.Add("Refresh", ExecutionType.Refresh);
            typeListDropdown.Add("Save", ExecutionType.Save);
            typeListDropdown.Add("SaveAll", ExecutionType.SaveAll);
            typeListDropdown.Add("Load", ExecutionType.Load);
            typeListDropdown.Add("Load All", ExecutionType.LoadAll);
            typeListDropdown.Add("Delete", ExecutionType.Delete);
            typeListDropdown.Add("Delete All", ExecutionType.DeleteAll);
            typeListDropdown.Add("Prepare", ExecutionType.Prepare);
            typeListDropdown.Add("Show", ExecutionType.Show);
            typeListDropdown.Add("Hide", ExecutionType.Hide);
            typeListDropdown.Add("Is Showing", ExecutionType.IsShowing);
            typeListDropdown.Add("Get Weight Limit", ExecutionType.GetWeightLimit);
            typeListDropdown.Add("Get Items Quantity", ExecutionType.GetItemsTotalQuantity);
            typeListDropdown.Add("Get Items Count", ExecutionType.GetItemsTotalCount);
            typeListDropdown.Add("Get Items Weight", ExecutionType.GetItemsTotalWeight);
            typeListDropdown.Add("Get Item", ExecutionType.GetItemData);
            typeListDropdown.Add("Get Item Name", ExecutionType.GetItemName);
            typeListDropdown.Add("Get Item Description", ExecutionType.GetItemDesc);
            typeListDropdown.Add("Get Item Cost", ExecutionType.GetItemCost);
            typeListDropdown.Add("Get Item Icon", ExecutionType.GetItemIcon);
            typeListDropdown.Add("Get Item Quantity", ExecutionType.GetItemQuantity);
            typeListDropdown.Add("Get Slot Count", ExecutionType.GetSlotCount);
            typeListDropdown.Add("Get Slot Per Row", ExecutionType.GetSlotPerRow);
            typeListDropdown.Add("Get Slot Quantity", ExecutionType.GetSlotQuantity);
            typeListDropdown.Add("Get Slot Durable", ExecutionType.GetSlotDurable);
            typeListDropdown.Add("Get Slot Item ID", ExecutionType.GetSlotItemId);
            typeListDropdown.Add("Get Slot Item Instance ID", ExecutionType.GetSlotItemInstanceId);
            typeListDropdown.Add("Apply Item Effect", ExecutionType.ApplyItemEffect);
            typeListDropdown.Add("Apply All Item Effect", ExecutionType.ApplyAllItemEffect);
            typeListDropdown.Add("Copy Behaviour", ExecutionType.CopyBehaviour);
            return typeListDropdown;
        }

        public static string displayName = "Inventory Behaviour Node";
        public static string nodeName = "Inventory";

        public override bool IsPortReachable (GraphNode node)
        {
            if (node is YesConditionNode or NoConditionNode)
            {
                if (executionType != ExecutionType.Load && executionType != ExecutionType.Have && executionType != ExecutionType.IsTag && executionType != ExecutionType.HaveItem &&
                    executionType != ExecutionType.IsShowing)
                    return false;
            }
            else if (node is ChoiceConditionNode)
            {
                return false;
            }

            return true;
        }

        public bool AcceptConditionNode ()
        {
            if (executionType is ExecutionType.Load or ExecutionType.Have or ExecutionType.IsTag or ExecutionType.HaveItem or ExecutionType.IsShowing)
                return true;
            return false;
        }

        public override string GetNodeInspectorTitle ()
        {
            return displayName;
        }

        public override string GetNodeViewTitle ()
        {
            return nodeName;
        }

        public override string GetNodeIdentityName ()
        {
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Gameplay/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType == ExecutionType.Create)
            {
                if (invBehaviour != null)
                {
                    if (!string.IsNullOrEmpty(invBehaviour.Name) && invBehaviour.Size > 0 && invBehaviour.Stack > 0)
                    {
                        if (invBehaviour.Rows > 0)
                            return $"Create {invBehaviour.Name} with size {invBehaviour.Size.ToString()}, stack {invBehaviour.Stack.ToString()} and {invBehaviour.Rows.ToString()} per row";
                        return $"Create {invBehaviour.Name} with size {invBehaviour.Size.ToString()} and stack {invBehaviour.Stack.ToString()}";
                    }
                }
            }
            else if (executionType == ExecutionType.Destroy)
            {
                if (inventoryName.IsAssigned())
                    return $"Destroy {inventoryName}";
            }
            else if (executionType == ExecutionType.DestroyAll)
            {
                return $"Destroy all inventory";
            }
            else if (executionType == ExecutionType.Delete)
            {
                if (inventoryName.IsAssigned())
                    return $"Delete {inventoryName}";
            }
            else if (executionType == ExecutionType.DeleteAll)
            {
                return $"Delete all inventory";
            }
            else if (executionType == ExecutionType.Clear)
            {
                if (inventoryName.IsAssigned())
                    return $"Clear {inventoryName}";
            }
            else if (executionType == ExecutionType.Have)
            {
                if (inventoryName.IsAssigned())
                    return $"Have {inventoryName} ?";
            }
            else if (executionType == ExecutionType.IsTag)
            {
                if (inventoryName.IsAssigned())
                    return $"{inventoryName} have {invTags.GetSelectedString()} tag?";
            }
            else if (executionType == ExecutionType.Save)
            {
                if (inventoryName.IsAssigned())
                    return $"Save {inventoryName}";
            }
            else if (executionType == ExecutionType.SaveAll)
            {
                return $"Save all inventory";
            }
            else if (executionType == ExecutionType.LoadAll)
            {
                return $"Load all saved inventory";
            }
            else if (executionType == ExecutionType.Load)
            {
                if (inventoryName.IsAssigned())
                    return $"Load {inventoryName}";
            }
            else if (executionType == ExecutionType.Prepare)
            {
                return $"Prepare UI";
            }
            else if (executionType == ExecutionType.Show)
            {
                if (inventoryName.IsAssigned() || paramStr1.IsAssigned())
                {
                    var invName = string.Empty;
                    if (paramStr1.IsAssigned())
                    {
                        invName = inventoryName;
                        if (string.IsNullOrEmpty(invName))
                            invName = inventoryName.GetDisplayName();
                    }

                    var paramName = string.Empty;
                    if (paramStr1.IsAssigned())
                    {
                        paramName = paramStr1;
                        if (string.IsNullOrEmpty(paramName))
                            paramName = paramStr1.GetDisplayName();
                    }

                    if (!string.IsNullOrEmpty(invName) && !string.IsNullOrEmpty(paramName))
                        return $"Show {invName} and {paramName}";
                    return !string.IsNullOrEmpty(invName) ? $"Show {invName}" : $"Show {paramName}";
                }
            }
            else if (executionType == ExecutionType.Refresh)
            {
                if (inventoryName.IsAssigned())
                {
                    return string.IsNullOrEmpty(inventoryName) ? $"Refresh {inventoryName.GetDisplayName()}" : $"Refresh {inventoryName}";
                }
            }
            else if (executionType == ExecutionType.IsShowing)
            {
                return $"Is showing inventory canvas ?";
            }
            else if (executionType == ExecutionType.Hide)
            {
                return $"Hide {inventoryName.GetDisplayName()}";
            }
            else if (executionType == ExecutionType.AddItem)
            {
                if (inventoryName.IsAssigned())
                {
                    if (size.IsVariable())
                    {
                        if (!size.IsNull())
                        {
                            if (itemType == 0 && item != null)
                                return $"{size.GetVariableName()} of {item.displayName} add to {inventoryName.GetDisplayName()}";
                            if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                                return $"{size.GetVariableName()} of {GetItemCacheName(itemCache)} add to {inventoryName.GetDisplayName()}";
                            if (itemType == 4 && !itemVariable.IsNull)
                                return $"{size.GetVariableName()} of {itemVariable.objectName} add to {inventoryName.GetDisplayName()}";
                        }
                    }
                    else if (size > 0)
                    {
                        if (itemType == 0 && item != null)
                            return $"Add {size} {item.displayName} to {inventoryName.GetDisplayName()}";
                        if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                            return $"Add {size} {GetItemCacheName(itemCache)} to {inventoryName.GetDisplayName()}";
                        if (itemType == 4 && !itemVariable.IsNull)
                            return $"Add {size} {itemVariable.objectName} to {inventoryName.GetDisplayName()}";
                    }
                }
            }
            else if (executionType == ExecutionType.RemoveItem)
            {
                if (inventoryName.IsAssigned() && size.IsValidPositive())
                {
                    if (itemType == 0 && item != null)
                        return $"Remove {size} {item.displayName} from {inventoryName.GetDisplayName()}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Remove {size} {GetItemCacheName(itemCache)} from {inventoryName.GetDisplayName()}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Remove {size} {itemVariable.objectName} from {inventoryName.GetDisplayName()}";
                }
            }
            else if (executionType == ExecutionType.HaveItem)
            {
                if (inventoryName.IsAssigned())
                {
                    if (itemType == 0 && item != null)
                        return $"Have {item.displayName} in {inventoryName.GetDisplayName()} ?";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Have {GetItemCacheName(itemCache)} in {inventoryName.GetDisplayName()} ?";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Have {itemVariable.objectName} in {inventoryName.GetDisplayName()} ?";
                }
            }
            else if (executionType == ExecutionType.DecayItem)
            {
                if (((inventoryName.IsVariable() && !inventoryName.IsNull()) || (!inventoryName.IsVariable() && inventoryName.IsAssigned())))
                {
                    if (!paramNumber1.IsVariable() && paramNumber1 > 0)
                    {
                        if (inventoryName.IsVariable() && string.IsNullOrEmpty(inventoryName))
                            return $"Decay {paramNumber1} to all items in {inventoryName.GetVariableName()}";
                        return $"Decay {paramNumber1} to all items in {inventoryName}";
                    }
                    else if (paramNumber1.IsVariable() && !paramNumber1.IsNull())
                    {
                        if (inventoryName.IsVariable() && string.IsNullOrEmpty(inventoryName))
                            return $"Decay {paramNumber1.GetDisplayName()} to all items in {inventoryName.GetVariableName()}";
                        return $"Decay {paramNumber1.GetDisplayName()} to all items in {inventoryName}";
                    }
                }
            }
            else if (executionType == ExecutionType.ArrangeItem)
            {
                if (inventoryName.IsAssigned())
                    return $"Arrange items for {inventoryName}";
            }
            else if (executionType == ExecutionType.GetItemQuantity)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                {
                    if (itemType == 0 && item != null)
                        return $"Get {item.displayName}'s quantity into {paramVariable.name}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Get {GetItemCacheName(itemCache)}'s quantity into {paramVariable.name}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Get {itemVariable.objectName}'s quantity into {paramVariable.name}";
                }
            }
            else if (executionType == ExecutionType.GetItemsTotalQuantity)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                    return $"Set total item quantity into {paramVariable.name}";
            }
            else if (executionType == ExecutionType.GetItemsTotalCount)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                    return $"Set total item count into {paramVariable.name}";
            }
            else if (executionType == ExecutionType.GetItemsTotalWeight)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                    return $"Set total item weight into {paramVariable.name}";
            }
            else if (executionType == ExecutionType.GetWeightLimit)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                    return $"Set inv weight limit into {paramVariable.name}";
            }
            else if (executionType == ExecutionType.GetSlotQuantity)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                {
                    if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        return $"Set slot {paramNumber1} quantity into {paramVariable.name}";
                    if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        return $"Set slot {GetItemCacheName(itemCache)} quantity into {paramVariable.name}";
                }
            }
            else if (executionType == ExecutionType.GetSlotDurable)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                {
                    if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        return $"Set slot {paramNumber1} durable into {paramVariable.name}";
                    if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        return $"Set slot {GetItemCacheName(itemCache)} durable into {paramVariable.name}";
                }
            }
            else if (executionType == ExecutionType.GetSlotItemId)
            {
                if (inventoryName.IsAssigned() && paramWord1 != null)
                {
                    if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        return $"Set slot {paramNumber1} id into {paramWord1.name}";
                    if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        return $"Set slot {GetItemCacheName(itemCache)} id into {paramWord1.name}";
                }
            }
            else if (executionType == ExecutionType.GetSlotItemInstanceId)
            {
                if (inventoryName.IsAssigned() && paramWord1 != null)
                {
                    if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        return $"Set slot {paramNumber1} item's instance id into {paramWord1.name}";
                    if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        return $"Set slot {GetItemCacheName(itemCache)} item's instance id into {paramWord1.name}";
                }
            }
            else if (executionType == ExecutionType.GetSlotCount)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                {
                    if (string.IsNullOrEmpty(inventoryName))
                        return $"Set {inventoryName.GetDisplayName()}'s slot count into {paramVariable.name}";
                    else
                        return $"Set {inventoryName}'s slot count into {paramVariable.name}";
                }
            }
            else if (executionType == ExecutionType.GetSlotPerRow)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                    return $"Set {inventoryName}'s slot per row into {paramVariable.name}";
            }
            else if (executionType == ExecutionType.LinkItemTotalQuantity)
            {
                if (inventoryName.IsAssigned() && paramVariable != null)
                {
                    if (itemType == 0 && item != null)
                        return $"Link {item.displayName}'s quantity to {paramVariable.name}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Link {GetItemCacheName(itemCache)}'s quantity to {paramVariable.name}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Link {itemVariable.objectName}'s quantity to {paramVariable.name}";
                }
            }
            else if (executionType == ExecutionType.GetItemName)
            {
                if (paramWord1 != null)
                {
                    if (itemType == 0 && item != null)
                        return $"Set {item.displayName}'s name to {paramWord1.name}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Set {GetItemCacheName(itemCache)}'s name to {paramWord1.name}";
                    if (itemType == 3 && inventoryName.IsVariable() && !inventoryName.IsNull())
                        return $"Set {inventoryName.GetVariableName()}'s name to {paramWord1.name}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Set {itemVariable.objectName}'s name to {paramWord1.name}";
                }
            }
            else if (executionType == ExecutionType.GetItemDesc)
            {
                if (paramWord1 != null)
                {
                    if (itemType == 0 && item != null)
                        return $"Set {item.displayName}'s description to {paramWord1.name}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Set {GetItemCacheName(itemCache)}'s description to {paramWord1.name}";
                    if (itemType == 3 && inventoryName.IsVariable() && !inventoryName.IsNull())
                        return $"Set {inventoryName.GetVariableName()}'s description to {paramWord1.name}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Set {itemVariable.objectName}'s description to {paramWord1.name}";
                }
            }
            else if (executionType == ExecutionType.GetItemCost)
            {
                if (paramVariable != null)
                {
                    if (itemType == 0 && item != null)
                        return $"Set {item.displayName}'s cost to {paramVariable.name}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Set {GetItemCacheName(itemCache)}'s cost to {paramVariable.name}";
                    if (itemType == 3 && inventoryName.IsVariable() && !inventoryName.IsNull())
                        return $"Set {inventoryName.GetVariableName()}'s cost to {paramVariable.name}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Set {itemVariable.objectName}'s cost to {paramVariable.name}";
                }
            }
            else if (executionType == ExecutionType.GetItemIcon)
            {
                if (!paramObject1.IsNull)
                {
                    if (itemType == 0 && item != null)
                        return $"Set {item.displayName}'s icon to {paramObject1.objectName}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Set {GetItemCacheName(itemCache)}'s icon to {paramObject1.objectName}";
                    if (itemType == 3 && inventoryName.IsVariable() && !inventoryName.IsNull())
                        return $"Set {inventoryName.GetVariableName()}'s icon to {paramObject1.objectName}";
                    if (itemType == 4 && !itemVariable.IsNull)
                        return $"Set {itemVariable.objectName}'s icon to {paramObject1.objectName}";
                }
            }
            else if (executionType == ExecutionType.GetItemData)
            {
                if (!itemVariable.IsNull)
                {
                    if (itemType == 0 && item != null)
                        return $"Set {item.displayName} into {itemVariable.objectName}";
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return $"Set {GetItemCacheName(itemCache)} into {itemVariable.objectName}";
                    if (itemType == 3 && inventoryName.IsVariable() && !inventoryName.IsNull())
                        return $"Set {inventoryName.GetVariableName()} into {itemVariable.objectName}";
                }
            }
            else if (executionType == ExecutionType.ApplyItemEffect)
            {
                if (inventoryName.IsAssigned())
                {
                    return $"Apply all item's effect of {inventoryName}";
                }
            }
            else if (executionType == ExecutionType.ApplyAllItemEffect)
            {
                return $"Apply item's effect of all inventory";
            }
            else if (executionType == ExecutionType.CopyBehaviour)
            {
                if (invBehaviour != null && invBehaviour2 != null)
                {
                    return $"Copy {invBehaviour.name}'s behaviour to {invBehaviour2.name}";
                }
            }

            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType is ExecutionType.Create)
                tip += "This is use to create an inventory into runtime system.\n\n";
            if (executionType is ExecutionType.Destroy)
                tip += "This is use to remove an inventory from runtime system.\n\n";
            if (executionType is ExecutionType.Clear)
                tip += "This is use to clean up an inventory by removing all items in it.\n\n";
            if (executionType is ExecutionType.Save or ExecutionType.Load or ExecutionType.Delete)
                tip += "This is use to save/load/delete an inventory at file system.\n\n";
            if (executionType is ExecutionType.Have)
                tip += "This is use to check an inventory is existed.\n\n";
            if (executionType is ExecutionType.IsTag)
                tip += "This is use to check an inventory have selected tags.\n\n";
            else if (executionType is ExecutionType.SaveAll or ExecutionType.LoadAll or ExecutionType.DeleteAll)
                tip += "This is use to save/load/delete all existing inventory at file system.\n\n";
            else if (executionType is ExecutionType.DestroyAll)
                tip += "This is use to remove all existing inventory from runtime system.\n\n";
            else if (executionType is ExecutionType.AddItem or ExecutionType.RemoveItem)
                tip += "This is use to add/remove item for an inventory.\n\n";
            else if (executionType is ExecutionType.ArrangeItem)
                tip += "This is use to arrange all items positioning in an inventory. Require further enhance to support various style of arrangement.\n\n";
            else if (executionType is ExecutionType.HaveItem)
                tip += "This is use to check an item is existed in the specific inventory.\n\n";
            else if (executionType is ExecutionType.Hide or ExecutionType.Refresh or ExecutionType.IsShowing or ExecutionType.Prepare)
                tip += "This will provide several controls to inventory UI.\n\n";
            else if (executionType is ExecutionType.Show)
                tip +=
                    "This will show inventory UI base on Inv Name and Trade Inv.\n\nInv Name : the inventory name of the unit\nTrade Inv : the inventory name of another unit who want to do trade. This is optional.\n\n";
            else if (executionType is ExecutionType.GetSlotQuantity or ExecutionType.GetSlotItemId or ExecutionType.GetSlotItemInstanceId or ExecutionType.GetSlotPerRow
                     or ExecutionType.GetSlotDurable)
                tip += "This will retrieve the slot contents in specific inventory.\n\n";
            else if (executionType is ExecutionType.GetSlotCount)
                tip += "This will retrieve the total slot count of specific inventory.\n\n";
            else if (executionType is ExecutionType.GetItemName or ExecutionType.GetItemDesc or ExecutionType.GetItemCost or ExecutionType.GetItemData or ExecutionType.GetItemIcon)
                tip += "This will retrieve the item information in specific inventory.\n\n";
            else if (executionType is ExecutionType.GetItemQuantity)
                tip += "This will retrieve the quantity of an item in specific inventory.\n\n";
            else if (executionType == ExecutionType.LinkItemTotalQuantity)
                tip += "This will bind a variable to specific item's quantity in specific inventory.\n\n";
            else if (executionType == ExecutionType.GetItemsTotalQuantity)
                tip += "This will retrieve total item quantity of all items in specific inventory.\n\n";
            else if (executionType == ExecutionType.GetItemsTotalCount)
                tip += "This will retrieve total item count of all items in specific inventory.\n\n";
            else if (executionType == ExecutionType.GetItemsTotalWeight)
                tip += "This will retrieve total item weight of all items in specific inventory.\n\n";
            else if (executionType == ExecutionType.GetWeightLimit)
                tip += "This will retrieve weight limit of specific inventory.\n\n";
            else if (executionType == ExecutionType.DecayItem)
                tip += "This will decay all item's durability in specific inventory.\n\n";
            else if (executionType is ExecutionType.ApplyItemEffect or ExecutionType.ApplyAllItemEffect)
                tip += "This will apply all item's effect of specific/all inventory to character who owned the inventory.\n\n";
            else if (executionType is ExecutionType.CopyBehaviour)
                tip += "This will copy all settings of an inventory behaviour to another inventory behaviour.\n\n";
            else
                tip += "This will execute all Inventory related behaviour.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}