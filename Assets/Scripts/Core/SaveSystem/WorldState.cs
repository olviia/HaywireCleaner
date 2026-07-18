using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;


namespace Core.SaveSystem
{
    
    /// <summary>
    /// This is the class to put data into save and access data from there
    /// </summary>
    public static class WorldState
    {
        //currrent save obviously, has to have another class in the future to store as 
        //a file and to load from file
        private static SaveData currentSaveData;
        
        //to notify about the fact
        public static event Action<string> FactChanged;

        //for graying out the loading button
        public static bool SaveExists => File.Exists(SavePath);
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
        private static SaveData Data => currentSaveData ??= NewSaveData();
        private static SaveData NewSaveData() => new SaveData();
        
        public static bool GetFlag(string key) => Data.flags.TryGetValue(key, out var value) && value;
        public static void SetFlag(string key, bool value)
        {
            Data.flags[key] = value;
            FactChanged?.Invoke(key);
        }

        public static int GetCounter(string key) => Data.counters.TryGetValue(key, out var value) ? value : 0;
        public static void SetCounter(string key, int value)
        {
            Data.counters[key] = value;
            FactChanged?.Invoke(key);
        }
        public static void AddToCounter(string key, int delta = 1) =>SetCounter(key, GetCounter(key) + delta);
        //keep expanding when saving new things

        public static bool TryGetPosition(string key, out Vector3 pos)
        {
            if (Data.positions.TryGetValue(key, out var stored))
            {
                pos = stored.ToVector3();
                return true;
            }
            pos = default;
            return false;
        }
        public static void SetPosition(string key, Vector3 position) =>
                Data.positions[key] = new SaveVec3(position);
        
        
        public static void Save()
        {
            Data.savedAt = DateTime.UtcNow.ToString("o");
            string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] written to {SavePath}");
        }

        public static bool Load()
        {
            if (!SaveExists) return false;
            try
            {
                string json = File.ReadAllText(SavePath);
                var loaded = JsonConvert.DeserializeObject<SaveData>(json);
                if (loaded == null) return false;
                currentSaveData = loaded;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] failed to read save, ignoring it: {e.Message}");
                return false;
            }
            
            FactChanged?.Invoke(null); //meaning that everything changed
            return true;
        }
        
        public static void NewSave()
        {
            currentSaveData = NewSaveData();
            FactChanged?.Invoke(null); //meaning that everything changed
        }
        

    }
}