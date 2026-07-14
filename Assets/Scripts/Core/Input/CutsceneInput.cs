using System;
using Core.Player;

namespace Core.Input
{
    public static class CutsceneInput
    {
        public static event Action SkipCutscene;
        
        public static void RaiseSkip() => SkipCutscene?.Invoke();
 //all the other commands here
    }
}