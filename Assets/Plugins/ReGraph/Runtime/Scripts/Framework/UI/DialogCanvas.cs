using System;
using System.Collections;
using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Reshape.Unity;
using TMPro;
using UnityEngine.EventSystems;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class DialogCanvas : ReSingletonBehaviour<DialogCanvas>
    {
        private static bool isDialogProceed;
        private static List<int> dialogProceedException;
        private static PointerEventData eventDataCurrentPosition;
        private static List<RaycastResult> cachedRaycastResult;

        public GameObject messagePanel;
        public GameObject actorPanel;
        public GameObject textPanel;
        public TMP_Text actorLabel;
        public TMP_Text textLabel;
        public Image actorImage;
        public GameObject nextIndicator;
#if ENABLE_INPUT_SYSTEM
        public InputActionAsset inputAction;
        public bool autoManageInput;
#endif
        [ValueDropdown("ParamProceedKeyChoice", ExpandAllMenuItems = false, AppendNextDrawer = true)]
        public string dialogProceedKey;
        [LabelText("Input Delay")]
        [Indent]
        [ShowIf("@string.IsNullOrEmpty(dialogProceedKey) == false")]
        [SuffixLabel("sec", true)]
        public float dialogProceedInputDelay;

        public GameObject choicePanel;
        public GameObject[] choiceButtons;
        public TMP_Text[] choiceLabels;

        public delegate void CommonDelegate ();

        public event CommonDelegate onKeyDialogProceed;

        private int chosenChoice;
        private InputAction dialogProceedAction;
        private GraphRunner messageGraph;
        private GraphRunner choiceGraph;
        private AnimatorEventHandler messagePanelAnim;
        private AnimatorEventHandler choicePanelAnim;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static bool haveDialogProceed => isDialogProceed;
        public static float proceedInputDelay => instance ? instance.dialogProceedInputDelay : 0f;
        
        public static void AddDialogExceptObject (int id)
        {
            dialogProceedException ??= new List<int>();
            if (!dialogProceedException.Contains(id))
                dialogProceedException.Add(id);
        }

        public static bool IsMouseOnDialogExceptionUI ()
        {
            if (EventSystem.current && dialogProceedException != null)
            {
                eventDataCurrentPosition ??= new PointerEventData(EventSystem.current);
                cachedRaycastResult ??= new List<RaycastResult>();
                eventDataCurrentPosition.position = new Vector2(ReInput.mousePosition.x, ReInput.mousePosition.y);
                EventSystem.current.RaycastAll(eventDataCurrentPosition, cachedRaycastResult);
                var totalResults = cachedRaycastResult.Count;
                for (var i = 0; i < totalResults; i++)
                {
                    if (dialogProceedException.Contains(cachedRaycastResult[i].gameObject.GetInstanceID()))
                        return true;
                }
            }

            return false;
        }

        public static void ResetDialogProceed ()
        {
            isDialogProceed = false;
        }

        public static void MarkDialogProceed ()
        {
            isDialogProceed = true;
        }

        public static string GetChosenChoice ()
        {
            if (instance == null || instance.chosenChoice < 0)
                return string.Empty;
            return instance.choiceLabels[instance.chosenChoice].text;
        }

        public static void HidePanel ()
        {
            if (instance == null)
                return;
            instance.HideMessage();
            instance.HideChoice();

#if ENABLE_INPUT_SYSTEM
            if (instance.autoManageInput)
                if (instance.inputAction.enabled)
                    instance.inputAction.Disable();
#endif
        }

        public static bool IsPanelHide ()
        {
            if (instance == null)
                return false;
            return !instance.messagePanel.activeSelf;
        }

        public static bool IsPanelShowReady ()
        {
            if (instance == null)
                return false;
            return instance.IsShowReady();
        }

        public static void ShowMessagePanel (string actor, string message, Sprite avatar, bool haveContinue)
        {
            if (instance == null)
                return;
            instance.ShowMessage(actor, message, avatar, haveContinue);
        }
        
        public static void ShowMessagePanel (CharacterOperator character, string message, bool haveContinue)
        {
            if (instance == null)
                return;
            instance.ShowMessage(character, message, haveContinue);
        }

        public static void ShowChoicePanel (List<string> choices)
        {
            instance.chosenChoice = -1;
            for (int i = 0; i < instance.choiceButtons.Length; i++)
                instance.choiceButtons[i].SetActiveOpt(false);
            for (int i = 0; i < choices.Count; i++)
            {
                if (i < instance.choiceButtons.Length)
                {
                    instance.choiceButtons[i].SetActiveOpt(true);
                    instance.choiceLabels[i].text = choices[i];
                }
                else
                {
                    break;
                }
            }

            instance.ShowChoice();
            instance.nextIndicator.SetActiveOpt(false);
        }

        public static void HideChoicePanel ()
        {
            instance.HideChoice();
        }
        
        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void OnClickChoice (int choiceIndex)
        {
            chosenChoice = choiceIndex;
        }
        
        public bool IsShowReady ()
        {
            if (messagePanelAnim != null)
                return messagePanelAnim.showReady;
            return true;
        }
        
        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        protected virtual void ShowMessage (CharacterOperator character, string message, bool haveContinue)
        {
            if (character)
                ShowMessage(character.GetDisplayName(), message, character.GetDisplayAvatar(), haveContinue);
            else
                ShowMessage(string.Empty, message, null, haveContinue);
        }
        
        protected virtual void ShowMessage (string actor, string message, Sprite avatar, bool haveContinue)
        {
            messagePanel.SetActiveOpt(true);
            if (actorLabel != null)
                actorLabel.text = actor;
            textLabel.text = message;
            if (actorImage != null)
            {
                if (avatar == null)
                {
                    actorImage.enabled = false;
                    actorImage.sprite = null;
                }
                else
                {
                    actorImage.sprite = avatar;
                    actorImage.enabled = true;
                }
            }
            actorPanel.SetActiveOpt(false);
            if (!string.IsNullOrEmpty(actorLabel.text))
                actorPanel.SetActiveOpt(true);
            textPanel.SetActiveOpt(true);
            nextIndicator.SetActiveOpt(haveContinue);

#if ENABLE_INPUT_SYSTEM
            if (autoManageInput)
                if (!inputAction.enabled)
                    inputAction.Enable();
#endif
        }

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected override void Awake ()
        {
            messagePanel.TryGetComponent(out messageGraph);
            choicePanel.TryGetComponent(out choiceGraph);
            messagePanel.TryGetComponent(out messagePanelAnim);
            choicePanel.TryGetComponent(out choicePanelAnim);
            messagePanel.SetActiveOpt(false);
            choicePanel.SetActiveOpt(false);
            base.Awake();
        }

        protected void Start ()
        {
#if ENABLE_INPUT_SYSTEM
            if (inputAction != null)
            {
                dialogProceedAction = inputAction.FindAction(dialogProceedKey);
                if (dialogProceedAction != null)
                    dialogProceedAction.performed += OnKeyDialogProceed;
            }
#endif
        }

        protected void OnDestroy ()
        {
#if ENABLE_INPUT_SYSTEM
            if (inputAction != null && dialogProceedAction != null)
                dialogProceedAction.performed -= OnKeyDialogProceed;
#endif
            ClearInstance();
        }
        
        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void HideChoice ()
        {
            if (choiceGraph == null)
                choicePanel.SetActiveOpt(false);
            else if (choicePanel.activeSelf)
            {
                var result = choiceGraph.TriggerDeactivate();
                if (result == null || result.isFailed)
                    choicePanel.SetActiveOpt(false);
            }
        }
        
        private void ShowChoice ()
        {
            if (choiceGraph == null)
                choicePanel.SetActiveOpt(true);
            else if (!choicePanel.activeSelf)
                choicePanel.SetActiveOpt(true);
            else
            {
                var result = choiceGraph.TriggerActivate();
                if (result == null || result.isFailed)
                    choicePanel.SetActiveOpt(true);
            }
        }

        private void HideMessage ()
        {
            if (messageGraph == null)
                messagePanel.SetActiveOpt(false);
            else if (messagePanel.activeSelf)
            {
                var result = messageGraph.TriggerDeactivate();
                if (result == null || result.isFailed)
                    messagePanel.SetActiveOpt(false);
            }
        }

        private void OnKeyDialogProceed (InputAction.CallbackContext content)
        {
            onKeyDialogProceed?.Invoke();
        }
        
        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
        
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
        private IEnumerable ParamProceedKeyChoice ()
        {
            ValueDropdownList<string> menu = new ValueDropdownList<string>();
            if (inputAction != null)
            {
                for (int i = 0; i < inputAction.actionMaps.Count; i++)
                {
                    string mapName = inputAction.actionMaps[i].name;
                    for (int j = 0; j < inputAction.actionMaps[i].actions.Count; j++)
                    {
                        menu.Add(mapName + "//" + inputAction.actionMaps[i].actions[j].name, inputAction.actionMaps[i].actions[j].name);
                    }
                }
            }

            return menu;
        }
#endif
    }
}