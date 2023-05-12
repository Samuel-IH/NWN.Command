# NWN.Command
Lightweight command lib for Anvil using reflection

![image](https://github.com/Samuel-IH/NWN.Command/assets/46057508/2597febd-e244-4e30-ada8-c5c6e7c337a8)
## Features
### Reflection-based command handling
Simply tag your static method with the CommandAttribute and it will become a command.
Parameters can optionally use a CommandArgumentAttribute to provide help to the end user.
```cs
[Command("give")]
public static void GiveItem(NwPlayer player,
    [CommandArgument("The item to give")] string item,
    [CommandArgument("The amount of the item to give")] int amount)
{
    ...
}
```

### In-game help
If a matching command cannot be found, all the similar commands will be listed.
Additionally, invoking a command with `help` as the only parameter (or incorrect parameters!) will cause the command to spit out its full help.

### Flexible Permissions
The CommandAttribute supports defining a list of required permissions. These can be used to implement just about any permissions system.
```cs
[Command("give", "dm")] // tier-based: player -> dm -> admin (etc.)
// or
[Command("give", "item-creation")] // functionality based
public static void GiveItem()
```

## Getting Started
First, you'll need to install this as a dependency in two places:
1. In your anvil paket file, so the server will load it as a service. See [here](https://github.com/nwn-dotnet/Anvil/wiki/Installing-Plugins:-Paket).
2. As a nuget dependency in your project, so you'll have access to the APIs.
Both of these will be explained in a later iteration of this Readme, as currently I have not set up the build action or nuget.

Next, you'll need to request an instance of the `CommandPlugin` in your own plugin. This can be done by requiring a plugin instance in your constructor, or by using Anvil's `[Inject]` attribute instead:
```cs
using SamuelIH.Nwn.Command;

[ServiceBinding(typeof(MyPlugin))]
public class MyPlugin
{
    public MyPlugin(CommandPlugin commandPlugin) // <- Can optionally use the [Inject] attribute instead
    {
        ...
    }
}
```

After injection, you'll need to set up the permission handler, and register your assemblies:
```cs
    public MyPlugin(CommandPlugin commandPlugin)
    {
        // By default, the command plugin is set to disallow and hide ALL commands. (safety)
        // You will want to replace this with your own logic. Here, we make it allow every command.
        commandPlugin.SetPermissionHandler((strings, player) => PermissionLevel.Allowed);
        
        // This tells the command plugin to scan our entire assembly for commands.
        // You can pass in any assembly you like, but generally it makes sense to pass in your own.
        commandPlugin.CacheCommands(typeof(MyPlugin).Assembly);
    }
```

Finally, you can begin adding commands:
```cs
[Command("echo")]
public static void Echo (NwPlayer player, // <- One NwPlayer parameter can  be specified, this one is
                                          // hidden from the command args and automatically assigned by the plugin
    [CommandArgument("The message to be echoed back")] // Other arguments can be annotated with [CommandArgument]
    string message)                                            // to provide a description of their purpose
{
    player.SendServerMessage(message);
}

// "dm" is not interpreted by the command system.
// In this example, our command handler that we setup on plugin load
// would implement this logic. eg:
// commandPlugin.SetPermissionHandler((strings, player) =>
// {
//     if (strings.Contains("dm") && !player.IsDM) return PermissionLevel.Hidden;
//     return PermissionLevel.Allowed;
// });
[Command("rainbowshout", "dm")]
public static void RainbowShout(NwPlayer player, [CommandArgument("The message to shout")] string message)
{
    if (player.ControlledCreature is not NwCreature creature) return;
    
    var colors = new []{ColorConstants.Red, ColorConstants.Orange, ColorConstants.Yellow, ColorConstants.Green,
        ColorConstants.Blue, ColorConstants.Cyan, ColorConstants.Purple};

    var toShout = "";
    for (var i = 0; i < message.Length; i++)
    {
        toShout += message[i].ToString().ColorString(colors[i % colors.Length]);
    }
    creature.SpeakString(toShout, TalkVolume.Shout);
}
```
![image](https://github.com/Samuel-IH/NWN.Command/assets/46057508/5a861906-3ce5-447d-9782-bb0f758db6ac)

## Requirements
- Command methods MUST be static.
- Command arguments MUST be convertible from strings via `TypeConverter.ConvertFrom`
- Command methods MUST have only one (or 0) NwPlayer arguments.
