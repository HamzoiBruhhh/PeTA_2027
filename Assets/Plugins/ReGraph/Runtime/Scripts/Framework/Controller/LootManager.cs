using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [Serializable]
    public struct ObtainedItemInfo
    {
        [HideInInspector]
        public string origin;

        [HideInInspector]
        public string invName;

        [HideInInspector]
        public string itemId;

        public CharacterOperator character;
        public string itemName;
        public int quantity;

        public ObtainedItemInfo (string fromInv, string toInv, string itemName, string itemId, int quantity, CharacterOperator character)
        {
            origin = fromInv;
            invName = toInv;
            this.itemId = itemId;
            this.itemName = itemName;
            this.quantity = quantity;
            this.character = character;
        }
    }

    [HideMonoScript]
    public class LootManager : BaseBehaviour
    {
        private static LootManager me;

        [ShowInInspector]
        [HideReferenceObjectPicker]
        private List<ObtainedItemInfo> obtained;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static int GetObtainedCount ()
        {
            if (me && me.obtained != null)
                return me.obtained.Count;
            return 0;
        }

        public static ObtainedItemInfo GetObtainedInfo (int index)
        {
            if (me && me.obtained != null && index < me.obtained.Count)
                return me.obtained[index];
            return default;
        }

        public static void RecordObtained (string fromInv, string toInv, string itemName, string itemId, int quantity)
        {
            InitManager();
            if (LootController.Contains(fromInv))
            {
                me.AddObtained(fromInv, toInv, itemName, itemId, quantity);
            }
        }

        private static void InitManager ()
        {
            if (!me)
            {
                var go = new GameObject("LootManager");
                me = go.AddComponent<LootManager>();
            }
        }

        public static void Clear ()
        {
            if (me)
                me.obtained?.Clear();
        }

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void Awake ()
        {
            obtained ??= new List<ObtainedItemInfo>();
            DontDestroyOnLoad(gameObject);
        }

        protected void OnDestroy ()
        {
            me = null;
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        private void AddObtained (string fromInv, string toInv, string itemName, string itemId, int quantity)
        {
            var character = CharacterOperator.GetWithInventory(toInv, true);
            obtained.Add(new ObtainedItemInfo(fromInv, toInv, itemName, itemId, quantity, character));
        }

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}