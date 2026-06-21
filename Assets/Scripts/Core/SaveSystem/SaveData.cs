using System.Collections.Generic;
using UnityEngine;


namespace Core.SaveSystem
{
    /// <summary>
    /// this is the class to store the data for saving. it can only be accessed through the
    /// it's mediator class that is currently named WorldState
    /// this is a separation of data and doing something with it
    /// </summary>
    internal class SaveData
    {
        public int saveVersion = 1;
        
        //what character it is
        public string characterId;
        public string characterName;
        
        //character rantime state
        public Dictionary<string, float> attributeValues;
        
        //modules/abilities
        public List<string> ownedModuleId;
        
        //skills? levels of them? maybe they go into attributes
        public List<string> skillId;
        
        //generic world state
        public Dictionary<string, bool> flags; // something happened or not
        
        public Dictionary<string, int> counters; // something happened or not
        
        public Dictionary<string, float> reactions; //I need better name for this timers, floats
        
        public Dictionary<string, string> names; // names
        
        public Dictionary<string, Vector3> positions; // positions

        public float inGameTimeSeconds;
        
        //metadata
        public string savedAt;
    }
}