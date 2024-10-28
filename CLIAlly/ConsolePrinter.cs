using System.Globalization;
using System.Text.RegularExpressions;

namespace CLIAlly;

/// <summary>
/// Allows you to fearlessly print colored strings without worrying about inter-process communication getting confused.
/// For ease of coloring, you may want to consider using a library such as <a href="https://github.com/silkfire/Pastel">Pastel</a>
/// </summary>
public static partial class Printer // mimic interface of System.Console
{
    //[GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~](?!\n)")]
    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex CreateAnsiRemoverRegex();

    private static readonly Regex AnsiRemover = CreateAnsiRemoverRegex();

    private static IFormatProvider? _internalFormatProvider;
    private static IFormatProvider FormatProvider => _internalFormatProvider ??= CultureInfo.CurrentCulture;

    private static readonly bool IsConsoleRedirected = Console.IsOutputRedirected;

    public static void WriteLine(string? value, bool containsAnsi = true)
    {
        Write(value);
        Console.WriteLine();
    }

    public static void WriteLine() => Console.WriteLine();

    public static void Write(string? value, bool containsAnsi = true)
    {
        if (value == null)
            return;

        if (containsAnsi && IsConsoleRedirected)
        {
            value = AnsiRemover.Replace(value, "");
        }

        Console.Write(value);
    }


    public static void Write(string format, params object[] arg)
    {
        var value = string.Format(FormatProvider, format, arg);
        Write(value);
    }

    public static void WriteLine(string format, params object[] arg)
    {
        var value = string.Format(FormatProvider, format, arg);
        WriteLine(value);
    }

    public static void WriteLine(string format, object arg0)
    {
        var value = string.Format(FormatProvider, format, arg0);
        WriteLine(value);
    }

    public static void WriteLine(string format, object arg0, object arg1)
    {
        var value = string.Format(FormatProvider, format, arg0, arg1);
        WriteLine(value);
    }

    public static void WriteLine(string format, object arg0, object arg1, object arg2)
    {
        var value = string.Format(FormatProvider, format, arg0, arg1, arg2);
        WriteLine(value);
    }

    public static void Write(string format, object arg0, object arg1)
    {
        var value = string.Format(FormatProvider, format, arg0, arg1);
        Write(value);
    }

    public static void Write(string format, object arg0, object arg1, object arg2)
    {
        var value = string.Format(FormatProvider, format, arg0, arg1, arg2);
        Write(value);
    }
    
    public static void Write(char value)
    {
        if (IsConsoleRedirected && char.IsControl(value))
            return;

        Console.Write(value);
    }

    public static void WriteLine(char value)
    {
        Write(value);
        Console.WriteLine();
    }

    public static void Write(char[] buffer, bool containsAnsi = true) => Write(buffer, 0, buffer.Length, containsAnsi);

    public static void WriteLine(char[] buffer, bool containsAnsi = true)
    {
        Write(buffer, 0, buffer.Length, containsAnsi);
        Console.WriteLine();
    }

    public static void Write(char[] buffer, int index, int count, bool containsAnsi = true)
    {
        if (!IsConsoleRedirected || !containsAnsi)
        {
            Console.Write(buffer);
            return;
        }
        
        var maxIndexExclusive = index + count;
        for (int i = index; i < maxIndexExclusive; ++i)
        {
            var c = buffer[i];
            if (char.IsControl(c))
                continue;

            Console.Write(c);
        }
    }
}