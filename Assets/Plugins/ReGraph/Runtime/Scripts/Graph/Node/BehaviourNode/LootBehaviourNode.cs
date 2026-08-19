using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class LootBehaviourNode : BehaviourNode
    {
        public enum ExecutionType
        {
            None,
            Create = 10,
            GetId = 20,
            PickUp = 50,
            Equip = 51,
            Show = 100,
            ObtainedCount = 1000,
            ObtainedItemId = 1001,
            ObtainedQuantity = 1002,
            ObtainedCharacter = 1003,
            ObtainedItemName = 1004,
        }

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.Create")]
        private LootPack lootPack;

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.Create")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(gameObject)")]
        [InlineButton("@gameObject.SetObjectValue(AssignGameObject())", "♺", ShowIf = "@gameObject.IsObjectValueType()")]
        [InfoBox("@gameObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@gameObject.IsShowMismatchWarning()")]
        private SceneObjectProperty gameObject = new SceneObjectProperty(SceneObject.ObjectType.GameObject, "Loot Prefab");

        [SerializeField]
        [ShowIf("@executionType == ExecutionType.Create")]
        [HideLabel, InlineProperty, OnInspectorGUI("@MarkPropertyDirty(transformObject)")]
        [InlineButton("@transformObject.SetObjectValue(AssignComponent<UnityEngine.Transform>())", "♺", ShowIf = "@transformObject.IsObjectValueType()")]
        [InfoBox("@transformObject.GetMismatchWarningMessage()", InfoMessageType.Error, "@transformObject.IsShowMismatchWarning()")]
        private SceneObjectProperty transformObject = new SceneObjectProperty(SceneObject.ObjectType.Transform, "Spawn Transform");

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(paramString)")]
        [InlineProperty]
        [ShowIf("@executionType == ExecutionType.PickUp || executionType == ExecutionType.Equip")]
        [LabelText("Inv Name")]
        private StringProperty paramString;

        [SerializeField]
        [OnInspectorGUI("@MarkPropertyDirty(paramFloat)")]
        [InlineProperty]
        [ShowIf("@executionType == ExecutionType.ObtainedItemId || executionType == ExecutionType.ObtainedQuantity || executionType == ExecutionType.ObtainedCharacter || executionType == ExecutionType.ObtainedItemName")]
        [LabelText("Obtained Index")]
        private FloatProperty paramFloat;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.PickUp || executionType == ExecutionType.Equip || executionType == ExecutionType.Show || executionType == ExecutionType.GetId")]
        private LootController lootController;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.GetId || executionType == ExecutionType.ObtainedItemId || executionType == ExecutionType.ObtainedItemName")]
        [LabelText("Variable")]
        private WordVariable paramWord1;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ShowIf("@executionType == ExecutionType.ObtainedCount || executionType == ExecutionType.ObtainedQuantity || executionType == ExecutionType.PickUp || executionType == ExecutionType.Equip")]
        [LabelText("@ParamNumber1Label()")]
        private NumberVariable paramNumber1;

        [ShowIf("@executionType == ExecutionType.Create || executionType == ExecutionType.ObtainedCharacter")]
        [LabelText("Character Store To")]
        [OnValueChanged("MarkDirty")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        public SceneObjectVariable objectVariable;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType is ExecutionType.None)
            {
                LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
            }
            else if (executionType is ExecutionType.Create)
            {
                if (!lootPack || gameObject.IsEmpty || !gameObject.IsMatchType())
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var loc = context.transform;
                    if (!transformObject.IsEmpty && transformObject.IsMatchType())
                        loc = (Transform) transformObject;
                    var go = context.runner.TakeFromPool(gameObject, loc, true);
                    var controller = LootController.Generate(go, lootPack);
                    if (controller)
                    {
                        if (controller.gameObject.TryGetComponent(out CharacterOperator character))
                        {
                            if (objectVariable)
                            {
                                objectVariable.Rebase();
                                objectVariable.SetValue(character);
                            }
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.PickUp)
            {
                if (!paramString.IsAssigned() || !lootController)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var result = false;
                    if (!string.IsNullOrEmpty(paramString))
                        result = lootController.PickUp(paramString, out _);
                    if (paramNumber1)
                        paramNumber1.SetValue(result ? 1 : 0);
                }
            }
            else if (executionType is ExecutionType.Equip)
            {
                if (!paramString.IsAssigned() || !lootController)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var result = false;
                    if (!string.IsNullOrEmpty(paramString))
                        result = lootController.Equip(paramString, out _);
                    if (paramNumber1)
                        paramNumber1.SetValue(result ? 1 : 0);
                }
            }
            else if (executionType is ExecutionType.Show)
            {
                if (!lootController)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    lootController.Show();
                }
            }
            else if (executionType is ExecutionType.GetId)
            {
                if (!paramWord1 || !lootController)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    paramWord1.SetValue(lootController.lootId);
                }
            }
            else if (executionType is ExecutionType.ObtainedCount)
            {
                if (!paramNumber1)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    paramNumber1.SetValue(LootManager.GetObtainedCount());
                }
            }
            else if (executionType is ExecutionType.ObtainedItemId)
            {
                if (paramFloat < 0 || !paramWord1)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var info = LootManager.GetObtainedInfo(paramFloat);
                    paramWord1.Rebase();
                    if (!string.IsNullOrEmpty(info.itemId))
                        paramWord1.SetValue(info.itemId);
                }
            }
            else if (executionType is ExecutionType.ObtainedItemName)
            {
                if (paramFloat < 0 || !paramWord1)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var info = LootManager.GetObtainedInfo(paramFloat);
                    paramWord1.Rebase();
                    if (!string.IsNullOrEmpty(info.itemId))
                        paramWord1.SetValue(info.itemName);
                }
            }
            else if (executionType is ExecutionType.ObtainedQuantity)
            {
                if (paramFloat < 0 || !paramNumber1)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var info = LootManager.GetObtainedInfo(paramFloat);
                    paramNumber1.Rebase();
                    if (!string.IsNullOrEmpty(info.itemId) && info.quantity > 0)
                        paramNumber1.SetValue(info.quantity);
                }
            }
            else if (executionType is ExecutionType.ObtainedCharacter)
            {
                if (paramFloat < 0 || !objectVariable)
                {
                    LogWarning("Found an empty Loot Behaviour node in " + context.objectName);
                }
                else
                {
                    var info = LootManager.GetObtainedInfo(paramFloat);
                    objectVariable.Rebase();
                    if (!string.IsNullOrEmpty(info.itemId) && info.quantity > 0 && info.character)
                        objectVariable.SetValue(info.character);
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private string ParamNumber1Label ()
        {
            if (executionType is ExecutionType.ObtainedCount or ExecutionType.ObtainedQuantity)
                return "Variable";
            if (executionType is ExecutionType.PickUp or ExecutionType.Equip)
                return "Result";
            return string.Empty;
        }
            
        private bool ShowObjectVariableWarning ()
        {
            if (objectVariable != null)
                if (objectVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                    return true;
            return false;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Create", ExecutionType.Create},
            {"Get Id", ExecutionType.GetId},
            {"Pick Up", ExecutionType.PickUp},
            {"Equip", ExecutionType.Equip},
            {"Show", ExecutionType.Show},
            {"Obtained Count", ExecutionType.ObtainedCount},
            {"Obtained Id", ExecutionType.ObtainedItemId},
            {"Obtained Name", ExecutionType.ObtainedItemName},
            {"Obtained Quantity", ExecutionType.ObtainedQuantity},
            {"Obtained Character", ExecutionType.ObtainedCharacter},
        };

        public static string displayName = "Loot Behaviour Node";
        public static string nodeName = "Loot";

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
            if (executionType is ExecutionType.None)
                return string.Empty;
            string message = "";
            if (executionType is ExecutionType.Create && lootPack && !gameObject.IsNull && gameObject.IsMatchType())
                message = $"Create {gameObject.objectName} to drop {lootPack.name}";
            else if (executionType is ExecutionType.PickUp && paramString.IsAssigned() && lootController)
                message = "Pick up loot drop";
            else if (executionType is ExecutionType.Equip && paramString.IsAssigned() && lootController)
                message = "Equip loot item";
            else if (executionType is ExecutionType.Show && lootController)
                message = "Show loot drop UI";
            else if (executionType == ExecutionType.GetId && lootController && lootController && paramWord1)
                message = "Get loot drop Id";
            else if (executionType is ExecutionType.ObtainedCount && paramNumber1)
                message = "Get obtained loot count";
            else if (executionType is ExecutionType.ObtainedItemId && paramFloat.IsAssigned() && paramWord1)
                message = "Get obtained item id";
            else if (executionType is ExecutionType.ObtainedItemName && paramFloat.IsAssigned() && paramWord1)
                message = "Get obtained item name";
            else if (executionType is ExecutionType.ObtainedQuantity && paramFloat.IsAssigned() && paramNumber1)
                message = "Get obtained item quantity";
            else if (executionType is ExecutionType.ObtainedCharacter && paramFloat.IsAssigned() && paramWord1)
                if (objectVariable && objectVariable.sceneObject.type == SceneObject.ObjectType.CharacterOperator)
                    message = "Get obtained loot character";
            return message;
        }

        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.PickUp)
                tip += "This will put loot items into the defined inventory.\n\n";
            if (executionType == ExecutionType.Equip)
                tip += "This will put the only 1 loot item into the defined inventory.\n\n";
            else if (executionType == ExecutionType.Create)
                tip += "This will create a loot gameObject and spawn it on the scene.\n\n";
            else if (executionType == ExecutionType.Show)
                tip += "This will open the loot drop UI.\n\n";
            else if (executionType == ExecutionType.GetId)
                tip += "This will store the loot id into defined variable.\n\n";
            else if (executionType == ExecutionType.ObtainedCount)
                tip += "This will get the total count of obtained loot items.\n\n";
            else if (executionType == ExecutionType.ObtainedItemId)
                tip += "This will get the specific obtained loot item id.\n\n";
            else if (executionType == ExecutionType.ObtainedItemName)
                tip += "This will get the specific obtained loot item name.\n\n";
            else if (executionType == ExecutionType.ObtainedQuantity)
                tip += "This will get the specific obtained loot item quantity.\n\n";
            else if (executionType == ExecutionType.ObtainedCharacter)
                tip += "This will get character owned the specific obtained loot item.\n\n";
            else
                tip += "This will provide functionality for Loot Drop.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}