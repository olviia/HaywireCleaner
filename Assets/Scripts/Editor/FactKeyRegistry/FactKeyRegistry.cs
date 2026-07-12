using System.Collections.Generic;

namespace Tools.FactKeyRegistry
{
    /// <summary>
    /// this is from where picker asks the fact, and this gathers all
    /// keys that are from interfaces IFactKeySource
    /// </summary>
    public static class FactKeyRegistry
    {
        private static readonly IFactKeySource[] Sources =
        {
            new ConstKeySource(),
            new CutsceneKeySource(),
            new QuestKeySource(),
        };

        public static List<string> Collect()
        {
            var seen = new  HashSet<string>();
            var keys = new List<string>();

            foreach (var source in Sources)
            {
                foreach (var key in source.GetFactKeys())
                {
                    if(seen.Add(key))
                        keys.Add(key);
                }
            }
            return keys;
        }
    }
}