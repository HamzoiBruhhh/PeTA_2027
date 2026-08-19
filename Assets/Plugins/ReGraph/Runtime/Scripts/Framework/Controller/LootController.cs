using System.Collections.Generic;
using Reshape.ReGraph;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.Unity;
using UnityEngine.Events;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class LootController : BaseBehaviour
    {
        private static List<LootController> list;

        public enum WhenEmptyBehaviour
        {
            None,
            Remove = 10,
            Event = 50
        }

        [SerializeField]
        [BoxGroup("Loot Info")]
        [InlineProperty]
        private StringProperty lootName;

        [SerializeField]
        [BoxGroup("Loot Info")]
        private WordVariable lootInvVariable;

        [SerializeField]
        [BoxGroup("Loot Info")]
        private GameObject lootModel;

        [ShowInInspector, HideInEditorMode]
        [BoxGroup("Loot Info")]
        public string lootId => lootData == null ? string.Empty : lootData.id;

        [ShowInInspector, HideInEditorMode]
        [BoxGroup("Loot Info")]
        public LootPack lootPack => lootData?.loot;

        [SerializeField]
        [BoxGroup("Behaviour")]
        [LabelText("Remove At Empty")]
        private WhenEmptyBehaviour emptyAction;

        [SerializeField]
        [BoxGroup("Behaviour")]
        [LabelText("Speech Upon Pick")]
        private bool pickSpeech;

        [SerializeField]
        [BoxGroup("Behaviour")]
        [LabelText("Equip Upon Pick")]
        private bool equipSpeech;

        [SerializeField]
        [BoxGroup("Spawn at Start")]
        [LabelText("Loot Pack")]
        private LootPack startLootPack;

        [BoxGroup("Behaviour")]
        [LabelText("Event")]
        [ShowIf("@emptyAction == WhenEmptyBehaviour.Event")]
        public UnityEvent emptyEvent;

        private LootData lootData;

        public static LootController Generate (GameObject lootGo, LootPack lootPack)
        {
            if (lootGo != null)
            {
                LevelManager.InsertAreaObject(lootGo);
                if (!lootGo.TryGetComponent(out LootController controller))
                    controller = lootGo.AddComponent<LootController>();
                controller.Init(new LootData(lootPack));
                return controller;
            }

            return null;
        }

        public static void Generate (GameObject lootGo, List<InventoryItem> loots)
        {
            if (lootGo != null)
            {
                LevelManager.InsertAreaObject(lootGo);
                if (!lootGo.TryGetComponent(out LootController controller))
                    controller = lootGo.AddComponent<LootController>();
                controller.Init(new LootData(loots));
            }
        }

        public static void CleanAll ()
        {
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                    list[i].Clear();
                list.Clear();
            }
        }

        public static bool Contains (string id)
        {
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                    if (list[i].lootId == id)
                        return true;
            return false;
        }

        public void GetLootName (WordVariable word)
        {
            word.SetValue(lootName);
        }

        public void Init (LootData data)
        {
            lootData = data;
            TriggerGenerateUsage();

            if (lootModel)
            {
                if (lootData.GetOccupiedSlotCountInInventory() == 1)
                {
                    var inv = lootData.GetInventory();
                    var item = inv?.GetItem(0);
                    if (item is {isSolid: true})
                    {
                        var manager = GraphManager.instance.runtimeSettings.itemManager;
                        var itemData = manager.GetItemData(item.ItemId);
                        if (itemData != null && itemData.model != null)
                        {
                            var itemModel = Instantiate(itemData.model, lootModel.transform.parent);
                            itemModel.transform.localPosition = Vector3.zero;
                            if (TryGetComponent<CharacterOperator>(out var co))
                                co.FeedbackChangeModel(lootModel, itemModel);
                            DestroyImmediate(lootModel);
                        }
                    }
                }
            }
        }

        public bool Equip (string inv, out InventoryItem equippedItem)
        {
            var result = false;
            if (lootData.PutItemIntoInventory(inv, 0, out equippedItem))
            {
                OnInventoryEmpty();
                result = true;
                if (equipSpeech)
                {
                    var unit = CharacterOperator.GetWithInventory(inv, true);
                    if (unit)
                    {
                        var message = GetItemNameQuantityList(new List<InventoryItem> {equippedItem});
                        if (!string.IsNullOrEmpty(message))
                            SpeechOwnerController.Show(unit, null, message, true);
                    }
                }
            }

            return result;
        }

        public bool PickUp (string inv, out List<InventoryItem> pickedItems)
        {
            var result = false;
            if (lootData.PutItemsIntoInventory(inv, out pickedItems))
            {
                OnInventoryEmpty();
                result = true;
            }

            if (pickSpeech)
            {
                var unit = CharacterOperator.GetWithInventory(inv, true);
                if (unit)
                {
                    var message = GetItemNameQuantityList(pickedItems);
                    if (!string.IsNullOrEmpty(message))
                        SpeechOwnerController.Show(unit, null, message, true);
                }
            }

            return result;
        }

        public void Show ()
        {
            lootInvVariable.SetValue(lootId);
            InventoryManager.TriggerUpdate(lootId);
            InventoryCanvas.ShowCanvas("", lootId, true);
            InventoryCanvas.OnInvClosed -= OnInvCanvasClosed;
            InventoryCanvas.OnInvClosed += OnInvCanvasClosed;
        }

        private void OnInventoryEmpty ()
        {
            if (emptyAction == WhenEmptyBehaviour.Remove)
                FinishUsage();
            else if (emptyAction == WhenEmptyBehaviour.Event)
                emptyEvent?.Invoke();
        }

        private void OnInvCanvasClosed (string panelName)
        {
            if (panelName == lootId)
            {
                InventoryCanvas.OnInvClosed -= OnInvCanvasClosed;
                if (lootData != null && !lootData.HaveItemsInInventory())
                    OnInventoryEmpty();
            }
        }

        protected virtual void FinishUsage ()
        {
            InventoryCanvas.OnInvClosed -= OnInvCanvasClosed;
            BackToPool();
            lootData?.TriggerClear();
            lootData = null;
        }

        public override void PostBegin ()
        {
            Init(new LootData(startLootPack));
            DonePostBegin();
        }

        protected void Awake ()
        {
            list ??= new List<LootController>();
            list.Add(this);
            if (startLootPack != null)
                PlanPostBegin();
        }

        protected void OnDestroy ()
        {
            InventoryCanvas.OnInvClosed -= OnInvCanvasClosed;
            list?.Remove(this);
            lootData?.Terminate();
            lootData = null;
        }

        private void TriggerGenerateUsage ()
        {
            lootData.TriggerGenerate();
        }

        private void BackToPool ()
        {
            var me = gameObject;
            LevelManager.DeleteAreaObject(me);
            me.SetActiveOpt(false);
            InsertIntoPool(me, true);
        }

        private void Clear ()
        {
            lootData?.TriggerClear();
            ClearPool(gameObject.name);
            Destroy(gameObject);
        }

        private string GetItemNameQuantityList (List<InventoryItem> fullList)
        {
            var message = string.Empty;
            var optimiseList = new Dictionary<string, int>();
            for (var i = 0; i < fullList.Count; i++)
            {
                if (fullList[i] != null && fullList[i].isSolid && fullList[i].Quantity > 0)
                {
                    if (optimiseList.ContainsKey(fullList[i].ItemId))
                        optimiseList[fullList[i].ItemId] += fullList[i].Quantity;
                    else
                        optimiseList.Add(fullList[i].ItemId, fullList[i].Quantity);
                }
            }

            if (optimiseList.Count > 0)
            {
                var manager = GraphManager.instance.runtimeSettings.itemManager;
                foreach (var kv in optimiseList)
                {
                    var itemData = manager.GetItemData(kv.Key);
                    if (itemData)
                        message += $"+{kv.Value} {itemData.name}, ";
                }

                message = message[..^2];
            }

            return message;
        }
    }
}