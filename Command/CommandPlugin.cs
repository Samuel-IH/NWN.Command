using System.Reflection;
using Anvil.API;
using Anvil.Services;
using JetBrains.Annotations;
using NLog;
using NWN.Native.API;

namespace SamuelIH.Nwn.Command;

[ServiceBinding(typeof(CommandPlugin))]
public class CommandPlugin
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private readonly CommandCache _cache = new();
    
    private bool _isUsingDefaultPerms = true;
    private Func<string[], NwPlayer, PermissionLevel> _getHasPerms = (_, _) => PermissionLevel.Disallowed;

    public CommandPlugin()
    {
        NwModule.Instance.OnChatMessageSend += (eventData) =>
        {
            var vm = NWNXLib.VirtualMachine();
            if (vm.m_nRecursionLevel > 6)
            {
                Log.Error("Recursion level is dangerously high. Dumping full stack trace:");
                for (var i = 1; i <= vm.m_nRecursionLevel; i++)
                {
                    Log.Error("    " + vm.m_pVirtualMachineScript[i].m_sScriptName);
                }
            }
            
            if (eventData.Sender is not NwCreature creature) return;
            if (creature.ControllingPlayer is not NwPlayer player) return;

            eventData.Skip = HandleCommand(eventData.Message, player);
        };
    }
    
    [PublicAPI]
    public void CacheCommands(Assembly forAssembly)
    {
        _cache.CacheCommands(forAssembly);
    }
    
    [PublicAPI]
    public void SetPermissionHandler(Func<string[], NwPlayer, PermissionLevel> handler)
    {
        _getHasPerms = handler;
        
        if (!_isUsingDefaultPerms)
        {
            Log.Warn($"Permission handler was already customized. The latest handler will be used: {handler.Method.Name}");
        }

        _isUsingDefaultPerms = false;
    }

    private bool HandleCommand(string message, NwPlayer player)
    {
        if (!message.StartsWith("/")) return false;
        message = message[1..];

        // find the command that this text starts with
        var searchIn = message;
        var command = _cache.Commands
            .Where(c => searchIn.StartsWith(c.name))
            .MaxBy(c => c.name.Length);
        
        if (command == null)
        {
            message = message.ToLower();
            var similarCommands = _cache.Commands
                .Where(c => c.name.StartsWith(message))
                .Where(c => _getHasPerms(c.perms, player) != PermissionLevel.Hidden)
                .ToList();
            if (similarCommands.Count == 0) return false;
            
            SendMonologueToPlayer(player, $"Did you mean:\n  {string.Join("\n  ", similarCommands.Select(c => c.InvocationHelp))}");
            return true;
        }
        
        switch (_getHasPerms(command.perms, player))
        {
            case PermissionLevel.Disallowed:
                SendMonologueToPlayer(player, $"You do not have permission to use {command.PrettyName}!".ColorString(ColorConstants.Red));
                return true;
            case PermissionLevel.Hidden:
                return false;
        }

        // remove the command name from the message
        message = message[command.name.Length..];

        // break the message into parts
        var parts = ParseMultipartString(message, new[] { ' ' });
        
        if (parts.Count != command.ArgumentCount || parts is ["help"])
        {
            var help = $"\nUsage: {command.InvocationHelp}";
            help += "\nArguments:";
            
            // In this case, a foreach looks cleaner than a LINQ query
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var argument in command.arguments)
            {
                if (argument.isPlayer) continue;
                help += $"\n  {argument.name.ColorString(ColorConstants.Yellow)}: ({argument.property.ParameterType.Name}) {argument.description}";
            }
            SendMonologueToPlayer(player, help);
            return true;
        }

        var args = new object[command.arguments.Count];
        for (var i = 0; i < command.arguments.Count; i++)
        {
            if (command.arguments[i].isPlayer)
            {
                args[i] = player;
                continue;
            }
            
            var value = parts[0];
            parts.RemoveAt(0);

            try
            {
                if (command.arguments[i].converter.ConvertFrom(value) is not object o)
                {
                    throw new Exception("Not convertible from string");
                }

                args[i] = o;
            }
            catch (Exception e)
            {
                player.SendServerMessage($"Error parsing argument {command.arguments[i].name}: {e.Message}", ColorConstants.Red);
                return true;
            }
        }

        try
        {
            command.method.Invoke(null, args);
        }
        catch (Exception e)
        {
            player.SendServerMessage($"Error executing command: {e.Message}", ColorConstants.Red);
            return true;
        }

        return true;
    }

    private void SendMonologueToPlayer(NwPlayer player, string monologue)
    {
        var m = NWNXLib.AppManager().m_pServerExoApp.GetNWSMessage();
        m.SendServerToPlayerChat_Talk(player.PlayerId, player.ControlledCreature?.ObjectId ?? 0, monologue.ToExoString());
    }

    /// <summary>
    /// Parses a string into a list of parts, using the specified delimiters.
    /// This method is similar to String.Split, but it supports quoted strings, and escaped newlines.
    /// </summary>
    /// <param name="input">The string to chop up</param>
    /// <param name="delimiters">List of characters that behave as delimiters</param>
    /// <returns></returns>
    private static List<string> ParseMultipartString(string input, char[] delimiters)
    {
        input = input.Replace("\\n", "\n");
        var parts = new List<string>();

        var startIndex = 0;
        var insideQuotes = false;

        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '"' && (i == 0 || input[i - 1] != '\\'))
            {
                // Toggle the insideQuotes flag
                insideQuotes = !insideQuotes;
            }
            else if (!insideQuotes && delimiters.Contains(input[i]))
            {
                // Get the substring between the start index and the current index
                var part = input.Substring(startIndex, i - startIndex);

                // Trim any whitespace
                part = part.Trim();

                // Remove any escape characters (\") from the part
                part = part.Replace("\\\"", "\"");

                // Add the part to the list
                if (!string.IsNullOrEmpty(part))
                {
                    parts.Add(part);
                }

                // Set the start index to the next character after the delimiter
                startIndex = i + 1;
            }
        }

        // Add the final part (if any)
        var finalPart = input[startIndex..].Trim('"', ' ');

        // Remove any escape characters (\") from the final part
        finalPart = finalPart.Replace("\\\"", "\"");

        if (!string.IsNullOrEmpty(finalPart))
        {
            parts.Add(finalPart);
        }

        return parts;
    }
}