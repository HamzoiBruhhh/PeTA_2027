using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReGraph
{
    [Serializable]
    public class InventoryTriggerNode : TriggerNode
    {
        public enum InventoryType
        {
            Single,
            Character,
        }

        [ValueDropdown("TriggerTypeChoice")]
        [OnValueChanged("OnChangeType")]
        public Type triggerType;

        [SerializeField]
        [ValueDropdown("InvTypeChoice")]
        [OnValueChanged("OnChangeInvType")]
        [ShowIf("@triggerType == Type.InventorySlotChange")]
        private InventoryType inventoryType;

        [SerializeField]
        [ShowIf("@triggerType == Type.InventorySlotChange && inventoryType == InventoryType.Character")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(character)")]
        [InfoBox("@character.GetMismatchWarningMessage()", InfoMessageType.Error, "@character.IsShowMismatchWarning()")]
        private SceneObjectProperty character = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator);

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(inventoryName)")]
        [InlineProperty]
        [LabelText("@InventoryNameLabel()")]
        [Tooltip("Inventory Name")]
        [HideIf("@triggerType == Type.InventoryItemChange")]
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
        private int itemType;

        [SerializeReference]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("GetCacheChoice")]
        [InlineButton("SwitchToItemData", "▼")]
        [ShowIf("ShowCache")]
        [LabelText("@GetCacheLabel()")]
        private string itemCache;

        [SerializeField]
        [ShowIf("ShowNumber1")]
        [OnInspectorGUI("UpdateNumber1")]
        [InlineProperty]
        [LabelText("Slot Index")]
        private FloatProperty paramNumber1;

        protected override State OnUpdate (GraphExecution execution, int updateId)
        {
            State state = execution.variables.GetState(guid, State.Running);
            if (state == State.Running)
            {
                if ((execution.type is Type.InventoryQuantityChange or Type.InventorySlotChange or Type.InventoryDecayChange or Type.InventoryUnLockRequest or Type.InventoryItemChange
                     && execution.type == triggerType) || execution.type == Type.All)
                {
                    if (execution.parameters.actionName != null && execution.parameters.actionName.Equals(TriggerId))
                    {
                        execution.variables.SetState(guid, State.Success);
                        state = State.Success;
                    }
                }

                if (state != State.Success)
                {
                    execution.variables.SetState(guid, State.Failure);
                    state = State.Failure;
                }
                else
                    OnSuccess();
            }

            if (state == State.Success)
                return base.OnUpdate(execution, updateId);
            return State.Failure;
        }

        protected override void OnInit ()
        {
            if (triggerType is Type.InventoryQuantityChange or Type.InventoryDecayChange && !string.IsNullOrEmpty(inventoryName))
            {
                if ((itemType == 0 && item) || (itemType == 1 && !string.IsNullOrEmpty(itemCache)))
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        if (triggerType == Type.InventoryQuantityChange)
                        {
                            inv.OnQuantityChange -= OnInventoryQuantityChange;
                            inv.OnQuantityChange += OnInventoryQuantityChange;
                        }
                        else if (triggerType == Type.InventoryDecayChange)
                        {
                            inv.OnDecayChange -= OnInventoryDecayChange;
                            inv.OnDecayChange += OnInventoryDecayChange;
                        }
                    }
                    else
                    {
                        ReInventoryController.OnInvCreated += OnInventoryCreated;
                    }
                }
                else
                {
                    LogWarning("Found an empty Inventory Trigger node in " + context.objectName);
                }
            }
            else if (triggerType == Type.InventoryItemChange)
            {
                if ((itemType == 0 && item) || (itemType == 1 && !string.IsNullOrEmpty(itemCache)))
                {
                    ReInventoryController.OnInvChanged -= OnInventoryChanged;
                    ReInventoryController.OnInvChanged += OnInventoryChanged;
                }
                else
                {
                    LogWarning("Found an empty Inventory Trigger node in " + context.objectName);
                }
            }
            else if (triggerType == Type.InventorySlotChange)
            {
                if (inventoryType == InventoryType.Single && inventoryName.IsAssigned())
                {
                    if ((itemType is 0 or 1 && !paramNumber1.IsNull()) || (itemType == 2 && !string.IsNullOrEmpty(itemCache)))
                    {
                        ReInventoryController.OnInvUpdated += OnInventoryUpdated;
                        if (!string.IsNullOrEmpty(inventoryName))
                        {
                            var inv = InventoryManager.GetInventory(inventoryName);
                            if (inv != null)
                            {
                                inv.OnSlotChange -= OnInventorySlotChange;
                                inv.OnSlotChange += OnInventorySlotChange;
                            }
                            else
                            {
                                ReInventoryController.OnInvCreated += OnInventoryCreated;
                            }
                        }
                    }
                    else
                    {
                        LogWarning("Found an empty Inventory Trigger node in " + context.objectName);
                    }
                }
                else if (inventoryType == InventoryType.Character && !character.IsEmpty)
                {
                    if ((itemType is 0 or 1 && !paramNumber1.IsNull()) || (itemType == 2 && !string.IsNullOrEmpty(itemCache)))
                    {
                        ReInventoryController.OnInvUpdated += OnInventoryUpdated;
                        var charOp = (CharacterOperator) character;
                        var invList = charOp.GetInventoryNameList();
                        for (var i = 0; i < invList.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(invList[i]))
                            {
                                var inv = InventoryManager.GetInventory(invList[i]);
                                if (inv != null)
                                {
                                    inv.OnSlotChange -= OnInventorySlotChange;
                                    inv.OnSlotChange += OnInventorySlotChange;
                                }
                                else
                                {
                                    ReInventoryController.OnInvCreated += OnInventoryCreated;
                                }
                            }
                        }
                    }
                    else
                    {
                        LogWarning("Found an empty Inventory Trigger node in " + context.objectName);
                    }
                }
                else
                {
                    LogWarning("Found an empty Inventory Trigger node in " + context.objectName);
                }
            }
            else if (triggerType == Type.InventoryUnLockRequest && inventoryName.IsAssigned())
            {
                InventoryCanvas.OnInvUnlockRequested -= OnInventoryUnlockRequested;
                InventoryCanvas.OnInvUnlockRequested += OnInventoryUnlockRequested;
            }
            else
            {
                LogWarning("Found an empty Inventory Trigger node in " + context.objectName);
            }
        }

        private void OnInventoryUpdated (string invName)
        {
            if (string.Equals(inventoryName, invName))
            {
                if (inventoryType == InventoryType.Single)
                {
                    var inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        inv.OnSlotChange -= OnInventorySlotChange;
                        inv.OnSlotChange += OnInventorySlotChange;
                        OnInventorySlotChange(inventoryName, GetSlotId());
                    }
                }
                else if (inventoryType == InventoryType.Character)
                {
                    var charOp = (CharacterOperator) character;
                    var invList = charOp.GetInventoryNameList();
                    for (var i = 0; i < invList.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(invList[i]))
                        {
                            var inv = InventoryManager.GetInventory(invList[i]);
                            if (inv != null)
                            {
                                inv.OnSlotChange -= OnInventorySlotChange;
                                inv.OnSlotChange += OnInventorySlotChange;
                                OnInventorySlotChange(invList[i], GetSlotId());
                            }
                        }
                    }
                }
            }
        }

        private void OnInventoryCreated (string invName)
        {
            if (inventoryType == InventoryType.Single)
            {
                if (string.Equals(inventoryName, invName))
                {
                    InventoryData inv = InventoryManager.GetInventory(inventoryName);
                    if (inv != null)
                    {
                        if (triggerType == Type.InventoryQuantityChange)
                        {
                            inv.OnQuantityChange -= OnInventoryQuantityChange;
                            inv.OnQuantityChange += OnInventoryQuantityChange;
                        }
                        else if (triggerType == Type.InventorySlotChange)
                        {
                            inv.OnSlotChange -= OnInventorySlotChange;
                            inv.OnSlotChange += OnInventorySlotChange;
                        }
                        else if (triggerType == Type.InventoryDecayChange)
                        {
                            inv.OnDecayChange -= OnInventoryDecayChange;
                            inv.OnDecayChange += OnInventoryDecayChange;
                        }

                        ReInventoryController.OnInvCreated -= OnInventoryCreated;
                    }
                    else
                    {
                        LogWarning($"{inventoryName} inventory not found when trigger inventory create changed in {context.objectName}.");
                    }
                }
            }
            else if (inventoryType == InventoryType.Character)
            {
                var charOp = (CharacterOperator) character;
                if (charOp != null)
                {
                    var invList = charOp.GetInventoryNameList();
                    for (var i = 0; i < invList.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(invList[i]))
                        {
                            if (string.Equals(invList[i], invName))
                            {
                                InventoryData inv = InventoryManager.GetInventory(invList[i]);
                                if (inv != null)
                                {
                                    if (triggerType == Type.InventorySlotChange)
                                    {
                                        inv.OnSlotChange -= OnInventorySlotChange;
                                        inv.OnSlotChange += OnInventorySlotChange;
                                    }

                                    //-- TODO Unlink listener after all character inventory have created
                                    //ReInventoryController.OnInvCreated -= OnInventoryCreated;
                                }
                                else
                                {
                                    LogWarning($"{inventoryName} inventory not found when trigger inventory create changed in {context.objectName}.");
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OnInventoryUnlockRequested (string invName)
        {
            if (string.IsNullOrEmpty(inventoryName))
            {
                LogWarning($"Found invalid unlock request trigger in {context.objectName}");
            }
            else
            {
                context.runner.TriggerInventory(Type.InventoryUnLockRequest, TriggerId);
            }
        }

        private void OnInventoryChanged (string invName, string itemId)
        {
            if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
            {
                item = null;
                var cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                if (cacheNode != null)
                {
                    var cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                    if (cacheObj != null)
                        item = (ItemData) cacheObj;
                }
            }

            if (item == null)
            {
                var tempId = string.Empty;
                if (item != null)
                    tempId = item.id;
                LogWarning($"Found invalid item ({tempId}) quantity change trigger in {context.objectName}");
            }
            else
            {
                if (itemId == item.id)
                    context.runner.TriggerInventory(Type.InventoryItemChange, TriggerId);
            }
        }

        private void OnInventoryQuantityChange (string invName, string itemId)
        {
            if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
            {
                item = null;
                var cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                if (cacheNode != null)
                {
                    var cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                    if (cacheObj != null)
                        item = (ItemData) cacheObj;
                }
            }

            if (string.IsNullOrEmpty(inventoryName) || item == null)
            {
                var tempId = string.Empty;
                if (item != null)
                    tempId = item.id;
                LogWarning($"Found invalid item ({tempId}) quantity change trigger in {context.objectName}");
            }
            else
            {
                if (itemId == item.id)
                {
                    context.runner.TriggerInventory(Type.InventoryQuantityChange, TriggerId);
                }
            }
        }

        private void OnInventorySlotChange (string invName, int slotId)
        {
            var thisSlot = GetSlotId();
            if (inventoryType == InventoryType.Single)
            {
                if (context.runner.activated)
                    if (!string.IsNullOrEmpty(inventoryName))
                        if (thisSlot < 0 || slotId == thisSlot)
                            context.runner.TriggerInventory(Type.InventorySlotChange, TriggerId);
            }
            else if (inventoryType == InventoryType.Character)
            {
                var charOp = (CharacterOperator) character;
                var invList = charOp.GetInventoryNameList();
                for (var i = 0; i < invList.Length; i++)
                {
                    if (!string.IsNullOrEmpty(invList[i]) && invList[i] == invName)
                    {
                        if (thisSlot < 0 || slotId == thisSlot)
                        {
                            if (context.runner.activated)
                            {
                                if (inventoryName.IsVariable() && !inventoryName.IsNull())
                                    inventoryName.SetVariableValue(invName);
                                context.runner.TriggerInventory(Type.InventorySlotChange, TriggerId);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void OnInventoryDecayChange (string itemId, int before, int after)
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

            if (string.IsNullOrEmpty(inventoryName) || item == null)
            {
                var tempId = string.Empty;
                if (item != null)
                    tempId = item.id;
                LogWarning($"Found invalid item ({tempId}) decay change trigger in {context.objectName}");
            }
            else
            {
                if (itemId == item.id)
                {
                    context.runner.TriggerInventory(Type.InventoryDecayChange, TriggerId);
                }
            }
        }

        protected override void OnReset ()
        {
            InventoryCanvas.OnInvUnlockRequested -= OnInventoryUnlockRequested;
            ReInventoryController.OnInvCreated -= OnInventoryCreated;
            ReInventoryController.OnInvUpdated -= OnInventoryUpdated;
            ReInventoryController.OnInvChanged -= OnInventoryChanged;
            if (!string.IsNullOrEmpty(inventoryName))
            {
                InventoryData inv = InventoryManager.GetInventory(inventoryName);
                if (inv != null)
                {
                    if (triggerType == Type.InventoryQuantityChange)
                    {
                        inv.OnQuantityChange -= OnInventoryQuantityChange;
                    }
                    else if (triggerType == Type.InventorySlotChange)
                    {
                        inv.OnSlotChange -= OnInventorySlotChange;
                    }
                    else if (triggerType == Type.InventoryDecayChange)
                    {
                        inv.OnDecayChange -= OnInventoryDecayChange;
                    }
                }
            }

            if (triggerType == Type.InventorySlotChange && inventoryType == InventoryType.Character)
            {
                var charOp = (CharacterOperator) character;
                if (charOp != null)
                {
                    var invList = charOp.GetInventoryNameList();
                    for (var i = 0; i < invList.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(invList[i]))
                        {
                            InventoryData inv = InventoryManager.GetInventory(invList[i]);
                            if (inv != null)
                                inv.OnSlotChange -= OnInventorySlotChange;
                        }
                    }
                }
            }
        }

        public override bool IsRequireInit ()
        {
            if (triggerType is Type.InventoryQuantityChange or Type.InventoryDecayChange)
            {
                if (!string.IsNullOrEmpty(inventoryName))
                {
                    if (itemType == 0 && item != null)
                        return true;
                    if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                        return true;
                }
            }
            if (triggerType is Type.InventoryItemChange)
            {
                if (itemType == 0 && item != null)
                    return true;
                if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                    return true;
            }
            else if (triggerType == Type.InventorySlotChange)
            {
                if (inventoryName.IsAssigned())
                {
                    if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        return true;
                    if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        return true;
                }
            }
            else if (triggerType == Type.InventoryUnLockRequest)
            {
                if (inventoryName.IsAssigned())
                {
                    return true;
                }
            }

            return false;
        }

        private int GetSlotId ()
        {
            int thisSlot = paramNumber1;
            if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
            {
                item = null;
                CacheBehaviourNode cacheNode = (CacheBehaviourNode) context.graph.GetNode(itemCache);
                if (cacheNode != null)
                {
                    object cacheObj = context.GetCache(cacheNode.GetCacheSavedName());
                    if (cacheObj != null)
                        thisSlot = (int) ((float) cacheObj);
                }
            }

            return thisSlot;
        }
        
        public override bool IsTrigger (TriggerNode.Type type, int paramInt = 0)
        {
            return type == triggerType;
        }

#if UNITY_EDITOR
        public string ItemCache => itemCache;

        public void SetItemCache (string value)
        {
            itemCache = value;
        }

        private string InventoryNameLabel ()
        {
            if (triggerType == Type.InventorySlotChange && inventoryType == InventoryType.Character)
                return "Inv Name Store To";
            return "Inv Name";
        }

        private void SwitchToItemCache ()
        {
            itemType = 1;
            MarkDirty();
        }

        private void SwitchToItemData ()
        {
            itemType = 0;
            MarkDirty();
        }

        private bool ShowItemData ()
        {
            switch (triggerType)
            {
                case Type.InventoryQuantityChange:
                case Type.InventoryItemChange:
                case Type.InventoryDecayChange:
                    if (itemType == 0)
                        return true;
                    else
                        return false;
                default:
                    return false;
            }
        }

        private bool ShowCache ()
        {
            switch (triggerType)
            {
                case Type.InventoryQuantityChange:
                case Type.InventoryItemChange:
                case Type.InventoryDecayChange:
                    return itemType == 1;
                case Type.InventorySlotChange:
                    return itemType == 2;
                default:
                    return false;
            }
        }

        private bool ShowNumber1 ()
        {
            if (triggerType is Type.InventorySlotChange)
                if (itemType is 0 or 1)
                    return true;
            return false;
        }

        private string GetCacheLabel ()
        {
            if (triggerType is Type.InventorySlotChange)
                return "Slot Index";
            return "Item Cache";
        }

        private IEnumerable ItemChoice ()
        {
            var itemListDropdown = new ValueDropdownList<ItemData>();
            var guids = AssetDatabase.FindAssets("t:ItemList");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var itemList = AssetDatabase.LoadAssetAtPath<ItemList>(path);
                for (int j = 0; j < itemList.items.Count; j++)
                {
                    var tempItem = itemList.items[j];
                    itemListDropdown.Add(itemList.name + "/" + tempItem.displayName, tempItem);
                }
            }

            return itemListDropdown;
        }

        private IEnumerable GetCacheChoice ()
        {
            var itemListDropdown = new ValueDropdownList<string>();
            var graph = GetGraph();
            if (graph is {nodes: { }})
            {
                for (var i = 0; i < graph.nodes.Count; i++)
                {
                    if (graph.nodes[i] is CacheBehaviourNode)
                    {
                        var cacheNode = (CacheBehaviourNode) graph.nodes[i];
                        if (triggerType is Type.InventorySlotChange)
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
                var found = false;
                for (var i = 0; i < itemListDropdown.Count; i++)
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
                for (int i = 0; i < graph.nodes.Count; i++)
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
            if (triggerType == Type.InventoryQuantityChange)
            {
                if (itemType == 1)
                    return true;
            }
            else if (triggerType == Type.InventoryItemChange)
            {
                if (itemType == 1)
                    return true;
            }
            else if (triggerType == Type.InventorySlotChange)
            {
                if (itemType == 2)
                    return true;
            }
            else if (triggerType == Type.InventoryDecayChange)
            {
                if (itemType == 1)
                    return true;
            }
            
            return false;
        }

        private void UpdateNumber1 ()
        {
            if (triggerType is Type.InventorySlotChange)
            {
                if (itemType == 0 && paramNumber1.type == 1)
                    itemType = 1;
                if (itemType == 1 && paramNumber1.type == 0)
                    itemType = 2;
            }

            MarkPropertyDirty(paramNumber1);
        }

        private void OnChangeType ()
        {
            if (triggerType is Type.InventoryQuantityChange or Type.InventoryDecayChange or Type.InventoryItemChange)
                if (itemType == 2)
                    itemType = 0;
            if (triggerType != Type.InventorySlotChange || inventoryType == InventoryType.Single)
                inventoryName.AllowAll();
            else if (inventoryType == InventoryType.Character)
                inventoryName.AllowVariableOnly();
            MarkDirty();
        }

        private void OnChangeInvType ()
        {
            if (inventoryType == InventoryType.Single)
                inventoryName.AllowAll();
            else if (inventoryType == InventoryType.Character)
                inventoryName.AllowVariableOnly();
            MarkDirty();
        }

        private static IEnumerable InvTypeChoice = new ValueDropdownList<InventoryType>()
        {
            {"Single", InventoryType.Single},
            {"Character", InventoryType.Character},
        };

        private IEnumerable TriggerTypeChoice ()
        {
            var menu = new ValueDropdownList<Type>
            {
                {"Quantity Changed", Type.InventoryQuantityChange},
                {"Slot Changed", Type.InventorySlotChange},
                {"Item Changed", Type.InventoryItemChange},
                {"Decay Changed", Type.InventoryDecayChange},
                {"UnLock Requested", Type.InventoryUnLockRequest}
            };
            return menu;
        }

        public static string displayName = "Inventory Trigger Node";
        public static string nodeName = "Inventory";
        
        public override Type GetNodeType ()
        {
            return triggerType;
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
            return triggerType.ToString();
        }

        public override string GetNodeViewDescription ()
        {
            string desc = String.Empty;
            if (triggerType == Type.InventoryQuantityChange)
            {
                if (inventoryName.IsAssigned())
                {
                    if (itemType == 0 && item != null)
                    {
                        desc = $"{item.displayName}'s quantity changed in {inventoryName.GetDisplayName()}";
                    }
                    else if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                    {
                        desc = $"{GetItemCacheName(itemCache)}'s quantity changed in {inventoryName.GetDisplayName()}";
                    }
                }
            }
            else if (triggerType == Type.InventoryItemChange)
            {
                if (itemType == 0 && item != null)
                {
                    desc = $"{item.displayName}'s global quantity changed";
                }
                else if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                {
                    desc = $"{GetItemCacheName(itemCache)}'s global quantity changed";
                }
            }
            else if (triggerType == Type.InventorySlotChange)
            {
                if (inventoryType == InventoryType.Single)
                {
                    if (inventoryName.IsAssigned())
                    {
                        if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        {
                            if (paramNumber1 < 0)
                                desc = $"Any slot changed in {inventoryName.GetDisplayName()}";
                            else
                                desc = $"Slot {paramNumber1} changed in {inventoryName.GetDisplayName()}";
                        }
                        else if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        {
                            desc = $"Slot {GetItemCacheName(itemCache)} changed in {inventoryName.GetDisplayName()}";
                        }
                    }
                }
                else if (inventoryType == InventoryType.Character)
                {
                    if (!character.IsNull)
                    {
                        if (itemType is 0 or 1 && !paramNumber1.IsNull())
                        {
                            if (paramNumber1 < 0)
                                desc = $"Any slot changed in {character.objectName}";
                            else
                                desc = $"Slot {paramNumber1} changed in {character.objectName}";
                        }
                        else if (itemType == 2 && !string.IsNullOrEmpty(itemCache))
                        {
                            desc = $"Slot {GetItemCacheName(itemCache)} changed in {character.objectName}";
                        }
                    }
                }
            }
            else if (triggerType == Type.InventoryDecayChange)
            {
                if (inventoryName.IsAssigned())
                {
                    if (itemType == 0 && item != null)
                    {
                        desc = $"{item.displayName}'s decay changed in {inventoryName.GetDisplayName()}";
                    }
                    else if (itemType == 1 && !string.IsNullOrEmpty(itemCache))
                    {
                        desc = $"{GetItemCacheName(itemCache)}'s decay changed in {inventoryName.GetDisplayName()}";
                    }
                }
            }
            else if (triggerType == Type.InventoryUnLockRequest)
            {
                if (inventoryName.IsAssigned())
                {
                    desc = $"{inventoryName.GetDisplayName()} being request for unlock";
                }
            }

            return desc;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (triggerType == Type.InventoryQuantityChange)
                tip += "This will get trigger when a specific item quantity change in the defined inventory.\n\n";
            else if (triggerType == Type.InventoryItemChange)
                tip += "This will get trigger when a specific item global quantity. This is very similar to InventoryQuantityChange, the different is this is global trigger while InventoryQuantityChange is optimise inventory trigger.\n\n";
            else if (triggerType == Type.InventorySlotChange)
                tip += "This will get trigger when a specific slot contents change in the defined inventory.\n\nSlot contents including slot item, slot quantity.\n\n";
            else if (triggerType == Type.InventoryDecayChange)
                tip += "This will get trigger when a specific slot durability change in the defined inventory.\n\n";
            else if (triggerType == Type.InventoryUnLockRequest)
                tip += "This will get trigger when the defined inventory receive request to use item in inventory as key for unlock.\n\n";
            else
                tip += "This will get trigger all Inventory related events.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}