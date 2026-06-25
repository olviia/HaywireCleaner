using System.Collections.Generic;

namespace Core.Player
{
    public interface IModule
    {
        IEnumerable<Intent> ReactsTo { get; }
        void Handle(Actor owner, Command cmd);
    }
}