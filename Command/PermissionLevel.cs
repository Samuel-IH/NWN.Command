namespace SamuelIH.Nwn.Command;

public enum PermissionLevel
{
    /// <summary>
    /// This user has permissions to execute this command.
    /// </summary>
    Allowed,
    
    /// <summary>
    /// This user does not have permissions to execute this command,
    /// and will be notified of this.
    /// </summary>
    Disallowed,
    
    /// <summary>
    /// This user does not have permissions to execute this command,
    /// and will not be notified of this. In fact, the command will
    /// just be executed as normal chat.
    /// </summary>
    Hidden,
}