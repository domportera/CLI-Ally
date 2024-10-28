using System.Text;

namespace CLIAlly;

public class InputCommand(CommandInfo commandInfo) : IBuildStrings
{
    public readonly CommandInfo CommandInfo = commandInfo;
    public IReadOnlyList<InputOption> Options => OptionsInternal;
    public IReadOnlyList<string> Errors => _errors ?? [];
    internal bool JsonRequested { get; set; }

    internal readonly List<InputOption> OptionsInternal = [];
    private List<string>? _errors;
    public bool IsValid()
    {
        // if any errors, return false
        if (_errors is { Count: > 0 })
            return false;

        // make sure all the options we have are complete
        if (OptionsInternal.Any(x => !x.IsComplete))
            return false;

        // make sure all required options are present
        foreach (var option in CommandInfo.Options)
        {
            if (option.Required && !ContainsOption(option))
            {
                _errors ??= [];
                _errors.Add($"Missing required option '{option.LongName}'");
                return false;
            }
        }

        return true;
    }

    public bool ContainsOption(OptionInfo optionInfo)
    {
        foreach (var opt in OptionsInternal)
        {
            if (opt.OptionInfo == optionInfo)
            {
                return true;
            }
        }

        return false;
    }

    public void AddError(string error)
    {
        _errors ??= [];
        _errors.Add(error);
    }

    public override string ToString()
    {
        _sb ??= new StringBuilder();
        AppendStringTo(_sb);
        var str = _sb.ToString();
        _sb.Clear();
        return str;
    }

    public StringBuilder AppendStringTo(StringBuilder sb)
    {
        sb.Append("Command ").AppendBetweenSingleQuotes(CommandInfo.Name).Append("':");

        foreach (var option in OptionsInternal)
        {
            sb.Append(' ').AppendBetweenBrackets(option);
        }

        if (_errors is { Count: > 0 })
        {
            sb.Append("\nErrors: ");
            foreach (var error in _errors)
            {
                sb.Append('\n').Append('\t').Append(error);
            }
        }

        return sb;
    }

    private StringBuilder? _sb;
}