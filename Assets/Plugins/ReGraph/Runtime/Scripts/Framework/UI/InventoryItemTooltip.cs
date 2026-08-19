using Reshape.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class InventoryItemTooltip : BaseBehaviour
    {
        public Canvas parentCanvas;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        [LabelText("BuySell Label")]
        public TMP_Text extraLabel;
        [LabelText("Durable Label")]
        public TMP_Text extra2Label;

        public void ShowItemInfo (ItemData itemData, InventoryItem slotData)
        {
            nameLabel.text = itemData.displayName;
            descriptionLabel.text = itemData.description;
            extra2Label.text = $"Durable : {slotData.Decay.ToString()}";
            extra2Label.gameObject.SetActiveOpt(slotData.Decay >= 0);
            SetPositionToCursor();
            gameObject.SetActiveOpt(true);
            extraLabel.gameObject.SetActiveOpt(true);
            extraLabel.gameObject.SetActiveOpt(false);
        }
            
        public void ShowBuyInfo (ItemData itemData, InventoryItem slotData)
        {
            var totalCost = itemData.cost * slotData.Quantity;
            extraLabel.text = $"Buy Price : {totalCost.ToString()}";
            extraLabel.gameObject.SetActiveOpt(true);
        }

        public void ShowSellInfo (ItemData itemData, InventoryItem slotData)
        {
            var totalCost = itemData.cost * slotData.Quantity;
            extraLabel.text = $"Sell Price : {totalCost.ToString()}";
            extraLabel.gameObject.SetActiveOpt(true);
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

        protected override void Start ()
        {
            Hide();
        }
        
        protected void Update ()
        {
            if (InventoryCanvas.IsUnderPickUp())
                SetPositionToCursor();
        }
    }
}