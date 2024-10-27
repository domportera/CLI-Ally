using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CLIAlly;

/// <summary>
/// This represents the entire CLI input surface - options and subcommands
/// This is the main way to define what the CLI can do
/// </summary>
public record CommandConfiguration
{
    public readonly IReadOnlyList<CommandInfo> CommandInfos;
    public readonly CommandInfo? DefaultCommand;

    /// <param name="commandInfos">A list of <see cref="CommandInfo"/>s that the app can perform - like "push" and "pull" are for git.</param>
    public CommandConfiguration(IReadOnlyList<CommandInfo> commandInfos)
    {
        if(commandInfos.Count == 0)
            throw new ArgumentException("No commands defined");
        
        CommandInfos = commandInfos;
        DefaultCommand = commandInfos.SingleOrDefault(x => x.IsDefaultCommand);
    }

    public bool TryGetDefaultCommand([NotNullWhen(true)] out CommandInfo? o)
    {
        o = DefaultCommand;
        return o != null;
    }

    public bool TryGetCommand(string commandName, [NotNullWhen(true)] out CommandInfo? foundCommand)
    {
        foreach (var cmd in CommandInfos)
        {
            if (cmd.Name == commandName)
            {
                foundCommand = cmd;
                return true;
            }
        }

        foundCommand = null;
        return false;
    }

    public void GetFullHelpText(StringBuilder sb, int indent)
    {
        // print out a standard help text
        if (CommandInfos.Count > 0)
        {
            sb.Append("~~~~~~~~~ ").Append("Commands").AppendLine(" ~~~~~~~~~~");

            var indentSpaces = indent * 4;
            foreach (var command in CommandInfos)
            {
                command.AppendHelpText(sb, false, indentSpaces);
                sb.AppendLine();
            }
        }
        else
        {
            sb.Append("No commands defined");
        }
    }
}