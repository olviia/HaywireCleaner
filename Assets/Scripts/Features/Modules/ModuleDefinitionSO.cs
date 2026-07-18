using UnityEngine;
using UnityEngine.Localization;

namespace Features.Modules
{

    /// <summary>
    /// module that the bot can own. analogue of skill
    /// </summary>
    [CreateAssetMenu(fileName = "ModuleDefinition", menuName = "Cleanbot/Modules/Definition")]
    public class ModuleDefinitionSO : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public GameObject prefab;
    }
    
}