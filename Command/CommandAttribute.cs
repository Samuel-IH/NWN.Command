namespace SamuelIH.Nwn.Command;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public readonly string Name;
    public readonly string[] PermsNeeded;

    public CommandAttribute(string name, params string[] permsNeeded)
    {
        Name = name;
        PermsNeeded = permsNeeded;
    }

    public CommandAttribute(string name)
    {
        Name = name;
        PermsNeeded = Array.Empty<string>();
    }
}