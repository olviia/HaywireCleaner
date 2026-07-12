using System.Collections.Generic;
using UnityEngine;

namespace Features.Quests
{
    [CreateAssetMenu(fileName = "QuestsCatalog", menuName = "Cleanbot/Quests/Catalog")]
    public class QuestCatalogSO:ScriptableObject
    {
        public List<QuestDefinitionSO> quests;
    }
}