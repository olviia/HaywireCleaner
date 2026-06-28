namespace Core.Player
{
    /// <summary>
    /// list of actions that modules can do
    /// is used to map ModuleInput to modules
    /// so modules know what input they are suppposed
    /// to react to
    /// </summary>
    public enum Intent
    {
        //where to automatically go
        //add more when there is new input in PlayerCommand
        
        Move,
        Interact,
        
        
        
        Charge,
        StopCharge
        
    }
}