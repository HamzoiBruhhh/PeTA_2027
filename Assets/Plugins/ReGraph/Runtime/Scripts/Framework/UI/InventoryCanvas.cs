using System;
using System.Collections;
using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Reshape.Unity;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class InventoryCanvas : ReSingletonBehaviour<InventoryCanvas>
    {
        public const int DROP_NOT_SPACE = 101;
        private const string WALLET = "Wallet";

        private static GameObject reusableGo;

        public Canvas canvas;
        public InventoryPanel[] panels;
        public RectTransform leftPanel;
        public RectTransform rightPanel;
        public GameObject currencyPanel;
        public InventoryItemTooltip itemTooltip;
        public InventoryItemPick itemPick;
        public InventoryItemContext itemContext;

        [LabelText("Cursor Inventory")]
        public InventoryBehaviour cursorInvBehaviour;

        private string currentInv1;
        private string currentInv2;
        private InventoryPanel buySellPanel;
        private string pickInv;
        private int pickSlotIndex;
        private bool pickApplyStatus;
        private bool pickApplySkill;
        private Vector3 leftPanelPos;
        private Vector3 rightPanelPos;
        private int showCanvasFrameNo;

        public delegate void TypeDelegate (int type);

        public static event ReInventoryController.InvNameDelegate OnInvClosed;
        public static event ReInventoryController.InvNameDelegate OnInvUnlockRequested;
        public static event TypeDelegate OnWarning;

        public static bool IsBuySellPanelAvailable ()
        {
            if (!instance)
                return false;
            return instance.buySellPanel;
        }

        public static bool IsUnderPickUp ()
        {
            if (!instance)
                return false;
            return !string.IsNullOrEmpty(instance.pickInv);
        }

        public static void HideCanvas ()
        {
            if (!instance)
                return;
            instance.Hide();
        }

        public static void HideCanvasIfCursorClear ()
        {
            if (!instance)
                return;
            instance.HideIfCursorClear();
        }

        public static void NotifyClosePanel (string invName)
        {
            OnInvClosed?.Invoke(invName);
        }

        public static void PrepareCanvas ()
        {
            if (!instance)
            {
                var go = Instantiate(GraphManager.instance.runtimeSettings.inventoryCanvas);
                go.name = GraphManager.instance.runtimeSettings.inventoryCanvas.name;
            }

            instance.Prepare();
        }

        public static void ShowCanvas (string invName, string tradeInvName = "", bool autoInit = false)
        {
            if (!instance)
            {
                if (autoInit)
                {
                    var go = Instantiate(GraphManager.instance.runtimeSettings.inventoryCanvas);
                    go.name = GraphManager.instance.runtimeSettings.inventoryCanvas.name;
                }
                else
                {
                    return;
                }
            }

            instance.Show(invName, tradeInvName);
        }

        public static bool isShowingCanvas => instance != null && instance.isShowing;

        public static bool isShowingLeftOnly => instance != null && instance.isShowing && !string.IsNullOrEmpty(instance.currentInv1) && string.IsNullOrEmpty(instance.currentInv2);

        public static bool isShowingRightOnly => instance != null && instance.isShowing && string.IsNullOrEmpty(instance.currentInv1) && !string.IsNullOrEmpty(instance.currentInv2);

        public static bool isShowingSingleOnly
        {
            get
            {
                if (instance != null && instance.isShowing)
                {
                    if (!string.IsNullOrEmpty(instance.currentInv1) && string.IsNullOrEmpty(instance.currentInv2))
                        return true;
                    if (string.IsNullOrEmpty(instance.currentInv1) && !string.IsNullOrEmpty(instance.currentInv2))
                        return true;
                }

                return false;
            }
        }

        public static bool IsShowingLeftForCharacterOperator (CharacterOperator co)
        {
            var invList = co.GetInventoryNameList();
            for (var i = 0; i < invList.Length; i++)
                if (invList[i] == instance.currentInv1)
                    return true;
            return false;
        }

        public static bool IsShowingRightForCharacterOperator (CharacterOperator co)
        {
            var invList = co.GetInventoryNameList();
            for (var i = 0; i < invList.Length; i++)
                if (invList[i] == instance.currentInv2)
                    return true;
            return false;
        }

        public static void ShowToolTip (string inv, int index, bool pickedToolTip)
        {
            if (instance == null)
                return;
            instance.DisplayToolTip(inv, index, pickedToolTip);
        }

        public static void ShowContext (InventoryBehaviour invBehave, int index)
        {
            if (instance == null)
                return;
            instance.DisplayContext(invBehave, index);
        }

        public static void HideContext (InventoryBehaviour invBehave, int index)
        {
            if (instance == null)
                return;
            instance.CloseContext(invBehave, index);
        }

        public static void HideToolTip (string inv, int index)
        {
            if (instance == null)
                return;
            instance.CloseToolTip(inv, index);
        }

        public static bool isCursorInvOccupied
        {
            get
            {
                if (instance == null)
                    return false;
                if (instance.cursorInvBehaviour == null || string.IsNullOrWhiteSpace(instance.cursorInvBehaviour.Name))
                    return false;
                var cursorInv = InventoryManager.GetInventory(instance.cursorInvBehaviour.Name);
                if (cursorInv == null)
                    return false;
                var cursorItem = cursorInv.GetItem(0);
                return cursorItem is {isOccupied: true};
            }
        }

        public static InventoryItem cursorInvItem
        {
            get
            {
                if (instance == null)
                    return null;
                if (instance.cursorInvBehaviour == null || string.IsNullOrWhiteSpace(instance.cursorInvBehaviour.Name))
                    return null;
                var cursorInv = InventoryManager.GetInventory(instance.cursorInvBehaviour.Name);
                if (cursorInv == null)
                    return null;
                var cursorItem = cursorInv.GetItem(0);
                return cursorItem;
            }
        }

        public static bool VirtualPick (string inv, int index, bool applyStatus, bool applySkill)
        {
            if (instance == null)
                return false;
            return instance.PerformVirtualPick(inv, index, applyStatus, applySkill);
        }

        public static int CursorPick (string inv, int index, int putIndex, bool applyStatus, bool applySkill, bool pickedToolTip, bool testOnly = false)
        {
            if (instance == null)
                return 0;
            return instance.PerformCursorPick(inv, index, putIndex, applyStatus, applySkill, pickedToolTip, testOnly);
        }

        public static void Trade (string inv, int index, bool applyStatus, bool applySkill)
        {
            if (instance == null)
                return;
            instance.PerformTradeWithCurrentInventories(inv, index, applyStatus, applySkill);
        }

        public static void Discard (string inv, int index)
        {
            if (instance == null)
                return;
            instance.PerformDiscard(inv, index);
        }

        public static void Drop (string inv, int index)
        {
            if (instance == null)
                return;
            instance.PerformDrop(inv, inv, index);
        }

        public static void DropCursorItem (Vector3 location)
        {
            if (instance == null)
                return;
            instance.PerformDropCursorItem(location);
        }

        public static bool Use (string inv, int index, MultiTag consume, int quantity)
        {
            if (instance == null)
                return false;
            return instance.PerformUse(inv, index, consume, quantity);
        }

        public static void Unlock (string inv)
        {
            if (instance == null)
                return;
            instance.PerformUnlock(inv);
        }

        public bool isShowing => canvas.enabled;

        private int PerformCursorPick (string inv, int index, int putIndex, bool applyStatus, bool applySkill, bool pickedToolTip, bool testOnly = false)
        {
            if (string.IsNullOrWhiteSpace(inv))
                return 0;
            var clickedInv = InventoryManager.GetInventory(inv);
            if (clickedInv == null)
                return 0;
            if (cursorInvBehaviour == null || string.IsNullOrWhiteSpace(cursorInvBehaviour.Name))
                return 0;
            var cursorInv = InventoryManager.GetInventory(cursorInvBehaviour.Name);
            if (cursorInv == null)
                return 0;
            var clickedInvName = inv;
            var cursorInvName = cursorInvBehaviour.Name;
            var cursorItem = cursorInv.GetItem(0);
            var clickedItem = clickedInv.GetItem(index);
            var clickedInvPanel = GetPanel(clickedInvName);
            var manager = GraphManager.instance.runtimeSettings.itemManager;
            if (cursorItem is {isSolid: true})
            {
                var cursorItemData = manager.GetItemData(cursorItem.ItemId);
                if (cursorItemData.invBehaviour != null && !testOnly)
                    InventoryManager.CreateSubInventory(cursorItemData, cursorInvName, 0);
                if (clickedItem is null or {isEmpty: true})
                {
                    if (clickedInvPanel == null || !clickedInvPanel.invBehave.RestrictAdd)
                    {
                        var give = false;
                        if (!IsBuySellPanelAvailable())
                            give = true;
                        else
                        {
                            if (IsBuySellItem(pickInv))
                            {
                                if (pickInv == clickedInvName)
                                {
                                    if (InventoryManager.Give(cursorInvName, 0, clickedInvName, pickSlotIndex, true, out _, testOnly) > 0)
                                        return testOnly ? 1 : EndPick();
                                }
                                else
                                {
                                    if (InventoryManager.Trade(cursorInvName, 0, clickedInvName, index, WALLET, buySellPanel.invBehave.Currency.id, true, testOnly))
                                    {
                                        if (testOnly)
                                            return 1;
                                        SlotIntoCursorItem(cursorItem, cursorItemData);
                                        return EndPick();
                                    }
                                }
                            }
                            else if (IsBuySellItem(clickedInvName))
                            {
                                if (InventoryManager.Trade(cursorInvName, 0, clickedInvName, index, WALLET, buySellPanel.invBehave.Currency.id, false, testOnly))
                                    return testOnly ? 1 : EndPick();
                            }
                            else
                                give = true;
                        }

                        if (give)
                        {
                            var giveResult = InventoryManager.Give(cursorInvName, 0, clickedInvName, index, false, out var swapIndex, testOnly);
                            if (giveResult > 0 && !testOnly)
                            {
                                SlotIntoCursorItem(cursorItem, cursorItemData);
                                if (giveResult == 1)
                                {
                                    cursorItem = cursorInv.GetItem(0);
                                    return cursorItem is {isSolid: true} ? BeginPick(cursorItemData, cursorItem) : EndPick();
                                }
                                else if (giveResult == 2)
                                {
                                    var swapItem = cursorInv.GetItem(0);
                                    if (swapItem is {isSolid: true})
                                    {
                                        var swapItemData = manager.GetItemData(swapItem.ItemId);
                                        if (swapItemData != null)
                                        {
                                            SlotOutCursorItem(swapItem, swapItemData);
                                            BeginPick(swapItemData, swapItem);
                                        }
                                    }

                                    return 2;
                                }
                            }

                            return giveResult;
                        }
                    }
                    else
                    {
                        if (pickInv == clickedInvName)
                            if (InventoryManager.Give(cursorInvName, 0, clickedInvName, pickSlotIndex, true, out _, testOnly) > 0)
                                return testOnly ? 1 : EndPick();
                    }
                }
                else if (clickedItem is {isSolid: true})
                {
                    var clickedItemData = manager.GetItemData(clickedItem.ItemId);
                    if (!IsBuySellPanelAvailable() || (clickedInvName != buySellPanel.invName && !IsBuySellItem(clickedInvName)))
                    {
                        if (clickedInvPanel == null || !clickedInvPanel.invBehave.RestrictAdd)
                        {
                            if (cursorItem.ItemId == clickedItem.ItemId)
                            {
                                if (clickedInv.GetAllItemQuantity() != clickedInv.CountLimit)
                                {
                                    if (clickedItemData.stack && cursorItemData.invBehaviour == null)
                                    {
                                        if (InventoryManager.Stack(cursorInvName, 0, clickedInvName, index, testOnly))
                                        {
                                            if (testOnly)
                                                return 1;
                                            cursorItem = cursorInv.GetItem(0);
                                            return cursorItem is {isSolid: true} ? BeginPick(cursorItemData, cursorItem) : EndPick();
                                        }
                                    }
                                }
                            }

                            if (InventoryManager.Swap(cursorInvName, 0, clickedInvName, index, putIndex, testOnly))
                            {
                                if (testOnly)
                                    return 2;
                                SlotIntoCursorItem(cursorItem, cursorItemData);
                                SlotOutCursorItem(clickedItem, clickedItemData);
                                BeginPick(clickedItemData, clickedItem);
                                return 2;
                            }
                        }
                        else
                        {
                            if (pickInv == clickedInvName)
                            {
                                if (testOnly)
                                    return 1;
                                if (InventoryManager.Give(cursorInvName, 0, clickedInvName, pickSlotIndex, true, out _) > 0)
                                    if (InventoryManager.Give(clickedInvName, index, cursorInvName, 0, true, out _) > 0)
                                        return BeginPick(clickedItemData, clickedItem);
                            }
                        }
                    }

                    if (IsBuySellPanelAvailable() && IsBuySellItem(clickedInvName))
                    {
                        if (clickedInvPanel == null || !clickedInvPanel.invBehave.RestrictAdd)
                        {
                            if (pickInv == clickedInvName)
                            {
                                if (testOnly)
                                    return 1;
                                if (InventoryManager.Give(cursorInvName, 0, clickedInvName, pickSlotIndex, true, out _) > 0)
                                    if (InventoryManager.Give(clickedInvName, index, cursorInvName, 0, true, out _) > 0)
                                        return BeginPick(clickedItemData, clickedItem);
                            }
                        }
                    }
                }
            }
            else if (cursorItem is null or {isEmpty: true} && clickedItem is {isSolid: true})
            {
                var clickedItemData = manager.GetItemData(clickedItem.ItemId);
                if (!cursorInvBehaviour.RestrictAdd)
                {
                    if (InventoryManager.Give(clickedInvName, index, cursorInvName, 0, true, out _, false) > 0)
                    {
                        SlotOutCursorItem(clickedItem, clickedItemData);
                        return BeginPick(clickedItemData, clickedItem);
                    }
                }
            }

            void SlotIntoCursorItem (InventoryItem item, ItemData itemData)
            {
                if (item.isUsable)
                {
                    if (!itemData.isEmptyStatus)
                        if (applyStatus)
                            InventoryManager.AddCharacterAttackStatus(clickedInvName, itemData);
                    if (!itemData.isEmptySkill)
                        if (applySkill)
                            InventoryManager.AddCharacterAttackSkill(clickedInvName, itemData);
                }
            }

            void SlotOutCursorItem (InventoryItem item, ItemData itemData)
            {
                if (item.isUsable)
                {
                    if (!itemData.isEmptyStatus)
                        if (applyStatus)
                            InventoryManager.RemoveCharacterAttackStatus(clickedInvName, itemData);
                    if (!itemData.isEmptySkill)
                        if (applySkill)
                            InventoryManager.RemoveCharacterAttackSkill(clickedInvName, itemData);
                }
            }


            int BeginPick (ItemData itemData, InventoryItem slotData)
            {
                pickInv = clickedInvName;
                pickSlotIndex = index;
                pickApplyStatus = applyStatus;
                pickApplySkill = applySkill;
                if (!pickedToolTip)
                    itemTooltip.Hide();
                itemPick.ShowPickInfo(itemData, slotData);
                return 1;
            }

            int EndPick ()
            {
                InventorySlotItem.UnhighlightHighlighted();
                pickInv = string.Empty;
                itemTooltip.Hide();
                itemPick.Hide();
                return 1;
            }

            return 0;
        }

        private void PerformTradeWithCurrentInventories (string inv, int index, bool applyStatus, bool applySkill)
        {
            if (currentInv1 != inv && currentInv2 != inv)
                return;
            var from = currentInv1;
            var to = currentInv2;
            if (currentInv2 == inv)
            {
                from = currentInv2;
                to = currentInv1;
            }

            var tradeInv = InventoryManager.GetInventory(from);
            var tradeItem = tradeInv.GetItem(index);
            var manager = GraphManager.instance.runtimeSettings.itemManager;
            var tradeItemData = manager.GetItemData(tradeItem.ItemId);
            bool slotInStatus, slotOutStatus, slotInSkill, slotOutSkill;
            slotInStatus = slotOutStatus = slotInSkill = slotOutSkill = false;
            if (tradeItemData.invBehaviour != null)
                InventoryManager.CreateSubInventory(tradeItemData, from, index);

            if (IsBuySellPanelAvailable())
            {
                if (IsBuySellItem(from))
                {
                    if (InventoryManager.Trade(from, index, to, WALLET, buySellPanel.invBehave.Currency.id, true, true))
                    {
                        if (InventoryManager.GetInventoryApplyStatusType(to) == InventoryBehaviour.ApplyStatusTrigger.SlotIn)
                            slotInStatus = true;
                        if (InventoryManager.GetInventoryApplySkillType(to) == InventoryBehaviour.ApplySkillTrigger.SlotIn)
                            slotInSkill = true;
                    }
                }
                else
                {
                    if (InventoryManager.Trade(from, index, to, WALLET, buySellPanel.invBehave.Currency.id, false, true))
                    {
                        if (applyStatus)
                            slotOutStatus = true;
                        if (applySkill)
                            slotOutSkill = true;
                    }
                }
            }
            else
            {
                var panel = GetPanel(to);
                if (!panel.invBehave.RestrictAdd)
                {
                    if (InventoryManager.Give(from, index, to, true))
                    {
                        if (InventoryManager.GetInventoryApplyStatusType(to) == InventoryBehaviour.ApplyStatusTrigger.SlotIn)
                            slotInStatus = true;
                        if (InventoryManager.GetInventoryApplySkillType(to) == InventoryBehaviour.ApplySkillTrigger.SlotIn)
                            slotInSkill = true;
                        if (applyStatus)
                            slotOutStatus = true;
                        if (applySkill)
                            slotOutSkill = true;
                    }
                }
            }

            if (tradeItem.isUsable)
            {
                if (!tradeItemData.isEmptyStatus)
                {
                    if (slotInStatus)
                        InventoryManager.AddCharacterAttackStatus(to, tradeItemData);
                    if (slotOutStatus)
                        InventoryManager.RemoveCharacterAttackStatus(from, tradeItemData);
                }

                if (!tradeItemData.isEmptySkill)
                {
                    if (slotInSkill)
                        InventoryManager.AddCharacterAttackSkill(from, tradeItemData);
                    if (slotOutSkill)
                        InventoryManager.RemoveCharacterAttackSkill(to, tradeItemData);
                }
            }
        }

        private bool PerformVirtualPick (string inv, int index, bool applyStatus, bool applySkill)
        {
            if (!IsUnderPickUp())
            {
                var fromInv = InventoryManager.GetInventory(inv);
                if (fromInv != null)
                {
                    var pickItem = fromInv.GetItem(index);
                    if (pickItem is {isSolid: true})
                    {
                        pickInv = inv;
                        pickSlotIndex = index;
                        pickApplyStatus = applyStatus;
                        pickApplySkill = applySkill;
                        var manager = GraphManager.instance.runtimeSettings.itemManager;
                        var itemData = manager.GetItemData(pickItem.ItemId);
                        itemPick.ShowPickInfo(itemData, pickItem);
                        return true;
                    }
                }
            }
            else
            {
                var fromInv = InventoryManager.GetInventory(pickInv);
                var toInv = InventoryManager.GetInventory(inv);
                var putItem = toInv.GetItem(index);
                var pickItem = fromInv.GetItem(pickSlotIndex);
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var pickItemData = manager.GetItemData(pickItem.ItemId);
                var success = false;
                bool slotInStatus, slotOutStatus, slotInSkill, slotOutSkill;
                slotInStatus = slotOutStatus = slotInSkill = slotOutSkill = false;
                if (putItem == null || putItem.isEmpty)
                {
                    if (pickInv == inv)
                    {
                        var relocate = false;
                        if (IsBuySellPanelAvailable())
                        {
                            if (pickInv != buySellPanel.invName)
                                relocate = true;
                        }
                        else
                            relocate = true;

                        if (relocate)
                            success = InventoryManager.Relocate(pickInv, pickSlotIndex, index);
                    }
                    else
                    {
                        if (pickItemData.invBehaviour != null)
                            InventoryManager.CreateSubInventory(pickItemData, pickInv, pickSlotIndex);

                        var give = false;
                        if (!IsBuySellPanelAvailable())
                        {
                            give = true;
                        }
                        else
                        {
                            if (IsBuySellItem(pickInv))
                            {
                                if (InventoryManager.Trade(pickInv, pickSlotIndex, inv, index, WALLET, buySellPanel.invBehave.Currency.id, true))
                                {
                                    success = true;
                                    if (applyStatus)
                                        slotInStatus = true;
                                    if (applySkill)
                                        slotInSkill = true;
                                }
                            }
                            else if (IsBuySellItem(inv))
                            {
                                if (InventoryManager.Trade(pickInv, pickSlotIndex, inv, index, WALLET, buySellPanel.invBehave.Currency.id, false))
                                {
                                    success = true;
                                    if (pickApplyStatus)
                                        slotOutStatus = true;
                                    if (pickApplySkill)
                                        slotOutSkill = true;
                                }
                            }
                            else
                            {
                                give = true;
                            }
                        }

                        if (give)
                        {
                            var panel = GetPanel(inv);
                            if (panel == null || !panel.invBehave.RestrictAdd)
                            {
                                if (InventoryManager.Give(pickInv, pickSlotIndex, inv, index, true, out _) > 0)
                                {
                                    success = true;
                                    if (pickApplyStatus)
                                        slotOutStatus = true;
                                    if (pickApplySkill)
                                        slotOutSkill = true;
                                    if (applyStatus)
                                        slotInStatus = true;
                                    if (applySkill)
                                        slotInSkill = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (pickInv == inv)
                    {
                        if (pickSlotIndex == index)
                        {
                            success = true;
                        }
                        else if (!IsBuySellPanelAvailable() || pickInv != buySellPanel.invName)
                        {
                            if (InventoryManager.Stack(pickInv, pickSlotIndex, pickInv, index))
                                success = true;
                        }
                    }
                }

                if (pickItem.isUsable)
                {
                    if (!pickItemData.isEmptyStatus)
                    {
                        if (slotInStatus)
                            InventoryManager.AddCharacterAttackStatus(inv, pickItemData);
                        if (slotOutStatus)
                            InventoryManager.RemoveCharacterAttackStatus(pickInv, pickItemData);
                    }

                    if (!pickItemData.isEmptySkill)
                    {
                        if (slotInSkill)
                            InventoryManager.AddCharacterAttackSkill(inv, pickItemData);
                        if (slotOutSkill)
                            InventoryManager.RemoveCharacterAttackSkill(pickInv, pickItemData);
                    }
                }

                if (success)
                {
                    InventorySlotItem.UnhighlightHighlighted();
                    pickInv = string.Empty;
                    itemTooltip.Hide();
                    itemPick.Hide();
                }
            }

            return false;
        }

        public void CloseToolTip (string inv, int index)
        {
            itemTooltip.Hide();
        }

        public void DisplayToolTip (string inv, int index, bool pickedToolTip)
        {
            if (IsUnderPickUp())
            {
                if (pickedToolTip)
                {
                    var cursorItem = cursorInvItem; 
                    if (cursorItem != null)
                        SetToolTipFromInventoryItem(cursorItem, pickInv);
                    else
                        SetToolTipFromNameIndex(pickInv, pickSlotIndex, pickInv);
                }
            }
            else
                SetToolTipFromNameIndex(inv, index, inv);

            void SetToolTipFromNameIndex (string invName, int invIndex, string currentInv)
            {
                var invData = InventoryManager.GetInventory(invName);
                if (invData != null)
                {
                    var item = invData.GetItem(invIndex);
                    SetToolTipFromInventoryItem(item, currentInv);
                }
            }
            
            void SetToolTipFromInventoryItem (InventoryItem item, string currentInv)
            {
                if (item is {isSolid: true})
                {
                    var manager = GraphManager.instance.runtimeSettings.itemManager;
                    var itemData = manager.GetItemData(item.ItemId);
                    itemTooltip.ShowItemInfo(itemData, item);
                    if (IsBuySellPanelAvailable())
                    {
                        if (currentInv == buySellPanel.invName)
                            itemTooltip.ShowBuyInfo(itemData, item);
                        else
                            itemTooltip.ShowSellInfo(itemData, item);
                    }
                }
            }
        }

        public void DisplayContext (InventoryBehaviour invBehave, int index)
        {
            if (IsUnderPickUp())
                return;
            var fromInv = InventoryManager.GetInventory(invBehave.Name);
            if (fromInv != null)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var tradeItem = fromInv.GetItem(index);
                if (tradeItem is {isSolid: true})
                {
                    var itemData = manager.GetItemData(tradeItem.ItemId);
                    var use = invBehave.Consume.ContainAny(itemData.tags, false);
                    itemContext.Show(invBehave, index, use, invBehave.Discard);
                }
            }
        }

        public void CloseContext (InventoryBehaviour invBehave, int index)
        {
            var fromInv = InventoryManager.GetInventory(invBehave.Name);
            var tradeItem = fromInv?.GetItem(index);
            if (tradeItem is {isSolid: true})
                itemContext.Hide();
        }

        private void Prepare ()
        {
            canvas.enabled = true;
            for (var i = 0; i < panels.Length; i++)
            {
                if (panels[i])
                {
                    panels[i].Show(false);
                    panels[i].Prepare();
                }
            }
        }

        public void Show (string invName1, string invName2)
        {
            if (!string.IsNullOrWhiteSpace(invName1) && isShowing)
                return;
            InventoryPanel panel1 = null;
            InventoryPanel panel2 = null;
            for (var i = 0; i < panels.Length; i++)
            {
                if (!string.IsNullOrEmpty(panels[i].invName))
                {
                    if (panels[i].invName == invName1 && !panel1)
                    {
                        panel1 = panels[i];
                    }
                    else if (panels[i].invName == invName2 && !panel2)
                    {
                        panel2 = panels[i];
                    }
                }
            }

            var isCurrencyDisplay = false;
            if (panel1)
            {
                currentInv1 = invName1;
                if (panel1.invBehave.BuySell)
                    buySellPanel = panel1;
                panel1.transform.position = leftPanelPos;
                panel1.Show(IsBuySellPanelAvailable());
                if (panel1.invBehave.CurrencyDisplay)
                    isCurrencyDisplay = true;
            }

            if (panel2)
            {
                currentInv2 = invName2;
                if (panel2.invBehave.BuySell)
                    buySellPanel = panel2;
                panel2.transform.position = rightPanelPos;
                panel2.Show(IsBuySellPanelAvailable());
                if (panel2.invBehave.CurrencyDisplay)
                    isCurrencyDisplay = true;
            }

            if (currencyPanel)
            {
                if (IsBuySellPanelAvailable())
                    currencyPanel.gameObject.SetActiveOpt(true);
                else if (isCurrencyDisplay)
                    currencyPanel.gameObject.SetActiveOpt(true);
                else
                    currencyPanel.gameObject.SetActiveOpt(false);
            }

            canvas.enabled = true;
            showCanvasFrameNo = ReTime.frameCount;
        }

        public void HideIfCursorClear ()
        {
            if (isCursorInvOccupied)
                return;
            Hide();
        }

        public void Hide ()
        {
            if (!isShowing)
                return;
            if (showCanvasFrameNo >= ReTime.frameCount)
                return;
            for (var i = 0; i < panels.Length; i++)
                if (panels[i].Hide())
                    OnInvClosed?.Invoke(panels[i].invName);
            buySellPanel = null;
            currentInv1 = string.Empty;
            currentInv2 = string.Empty;
            if (currencyPanel)
                currencyPanel.gameObject.SetActiveOpt(false);
            pickInv = string.Empty;
            if (itemTooltip)
                itemTooltip.Hide();
            if (itemPick)
                itemPick.Hide();
            canvas.enabled = false;
        }

        protected override void Awake ()
        {
            base.Awake();
            if (leftPanel)
                leftPanelPos = leftPanel.position;
            if (rightPanel)
                rightPanelPos = rightPanel.position;
            Hide();
        }

        protected void OnDestroy ()
        {
            ClearInstance();
        }

        private void PerformDropCursorItem (Vector3 location)
        {
            if (cursorInvBehaviour == null || string.IsNullOrWhiteSpace(cursorInvBehaviour.Name))
                return;
            var cursorInv = InventoryManager.GetInventory(cursorInvBehaviour.Name);
            var cursorItem = cursorInv?.GetItem(0);
            if (cursorItem is {isSolid: true})
            {
                var cursorInvName = cursorInvBehaviour.Name;
                if (reusableGo == null) { reusableGo = new GameObject("InventoryReusableGo") {hideFlags = HideFlags.HideInHierarchy}; }
                reusableGo.transform.position = location;
                if (DropItem(pickInv, cursorInvName, 0, reusableGo.transform))
                {
                    pickInv = string.Empty;
                    itemTooltip.Hide();
                    itemPick.Hide();
                }
            }
        }

        private void PerformDrop (string charInv, string inv, int index)
        {
            if (IsUnderPickUp())
            {
                InventorySlotItem.UnhighlightHighlighted();
                pickInv = string.Empty;
                itemTooltip.Hide();
                itemPick.Hide();
            }

            DropItem(charInv, inv, index, null);
        }

        private bool DropItem (string charInv, string inv, int index, Transform location)
        {
            var unit = CharacterOperator.GetWithInventory(charInv, true);
            if (unit != null)
            {
                var loc = unit.GetDropPoint(location);
                if (loc != null)
                {
                    var dropped = InventoryManager.Discard(inv, index);
                    if (dropped is {Count: > 0})
                    {
                        var go = TakeFromPool(GraphManager.instance.runtimeSettings.dropInvGo, loc.position, loc.rotation, true);
                        LootController.Generate(go, dropped);
                        return true;
                    }
                }
                else
                    OnWarning?.Invoke(DROP_NOT_SPACE);
            }

            return false;
        }

        private void PerformDiscard (string inv, int index)
        {
            if (IsUnderPickUp())
            {
                InventorySlotItem.UnhighlightHighlighted();
                pickInv = string.Empty;
                itemTooltip.Hide();
                itemPick.Hide();
            }

            InventoryManager.Discard(inv, index);
        }

        private bool PerformUse (string inv, int index, MultiTag consume, int quantity)
        {
            if (IsUnderPickUp())
            {
                InventorySlotItem.UnhighlightHighlighted();
                pickInv = string.Empty;
                itemTooltip.Hide();
                itemPick.Hide();
            }

            return InventoryManager.Use(inv, index, consume, quantity);
        }

        private void PerformUnlock (string invName)
        {
            OnInvUnlockRequested?.Invoke(invName);
        }

        private InventoryPanel GetPanel (string invName)
        {
            for (var i = 0; i < panels.Length; i++)
                if (panels[i].invName == invName)
                    return panels[i];
            return null;
        }

        private bool IsBuySellItem (string invName)
        {
            InventoryPanel panel = null;
            for (var i = 0; i < panels.Length; i++)
            {
                if (!string.IsNullOrEmpty(panels[i].invName))
                {
                    if (panels[i].invName == invName)
                    {
                        panel = panels[i];
                        break;
                    }
                }
            }

            return panel != null && panel.invBehave.BuySell;
        }
    }
}