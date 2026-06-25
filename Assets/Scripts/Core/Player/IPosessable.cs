namespace Core.Player
{
    /// <summary>
    /// this interface is for all the playable characters
    /// identifies that control over this character can be taken
    /// </summary>
    public interface IPosessable
    {
        void OnPosessed();
        void OnUnposessed();
    }
}