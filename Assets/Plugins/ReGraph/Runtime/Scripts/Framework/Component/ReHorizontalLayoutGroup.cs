using UnityEngine.UI;
using System.Threading.Tasks;

namespace Reshape.ReFramework
{
    public class ReHorizontalLayoutGroup : HorizontalLayoutGroup
    {
        private LayoutGroup parentLayoutGroup;

        public void UpdateLayout ()
        {
            if (!this) return;
            enabled = false;
            enabled = true;
        }

        private async void UpdateParentLayout ()
        {
            if (parentLayoutGroup)
            {
                await Task.Delay(1);
                if (parentLayoutGroup is ReVerticalLayoutGroup v)
                {
                    v.UpdateLayout();
                }
                else if (parentLayoutGroup is ReHorizontalLayoutGroup h)
                {
                    h.UpdateLayout();
                }
                else if (parentLayoutGroup is ReGridLayoutGroup g)
                {
                    g.UpdateLayout();
                }
            }
        }

        public override void CalculateLayoutInputHorizontal ()
        {
            base.CalculateLayoutInputHorizontal();
            UpdateParentLayout();
        }

        public override void CalculateLayoutInputVertical ()
        {
            base.CalculateLayoutInputVertical();
            UpdateParentLayout();
        }

        protected override void Awake ()
        {
            if (transform.parent)
                parentLayoutGroup = transform.parent.GetComponentInParent<LayoutGroup>();
            base.Awake();
        }
    }
}