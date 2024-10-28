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


    public void AppendHelpText(StringBuilder sb, bool verbose, int indentSpaces, bool prettyPrint)
    {
        int startLinePos = sb.Length;
        const string machineSeparator = "\0\n";

        int windowWidth = 80;

        if (prettyPrint)
        {
            try
            {
                windowWidth = Console.WindowWidth - 1;
            }
            catch (Exception)
            {
                prettyPrint = false;
            }
        }

        if (prettyPrint)
            sb.AppendRepeating(' ', indentSpaces);

        sb.Append("--").Append(LongName);

        foreach (var s in ShortNames)
        {
            sb.Append(", -").Append(s);
        }

        sb.Append(prettyPrint ? ": " : machineSeparator);

        // generate type information
        var typeInformationSb = new StringBuilder();
        typeInformationSb.AppendBetweenChevrons(Type.Name).Append(' ');

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

            typeInformationSb.AppendBetweenBrackets("Default: '" + defaultValue + '\'');
        }

        if (!prettyPrint)
        {
            sb.Append(typeInformationSb);

            // insert separator to indicate description - empty or not 
            sb.Append(machineSeparator);
            if (Description != null)
            {
                sb.Append(Description);
            }

            // insert machine separator to indicate further information - empty or not
            sb.Append(machineSeparator);
            if (verbose && FurtherInformation != null)
            {
                sb.AppendLine(FurtherInformation);
            }

            sb.Append(machineSeparator);

            return;
        }

        // variable space chars for alignment
        const int targetDescriptionIndentation = 40;
        var targetCharCount = startLinePos + targetDescriptionIndentation;
        sb.AppendRepeating(HorizontalSeparatorChar, targetCharCount - sb.Length);

        // in case the name itself takes up > targetDescriptionIndentation characters, or the description gets super squished
        windowWidth = windowWidth < sb.Length - startLinePos + 20 ? int.MaxValue : windowWidth;

        var descriptionStartString = prettyPrint ? string.Format(LineStartFormat, BeginDescriptionChar.ToString()) : "";
        sb.Append(BeginDescriptionChar);

        AddSeparatorRow(sb, descriptionStartString);

        // append type information
        var lineSeparator = string.Format(LineStartFormat, DescriptionLineStart.ToString());
        typeInformationSb.Append('\n');
        AppendWrappedText(typeInformationSb.ToString(), sb, ref startLinePos, windowWidth, lineSeparator);

        // append description information
        var wroteDescription = AppendWrappedText(Description, sb, ref startLinePos, windowWidth, lineSeparator);
        if (verbose)
        {
            if (wroteDescription)
            {
                // force new line
                AppendWrappedText("\n", sb, ref startLinePos, windowWidth, lineSeparator);
                AppendWrappedText(FurtherInformation, sb, ref startLinePos, windowWidth, lineSeparator);
            }
            else
            {
                wroteDescription = AppendWrappedText(FurtherInformation, sb, ref startLinePos, windowWidth,
                    lineSeparator);
            }
        }

        if (wroteDescription)
        {
            AddSeparatorRow(sb, lineSeparator);
            // find previous line start and replace characters
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] == DescriptionLineStart)
                {
                    sb[i] = EndDescriptionChar;

                    // remove the space
                    sb[i + 1] = HorizontalSeparatorChar;
                    break;
                }
            }
        }

        sb.AppendLine();

        return;

        static bool AppendWrappedText(string? text, StringBuilder sb, ref int startLinePos, int maxWidth,
            string lineSeparator)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            for (var index = 0; index < text.Length; index++)
            {
                var c = text[index];
                var charsUntilNextWhitespace = text.IndexOf(' ', index) - index;
                if (charsUntilNextWhitespace < 0)
                    charsUntilNextWhitespace = 0;

                var currentLineCharCount = sb.Length - startLinePos;

                var charIsNewLine = c == '\n';
                if (charIsNewLine || currentLineCharCount + charsUntilNextWhitespace > maxWidth)
                {
                    sb.Append('\n');
                    startLinePos = sb.Length;
                    sb.AppendRepeating(' ', targetDescriptionIndentation);
                    sb.Append(lineSeparator);
                }

                if (!charIsNewLine) // we don't need to append a line break since we already did that
                    sb.Append(c);
            }

            return true;
        }

        void AddSeparatorRow(StringBuilder stringBuilder, string lineStartString)
        {
            if (!prettyPrint)
                return;

            // add a row of dashes to separate from the next option

            // add a newline if necessary, force-wrapping
            var currentLineCharCount = stringBuilder.Length - startLinePos;
            if (currentLineCharCount > targetDescriptionIndentation + lineStartString.Length)
                AppendWrappedText("\n", stringBuilder, ref startLinePos, windowWidth, lineStartString);

            // now add the row of dashes
            var charsRemainingInLine = windowWidth - stringBuilder.Length + startLinePos;
            stringBuilder.AppendRepeating(HorizontalSeparatorChar, charsRemainingInLine);
        }
    }

    // http://shapecatcher.com/unicode/block/Box_Drawing
    private static readonly char BeginDescriptionChar = (char)0x252C; // corner
    private static readonly char EndDescriptionChar = (char)0x2514; // corner
    private static readonly char HorizontalSeparatorChar = (char)0x2500; // horizontal line
    private static readonly char DescriptionLineStart = (char)0x2502; // vertical line
    private const string LineStartFormat = "{0} ";
}