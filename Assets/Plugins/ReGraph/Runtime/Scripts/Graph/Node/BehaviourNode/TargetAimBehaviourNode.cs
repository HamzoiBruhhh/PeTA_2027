using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Reshape.ReFramework;
using Reshape.Unity;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Reshape.ReGraph
{
    [System.Serializable]
    public class TargetAimBehaviourNode : BehaviourNode
    {
        private const float TOLERANCE = 0.00001f;

        public enum ExecutionType
        {
            None,
            AddCharacterIntoList = 101,
            RemoveCharacterFromList = 102,
            FilterCharacterList = 111,
            SortCharacterList = 121,
            CheckCharacterFlag = 201,
        }

        public enum FilterCompare
        {
            None = 0,
            Least = 11,
            Most = 12,
            Equal = 31,
            NotEqual = 32,
            LessThan = 51,
            MoreThan = 52,
            LessThanAndEqual = 53,
            MoreThanAndEqual = 54,
            Inside = 101,
            Outside = 102,
        }

        [SerializeField]
        [OnValueChanged("OnChangeType")]
        [LabelText("Execution")]
        [ValueDropdown("TypeChoice")]
        private ExecutionType executionType;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [InlineButton("@SceneObjectList.OpenCreateMenu(list)", "✚")]
        [OnInspectorGUI("CheckSceneObjectListDirty")]
        [ShowIf("ShowList")]
        [InfoBox("The assigned list have not specific type!", InfoMessageType.Warning, "ShowListWarning", GUIAlwaysEnabled = true)]
        private SceneObjectList list;

        [SerializeField]
        [OnValueChanged("FilterTypeValueChanged")]
        [ValueDropdown("FilterTypeChoice", DropdownWidth = 250, AppendNextDrawer = true)]
        [ShowIf("@executionType == ExecutionType.FilterCharacterList || executionType == ExecutionType.SortCharacterList")]
        [LabelText("@FilterTypeLabel()")]
        [DisplayAsString]
        private string filterType;

        [SerializeField]
        [LabelText("@CharacterVariableLabel()")]
        [OnValueChanged("MarkDirty")]
        [ShowIf("ShowCharacterVariable")]
        [InfoBox("The assigned variable is not match type!", InfoMessageType.Warning, "ShowObjectVariableWarning", GUIAlwaysEnabled = true)]
        private SceneObjectVariable characterVariable;

        [SerializeField]
        [OnInspectorGUI("MarkUnitFlagsDirty")]
        [LabelText("Search Flag")]
        [ShowIf("@executionType == ExecutionType.AddCharacterIntoList || executionType == ExecutionType.RemoveCharacterFromList || executionType == ExecutionType.CheckCharacterFlag")]
        private MultiTag unitFlags = new MultiTag("Unit Flags", typeof(MultiTagUnit));

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [ValueDropdown("FilterCompareChoice")]
        [ShowIf("@ShowFilterCompare()")]
        [LabelText("Method")]
        private FilterCompare filterCompare;

        [LabelText("@NumberLabel()")]
        [ShowIf("ShowNumber")]
        [OnInspectorGUI("@MarkPropertyDirty(number)")]
        [InlineProperty]
        public FloatProperty number;

        [SerializeField]
        [OnValueChanged("MarkDirty")]
        [InlineButton("@SceneObjectList.OpenCreateMenu(listParam)", "✚")]
        [OnInspectorGUI("CheckSceneObjectListParamDirty")]
        [ShowIf("@executionType == ExecutionType.FilterCharacterList || executionType == ExecutionType.SortCharacterList")]
        [InfoBox("The assigned list have not specific type!", InfoMessageType.Warning, "ShowListParamWarning", GUIAlwaysEnabled = true)]
        [LabelText("Save To")]
        private SceneObjectList listParam;

        protected override void OnStart (GraphExecution execution, int updateId)
        {
            if (executionType == ExecutionType.CheckCharacterFlag)
            {
                if (characterVariable == null || characterVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator || unitFlags == 0)
                {
                    LogWarning("Found an empty Target Aim Behaviour node in " + context.objectName);
                }
                else
                {
                    var character = (CharacterOperator) characterVariable.GetComponent();
                    if (character == null)
                    {
                        LogWarning("Found an invalid Target Aim Check Character Flag Behaviour node in " + context.objectName);
                    }
                    else
                    {
                        var result = character.flags.ContainAll(unitFlags);
                        UpdateConditionNode(execution, updateId, result);
                    }
                }
            }
            else if (executionType is ExecutionType.AddCharacterIntoList or ExecutionType.RemoveCharacterFromList)
            {
                if (list == null || unitFlags == 0 || list.type != SceneObject.ObjectType.CharacterOperator)
                {
                    LogWarning("Found an empty Target Aim Behaviour node in " + context.objectName);
                }
                else
                {
                    var found = CharacterOperator.GetAllWithTags(unitFlags, false, false, false);
                    var objectList = new List<SceneObjectProperty>();
                    for (int i = 0; i < found.Count; i++)
                    {
                        var so = new SceneObjectProperty(SceneObject.ObjectType.CharacterOperator);
                        so.SetObjectValue(found[i]);
                        objectList.Add(so);
                    }

                    if (executionType == ExecutionType.AddCharacterIntoList)
                        list.InsertObject(objectList);
                    if (executionType == ExecutionType.RemoveCharacterFromList)
                        list.RemoveObject(objectList);
                }
            }
            else if (executionType is ExecutionType.FilterCharacterList)
            {
                var empty = false;
                if (string.IsNullOrEmpty(filterType))
                {
                    empty = true;
                }
                else if (!list || list.type != SceneObject.ObjectType.CharacterOperator)
                {
                    empty = true;
                }
                else if (!listParam || listParam.type != SceneObject.ObjectType.CharacterOperator)
                {
                    empty = true;
                }
                else
                {
                    if (filterType is StatType.STAT_TEAM_FOV or StatType.STAT_UNIT_FOV or StatType.STAT_GUARD_ZONE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
                    {
                        if (!characterVariable || characterVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                            empty = true;
                        else if (filterCompare != FilterCompare.Inside && filterCompare != FilterCompare.Outside)
                            empty = true;
                    }
                    else if (filterType == StatType.STAT_DISTANCE)
                    {
                        if (!characterVariable || characterVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                            empty = true;
                        else if (filterCompare == FilterCompare.None)
                            empty = true;
                        else if (filterCompare != FilterCompare.Most && filterCompare != FilterCompare.Least)
                        {
                            if (number == null)
                                empty = true;
                        }
                    }
                    else if (filterCompare == FilterCompare.None)
                    {
                        empty = true;
                    }
                    else if (filterCompare != FilterCompare.Most && filterCompare != FilterCompare.Least)
                    {
                        if (number == null)
                            empty = true;
                    }
                }

                if (empty)
                {
                    LogWarning("Found an empty Target Aim Behaviour node in " + context.objectName);
                }
                else
                {
                    var listCount = list.GetCount();
                    if (listCount > 0)
                    {
                        var charOpList = new List<CharacterOperatorSceneObject>();
                        for (var i = 0; i < listCount; i++)
                        {
                            var item = list.GetByIndex(i);
                            if (!item.IsNull && !item.IsEmpty)
                            {
                                Component comp = item;
                                var charOp = (CharacterOperator) comp;
                                if (charOp)
                                {
                                    charOpList.Add(new CharacterOperatorSceneObject(charOp, i));
                                }
                            }
                        }

                        listCount = charOpList.Count;
                        if (listCount <= 0)
                        {
                            LogWarning("Found an invalid Target Aim Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            CharacterOperator compChar = null;
                            if (characterVariable)
                                compChar = (CharacterOperator) characterVariable.GetComponent();
                            for (var i = 0; i < listCount; i++)
                            {
                                var theChar = charOpList[i];
                                theChar.value = (theChar.character.GetStatValue(filterType, compChar));
                                charOpList[i] = theChar;
                            }
                            
                            var compareValue = (float) number;
                            if (filterCompare == FilterCompare.Least)
                                compareValue = float.MaxValue;
                            var remainingChars = new List<CharacterOperatorSceneObject>();
                            for (var i = 0; i < listCount; i++)
                            {
                                if (filterCompare is FilterCompare.Inside)
                                {
                                    if (charOpList[i].value >= 0)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare is FilterCompare.Outside)
                                {
                                    if (charOpList[i].value <= 0)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare is FilterCompare.Most or FilterCompare.Least)
                                {
                                    if (Math.Abs(compareValue - charOpList[i].value) < TOLERANCE)
                                    {
                                        remainingChars.Add(charOpList[i]);
                                    }
                                    else if (filterCompare == FilterCompare.Most && charOpList[i].value > compareValue)
                                    {
                                        remainingChars.Clear();
                                        compareValue = charOpList[i].value;
                                        remainingChars.Add(charOpList[i]);
                                    }
                                    else if (filterCompare == FilterCompare.Least && charOpList[i].value < compareValue)
                                    {
                                        remainingChars.Clear();
                                        compareValue = charOpList[i].value;
                                        remainingChars.Add(charOpList[i]);
                                    }
                                }
                                else if (filterCompare == FilterCompare.Equal)
                                {
                                    if (Math.Abs(compareValue - charOpList[i].value) < TOLERANCE)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare == FilterCompare.NotEqual)
                                {
                                    if (Math.Abs(compareValue - charOpList[i].value) >= TOLERANCE)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare == FilterCompare.MoreThan)
                                {
                                    if (charOpList[i].value > compareValue)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare == FilterCompare.MoreThanAndEqual)
                                {
                                    if (charOpList[i].value >= compareValue)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare == FilterCompare.LessThan)
                                {
                                    if (charOpList[i].value < compareValue)
                                        remainingChars.Add(charOpList[i]);
                                }
                                else if (filterCompare == FilterCompare.LessThanAndEqual)
                                {
                                    if (charOpList[i].value <= compareValue)
                                        remainingChars.Add(charOpList[i]);
                                }
                            }

                            if (remainingChars.Count > 0)
                            {
                                var tempList = new List<SceneObjectProperty>();
                                for (var i = 0; i < remainingChars.Count; i++)
                                    tempList.Add(list.GetByIndex(remainingChars[i].index));
                                listParam.ClearObject();
                                listParam.InsertObject(tempList);
                            }
                            else
                            {
                                listParam.ClearObject();
                            }
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.SortCharacterList)
            {
                if (string.IsNullOrEmpty(filterType) || !list || list.GetCount() <= 0 || !listParam || list.type != SceneObject.ObjectType.CharacterOperator ||
                    listParam.type != SceneObject.ObjectType.CharacterOperator)
                {
                    LogWarning("Found an empty Target Aim Behaviour node in " + context.objectName);
                }
                else
                {
                    var listCount = list.GetCount();
                    if (listCount > 1)
                    {
                        var charOpList = new List<CharacterOperatorSceneObject>();
                        for (var i = 0; i < listCount; i++)
                        {
                            var item = list.GetByIndex(i);
                            if (!item.IsNull && !item.IsEmpty)
                            {
                                Component comp = item;
                                var charOp = (CharacterOperator) comp;
                                if (charOp != null)
                                {
                                    charOpList.Add(new CharacterOperatorSceneObject(charOp, i));
                                }
                            }
                        }

                        listCount = charOpList.Count;
                        if (listCount <= 0)
                        {
                            LogWarning("Found an invalid Target Aim Behaviour node in " + context.objectName);
                        }
                        else
                        {
                            CharacterOperator compChar = null;
                            if (characterVariable)
                                compChar = (CharacterOperator) characterVariable.GetComponent();
                            if (filterType == StatType.STAT_DISTANCE && !compChar)
                            {
                                LogWarning("Found an empty Target Aim Behaviour node in " + context.objectName);
                            }
                            else
                            {
                                for (var i = 0; i < listCount; i++)
                                {
                                    var theChar = charOpList[i];
                                    theChar.value = (theChar.character.GetStatValue(filterType, compChar));
                                    charOpList[i] = theChar;
                                }

                                charOpList.Sort(delegate (CharacterOperatorSceneObject a, CharacterOperatorSceneObject b)
                                {
                                    if (Math.Abs(a.value - b.value) < TOLERANCE) return 0;
                                    return a.value > b.value ? 1 : -1;
                                });

                                var tempList = new List<SceneObjectProperty>();
                                for (var i = 0; i < listCount; i++)
                                    tempList.Add(list.GetByIndex(charOpList[i].index));
                                listParam.ClearObject();
                                listParam.InsertObject(tempList);
                            }
                        }
                    }
                }
            }

            base.OnStart(execution, updateId);
        }

#if UNITY_EDITOR
        private bool ShowFilterCompare ()
        {
            if (executionType == ExecutionType.FilterCharacterList)
                if (!string.IsNullOrEmpty(filterType))
                    return true;
            return false;
        }

        private string CharacterVariableLabel ()
        {
            if (filterType is StatType.STAT_UNIT_FOV or StatType.STAT_TEAM_FOV or StatType.STAT_GUARD_ZONE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
                return "Unit";
            return "Compare With";
        }

        private string NumberLabel ()
        {
            return "Value";
        }

        private IEnumerable FilterTypeChoice ()
        {
            var statNameListDropdown = new ValueDropdownList<string>();
            if (executionType == ExecutionType.FilterCharacterList)
            {
                statNameListDropdown.Add("Motor/" + StatType.STAT_TEAM_FOV, StatType.STAT_TEAM_FOV);
                statNameListDropdown.Add("Motor/" + StatType.STAT_UNIT_FOV, StatType.STAT_UNIT_FOV);
                statNameListDropdown.Add("Motor/" + StatType.STAT_ANGLE, StatType.STAT_ANGLE);
                statNameListDropdown.Add("Muscle/" + StatType.STAT_GUARD_ZONE, StatType.STAT_GUARD_ZONE);
                statNameListDropdown.Add("Muscle/" + StatType.STAT_RANGED_ATTACK_REACHABLE, StatType.STAT_RANGED_ATTACK_REACHABLE);
            }

            statNameListDropdown.Add("Motor/" + StatType.STAT_DISTANCE, StatType.STAT_DISTANCE);
            statNameListDropdown.Add("Motor/" + StatType.STAT_MELEE_ATTACK_DISTANCE, StatType.STAT_MELEE_ATTACK_DISTANCE);
            return StatType.GetAllStatNameListDropdown(statNameListDropdown);
        }

        private string FilterTypeLabel ()
        {
            if (executionType == ExecutionType.FilterCharacterList)
            {
                return "Filter By";
            }

            if (executionType == ExecutionType.SortCharacterList)
            {
                return "Sort By";
            }

            return string.Empty;
        }

        private bool ShowNumber ()
        {
            if (executionType == ExecutionType.FilterCharacterList)
            {
                if (filterType is StatType.STAT_UNIT_FOV or StatType.STAT_TEAM_FOV or StatType.STAT_GUARD_ZONE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
                    return false;
                if (filterCompare is FilterCompare.Least or FilterCompare.Most or FilterCompare.None)
                    return false;
                return true;
            }

            return false;
        }

        private bool ShowList ()
        {
            if (executionType is ExecutionType.AddCharacterIntoList or ExecutionType.RemoveCharacterFromList or ExecutionType.FilterCharacterList or ExecutionType.SortCharacterList)
                return true;
            return false;
        }
        
        private void FilterTypeValueChanged ()
        {
            if (filterType is StatType.STAT_TEAM_FOV or StatType.STAT_UNIT_FOV or StatType.STAT_GUARD_ZONE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
            {
                if (filterCompare != FilterCompare.Inside && filterCompare != FilterCompare.Outside)
                    filterCompare = FilterCompare.None;
            }
            else if (filterCompare is FilterCompare.Inside or FilterCompare.Outside)
            {
                filterCompare = FilterCompare.None;
            }
            MarkDirty();
        }

        private void MarkUnitFlagsDirty ()
        {
            if (unitFlags.dirty)
            {
                unitFlags.dirty = false;
                MarkDirty();
            }
        }

        private bool ShowCharacterVariable ()
        {
            if (executionType == ExecutionType.CheckCharacterFlag)
                return true;
            if (executionType == ExecutionType.FilterCharacterList)
            {
                if (!string.IsNullOrEmpty(filterType))
                {
                    if (filterType is StatType.STAT_DISTANCE or StatType.STAT_ANGLE or StatType.STAT_UNIT_FOV or StatType.STAT_TEAM_FOV or StatType.STAT_GUARD_ZONE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
                        return true;
                }
            }

            if (executionType == ExecutionType.SortCharacterList)
                if (!string.IsNullOrEmpty(filterType) && filterType == StatType.STAT_DISTANCE)
                    return true;
            return false;
        }


        private bool ShowObjectVariableWarning ()
        {
            if (characterVariable != null)
                if (characterVariable.sceneObject.type != SceneObject.ObjectType.CharacterOperator)
                    return true;
            return false;
        }

        private void CheckSceneObjectListDirty ()
        {
            if (!HaveGraphSelectionObject())
                return;
            var createVarPath = GraphEditorVariable.GetString(GetGraphSelectionInstanceID(), "createVariable");
            if (!string.IsNullOrEmpty(createVarPath))
            {
                GraphEditorVariable.SetString(GetGraphSelectionInstanceID(), "createVariable", string.Empty);
                var createVar = (SceneObjectList) AssetDatabase.LoadAssetAtPath(createVarPath, typeof(SceneObjectList));
                list = createVar;
                MarkDirty();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private void CheckSceneObjectListParamDirty ()
        {
            if (!HaveGraphSelectionObject())
                return;
            var createVarPath = GraphEditorVariable.GetString(GetGraphSelectionInstanceID(), "createVariable");
            if (!string.IsNullOrEmpty(createVarPath))
            {
                GraphEditorVariable.SetString(GetGraphSelectionInstanceID(), "createVariable", string.Empty);
                var createVar = (SceneObjectList) AssetDatabase.LoadAssetAtPath(createVarPath, typeof(SceneObjectList));
                listParam = createVar;
                MarkDirty();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private bool ShowListWarning ()
        {
            if (list == null)
                return false;
            if (list != null && !list.IsNoneType())
                if (list.type == SceneObject.ObjectType.CharacterOperator)
                    return false;
            return true;
        }

        private bool ShowListParamWarning ()
        {
            if (listParam != null && !listParam.IsNoneType())
                if (listParam.type == SceneObject.ObjectType.CharacterOperator)
                    return false;
            return true;
        }

        public bool AcceptConditionNode ()
        {
            if (executionType == ExecutionType.CheckCharacterFlag)
                return true;
            return false;
        }
        
        public override bool IsPortReachable (GraphNode node)
        {
            if (node is YesConditionNode or NoConditionNode)
            {
                if (executionType != ExecutionType.CheckCharacterFlag)
                    return false;
            }
            else if (node is ChoiceConditionNode)
            {
                return false;
            }

            return true;
        }
        
        private void OnChangeType ()
        {
            MarkDirty();
            MarkRepaint();
        }

        private IEnumerable FilterCompareChoice ()
        {
            var listDropdown = new ValueDropdownList<FilterCompare>();
            if (filterType is StatType.STAT_UNIT_FOV or StatType.STAT_TEAM_FOV or StatType.STAT_GUARD_ZONE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
            {
                listDropdown.Add("Inside", FilterCompare.Inside);
                listDropdown.Add("Outside", FilterCompare.Outside);
            }
            else
            {
                listDropdown.Add("Least", FilterCompare.Least);
                listDropdown.Add("Most", FilterCompare.Most);
                listDropdown.Add("Equal", FilterCompare.Equal);
                listDropdown.Add("Not Equal", FilterCompare.NotEqual);
                listDropdown.Add("Less Than", FilterCompare.LessThan);
                listDropdown.Add("More Than", FilterCompare.MoreThan);
                listDropdown.Add("Less Than And Equal", FilterCompare.LessThanAndEqual);
                listDropdown.Add("More Than And Equal", FilterCompare.MoreThanAndEqual);
            }

            return listDropdown;
        }

        private static IEnumerable TypeChoice = new ValueDropdownList<ExecutionType>()
        {
            {"Add To List", ExecutionType.AddCharacterIntoList},
            {"Remove From List", ExecutionType.RemoveCharacterFromList},
            {"Filter List", ExecutionType.FilterCharacterList},
            {"Sort List", ExecutionType.SortCharacterList},
            {"Check Flag", ExecutionType.CheckCharacterFlag},
        };

        public static string displayName = "Target Aim Behaviour Node";
        public static string nodeName = "Target Aim";

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
            return executionType.ToString();
        }

        public override string GetNodeMenuDisplayName ()
        {
            return $"Gameplay/{nodeName}";
        }

        public override string GetNodeViewDescription ()
        {
            if (executionType is ExecutionType.AddCharacterIntoList)
            {
                if (list != null && list.type == SceneObject.ObjectType.CharacterOperator && unitFlags != 0)
                    return $"Add {unitFlags.GetSelectedString(9)} into {list.name}";
            }
            else if (executionType is ExecutionType.RemoveCharacterFromList)
            {
                if (list != null && list.type == SceneObject.ObjectType.CharacterOperator && unitFlags != 0)
                    return $"Remove {unitFlags.GetSelectedString(9)} from {list.name}";
            }
            else if (executionType is ExecutionType.FilterCharacterList)
            {
                if (list != null && list.type == SceneObject.ObjectType.CharacterOperator && listParam != null && listParam.type == SceneObject.ObjectType.CharacterOperator)
                {
                    if (!string.IsNullOrEmpty(filterType))
                    {
                        var filterCompareString = filterCompare.ToString().SplitCamelCase();
                        if (filterCompare == FilterCompare.MoreThan)
                            filterCompareString = ">";
                        else if (filterCompare == FilterCompare.MoreThanAndEqual)
                            filterCompareString = ">=";
                        else if (filterCompare == FilterCompare.LessThan)
                            filterCompareString = "<";
                        else if (filterCompare == FilterCompare.LessThanAndEqual)
                            filterCompareString = "<=";
                        if (filterType is StatType.STAT_TEAM_FOV or StatType.STAT_UNIT_FOV or StatType.STAT_GUARD_ZONE)
                        {
                            if (characterVariable != null && characterVariable.sceneObject.type == SceneObject.ObjectType.CharacterOperator)
                                if (filterCompare is FilterCompare.Inside or FilterCompare.Outside)
                                    return $"Filter {list.name} by {filterCompareString} {characterVariable.name}'s {filterType}";
                        }
                        else if (filterType is StatType.STAT_DISTANCE or StatType.STAT_ANGLE or StatType.STAT_MELEE_ATTACK_DISTANCE or StatType.STAT_RANGED_ATTACK_REACHABLE)
                        {
                            if (characterVariable != null && characterVariable.sceneObject.type == SceneObject.ObjectType.CharacterOperator)
                            {
                                if (filterCompare != FilterCompare.None)
                                {
                                    if (filterCompare is FilterCompare.Most or FilterCompare.Least)
                                        return $"Filter {list.name} by {filterCompareString} {characterVariable.name}'s {filterType}";
                                    if (filterCompare is FilterCompare.Inside or FilterCompare.Outside)
                                        return $"Filter {list.name} by {filterCompareString} {characterVariable.name}'s {filterType}";
                                    if (number != null)
                                        return $"Filter {list.name} by {characterVariable.name}'s {filterType} {filterCompareString} {number.GetDisplayName()}";
                                }
                            }
                        }
                        else if (filterCompare != FilterCompare.None)
                        {
                            if (filterCompare is FilterCompare.Most or FilterCompare.Least)
                                return $"Filter {list.name} by {filterCompareString} {filterType}";
                            if (number != null)
                                return $"Filter {list.name} by {filterType} {filterCompareString} {number.GetDisplayName()}";
                        }
                    }
                }
            }
            else if (executionType is ExecutionType.SortCharacterList)
            {
                if (list != null && list.type == SceneObject.ObjectType.CharacterOperator && !string.IsNullOrEmpty(filterType) && listParam != null &&
                    listParam.type == SceneObject.ObjectType.CharacterOperator)
                {
                    if (filterType == StatType.STAT_DISTANCE)
                    {
                        if (characterVariable != null && characterVariable.sceneObject.type == SceneObject.ObjectType.CharacterOperator)
                            return $"Sort {list.name} with {filterType} from {characterVariable.name}";
                    }
                    else
                        return $"Sort {list.name} with {filterType}";
                }
            }
            else if (executionType == ExecutionType.CheckCharacterFlag)
            {
                if (characterVariable != null && characterVariable.sceneObject.type == SceneObject.ObjectType.CharacterOperator && unitFlags != 0)
                    return $"Check {characterVariable.name} is {unitFlags.GetSelectedString(9)}";
            }

            return string.Empty;
        }
        
        public override string GetNodeViewTooltip ()
        {
            var tip = string.Empty;
            if (executionType == ExecutionType.AddCharacterIntoList)
                tip += "This will add character found on the scene into the list base on the configuration.\n\n";
            else if (executionType == ExecutionType.RemoveCharacterFromList)
                tip += "This will remove character from the list base on the configuration.\n\n";
            else if (executionType == ExecutionType.FilterCharacterList)
                tip += "This will filter character in the list base on the configuration.\n\n";
            else if (executionType == ExecutionType.SortCharacterList)
                tip += "This will sort character in the list base on the configuration.\n\n";
            else if (executionType == ExecutionType.CheckCharacterFlag)
                tip += "This will use to check the character flag.\n\n";
            else
                tip += "This will execute all Target Aim related behaviour. Target Aim control how to choose a character as aim target for attack.\n\n";
            return tip + base.GetNodeViewTooltip();
        }
#endif
    }
}