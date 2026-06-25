using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// this class is for everything that can be controlled: playable characters, npc
    /// </summary>
    public class Actor : IPosessable
    {
        public TagSet Tags { get; } = new();
        private List<IModule> modules = new();
        
        public void RegisterModule(IModule module) => modules.Add(module);
        public void RemoveModule(IModule module) => modules.Remove(module);
        public void OnPosessed() => ModuleInput.OnIntent += Send;
        public void OnUnposessed() => ModuleInput.OnIntent -= Send;

        void Send(Intent intent, Vector2 extraInfo)
        {
            var cmd = new Command(intent, extraInfo);
            foreach (var module in modules)
            {
                if (module.ReactsTo.Contains(intent))
                {
                    module.Handle(this, cmd);
                }
            }
        }
    }
}