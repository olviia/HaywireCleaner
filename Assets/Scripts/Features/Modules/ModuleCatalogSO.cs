using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

namespace Features.Modules
{
    [CreateAssetMenu(fileName = "ModulesCatalog", menuName = "Cleanbot/Modules/Catalog")]
    public class ModuleCatalogSO:ScriptableObject
    {
        public List<ModuleDefinitionSO> modules;
    }
}