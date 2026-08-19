using System;

namespace Reshape.ReFramework
{
    public class HintAttribute : Attribute
    {
        public string showInfoBoxCondition;
        public string message;

        public HintAttribute (string showInfoBoxCondition, string message)
        {
            this.showInfoBoxCondition = showInfoBoxCondition;
            this.message = message;
        }
    }
}