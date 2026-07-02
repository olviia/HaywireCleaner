using Core.Interaction;

namespace Core.Player
{
    public interface IChargeable
    {
        void StartDocking(IDock dock);
    }
}