using System.Collections.Generic;

namespace Core
{
    public class SaveData
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
        public Dictionary<string, int> facts; //int, boolean
        public Dictionary<string, float> reactions; // timers, floats

        public float inGameTimeSeconds;
        
        //metadata
        public string savedAt;
    }
}