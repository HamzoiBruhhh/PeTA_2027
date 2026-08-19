using Reshape.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class InventoryItemContext : BaseBehaviour
    {
        public Canvas parentCanvas;
        public Button useButton;
        public Button discardButton;
        public NumberVariable showVariable;

        private InventoryBehaviour itemInvBehave;
        private int itemInvIndex;

        public void Show (InventoryBehaviour invBehave, int index, bool use = false, bool discard = false)
        {
            if (use || discard)
            {
                itemInvBehave = invBehave;
                itemInvIndex = index;
                useButton.gameObject.SetActiveOpt(use);
                discardButton.gameObject.SetActiveOpt(discard);
                SetPositionToCursor();
                gameObject.SetActiveOpt(true);
            }
        }
        
        public void Hide ()
        {
            showVariable.SetValue(0);
            gameObject.SetActiveOpt(false);
        }
        
        public void OnClickUse ()
        {
            InventoryCanvas.Use(itemInvBehave.Name, itemInvIndex, itemInvBehave.Consume, 1);
            Hide();
        }
        
        public void OnClickDrop ()
        {
            InventoryCanvas.Drop(itemInvBehave.Name, itemInvIndex);
            Hide();
        }
        
        public void OnClickDiscard ()
        {
            InventoryCanvas.Discard(itemInvBehave.Name, itemInvIndex);
            Hide();
        }

        protected override void Start ()
        {
            Hide();
        }
        
        private void SetPositionToCursor ()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, ReInput.mousePosition, parentCanvas.worldCamera, out var movePos);
            transform.position = parentCanvas.transform.TransformPoint(movePos);
        }
    }
}