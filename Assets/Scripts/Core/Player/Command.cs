using UnityEngine;

namespace Core.Player
{
    
    /// <summary>
    /// first, this is command pattern
    ///
    /// second. while i kinda understand how it works, i still cant write it myself.
    /// this concept is slightly too abstract for me yet i guess. hope one day i will
    /// master it.
    ///
    /// anyway, the struct because if it was a class, there would be heap allocation every
    /// update, while structs are fast to create and remove
    /// 
    /// </summary>
    public readonly struct Command
    {
        public readonly Intent WhatToDo;
        public readonly Vector2 ExtraInfo;

        public Command(Intent whatToDo, Vector2 extraInfo = default)
        {
            WhatToDo = whatToDo;
            ExtraInfo = extraInfo;
        }
    }
}