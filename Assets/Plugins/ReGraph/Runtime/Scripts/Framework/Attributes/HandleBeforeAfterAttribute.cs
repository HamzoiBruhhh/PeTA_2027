using System;

namespace Reshape.ReFramework
{
    public class HandleBeforeAfterAttribute : Attribute
    {
        public string type;
        public string graphObject;
        public string triggerId;

        public HandleBeforeAfterAttribute (string type, string graphObject, string triggerId)
        {
            this.type = type;
            this.graphObject = graphObject;
            this.triggerId = triggerId;
        }
    }
}