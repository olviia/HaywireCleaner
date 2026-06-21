using System.Collections.Generic;
using UnityEngine;

namespace Features.Cutscenes
{
    /// <summary>
    /// analog of the database - holds all the cutscenes
    /// </summary>
    [CreateAssetMenu(fileName = "CutsceneCatalog", menuName = "Cleanbot/Cutscenes/Catalog")]
    public class CutsceneCatalogSO : ScriptableObject
    {
        public List<CutsceneDefinitionSO> all;
    }
}