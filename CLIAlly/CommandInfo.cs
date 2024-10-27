using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Text;

namespace CLIAlly;

/// <summary>
/// A subcommand of the application
/// Can contain several options
/// These are not necessarily required by the user to call - they can be by one's implementation, however
/// </summary>
public record CommandInfo : IBuildStrings
{
    public readonly string Name;
    public readonly bool IsCaseSensitive;
    public readonly string? Description;
    public readonly string? FurtherInformation;
    public readonly IReadOnlyList<OptionInfo> Options;
    public bool AcceptsOption => Options.Any();
    public readonly bool IsDefaultCommand;
    public readonly DynamicMethodInfo MethodInfo;

    public CommandInfo(string name, string? description, string? furtherInformation, IReadOnlyList<OptionInfo>? options, bool isDefaultCommand, bool isCaseSensitive, DynamicMethodInfo methodInfo)
    {
        Options = options ?? [];
        Name = name;
        Description = description;
        FurtherInformation = furtherInformation;
        IsDefaultCommand = isDefaultCommand;
        IsCaseSensitive = isCaseSensitive;
        MethodInfo = methodInfo;
    }

    public void AppendHelpText(StringBuilder sb, bool verbose, int indentSpaces)
    {
        sb.AppendRepeating(' ', indentSpaces);

        sb.Append($"'{Name}': ");
        if (Description is not null)
        {
            sb.Append($"{Description} ");
        }

        if (verbose && FurtherInformation != null)
        {
            sb.Append(FurtherInformation);
        }

        sb.AppendLine();
        
        var optionIndentation = indentSpaces + 4;
        foreach (var option in Options)
        {
            option.AppendHelpText(sb, true, optionIndentation);
        }
    }

    public StringBuilder AppendStringTo(StringBuilder sb)
    {
        sb.Append(Name);
        return sb;
    }
}