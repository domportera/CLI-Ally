using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CLIAlly;

/// <summary>
/// An option that was specified on the command line, with its raw string value if it has one
/// </summary>
public class InputOption : IBuildStrings
{
    public readonly OptionInfo OptionInfo;

    // not null if IsComplete is true
    private object? _parsedValue;

    private string? _argument;

    public string? Argument => _argument;

    public bool IsComplete => _parsedValue is not null;


    /// <summary>
    /// An option that was specified on the command line, with its raw string value if it has one
    /// </summary>
    /// <param name="optionInfo"></param>
    public InputOption(OptionInfo optionInfo)
    {
        _optionInfo = optionInfo;
        OptionInfo = optionInfo;
        if (optionInfo.Type == typeof(bool))
            _parsedValue = optionInfo.DefaultValue;
    }

    public bool TryGetArgument([NotNullWhen(true)] out string? value)
    {
        value = _argument;
        return value is not null;
    }

    public bool TryGetValue([NotNullWhen(true)] out object? value)
    {
        value = _parsedValue;
        return value is not null;
    }

    public bool TrySetValue(string value, [NotNullWhen(false)] out string? reason)
    {
        if (_argument != null || _parsedValue != null)
        {
            reason = "Option already has a value";
            return false;
        }

        if (OptionInfo.RequiredAttribute != null)
        {
            if (string.IsNullOrWhiteSpace(value) && !OptionInfo.RequiredAttribute.AllowEmptyStrings)
            {
                reason = "Option does not allow empty strings";
                return false;
            }
        }

        if (!OptionInfo.Validator(value, _optionInfo.Type, out var parsedValue, out var error))
        {
            reason = error;
            return false;
        }

        _parsedValue = parsedValue;
        _argument = value;
        reason = null;
        return true;
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
        sb.Append("--").Append(OptionInfo.LongName);

        foreach (var s in OptionInfo.ShortNames)
            sb.Append(", -").Append(s);


        if (_argument != null)
        {
            sb.Append(": ").AppendBetween('\'', '\'', _argument);
        }

        return sb;
    }

    private StringBuilder? _sb;
    private readonly OptionInfo _optionInfo;
}