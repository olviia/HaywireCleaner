using System.Collections.Generic;

namespace Core.Player
{
    public interface IModule
    {
        IEnumerable<Intent> ReactsTo { get; }
        Tag BlockedBy => Tag.None; //default tag
        void Handle(Actor owner, Command cmd);
    }
}