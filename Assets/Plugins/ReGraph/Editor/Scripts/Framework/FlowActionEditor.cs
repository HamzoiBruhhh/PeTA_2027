using System;
using System.Collections.Generic;
using System.Linq;
using Reshape.ReGraph;
using Reshape.Unity.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Reshape.ReFramework
{
    public class FlowActionEditor : OdinValueDrawer<FlowAction>
    {
        private ValueDropdownList<ActionNameChoice> dropdown;
        private float timer;
        
        protected override void DrawPropertyLayout (GUIContent label)
        {
            var value = this.ValueEntry.SmartValue;
            var previousRunner = value.runner;
            var previousAction = value.actionName;
            var changed = false;
            timer += ReEditorTime.deltaTime;
            var timedUpdate = false;
            if (timer > 1)
            {
                timedUpdate = true;
                timer = 0;
            }
            
            GUILayout.BeginHorizontal();
            value.runner = (GraphRunner) EditorGUILayout.ObjectField(value.runner, typeof(GraphRunner), true);
            if (previousRunner != value.runner)
                changed = true;

            if (value.runner)
            {
                if (dropdown == null || changed || timedUpdate)
                    dropdown = DrawActionNameListDropdown(value.runner);
                var orderedDropdown = dropdown.OrderBy(item => item.Text);
                var textList = orderedDropdown.Select(item => item.Text).ToArray();
                var actionList = orderedDropdown.Select(item => item.Value).ToArray();
                var dropdownRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup, GUILayout.Width(Screen.width * 0.35f));
                if (GUI.Button(dropdownRect, value.actionName == null ? string.Empty : GetListItemName(value.actionName), EditorStyles.popup))
                {
                    var selector = new GenericSelector<ActionNameChoice>(string.Empty, false, GetListItemName, actionList);
                    selector.EnableSingleClickToSelect();
                    selector.SetSelection(value.actionName);
                    selector.SelectionConfirmed -= OnActionSelected;
                    selector.SelectionConfirmed += OnActionSelected;
                    var popupRect = new Rect(dropdownRect.x, dropdownRect.y + EditorGUIUtility.singleLineHeight, dropdownRect.width, 0);
                    selector.ShowInPopup(popupRect, new Vector2(dropdownRect.width, 400));
                }
                
                void OnActionSelected(IEnumerable<ActionNameChoice> selections)
                {
                    var selected = selections.FirstOrDefault();
                    if (selected != null)
                        value.actionName = selected;
                    if (previousAction != value.actionName)
                        ValueEntry.SmartValue = value;
                } 

                string GetListItemName (ActionNameChoice action)
                {
                    for (var i = 0; i < actionList.Length; i++)
                        if (actionList[i] == action)
                            return textList[i];
                    return string.Empty;
                }
            }
            else
            {
                dropdown = null;
                EditorGUILayout.Popup(0, Array.Empty<string>(), GUILayout.Width(Screen.width * 0.35f));
                value.actionName = null;
            }

            if (previousAction != value.actionName)
                changed = true;

            GUILayout.EndHorizontal();
            if (changed)
                ValueEntry.SmartValue = value;
            CallNextDrawer(label);
        }

        private ValueDropdownList<ActionNameChoice> DrawActionNameListDropdown (GraphRunner runner)
        {
            var actionNameListDropdown = ActionNameChoice.GetActionNameListDropdown();

            var currentSelectionActionNames = GetActionsFromCurrentRunner(runner);
            for (var i = 0; i < actionNameListDropdown.Count; i++)
            {
                var actionNameItem = actionNameListDropdown[i];
                if (currentSelectionActionNames.Contains(actionNameItem.Text))
                {
                    actionNameItem.Text = $"Exist/{actionNameItem.Value.name}";
                    actionNameListDropdown[i] = actionNameItem;
                }
                else
                {
                    actionNameItem.Text = $"New/{actionNameItem.Value.name}";
                    actionNameListDropdown[i] = actionNameItem;
                }
            }

            return actionNameListDropdown;
        }

        private List<string> GetActionsFromCurrentRunner (GraphRunner runner)
        {
            var currentSelectionActionNames = new List<string>();
            var graph = runner.graph;
            if (graph is {RootNode: {children: {Count: > 0}}})
            {
                for (var i = 0; i < graph.RootNode.children.Count; i++)
                {
                    if (graph.RootNode.children[i] is ActionTriggerNode actionTrigger)
                    {
                        var actionTriggerName = actionTrigger.GetActionName();
                        if (!actionTriggerName.IsNullOrWhitespace())
                            currentSelectionActionNames.Add(actionTriggerName);
                    }
                }
            }

            return currentSelectionActionNames;
        }
    }
}