using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    [Serializable]
    [CreateAssetMenu(menuName = "Reshape/Inventory Behaviour", fileName = "InventoryBehaviour", order = 504)]
    public class InventoryBehaviour : BaseScriptable
    {
        public const int DEFAULT_SIZE = 100;
        public const int DEFAULT_STACK = 9999999;

        public enum ClickAction
        {
            None = 0,
            Trade = 10,
            VirtualPick = 50,
            CursorPick = 100,
        }

        public enum ApplyStatusTrigger
        {
            None = 0,
            SlotIn = 10,
            Use = 20
        }
        
        public enum ApplySkillTrigger
        {
            None = 0,
            SlotIn = 10,
            Use = 20
        }

        [Hint("showHints", "Define name of the inventory.")]
        [SerializeField]
        [InlineProperty]
        [LabelText("Name")]
        private StringProperty invName;

        [Hint("showHints", "Define categories of the inventory. Only item with same category allow to put into the inventory.")]
        [LabelText("Tags")]
        public MultiTag tags = new MultiTag("Tags", typeof(MultiTagInv));

        [Hint("showHints", "Define number of inventory slots available.")]
        [SerializeField]
        [ValidateInput("ValidateMoreThan0", "Value must greater than 0!", InfoMessageType.Warning)]
        [InlineProperty]
        private FloatProperty size = new FloatProperty(DEFAULT_SIZE);

        [Hint("showHints", "Define the inventory have support item stacking.")]
        [SerializeField]
        [ValidateInput("ValidateMoreThan0", "Value must greater than 0!", InfoMessageType.Warning)]
        [InlineProperty]
        private FloatProperty stack = new FloatProperty(DEFAULT_STACK);

        [Hint("showHints", "Define the inventory slots have 2 dimension layout.")]
        [SerializeField]
        [ValidateInput("ValidateNotNegative", "Value must not be negative!", InfoMessageType.Warning)]
        [InlineProperty]
        [LabelText("# Per Row")]
        private FloatProperty rows = new FloatProperty(0);

        [Hint("showHints", "Define the inventory have size limit, item size above the limit is not allow to slot in.")]
        [SerializeField]
        private Vector2Int sizeLimit;
        
        [Hint("showHints", "Define the inventory have count limit, inventory not able to store items more than this number.")]
        [SerializeField]
        private int countLimit;
        
        [Hint("showHints", "Define the inventory have weight limit, inventory not able to store items exceed this number.")]
        [SerializeField]
        private int weightLimit;
        
        [Hint("showHints", "Define the execute action when the item being clicked in Inventory UI.")]
        [SerializeField]
        private ClickAction clickAction;

        [Hint("showHints", "Define what action could trigger the status apply to inventory owner.")]
        [SerializeField]
        private ApplyStatusTrigger applyStatusTrigger;
        
        [Hint("showHints", "Define what action could trigger the skill apply to inventory owner.")]
        [SerializeField]
        private ApplySkillTrigger applySkillTrigger;
        
        [Hint("showHints", "Define item in which category can be consume in the inventory.")]
        [SerializeField]
        [LabelText("Consume")]
        private MultiTag consume = new MultiTag("Consume", typeof(MultiTagInv));
        
        [Hint("showHints", "Define item in the inventory can be discard.")]
        [SerializeField]
        private bool discard;
        
        [Hint("showHints", "Define the inventory require currency as cost to do item transfer.")]
        [SerializeField]
        [LabelText("Treat as Buy/Sell")]
        private bool buySell;

        [Hint("showHints", "Define what item is use as currency for item transfer.")]
        [SerializeField]
        [ShowIf("@buySell == true")]
        [ValueDropdown("ItemChoice")]
        private ItemData currency;

        [Hint("showHints", "Define the inventory is restrict to put in item through Inventory UI.")]
        [SerializeField]
        private bool restrictAdd;
        
        [Hint("showHints", "Define the inventory require currency UI to be display together.")]
        [SerializeField]
        [LabelText("Show Currency UI")]
        private bool currencyDisplay;
        
        [Hint("showHints", "Define the inventory will only display as one single slot.")]
        [SerializeField]
        [LabelText("Show Single Slot")]
        private bool singleDisplay;
        
        public string Name => invName;
        public MultiTag Tags => tags;
        public int Size => size;
        public int Stack => stack;
        public int Rows => rows;
        public Vector2Int SizeLimit => sizeLimit;
        public int CountLimit => countLimit;
        public int WeightLimit => weightLimit;
        public ApplyStatusTrigger ApplyStatusType => applyStatusTrigger;
        public ApplySkillTrigger ApplySkillType => applySkillTrigger;
        public MultiTag Consume => consume;
        public bool Discard => discard;
        public bool BuySell => buySell;
        public ItemData Currency => currency;
        public bool RestrictAdd => restrictAdd;
        public bool CurrencyDisplay => currencyDisplay;
        public bool SingleDisplay => singleDisplay;
        public bool isTradeClickAction => clickAction == ClickAction.Trade;
        public bool isVirtualClickAction => clickAction == ClickAction.VirtualPick;
        public bool isCursorPickClickAction => clickAction == ClickAction.CursorPick;
        public bool isSlotInApplyStatus => applyStatusTrigger == ApplyStatusTrigger.SlotIn;
        public bool isUseApplyStatus => applyStatusTrigger == ApplyStatusTrigger.Use;
        public bool isNoneApplyStatus => applyStatusTrigger == ApplyStatusTrigger.None;
        public bool isSlotInApplySkill => applySkillTrigger == ApplySkillTrigger.SlotIn;
        public bool isUseApplySkill => applySkillTrigger == ApplySkillTrigger.Use;
        public bool isNoneApplySkill => applySkillTrigger == ApplySkillTrigger.None;

        public void CopyTo (InventoryBehaviour invBehaviour2)
        {
            invBehaviour2.invName = invName;
            invBehaviour2.tags = tags;
            invBehaviour2.size = size;
            invBehaviour2.stack = stack;
            invBehaviour2.rows = rows;
            invBehaviour2.sizeLimit = sizeLimit;
            invBehaviour2.countLimit = countLimit;
            invBehaviour2.clickAction = clickAction;
            invBehaviour2.applyStatusTrigger = applyStatusTrigger;
            invBehaviour2.applySkillTrigger = applySkillTrigger;
            invBehaviour2.consume = consume;
            invBehaviour2.discard = discard;
            invBehaviour2.buySell = buySell;
            invBehaviour2.currency = currency;
            invBehaviour2.restrictAdd = restrictAdd;
            invBehaviour2.currencyDisplay = currencyDisplay;
        }
        
        public void SetCurrencyDisplay (bool value)
        {
            currencyDisplay = value;
        }
        
#if UNITY_EDITOR
        private bool ValidateMoreThan0 (int value)
        {
            return value > 0f && value < int.MaxValue - 1;
        }

        private bool ValidateNotNegative (int value)
        {
            return value >= 0f;
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
                    if (tempItem != null)
                        itemListDropdown.Add(itemList.name + "/" + tempItem.displayName, tempItem);
                }
            }

            return itemListDropdown;
        }
#endif
    }
}