using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace CLIAlly;

public delegate bool OptionValidator(string? value, Type type, out object? result,
    [NotNullWhen(false)] out string? error);

/// <summary>
/// Defines an option that can be specified on the command line
/// Can belong to either a subcommand or the root application itself
/// This definition does not contain the actual value of the option - it only describes it and validates potential values
/// </summary>
public record OptionInfo
{
    public readonly string LongName;
    public readonly char[] ShortNames;
    public readonly string? Description;
    public readonly string? FurtherInformation;
    public readonly object? DefaultValue;
    public readonly bool Required;
    public readonly RequiredAttribute? RequiredAttribute;
    public readonly OptionValidator Validator;
    public readonly Type Type;
    public readonly bool IsLongNameCaseSensitive;
    public readonly int Order;
    public readonly FieldInfo FieldInfo;

    public OptionInfo(string longName, char[] shortNames, string? description, OptionValidator validator,
        bool required, RequiredAttribute? requiredAttribute, string? furtherInformation, Type type,
        bool isLongNameCaseSensitive, object? defaultValue, int order, FieldInfo fieldInfo)
    {
        LongName = longName;
        ShortNames = shortNames;
        Description = description;
        Required = required;
        RequiredAttribute = requiredAttribute;
        Validator = validator;
        FurtherInformation = furtherInformation;
        Type = type;
        IsLongNameCaseSensitive = isLongNameCaseSensitive;
        DefaultValue = defaultValue;
        Order = order;
        FieldInfo = fieldInfo;

        if (required && validator == null)
        {
            throw new ArgumentException("A required option must accept an argument", nameof(validator));
        }
    }


    public void AppendHelpText(StringBuilder sb, bool verbose, int indentSpaces)
    {
        int startLinePos = sb.Length;
        sb.AppendRepeating(' ', indentSpaces);

        const int targetDescriptionIndentation = 40;

        sb.Append("--").Append(LongName);

        foreach (var s in ShortNames)
        {
            sb.Append(", -").Append(s);
        }

        sb.Append(": ");

        // variable space chars for alignment
        var targetCharCount = startLinePos + targetDescriptionIndentation - 1; // - 1 for the space after these dashes
        sb.AppendRepeating('-', targetCharCount - sb.Length);
        sb.Append(' ');

        int windowWidth;
        try
        {
            windowWidth = Console.WindowWidth - 1;
        }
        catch (Exception)
        {
            windowWidth = int.MaxValue;
        }

        // in case the name itself takes up > targetDescriptionIndentation characters, or the description gets super squished
        windowWidth = windowWidth < sb.Length - startLinePos + 20 ? int.MaxValue : windowWidth;
        
        // generate type information
        var typeInformationSb = new StringBuilder();
        typeInformationSb.Append("| ").Append(Type.Name).Append(' ');

        if (Required)
        {
            typeInformationSb.AppendBetweenBrackets("Required");
        }
        else
        {
            var defaultValue = DefaultValue switch
            {
                null => "null",
                string s => s,
                _ => DefaultValue.ToString() ?? "null"
            };

            typeInformationSb.AppendBetweenBrackets("Default value: '" + defaultValue + '\'');
        }
        
        typeInformationSb.Append(" | ");

        // append type information
        for(int i = 0; i < typeInformationSb.Length; i++)
        {
            Wrap(sb, ref startLinePos, windowWidth);
            sb.Append(typeInformationSb[i]);
        }

        // append description information
        if (Description != null)
        {
            foreach (var c in Description)
            {
                Wrap(sb, ref startLinePos, windowWidth);
                sb.Append(c);
            }
        }

        if (verbose && FurtherInformation != null)
        {
            foreach (var c in FurtherInformation)
            {
                Wrap(sb, ref startLinePos, windowWidth);
                sb.Append(c);
            }
        }

        sb.AppendLine();

        return;

        static void Wrap(StringBuilder sb, ref int startLinePos, int maxWidth)
        {
            if (sb.Length - startLinePos <= maxWidth) return;

            sb.AppendLine();
            startLinePos = sb.Length;
            sb.AppendRepeating(' ', targetDescriptionIndentation);
        }
    }
}