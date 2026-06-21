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

        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static bool GetFlag(string key) => currentSaveData.flags.TryGetValue(key, out var value) && value;
        public static void SetFlag(string key, bool value) => currentSaveData.flags[key] = value;
        
        //keep expanding when saving new things

        public static void Save()
        {
            string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }

        public static void Load()
        {
            string json = File.ReadAllText(SavePath);
            currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
        }
        
        public static void NewSave()
        {
            currentSaveData = new SaveData
            {
                flags = new Dictionary<string, bool>(),
                counters = new Dictionary<string, int>(),
                reactions = new Dictionary<string, float>(),
                names = new Dictionary<string, string>(),
                positions = new Dictionary<string, Vector3>(),
                attributeValues = new Dictionary<string, float>(),     
                ownedModuleId = new List<string>(),
                skillId = new List<string>()
            };
        }

    }
}