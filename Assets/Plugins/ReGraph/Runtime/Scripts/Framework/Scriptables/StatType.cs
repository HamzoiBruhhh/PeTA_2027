using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Reshape.ReFramework
{
    [HideMonoScript]
    [Serializable]
    public class StatType : ScriptableObject
    {
        public const string STAT_TEAM_FOV = "Team FOV";
        public const string STAT_UNIT_FOV = "Unit FOV";
        public const string STAT_GUARD_ZONE = "Unit Guard Zone";
        public const string STAT_DISTANCE = "Distance";
        public const string STAT_ANGLE = "Angle";
        public const string STAT_MELEE_ATTACK_DISTANCE = "Melee Attack Distance";
        public const string STAT_RANGED_ATTACK_REACHABLE = "Ranged Attack Reacbable";
        public const string STAT_CURRENT_HEALTH = "Current Health";
        public const string STAT_CURRENT_MAX_HEALTH = "Current Max Health";
        public const string STAT_CURRENT_BAR_HEALTH = "Current Bar Health";
        public const string STAT_CURRENT_BAR_MAX_HEALTH = "Current Bar Max Health";
        public const string STAT_BASE_HEALTH = "Base Health";
        public const string STAT_MAX_HEALTH = "Max Health";
        public const string STAT_CURRENT_STAMINA = "Current Stamina";
        public const string STAT_CURRENT_MAX_STAMINA = "Current Max Stamina";
        public const string STAT_BASE_STAMINA = "Base Stamina";
        public const string STAT_MAX_STAMINA = "Max Stamina";
        public const string STAT_STAMINA_CHARGE_RATE = "Stamina Charge Rate";
        public const string STAT_STAMINA_CHARGE_VALUE = "Stamina Charge Value";
        public const string STAT_STAMINA_DODGE_COST = "Stamina Dodge Cost";
        public const string STAT_CURRENT_MORALE = "Current Morale";
        public const string STAT_CURRENT_MAX_MORALE = "Current Max Morale";
        public const string STAT_CURRENT_TARGET_MORALE = "Current Target Morale";
        public const string STAT_BASE_MORALE = "Base Morale";
        public const string STAT_TARGET_MORALE = "Target Morale";
        public const string STAT_MAX_MORALE = "Max Morale";
        public const string STAT_HIGH_CAP_MORALE = "High Cap Morale";
        public const string STAT_LOW_CAP_MORALE = "Low Cap Morale";
        public const string STAT_MORALE_UPDATE_TIME = "Morale Update Time";
        public const string STAT_MORALE_UPDATE_MOD = "Morale Update Modifier";
        public const string STAT_MORALE_UPDATE_MTP = "Morale Update Multiplier";
        public const string STAT_MORALE_UPDATE_MIN = "Morale Update Min";
        public const string STAT_STANCE_DURATION = "Stance Duration";
        public const string STAT_UNSTANCE_DURATION = "Unstance Duration";
        public const string STAT_MELEE_ATTACK_COOLDOWN = "Melee Attack Cooldown";
        public const string STAT_MELEE_ATTACK_RANGE = "Melee Attack Range";
        public const string STAT_MELEE_ATTACK_SPEED = "Melee Attack Speed";
        public const string STAT_MELEE_ATTACK_DURATION = "Melee Attack Duration";
        public const string STAT_RANGED_ATTACK_COOLDOWN = "Ranged Attack Cooldown";
        public const string STAT_RANGED_ATTACK_RANGE = "Ranged Attack Range";
        public const string STAT_RANGED_AIM_DURATION = "Ranged Aim Duration";
        public const string STAT_RANGED_ATTACK_SPEED = "Ranged Attack Speed";
        public const string STAT_RANGED_RELOAD_SPEED = "Ranged Reload Speed";
        public const string STAT_RANGED_AMMO_COUNT = "Ranged Ammo Count";
        public const string STAT_RANGED_AMMO_TRAVEL_SPEED = "Ranged Ammo Travel Speed";
        public const string STAT_RANGED_AMMO_ACCURACY = "Ranged Ammo Accuracy";
        public const string STAT_OUT_OF_CONTROL = "Out of Control";
        public const string STAT_OUT_OF_TARGET_AIM = "Out of Target Aim";
        public const string STAT_OUT_OF_GO_SIGHT = "Out of Go Sight";
        public const string STAT_OUT_OF_MOVE = "Out of Move";
        public const string STAT_OUT_OF_SKILL = "Out of Skill";
        public const string STAT_OUT_OF_ATTACK = "Out of Attack";

        [ReadOnly]
        [LabelWidth(60)]
        [LabelText("Stat Type")]
        public string statName;

        public static implicit operator string (StatType type)
        {
            if (type == null)
                return "None";
            return type.ToString();
        }

        public override string ToString ()
        {
            return statName;
        }

#if UNITY_EDITOR
        private static IEnumerable DrawStatNameListDropdown ()
        {
            var statNameListDropdown = new ValueDropdownList<string>();
            return GetStatNameListDropdown(statNameListDropdown);
        }
        
        private static IEnumerable DrawAllStatNameListDropdown ()
        {
            var statNameListDropdown = new ValueDropdownList<string>();
            return GetAllStatNameListDropdown(statNameListDropdown);
        }
        
        public static ValueDropdownList<string> GetAllStatNameListDropdown (ValueDropdownList<string> statNameListDropdown)
        {
            statNameListDropdown.Add("Character/" + STAT_CURRENT_HEALTH, STAT_CURRENT_HEALTH);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_MAX_HEALTH, STAT_CURRENT_MAX_HEALTH);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_BAR_HEALTH, STAT_CURRENT_BAR_HEALTH);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_BAR_MAX_HEALTH, STAT_CURRENT_BAR_MAX_HEALTH);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_STAMINA, STAT_CURRENT_STAMINA);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_MAX_STAMINA, STAT_CURRENT_MAX_STAMINA);
            statNameListDropdown.Add("Character/" + STAT_STAMINA_DODGE_COST, STAT_STAMINA_DODGE_COST);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_MORALE, STAT_CURRENT_MORALE);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_MAX_MORALE, STAT_CURRENT_MAX_MORALE);
            statNameListDropdown.Add("Character/" + STAT_CURRENT_TARGET_MORALE, STAT_CURRENT_TARGET_MORALE);
            return GetStatNameListDropdown(statNameListDropdown);
        }

        public static ValueDropdownList<string> GetStatNameListDropdown (ValueDropdownList<string> statNameListDropdown)
        {
            statNameListDropdown.Add("Character/" + STAT_BASE_HEALTH, STAT_BASE_HEALTH);
            statNameListDropdown.Add("Character/" + STAT_MAX_HEALTH, STAT_MAX_HEALTH);
            statNameListDropdown.Add("Character/" + STAT_BASE_STAMINA, STAT_BASE_STAMINA);
            statNameListDropdown.Add("Character/" + STAT_MAX_STAMINA, STAT_MAX_STAMINA);
            statNameListDropdown.Add("Character/" + STAT_STAMINA_CHARGE_RATE, STAT_STAMINA_CHARGE_RATE);
            statNameListDropdown.Add("Character/" + STAT_STAMINA_CHARGE_VALUE, STAT_STAMINA_CHARGE_VALUE);
            statNameListDropdown.Add("Character/" + STAT_BASE_MORALE, STAT_BASE_MORALE);
            statNameListDropdown.Add("Character/" + STAT_TARGET_MORALE, STAT_TARGET_MORALE);
            statNameListDropdown.Add("Character/" + STAT_MAX_MORALE, STAT_MAX_MORALE);
            statNameListDropdown.Add("Character/" + STAT_HIGH_CAP_MORALE, STAT_HIGH_CAP_MORALE);
            statNameListDropdown.Add("Character/" + STAT_LOW_CAP_MORALE, STAT_LOW_CAP_MORALE);
            statNameListDropdown.Add("Character/" + STAT_MORALE_UPDATE_TIME, STAT_MORALE_UPDATE_TIME);
            statNameListDropdown.Add("Character/" + STAT_MORALE_UPDATE_MOD, STAT_MORALE_UPDATE_MOD);
            statNameListDropdown.Add("Character/" + STAT_MORALE_UPDATE_MTP, STAT_MORALE_UPDATE_MTP);
            statNameListDropdown.Add("Character/" + STAT_MORALE_UPDATE_MIN, STAT_MORALE_UPDATE_MIN);
            statNameListDropdown.Add("Muscle/" + STAT_STANCE_DURATION, STAT_STANCE_DURATION);
            statNameListDropdown.Add("Muscle/" + STAT_UNSTANCE_DURATION, STAT_UNSTANCE_DURATION);
            statNameListDropdown.Add("Muscle/" + STAT_MELEE_ATTACK_COOLDOWN, STAT_MELEE_ATTACK_COOLDOWN);
            statNameListDropdown.Add("Muscle/" + STAT_MELEE_ATTACK_RANGE, STAT_MELEE_ATTACK_RANGE);
            statNameListDropdown.Add("Muscle/" + STAT_MELEE_ATTACK_SPEED, STAT_MELEE_ATTACK_SPEED);
            statNameListDropdown.Add("Muscle/" + STAT_MELEE_ATTACK_DURATION, STAT_MELEE_ATTACK_DURATION);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_ATTACK_COOLDOWN, STAT_RANGED_ATTACK_COOLDOWN);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_ATTACK_RANGE, STAT_RANGED_ATTACK_RANGE);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_AIM_DURATION, STAT_RANGED_AIM_DURATION);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_ATTACK_SPEED, STAT_RANGED_ATTACK_SPEED);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_RELOAD_SPEED, STAT_RANGED_RELOAD_SPEED);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_AMMO_COUNT, STAT_RANGED_AMMO_COUNT);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_AMMO_TRAVEL_SPEED, STAT_RANGED_AMMO_TRAVEL_SPEED);
            statNameListDropdown.Add("Muscle/" + STAT_RANGED_AMMO_ACCURACY, STAT_RANGED_AMMO_ACCURACY);
            statNameListDropdown.Add("Muscle/" + STAT_OUT_OF_CONTROL, STAT_OUT_OF_CONTROL);
            statNameListDropdown.Add("Muscle/" + STAT_OUT_OF_TARGET_AIM, STAT_OUT_OF_TARGET_AIM);
            statNameListDropdown.Add("Muscle/" + STAT_OUT_OF_SKILL, STAT_OUT_OF_SKILL);
            statNameListDropdown.Add("Muscle/" + STAT_OUT_OF_ATTACK, STAT_OUT_OF_ATTACK);
            statNameListDropdown.Add("Muscle/" + STAT_OUT_OF_GO_SIGHT, STAT_OUT_OF_GO_SIGHT);
            statNameListDropdown.Add("Muscle/" + STAT_OUT_OF_MOVE, STAT_OUT_OF_MOVE);
            var assets = AssetDatabase.FindAssets("t:StatType");
            if (assets.Length > 0)
            {
                for (var i = 0; i < assets.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(assets[i]);
                    var typeChoice = AssetDatabase.LoadAssetAtPath<StatType>(path);
                    var folderName = Path.GetFileName(Path.GetDirectoryName(path));
                    folderName = folderName.Substring(0, folderName.Length - 5);
                    statNameListDropdown.Add(folderName + "/" + typeChoice.statName, typeChoice.statName);
                }
            }

            return statNameListDropdown;
        }
#endif
    }
}