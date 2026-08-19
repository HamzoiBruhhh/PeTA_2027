using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class EventBehaviourNode : BehaviourNode
    {
        [PropertyOrder(2)]
        [OnValueChanged("UpdateEvent")]
        [ValidateInput("ValidateEvent", "Please use TriggerAction in Unity Event with caution. It might run into infinite loop!", InfoMessageType.Warning)]
        public UnityEvent unityEvent;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (unityEvent == null || unityEvent.GetPersistentEventCount() <= 0)
                LogWarning("Found an empty Event Behaviour node in " + context.objectName);
            else
                unityEvent?.Invoke();
            base.OnStart(execution, updateId);
        }

        /* [PropertyOrder(1)]
        [OnValueChanged("MarkDirty")]
        [Tooltip("Only use in Graph Editor")]
        [NonSerialized] */
        //-- NOTE public string devNote;

#if UNITY_EDITOR
        [Button, PropertyOrder(999), PropertySpace(4)]
        private void RefreshNodeDesc ()
        {
            MarkDirty();
        }

        private bool ValidateEvent ()
        {
            if (unityEvent != null)
            {
                int count = unityEvent.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    if (unityEvent.GetPersistentMethodName(i) == "TriggerAction")
                        return false;
                }
            }

            return true;
        }

        private void UpdateEvent ()
        {
            MarkDirty();
            ValidateEvent();
        }

        public static string displayName = "Event Behaviour Node";
        public static string nodeName = "Event";

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
            if (unityEvent != null)
            {
                var count = unityEvent.GetPersistentEventCount();
                return count.ToString();
            }

            return base.GetNodeIdentityName();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return nodeName;
        }

        public override string GetNodeViewDescription ()
        {
            if (unityEvent != null)
            {
                var count = unityEvent.GetPersistentEventCount();
                /* if (!string.IsNullOrEmpty(devNote))
                     return "Contain "+count+" unity events" + "\\n" + devNote;
                else*/

                if (count is 1)
                {
                    var message = string.Empty;
                    var targetObj = unityEvent.GetPersistentTarget(0);
                    if (targetObj != null)
                    {
                        message = unityEvent.GetPersistentMethodName(0);
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            ParameterInfo paramInfo = null;
                            try
                            {
                                var method = targetObj.GetType().GetMethod(message, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (method != null)
                                {
                                    var parameters = method.GetParameters();
                                    foreach (var p in parameters)
                                        paramInfo = p;
                                }
                            }
                            catch (Exception)
                            {
                                //-- ignored
                            }

                            var objectName = targetObj is Component comp ? comp.gameObject.name : targetObj.name;
                            var name = targetObj.GetType().Name;
                            name = name.Substring(name.LastIndexOf(".", StringComparison.Ordinal) + 1);
                            message = objectName + "." + name + "." + message;

                            if (paramInfo != null || message.Contains("GraphRunner.ExecuteAction"))
                            {
                                var callField = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (callField != null)
                                {
                                    var callGroup = callField.GetValue(unityEvent);
                                    var callsField = callGroup.GetType().GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (callsField != null)
                                    {
                                        var callList = callsField.GetValue(callGroup) as System.Collections.IList;
                                        if (callList is {Count: > 0})
                                        {
                                            var call = callList[0];
                                            var argsField = call.GetType().GetField("m_Arguments", BindingFlags.NonPublic | BindingFlags.Instance);
                                            if (argsField != null)
                                            {
                                                var arguments = argsField.GetValue(call);
                                                var stringArg = arguments.GetType().GetField("m_StringArgument", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(arguments);
                                                var intArg = arguments.GetType().GetField("m_IntArgument", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(arguments);
                                                var floatArg = arguments.GetType().GetField("m_FloatArgument", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(arguments);
                                                var boolArg = arguments.GetType().GetField("m_BoolArgument", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(arguments);
                                                var objectArg = (UnityEngine.Object)arguments.GetType().GetField("m_ObjectArgument", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(arguments);
                                                if (paramInfo == null)
                                                    message += " with " + (objectArg == null ? stringArg : objectArg.name);
                                                else if (paramInfo.ParameterType == typeof(int))
                                                    message += " with " + intArg;
                                                else if (paramInfo.ParameterType == typeof(float))
                                                    message += " with " + floatArg;
                                                else if (paramInfo.ParameterType == typeof(bool))
                                                    message += " with " + boolArg;
                                                else if (paramInfo.ParameterType == typeof(string))
                                                    message += " with " + stringArg;
                                                else
                                                    message += " with " + (objectArg == null ? "null" : objectArg.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return !string.IsNullOrWhiteSpace(message) ? message : string.Empty;
                }

                if (count is 1 or 2 or 3)
                {
                    var message = string.Empty;
                    string name;
                    if (unityEvent.GetPersistentTarget(0) != null)
                    {
                        name = unityEvent.GetPersistentMethodName(0);
                        if (!string.IsNullOrWhiteSpace(name))
                            message = name;
                    }

                    if (count > 1 && unityEvent.GetPersistentTarget(1) != null)
                    {
                        name = unityEvent.GetPersistentMethodName(1);
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            if (!string.IsNullOrWhiteSpace(message))
                                message = message + " & " + name;
                            else
                                message = name;
                        }
                    }

                    if (count > 2 && unityEvent.GetPersistentTarget(2) != null)
                    {
                        name = unityEvent.GetPersistentMethodName(2);
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            if (!string.IsNullOrWhiteSpace(message))
                                message = message + " & " + name;
                            else
                                message = name;
                        }
                    }

                    return !string.IsNullOrWhiteSpace(message) ? message : string.Empty;
                }

                if (count > 0)
                    return "Contain " + count + " unity events";
            }

            /* if (!string.IsNullOrEmpty(devNote))
                return devNote;*/
            return string.Empty;
        }

        public override string GetNodeViewTooltip ()
        {
            return "This will execute Unity Events that setup in the configuration.\n\n" + base.GetNodeViewTooltip();
        }
#endif
    }
}