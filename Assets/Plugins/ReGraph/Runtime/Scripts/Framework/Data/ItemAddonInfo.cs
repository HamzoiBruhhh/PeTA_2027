using System;

namespace Reshape.ReFramework
{
    [Serializable]
    public partial class ItemAddonInfo
    {
        private ItemData data;
        
        public void Init (ItemData itemData)
        {
            if (data == null)
                data = itemData;
        }
    }
}