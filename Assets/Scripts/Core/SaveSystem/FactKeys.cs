using System;

namespace Core.SaveSystem
{

    /// <summary>
    /// something to do with publick const string fields fo ra discovery by reflection
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class FactKeySourceAttribute : Attribute { }

    
    /// <summary>
    /// single place in the whole project that created the keys
    /// </summary>
    public static class FactKeys
    {
        //there can be fixed keys that i write myself
        
        //and methods to build those keys

        public static string CutsceneFinished(string cutsceneId) => $"cutscene.{cutsceneId}.finished";
    }
}