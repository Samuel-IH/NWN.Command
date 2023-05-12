namespace SamuelIH.Nwn.Command;

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentAttribute : Attribute
{
    public readonly string description;

    public CommandArgumentAttribute(string description)
    {
        this.description = description;
    }
}
