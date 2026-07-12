using System;
using Core.SaveSystem;

namespace Features.Quests
{
    /// <summary>
    /// this is what condition we are testing against
    /// </summary>
    public enum FactTest
    {
        FlagIsTrue,
        FlagIsFalse,
        CounterAtLeast
        
        //add other tests as we need them for different quests
    }
    
    [Serializable]
    public struct FactCondition
    {
        public string factKey;
        public FactTest test;
        public int value; //for objectives with numbers: kill 3 bunnies

        public bool IsMet()
        {
            switch (test)
            {
                case FactTest.FlagIsTrue:
                    return WorldState.GetFlag(factKey);
                case FactTest.FlagIsFalse: 
                    return !WorldState.GetFlag(factKey);
                case FactTest.CounterAtLeast:
                    return WorldState.GetCounter(factKey) >= value;
                default: return false;
            }
        }
    }
}