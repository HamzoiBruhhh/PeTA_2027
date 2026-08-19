using Reshape.ReGraph;
using Reshape.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class InventoryItemPick : BaseBehaviour
    {
        public Canvas parentCanvas;
        public Image itemIcon;
        public TMP_Text quantityLabel;
        public Image background;
        public Image quantity;
        
        [InlineProperty]
        public FloatProperty scaleGap;
        
        public void ShowPickInfo (ItemData itemData, InventoryItem slotData)
        {
            if (itemData == null || slotData == null)
                return;
            itemIcon.sprite = itemData.icon;
            quantityLabel.text = slotData.Quantity.ToString();
            var sizeX = itemData.size.x;
            var sizeY = itemData.size.y;
            if (itemData.isMultiSlot)
            {
                background.transform.localScale = itemData.size is {x: >= 1, y: >= 1} ? CalculateScale(sizeX, sizeY) : Vector3.one;
                quantity.transform.localScale = new Vector3(sizeX > 1 ? 1f / sizeX : 1, sizeY > 1 ? 1f / sizeY : 1, 1);
                background.pixelsPerUnitMultiplier = Mathf.Max(background.transform.localScale.x, background.transform.localScale.y);
            }
            else if (background.transform.localScale.x > 1 || background.transform.localScale.y > 1)
            {
                background.transform.localScale = Vector3.one;
                quantity.transform.localScale = Vector3.one;
                background.pixelsPerUnitMultiplier = 1;
            }
            
            SetPositionToCursor();
            itemIcon.gameObject.SetActiveOpt(true);
            gameObject.SetActiveOpt(true);
            
            Vector3 CalculateScale (int sizeX, int sizeY)
            {
                var x = sizeX + ((sizeX - 1f) * scaleGap);
                var y = sizeY + ((sizeY - 1f) * scaleGap);
                return new Vector3(x, y, 1f);
            }
        }

        public void Hide ()
        {
            gameObject.SetActiveOpt(false);
        }
        
        private void SetPositionToCursor ()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, ReInput.mousePosition, parentCanvas.worldCamera, out var movePos);
            transform.position = parentCanvas.transform.TransformPoint(movePos);
        }

        protected void Update ()
        {
            SetPositionToCursor();
        }
    }
}