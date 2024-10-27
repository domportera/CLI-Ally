using System.Text;

namespace CLIAlly;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendBetween(this StringBuilder sb, char start, char end, string value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, string start, string end, char value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, string start, string end, string value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char start, string end, char value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char start, string end, string value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, string start, char end, char value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, string start, char end, string value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char start, char end, char value)
    {
        return sb.Append(start).Append(value).Append(end);
    }

    public static StringBuilder AppendRepeating(this StringBuilder sb, char c, int count)
    {
        for (int i = 0; i < count; i++)
        {
            sb.Append(c);
        }

        return sb;
    }

    public static StringBuilder AppendRepeating(this StringBuilder sb, string s, int count)
    {
        for (int i = 0; i < count; i++)
        {
            sb.Append(s);
        }

        return sb;
    }

  

    public static StringBuilder AppendBetween(this StringBuilder sb, string startAndEnd, string value)
    {
        return sb.Append(startAndEnd).Append(value).Append(startAndEnd);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char startAndEnd, string value)
    {
        return sb.Append(startAndEnd).Append(value).Append(startAndEnd);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, string startAndEnd, char value)
    {
        return sb.Append(startAndEnd).Append(value).Append(startAndEnd);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char startAndEnd, char value)
    {
        return sb.Append(startAndEnd).Append(value).Append(startAndEnd);
    }

    public static StringBuilder AppendBetweenBrackets(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('[', ']', value);
    }

    public static StringBuilder AppendBetweenBrackets(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('[', ']', value);
    }

    public static StringBuilder AppendBetweenBraces(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('{', '}', value);
    }

    public static StringBuilder AppendBetweenBraces(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('{', '}', value);
    }

    public static StringBuilder AppendBetweenParentheses(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('(', ')', value);
    }

    public static StringBuilder AppendBetweenParentheses(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('(', ')', value);
    }

    public static StringBuilder AppendBetweenChevrons(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('<', '>', value);
    }

    public static StringBuilder AppendBetweenChevrons(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('<', '>', value);
    }

    public static StringBuilder AppendBetweenDoubleQuotes(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('"', '"', value);
    }

    public static StringBuilder AppendBetweenDoubleQuotes(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('"', '"', value);
    }

    public static StringBuilder AppendBetweenSingleQuotes(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('\'', '\'', value);
    }

    public static StringBuilder AppendBetweenSingleQuotes(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('\'', '\'', value);
    }

    public static StringBuilder AppendBetweenBackticks(this StringBuilder sb, string value)
    {
        return sb.AppendBetween('`', '`', value);
    }

    public static StringBuilder AppendBetweenBackticks(this StringBuilder sb, char value)
    {
        return sb.AppendBetween('`', '`', value);
    }
}

public static class StringBuilderBuildsStrings
{
    public static StringBuilder Append(this StringBuilder sb, IBuildStrings value)
    {
#if !DEBUG
        return value.AppendStringTo(sb);
#else
        return ValidatedAppend(sb, value);
#endif
    }

    public static StringBuilder AppendLine(this StringBuilder sb, IBuildStrings value)
    {
        return Append(sb, value).AppendLine();
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char start, char end, IBuildStrings value)
    {
        sb.Append(start);
        return Append(sb, value).Append(end);
    }

    public static StringBuilder AppendBetween(this StringBuilder sb, char startAndEnd, IBuildStrings value)
    {
        sb.Append(startAndEnd);
        return Append(sb, value).Append(startAndEnd);
    }


    public static StringBuilder AppendBetweenBrackets(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('[', ']', value);
    }

    public static StringBuilder AppendBetweenBraces(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('{', '}', value);
    }

    public static StringBuilder AppendBetweenParentheses(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('(', ')', value);
    }

    public static StringBuilder AppendBetweenChevrons(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('<', '>', value);
    }

    public static StringBuilder AppendBetweenDoubleQuotes(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('"', '"', value);
    }

    public static StringBuilder AppendBetweenSingleQuotes(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('\'', '\'', value);
    }

    public static StringBuilder AppendBetweenBackticks(this StringBuilder sb, IBuildStrings value)
    {
        return sb.AppendBetween('`', '`', value);
    }

    public static StringBuilder AppendJoin(this StringBuilder sb, string separator, params IBuildStrings[] values)
    {
        if (values.Length == 0)
            return sb;

        values[0].AppendStringTo(sb);
        for (int i = 1; i < values.Length; i++)
        {
            sb.Append(separator);
            values[i].AppendStringTo(sb);
        }

        return sb;
    }

    public static StringBuilder AppendJoin(this StringBuilder sb, char separator, params IBuildStrings[] values)
    {
        if (values.Length == 0)
            return sb;

        values[0].AppendStringTo(sb);
        for (int i = 1; i < values.Length; i++)
        {
            sb.Append(separator);
            values[i].AppendStringTo(sb);
        }

        return sb;
    }

#if DEBUG
    private static StringBuilder ValidatedAppend(StringBuilder sb, IBuildStrings value)
    {
        var returnedSb = value.AppendStringTo(sb);
        if (sb != returnedSb)
        {
            const string error = "The StringBuilder returned from AppendStringTo must be the same as the one passed in";
            throw new InvalidOperationException(error);
        }

        return sb;
    }
#endif
}

public enum StringBuilderMode
{
    Append,
    ReturnCleared
};

public interface IBuildStrings
{
    StringBuilder AppendStringTo(StringBuilder sb)
    {
        sb.Append(ToString());
        return sb;
    }
}