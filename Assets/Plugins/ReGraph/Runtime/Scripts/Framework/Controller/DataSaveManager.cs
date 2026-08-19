using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class DataSaveManager : BaseBehaviour
    {
        private static DataSaveManager me;
        
        [ShowInInspector]
        [InlineEditor]
        [LabelText("Save Data List")]
        private List<DataSavePack> currentSaveDataList;
        
        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        public static void SetSaveValue (DataSavePack savePack, string dataName, string dataValue)
        {
            InitManager();
            me.SetValue(savePack, SavePack.TYPE_DATA, string.Empty, dataName, dataValue);
        }
        
        public static void SetCharacterSaveValue (DataSavePack savePack, string id, string dataName, string dataValue)
        {
            InitManager();
            me.SetValue(savePack, SavePack.TYPE_CHARACTER, id, dataName, dataValue);
        }
        
        public static VariableScriptableObject GetSaveValue (DataSavePack savePack, string dataName, VariableScriptableObject variable)
        {
            return me ? me.GetValue(savePack, SavePack.TYPE_DATA, string.Empty, dataName, variable) : variable;
        }
        
        
        public static bool GetCharacterSaveValue (DataSavePack savePack, string id, string dataName, out string value)
        {
            value = string.Empty;
            return me && me.GetValue(savePack, SavePack.TYPE_CHARACTER, id, dataName, out value);
        }
        
        public static bool GetCharacterSaveValue (DataSavePack savePack, string id, string dataName, out float value)
        {
            value = 0;
            return me && me.GetValue(savePack, SavePack.TYPE_CHARACTER, id, dataName, out value);
        }
        
        public static VariableScriptableObject GetCharacterSaveValue (DataSavePack savePack, string id, string dataName, VariableScriptableObject variable)
        {
            return me ? me.GetValue(savePack, SavePack.TYPE_CHARACTER, id, dataName, variable) : variable;
        }
        
        public static void StoreSaveValue (DataSavePack savePack)
        {
            if (me)
                me.SaveValue(savePack, SavePack.TYPE_DATA, string.Empty);
        }
        
        public static void StoreCharacterSaveValue (DataSavePack savePack, string id)
        {
            if (me)
                me.SaveValue(savePack, SavePack.TYPE_CHARACTER, id);
        }
        
        public static void LoadSaveValue (DataSavePack savePack)
        {
            InitManager();
            me.LoadValue(savePack, SavePack.TYPE_DATA, string.Empty);
        }
        
        public static void LoadCharacterSaveValue (DataSavePack savePack, string id)
        {
            InitManager();
            me.LoadValue(savePack, SavePack.TYPE_CHARACTER, id);
        }
        
        public static void DeleteSaveValue (DataSavePack savePack)
        {
            InitManager();
            me.DeleteValue(savePack, SavePack.TYPE_DATA, string.Empty);
        }
        
        public static void DeleteCharacterSaveValue (DataSavePack savePack, string id)
        {
            InitManager();
            me.DeleteValue(savePack, SavePack.TYPE_CHARACTER, id);
        }
        
        public static void ClearSaveValue (DataSavePack savePack)
        {
            if (me)
                me.ClearValue(savePack, SavePack.TYPE_DATA, string.Empty);
        }
        
        public static void ClearCharacterSaveValue (DataSavePack savePack, string id)
        {
            if (me)
                me.ClearValue(savePack, SavePack.TYPE_CHARACTER, id);
        }
        
        private static void InitManager ()
        {
            if (!me)
            {
                var go = new GameObject("DataSaveManager");
                me = go.AddComponent<DataSaveManager>();
            }
        }
        
        public static void UninitManager ()
        {
            if (me)
            {
                me.currentSaveDataList.Clear();
                Destroy(me.gameObject);
                me = null;
            }
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
            currentSaveDataList ??= new List<DataSavePack>();
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

        private void SaveValue (DataSavePack savePack, int type, string id)
        {
            if (!savePack)
                return;
            if (currentSaveDataList != null)
            {
                var saveData = GetSaveData(savePack.fileName, type, id);
                if (saveData)
                    saveData.Save();
            }
        }
        
        private void LoadValue (DataSavePack savePack, int type, string id)
        {
            if (!savePack)
                return;
            if (currentSaveDataList != null)
            {
                var saveData = GetSaveData(savePack.fileName, type, id);
                if (!saveData)
                    saveData = CreateSaveData(savePack, type, id);
                else
                    saveData.Clear();
                if (saveData)
                    saveData.Load();
            }
        }
        
        private void ClearValue (DataSavePack savePack, int type, string id)
        {
            if (!savePack)
                return;
            if (currentSaveDataList != null)
            {
                var saveData = GetSaveData(savePack.fileName, type, id);
                if (saveData)
                    saveData.Clear();
            }
        }
        
        private void DeleteValue (DataSavePack savePack, int type, string id)
        {
            if (!savePack)
                return;
            if (currentSaveDataList != null)
            {
                var saveData = GetSaveData(savePack.fileName, type, id);
                if (saveData)
                    saveData.Delete(type == SavePack.TYPE_CHARACTER);
            }
        }
        
        private bool GetValue (DataSavePack savePack, int type, string id, string dataName, out string value)
        {
            value = string.Empty;
            if (savePack && !string.IsNullOrEmpty(dataName))
            {
                if (currentSaveDataList != null)
                {
                    var saveData = GetSaveData(savePack.fileName, type, id);
                    if (saveData)
                        return saveData.GetValue(dataName, out value);
                }
            }

            return false;
        }
        
        private bool GetValue (DataSavePack savePack, int type, string id, string dataName, out float value)
        {
            value = 0;
            if (savePack && !string.IsNullOrEmpty(dataName))
            {
                if (currentSaveDataList != null)
                {
                    var saveData = GetSaveData(savePack.fileName, type, id);
                    if (saveData)
                        return saveData.GetValue(dataName, out value);
                }
            }

            return false;
        }
        
        private VariableScriptableObject GetValue (DataSavePack savePack, int type, string id, string dataName, VariableScriptableObject variable)
        {
            if (savePack && !string.IsNullOrEmpty(dataName))
            {
                if (currentSaveDataList != null)
                {
                    var saveData = GetSaveData(savePack.fileName, type, id);
                    if (saveData)
                        return saveData.GetValue(dataName, variable);
                }
            }

            return variable;
        }
        
        private void SetValue (DataSavePack savePack, int type, string id, string dataName, string dataValue)
        {
            if (!savePack || string.IsNullOrEmpty(dataName))
                return;
            if (currentSaveDataList != null)
            {
                var saveData = GetSaveData(savePack.fileName, type, id);
                if (!saveData)
                    saveData = CreateSaveData(savePack, type, id);
                if (saveData)
                    saveData.SetValue(dataName, dataValue);
            }
        }

        private DataSavePack CreateSaveData (DataSavePack pack, int type, string id)
        {
            if (pack)
            {
                if (currentSaveDataList != null)
                {
                    var saveData = Instantiate(pack);
                    if (type == SavePack.TYPE_CHARACTER)
                        saveData.name = "Character " + id +"'s " + pack.fileName;
                    else
                        saveData.name = pack.fileName;
                    saveData.SetType(type);
                    saveData.SetId(id);
                    saveData.LockFileName();
                    saveData.Init();
                    currentSaveDataList.Add(saveData);
                    return saveData;
                }
            }

            return null;
        }

        private DataSavePack GetSaveData (string packName, int type, string id)
        {
            for (var i = 0; i < currentSaveDataList.Count; i++)
                if (currentSaveDataList[i] && currentSaveDataList[i].Match(packName, type, id))
                    return currentSaveDataList[i];
            return null;
        }
        
        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}