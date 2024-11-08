using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
public class OptionInfo
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
    public bool IsNullable => _nullabilityInfo.ReadState == NullabilityState.Nullable;
    private readonly NullabilityInfo _nullabilityInfo;

    public OptionInfo(string longName, char[] shortNames, string? description, OptionValidator validator,
        bool required, RequiredAttribute? requiredAttribute, string? furtherInformation, Type type,
        bool isLongNameCaseSensitive, object? defaultValue, int order, FieldInfo fieldInfo, NullabilityInfo nullabilityInfo)
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
        _nullabilityInfo = nullabilityInfo;

        if (required && validator == null)
        {
            throw new ArgumentException("A required option must accept an argument", nameof(validator));
        }
    }


    public void AppendHelpText(StringBuilder sb, bool verbose, int indentSpaces, bool prettyPrint)
    {
        const string machineSeparator = "\0\n";

        int startLinePos = sb.Length;
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
        var nameStringCount = sb.Length - startLinePos;

        // generate type information
        var typeInformationSb = new StringBuilder();
        
        
        string typeName = IsNullable ? Nullable.GetUnderlyingType(Type)?.Name ?? Type.Name : Type.Name;
        typeInformationSb.AppendBetweenChevrons(typeName).Append(' ');

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
        var writtenSoFar = sb.Length - startLinePos;

        // determine description indentation
        int preferredDescriptionPadding = Math.Max(windowWidth / 10, 1);
        const int minDescriptionPadding = 1;
        const int preferredDescriptionIndentation = 24;
        const int preferredMinLineLengthOfDescription = 20;
        int descIndent = preferredDescriptionIndentation + preferredDescriptionPadding;
        //int descIndent = writtenSoFar + preferredDescriptionPadding;

        if (windowWidth < descIndent + preferredMinLineLengthOfDescription)
        {
            descIndent = writtenSoFar + minDescriptionPadding;
        }

        while (windowWidth < descIndent + preferredMinLineLengthOfDescription)
        {
            descIndent--;
        }

        descIndent = Math.Max(descIndent, 0);

        var targetCharCount = startLinePos + descIndent;
        sb.AppendRepeating(HorizontalSeparatorChar, targetCharCount - sb.Length);


        // in case the name itself takes up > targetDescriptionIndentation characters, or the description gets super squished

        var descriptionStartString = BeginDescriptionChar.ToString();
        
        if(descIndent > nameStringCount)
            sb.Append(BeginDescriptionChar);

        AddSeparatorRow(sb, descriptionStartString, descIndent);

        // append type information
        var lineSeparator = string.Format(LineStartFormat, DescriptionLineStart.ToString());
        var endLineSeparator = $"{EndDescriptionChar}{HorizontalSeparatorChar}";
        AppendWrappedText(typeInformationSb.ToString(), sb, ref startLinePos, windowWidth, lineSeparator, descIndent);
        NextLine(sb, lineSeparator, descIndent);

        // append description information
        var wroteDescription =
            AppendWrappedText(Description, sb, ref startLinePos, windowWidth, lineSeparator, descIndent);

        var hasFurtherInfo = !string.IsNullOrEmpty(FurtherInformation);
        if (wroteDescription)
        {
            var separator = hasFurtherInfo ? lineSeparator : endLineSeparator;
            NextLine(sb, separator, descIndent);
            if (hasFurtherInfo) // spacing between description and further info
                NextLine(sb, separator, descIndent);
        }

        if (verbose && hasFurtherInfo)
        {
            // write further information like normal
            wroteDescription |= AppendWrappedText(FurtherInformation, sb, ref startLinePos, windowWidth, lineSeparator,
                descIndent);
        }

        // draw the bottom border
        if (wroteDescription)
        {
            AddSeparatorRow(sb, endLineSeparator, descIndent);
        }
        else
        {
            // we're on the last line, which is empty and starts with `lineSeparator` 
            // but we want `endLineSeparator` so we're gonna replace it
            sb[^2] = EndDescriptionChar;
            sb[^1] = HorizontalSeparatorChar;

            // draw the line
            FillHorizontalLine(sb, windowWidth, startLinePos);
        }

        sb.AppendLine();

        return;

        static bool AppendWrappedText(string? text, StringBuilder sb, ref int startLinePos, int maxLineLength,
            string lineStartText, int indentationSpaces)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            int GetCurrentLineLength(in int startLinePos) => sb.Length - startLinePos;

            const char newLine = '\n';

            int i = 0;
            while (i < text.Length)
            {
                var currentLineLength = GetCurrentLineLength(startLinePos);
                var lineRemaining = maxLineLength - currentLineLength;
                if (lineRemaining <= 0)
                {
                    StartNewLine(ref i, out startLinePos);
                    continue;
                }
                
                var remainingCharCount = text.Length - i;

                // fill line or until end of text
                var maxLength = Math.Min(remainingCharCount, lineRemaining);
                var segmentEndIndex = i + maxLength - 1;

                Debug.Assert(segmentEndIndex >= i);

                // find an appropriate place to fill the line and do so
                var chosenMaxIndex = segmentEndIndex;
                bool foundWhitespace = false;
                
                for (int j = segmentEndIndex; j >= i; --j)
                {
                    if(char.IsWhiteSpace(text[j]))
                    {
                        foundWhitespace = true;
                        chosenMaxIndex = j;
                        break;
                    }
                }

                if (!foundWhitespace)
                {
                    // if no whitespace was found, then just fill the line to the end
                    chosenMaxIndex = segmentEndIndex;
                }

                bool enteredNewline = false;
                // fill the line
                for (; i <= chosenMaxIndex; ++i)
                {
                    var c = text[i];
                    if (c == newLine)
                    {
                        StartNewLine(ref i, out startLinePos);
                        enteredNewline = true;
                        break;
                    }
                    
                    sb.Append(c);
                }
                
                // check if we need to start a new line
                // if we didn't interrupt the previous loop with a newline and we had found whitespace, 
                // then we need to start a new line
                if (!enteredNewline && foundWhitespace && text.Length - i + GetCurrentLineLength(startLinePos) > maxLineLength)
                {
                    StartNewLine(ref i, out startLinePos);
                }
            }

            return true;

            void StartNewLine(ref int atTextIndex, out int newStartLinePos)
            {
                // if we are not at a newline character, then it is a forced newline
                if (text[atTextIndex] != newLine)
                {
                    // skip all whitespace until the next printable character - a non-whitespace character or a newline
                    bool IsPrintable(char c) => c == newLine || !char.IsWhiteSpace(c);
                    while (atTextIndex < text.Length && !IsPrintable(text[atTextIndex]))
                    {
                        ++atTextIndex;
                    }
                    
                    // the final printable char here might be a newline, so we allow the next block
                    // to check for that even though we technically just did
                }
                
                // skip the newline character as we are appending it
                if(atTextIndex < text.Length && text[atTextIndex] == newLine)
                {
                    // skip the newline character as we are appending it 
                    atTextIndex++;
                }

                sb.Append(newLine);
                newStartLinePos = sb.Length;
                sb.AppendRepeating(' ', indentationSpaces);
                sb.Append(lineStartText);
            }
        }


        void NextLine(StringBuilder builder, string lineStartString, int indentationSpaces)
        {
            AppendWrappedText("\n", builder, ref startLinePos, windowWidth, lineStartString, indentationSpaces);
        }

        void AddSeparatorRow(StringBuilder stringBuilder, string lineStartString, int indentationSpaces)
        {
            if (!prettyPrint)
                return;

            // add a row of dashes to separate from the next option

            // add a newline if necessary, force-wrapping
            var currentLineCharCount = stringBuilder.Length - startLinePos;
            if (currentLineCharCount > descIndent + lineStartString.Length)
                AppendWrappedText("\n", stringBuilder, ref startLinePos, windowWidth, lineStartString,
                    indentationSpaces);

            // now add the row of dashes
            FillHorizontalLine(stringBuilder, windowWidth, startLinePos);
        }

        static void FillHorizontalLine(StringBuilder stringBuilder, int windowWidth, int startLinePos)
        {
            var charsRemainingInLine = windowWidth - (stringBuilder.Length - startLinePos);
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