using Core.SaveSystem;
using UnityEngine;

namespace Features.Quests.Progression
{
    [CreateAssetMenu(menuName = "Cleanbot/Quests/Fact Setter")] 
    public class FactSetterSO :  ScriptableObject
    {
        [SerializeField] private FactCondition fact;

        public void Write()
        {
            switch (fact.test)
            {
                case FactTest.FlagIsTrue:
                    WorldState.SetFlag(fact.factKey, true);
                    break;
                case FactTest.FlagIsFalse:
                    WorldState.SetFlag(fact.factKey, false);
                    break;
                case FactTest.CounterAtLeast:
                    Debug.LogError("this is not implemented yet. please implement it.");
                    WorldState.SetCounter(fact.factKey, fact.value);
                    break;
                default:
                    break;
                
            }
        }
    }
}