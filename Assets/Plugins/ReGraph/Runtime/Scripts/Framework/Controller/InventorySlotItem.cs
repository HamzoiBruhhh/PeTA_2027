using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using Reshape.ReGraph;
using Reshape.Unity;
using UnityEngine.EventSystems;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class InventorySlotItem : BaseBehaviour, IPointerMoveHandler, IPointerDownHandler
    {
        private static InventorySlotItem highlighted;
        private static List<InventorySlotItem> list;
        private static List<InventorySlotItem> statusList;

        [InlineProperty]
        public StringProperty itemId;

        public InventoryBehaviour behaviour;

        public InventoryPanel panel;
        public Image background;
        public Image icon;
        public Image quantity;
        public Image status;
        public Color statusAlert;
        public Color statusWarning;
        public Color statusGood;
        public TMP_Text nameLabel;
        public TMP_Text descLabel;
        public Color highlight;
        public Sprite highlightSprite;
        public bool pickedToolTip;

        [InlineProperty]
        public FloatProperty scaleGap;

        [LabelText("Scale Quantity")]
        public bool scaleQuantityLabel;

        [LabelText("Hide Multislot Background")]
        public bool hideMultiSlotBackground;
        
        [LabelText("Background Pixel Multiply")]
        public bool slotBackgroundPixelMultiply = true;

        private int slotIndex;
        private Color originBackgroundColor;
        private Sprite originalBackgroundSprite;
        private string updateItemId;
        private bool firstUpdate;
        private Vector2 pointerDownPos;

        private delegate void UpdateDelegate ();

        private UpdateDelegate updateDelegate;

        public static void UnhighlightHighlighted ()
        {
            if (highlighted)
                highlighted.SetBackgroundToOriginal();
        }

        private static void UpdateSlotSizeDisplay (InventorySlotItem slot, int sizeX, int sizeY)
        {
            var inv = InventoryManager.GetInventory(slot.behaviour.Name);
            if (inv is {PerRow: > 0})
            {
                if (sizeX == 1 && sizeY == 1)
                {
                    var slotScale = slot.transform.localScale;
                    var dim = inv.GetSlotDimension(slot.slotIndex, (int) slotScale.x, (int) slotScale.y);
                    var affectedSlot = inv.GetMultiSlotIndexes(dim, false, true);
                    if (list != null)
                    {
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i].behaviour.Name.Equals(slot.behaviour.Name))
                            {
                                for (var j = 0; j < affectedSlot.Count; j++)
                                {
                                    if (list[i].slotIndex == affectedSlot[j])
                                    {
                                        list[i].ShowSlotDisplay();
                                        affectedSlot.RemoveAt(j);
                                        break;
                                    }
                                }
                            }

                            if (affectedSlot.Count <= 0)
                                break;
                        }
                    }
                }
                else
                {
                    var affectedSlot = inv.GetMultiSlotIndexes(slot.slotIndex, sizeX, sizeY, false, true);
                    if (list != null)
                    {
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i].behaviour.Name.Equals(slot.behaviour.Name))
                            {
                                for (var j = 0; j < affectedSlot.Count; j++)
                                {
                                    if (list[i].slotIndex == affectedSlot[j])
                                    {
                                        list[i].HideSlotDisplay();
                                        affectedSlot.RemoveAt(j);
                                        break;
                                    }
                                }
                            }

                            if (affectedSlot.Count <= 0)
                                break;
                        }
                    }
                }
            }
        }
        
        private static void UpdateSlotStatusDisplay (InventorySlotItem slot, int sizeX, int sizeY, int status)
        {
            statusList ??= new List<InventorySlotItem>();
            if (slot == null)
            {
                HideShowed();
            }
            else
            {
                HideShowed();
                var inv = InventoryManager.GetInventory(slot.behaviour.Name);
                if (inv is {PerRow: > 0})
                {
                    var slotIndex = slot.CalcClickedIndex();
                    var dim = inv.GetSlotDimension(slotIndex, sizeX, sizeY);
                    var affectedSlot = inv.GetMultiSlotIndexes(dim, true, false);
                    if (list != null)
                    {
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i].behaviour.Name.Equals(slot.behaviour.Name))
                            {
                                for (var j = 0; j < affectedSlot.Count; j++)
                                {
                                    if (list[i].slotIndex == affectedSlot[j])
                                    {
                                        list[i].ShowSlotStatus(status);
                                        statusList.Add(list[i]);
                                        affectedSlot.RemoveAt(j);
                                        break;
                                    }
                                }
                            }

                            if (affectedSlot.Count <= 0)
                                break;
                        }
                    }
                }
            }

            void HideShowed ()
            {
                for (var i = 0; i < statusList.Count; i++)
                    statusList[i].HideSlotStatus();
                statusList.Clear();
            }
        }  

        public void ClickSlot ()
        {
            if (panel.discardToggle != null && panel.discardToggle.isOn)
            {
                InventoryCanvas.Discard(behaviour.Name, slotIndex);
            }

            if (panel.useToggle != null && panel.useToggle.isOn)
            {
                InventoryCanvas.Use(behaviour.Name, slotIndex, behaviour.Consume, 1);
            }
            else
            {
                if (behaviour.isTradeClickAction)
                {
                    InventoryCanvas.Trade(behaviour.Name, slotIndex, behaviour.isSlotInApplyStatus, behaviour.isSlotInApplySkill);
                }
                else if (behaviour.isVirtualClickAction)
                {
                    if (InventoryCanvas.VirtualPick(behaviour.Name, slotIndex, behaviour.isSlotInApplyStatus, behaviour.isSlotInApplySkill))
                    {
                        background.color = highlight;
                        background.sprite = highlightSprite;
                        highlighted = this;
                    }
                }
                else if (behaviour.isCursorPickClickAction)
                {
                    if (InventoryCanvas.CursorPick(behaviour.Name, slotIndex, CalcClickedIndex(), behaviour.isSlotInApplyStatus, behaviour.isSlotInApplySkill, pickedToolTip) > 0)
                    {
                        HideContext();
                        ShowStatusDisplay();
                    }
                }
            }
        }

        public void ClickUnlock ()
        {
            InventoryCanvas.Unlock(behaviour.Name);
        }

        public void UpdateSlotIndex (NumberVariable number)
        {
            slotIndex = number;
        }

        public void UpdateToolTip ()
        {
            InventoryCanvas.ShowToolTip(behaviour.Name, slotIndex, pickedToolTip);
            ShowStatusDisplay();
        }

        public void ShowToolTip ()
        {
            InventoryCanvas.ShowToolTip(behaviour.Name, slotIndex, pickedToolTip);
            ShowStatusDisplay();
        }

        public void HideToolTip ()
        {
            if (pickedToolTip)
            {
                if (!InventoryCanvas.IsUnderPickUp())
                    InventoryCanvas.HideToolTip(behaviour.Name, slotIndex);
            }
            else
            {
                InventoryCanvas.HideToolTip(behaviour.Name, slotIndex);
            }

            HideStatusDisplay();
        }

        public void ShowContext ()
        {
            InventoryCanvas.ShowContext(behaviour, slotIndex);
        }

        public void HideContext ()
        {
            InventoryCanvas.HideContext(behaviour, slotIndex);
        }

        public void UpdateIcon ()
        {
            if (itemId != null && icon != null)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(itemId);
                if (itemData != null)
                {
                    icon.sprite = itemData.icon;
                    SetBackgroundToOriginal();
                }
            }
        }

        public void UpdateSize ()
        {
            updateItemId = itemId;
            if (firstUpdate)
            {
                updateDelegate = ProcessUpdateSize;
                firstUpdate = false;
            }
            else
            {
                ProcessUpdateSize();
            }
        }

        public void UpdateBackground ()
        {
            var inv = InventoryManager.GetInventory(behaviour.Name);
            if (inv is {PerRow: > 0})
            {
                var itemData = inv.GetItem(slotIndex);
                if (itemData == null)
                {
                    background.enabled = true;
                }
                else if (itemData.isSolid)
                {
                    background.enabled = true;
                }
                else if (itemData.isEmpty)
                {
                    background.enabled = true;
                }
                else
                {
                    background.enabled = false;
                }
            }
        }

        public void UpdateName ()
        {
            if (nameLabel != null && itemId != null && icon != null)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(itemId);
                if (itemData != null)
                {
                    nameLabel.text = itemData.displayName;
                }
            }
        }

        public void UpdateDescription ()
        {
            if (descLabel != null && itemId != null && icon != null)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(itemId);
                if (itemData != null)
                {
                    descLabel.text = itemData.description;
                }
            }
        }

        public void Reset () { }

        public void OnPointerMove (PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
            pointerDownPos = new Vector2(localPoint.x, localPoint.y * -1);
        }

        public void OnPointerDown (PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
            pointerDownPos = new Vector2(localPoint.x, localPoint.y * -1);
        }
        
        protected void Awake ()
        {
            list ??= new List<InventorySlotItem>();
            list.Add(this);
            status.gameObject.SetActiveOpt(false);
            originBackgroundColor = background.color;
            originalBackgroundSprite = background.sprite;
            firstUpdate = true;
        }

        protected void Update ()
        {
            updateDelegate?.Invoke();
        }

        protected void OnDisable ()
        {
            SetBackgroundToOriginal();
        }

        protected void OnDestroy ()
        {
            if (highlighted == this)
                highlighted = null;
            list?.Remove(this);
            statusList?.Remove(this);
        }

        private void HideSlotDisplay ()
        {
            icon.gameObject.SetActiveOpt(false);
            quantity.gameObject.SetActiveOpt(false);
            background.enabled = false;
        }

        private void ShowSlotDisplay ()
        {
            background.enabled = true;
        }

        private void SetBackgroundToOriginal ()
        {
            background.color = originBackgroundColor;
            background.sprite = originalBackgroundSprite;
        }
        
        private void ShowSlotStatus (int result)
        { 
            status.color = statusGood;
            if (result == 0)
                status.color = statusAlert;
            else if (result == 2)
                status.color = statusWarning;
            status.gameObject.SetActiveOpt(true);
        }

        private int CalcClickedIndex ()
        {
            var offsetX = 0;
            var offsetY = 0;
            var inv = InventoryManager.GetInventory(behaviour.Name);
            if (!behaviour.SingleDisplay && inv is {PerRow: > 0})
            {
                var localScale = transform.localScale;
                offsetX = (int)Mathf.Floor(pointerDownPos.x / (rectTransform.rect.width / localScale.x));
                offsetY = (int)Mathf.Floor(pointerDownPos.y / (rectTransform.rect.height / localScale.y));
                offsetY *= inv.PerRow;
            }

            return slotIndex + offsetX + offsetY;
        }
        
        private void HideSlotStatus ()
        {
            status.gameObject.SetActiveOpt(false);
        }

        private void HideStatusDisplay ()
        {
            UpdateSlotStatusDisplay(null, 0, 0, 0);
        }
        
        private void ShowStatusDisplay ()
        {
            var item = InventoryCanvas.cursorInvItem;
            if (item is {isSolid: true})
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(item.ItemId);
                if (itemData != null)
                {
                    var sizeX = itemData.size.x > 0 ? itemData.size.x : 1;
                    var sizeY = itemData.size.y > 0 ? itemData.size.y : 1;
                    var status = InventoryCanvas.CursorPick(behaviour.Name, slotIndex, CalcClickedIndex(), false, false, false, true);
                    UpdateSlotStatusDisplay(this, sizeX, sizeY, status);
                }
            }
            else
            {
                HideStatusDisplay();
            }
        }

        private void ProcessUpdateSize ()
        {
            updateDelegate = null;
            if (updateItemId != null)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                var itemData = manager.GetItemData(updateItemId);
                if (itemData != null)
                {
                    var sizeX = itemData.size.x;
                    var sizeY = itemData.size.y;
                    if (itemData.isMultiSlot)
                    {
                        transform.localScale = itemData.size is {x: >= 1, y: >= 1} ? CalculateScale(sizeX, sizeY) : Vector3.one;
                        status.transform.localScale = new Vector3(sizeX > 1 ? 1f / sizeX : 1, sizeY > 1 ? 1f / sizeY : 1, 1);
                        if (scaleQuantityLabel)
                            quantity.transform.localScale = new Vector3(sizeX > 1 ? 1f / sizeX : 1, sizeY > 1 ? 1f / sizeY : 1, 1);
                        if (hideMultiSlotBackground)
                            UpdateSlotSizeDisplay(this, sizeX, sizeY);
                        HandleBackgroundImagePixelMultiplier();
                        return;
                    }
                }

                if (transform.localScale.x > 1 || transform.localScale.y > 1)
                {
                    if (hideMultiSlotBackground)
                        UpdateSlotSizeDisplay(this, 1, 1);
                    transform.localScale = Vector3.one;
                    status.transform.localScale = Vector3.one;
                    if (scaleQuantityLabel)
                        quantity.transform.localScale = Vector3.one;
                    HandleBackgroundImagePixelMultiplier();
                }
            }

            void HandleBackgroundImagePixelMultiplier ()
            {
                if (slotBackgroundPixelMultiply)
                    background.pixelsPerUnitMultiplier = Mathf.Max(transform.localScale.x, transform.localScale.y);
            }

            Vector3 CalculateScale (int sizeX, int sizeY)
            {
                var x = sizeX + ((sizeX - 1f) * scaleGap);
                var y = sizeY + ((sizeY - 1f) * scaleGap);
                return new Vector3(x, y, 1f);
            }
        }
    }
}