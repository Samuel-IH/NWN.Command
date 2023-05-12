namespace SamuelIH.Nwn.Command;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public readonly string name;
    public readonly string[] permsNeeded;

    public CommandAttribute(string name, params string[] permsNeeded)
    {
        this.name = name;
        this.permsNeeded = permsNeeded;
    }

    public CommandAttribute(string name)
    {
        this.name = name;
        permsNeeded = Array.Empty<string>();
    }
}