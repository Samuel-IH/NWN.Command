using System.ComponentModel;
using System.Reflection;
using Anvil.API;

namespace SamuelIH.Nwn.Command;

internal class CommandCache
{
    internal class ReflectedCommand
    {
        public string name = "";
        public string[] perms = null!;
        public MethodInfo method = null!;
        public List<ReflectedArgument> arguments = new();

        private bool? _hasPlayerArgument;
        internal bool HasPlayerArgument => _hasPlayerArgument ??= arguments.Any(a => a.isPlayer);
        public int ArgumentCount => HasPlayerArgument ? arguments.Count - 1 : arguments.Count;

        // ------ help ------ //
        public string PrettyName => _prettyName ??= ("/" + name).ColorString(new Color(0, 200, 50));
        private string? _prettyName;

        public string InvocationHelp => _invocationHelp ??=
            $"{PrettyName} {string.Join(" ", arguments.Where(a => !a.isPlayer).Select(a => a.name)).ColorString(ColorConstants.Yellow)}";
        private string? _invocationHelp;
    }

    internal class ReflectedArgument
    {
        public string name = "";
        public string description = "";
        public ParameterInfo property = null!;
        public TypeConverter converter = null!;
        public bool isPlayer;
    }
    
    private readonly List<ReflectedCommand> _commands = new();
    internal IReadOnlyList<ReflectedCommand> Commands => _commands;
    
    internal void CacheCommands(Assembly forAssembly)
    {
        var methods = forAssembly.GetTypes()
            // only static methods
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            // select the CommandAttribute, if it has one. Else ignore that element.
            .Select(m => (m, a: m.GetCustomAttribute<CommandAttribute>()))
            // only methods with the CommandAttribute
            .Where(ma => ma.a != null)
            // select the method, the attribute, and the parameters
            .Select(ma => (ma.m, ma.a, p: ma.m.GetParameters()))
            .ToList();

        foreach (var (method, attribute, paramInfos) in methods)
        {
            if (attribute == null) continue; // not required, due to check above, but just in case the code changes

            var command = new ReflectedCommand
            {
                name = attribute.name,
                perms = attribute.permsNeeded,
                method = method,
                arguments = ExtractMethodArgs(paramInfos, method),
            };
            _commands.Add(command);
        }
        
        _commands.Sort((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
    }

    private List<ReflectedArgument> ExtractMethodArgs(IEnumerable<ParameterInfo> paramInfos, MethodInfo method)
    {
        var args = new List<ReflectedArgument>();
        
        foreach (var paramInfo in paramInfos)
        {
            var arg = new ReflectedArgument();
            args.Add(arg);

            if (paramInfo.Name == null)
            {
                throw new Exception($"Command argument must have a name: {method.Name}"); // is this even possible?
            }

            arg.name = paramInfo.Name;
            arg.property = paramInfo;

            if (paramInfo.ParameterType == typeof(NwPlayer))
            {
                if (args.Any(a => a.isPlayer))
                {
                    throw new Exception($"Command cannot have more than one NwPlayer argument: {method.Name}");
                }

                arg.isPlayer = true;
                continue;
            }

            var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);
            if (!converter.CanConvertFrom(typeof(string)))
            {
                throw new Exception(
                    $"All command arguments must be convertible from string: {method.Name} {paramInfo.Name}");
            }

            arg.converter = converter;

            if (paramInfo.GetCustomAttribute<CommandArgumentAttribute>() is CommandArgumentAttribute argAttribute)
            {
                arg.description = argAttribute.description;
            }
        }

        return args;
    }
}