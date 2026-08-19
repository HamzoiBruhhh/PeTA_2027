using System;
using System.Collections;
using Sirenix.OdinInspector;
using Reshape.ReFramework;

namespace Reshape.ReGraph
{
    [Serializable]
    public struct GraphEventListener
    {
        public enum EventType
        {
            None,
            ProjectileFinishUsage = 1000,
            ProjectileStartUsage = 1001,
            VfxStart = 2000,
            VfxFinish = 2001,
        }

        [LabelText("Event")]
        [ValueDropdown("EventTypeChoice")]
        public EventType eventType;

        [HorizontalGroup]
        [HideLabel]
        public GraphRunner runner;

        [HorizontalGroup]
        [HideLabel]
        [ValueDropdown("DrawActionNameListDropdown", ExpandAllMenuItems = true)]
        public ActionNameChoice actionName;

        public static GraphExecution ExecuteList (EventType type, GraphEventListener[] actions)
        {
            for (var i = 0; i < actions.Length; i++)
                if (actions[i].runner && actions[i].actionName)
                    if (type == actions[i].eventType)
                        return actions[i].runner.TriggerAction(actions[i].actionName);
            return null;
        }

#if UNITY_EDITOR
        private static IEnumerable EventTypeChoice = new ValueDropdownList<EventType>()
        {
            {"Projectile / Start Usage", EventType.ProjectileStartUsage},
            {"Projectile / Finish Usage", EventType.ProjectileFinishUsage},
            {"Visual Effect / Start", EventType.VfxStart},
            {"Visual Effect / Finish", EventType.VfxFinish},
        };

        private static IEnumerable DrawActionNameListDropdown ()
        {
            return ActionNameChoice.GetActionNameListDropdown();
        }
#endif
    }
}