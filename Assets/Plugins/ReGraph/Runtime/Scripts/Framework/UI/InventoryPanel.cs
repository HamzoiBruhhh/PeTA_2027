using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using TMPro;
using Reshape.Unity;
using UnityEngine.Events;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class InventoryPanel : BaseBehaviour
    {
        public InventoryBehaviour invBehave;
        public Toggle useToggle;
        public Toggle discardToggle;

        [LabelText("On Prepare")]
        public UnityEvent onPrepareAction;

        public string invName => invBehave.Name;

        public void Show (bool underBuySell)
        {
            if (useToggle)
            {
                useToggle.gameObject.SetActiveOpt(!underBuySell);
                useToggle.isOn = false;
            }

            if (discardToggle)
            {
                discardToggle.gameObject.SetActiveOpt(!underBuySell);
                discardToggle.isOn = false;
            }

            gameObject.SetActiveOpt(true);
        }

        public void Prepare ()
        {
            onPrepareAction?.Invoke();
        }

        public void Close ()
        {
            gameObject.SetActiveOpt(false);
            InventoryCanvas.NotifyClosePanel(invBehave.Name);
        }

        public bool Hide ()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActiveOpt(false);
                return true;
            }

            return false;
        }

        protected override void Start ()
        {
            if (useToggle != null)
                useToggle.onValueChanged.AddListener(delegate { OnUseChanged(useToggle); });
            if (discardToggle != null)
                discardToggle.onValueChanged.AddListener(delegate { OnDiscardChanged(discardToggle); });
        }

        protected void OnDestroy ()
        {
            if (useToggle != null)
                useToggle.onValueChanged.RemoveAllListeners();
            if (discardToggle != null)
                discardToggle.onValueChanged.RemoveAllListeners();
        }

        private void OnUseChanged (Toggle change)
        {
            if (change.isOn)
                discardToggle.isOn = false;
        }

        private void OnDiscardChanged (Toggle change)
        {
            if (change.isOn)
                useToggle.isOn = false;
        }
    }
}