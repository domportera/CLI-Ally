﻿using System.Reflection;
using System.Text;

namespace CLIAlly;

/// <summary>
/// Simple helper methods that print help information to the console
/// </summary>
public static class Help
{
    /// <returns>Returns true if help was printed - you may want to exit your application in this scenario.</returns>
    public static bool PrintHelpIfRequested(this ICliParser parser, string? header = null, string? footer = null,
        StringBuilder? sb = null, StringBuilderMode sbMode = StringBuilderMode.ReturnCleared)
    {
        if (parser.AppVersionRequested)
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            Console.WriteLine(version);
            return true;
        }
        
        footer ??= "Hope this helps!";

        var helpWasRequested = false;
        sb ??= new StringBuilder(256);

        if (sbMode == StringBuilderMode.ReturnCleared)
            sb.Clear();
        
        var hasMultipleCommands = parser.Commands.CommandInfos.Count > 1;

        if (parser.AppHelpRequested)
        {
            helpWasRequested = true;

            if (header == null)
            {
                if (hasMultipleCommands)
                {
                    header = "The following are the available options and commands for this application.\n" +
                             "For more information on a specific command, use a '--help' or '-h' argument with the command name. (ex: 'git push --help')";
                }
                else
                {
                    header = "The following are the available options for this application.";
                }
            }

            sb.AppendLine(header).AppendLine();
            parser.Commands.GetFullHelpText(sb, 0, true);
        }
        else
        {
            var count = parser.CommandHelpRequested.Count;
            var hasMultipleHelpRequests = count > 1;
            
            foreach (var command in parser.CommandHelpRequested)
            {
                helpWasRequested = true;
                command.CommandInfo.AppendHelpText(sb, true, 0, true, hasMultipleHelpRequests);
            }
        }

        if (helpWasRequested)
        {
            sb.AppendLine().AppendLine(footer);
            Console.WriteLine(sb.ToString());
        }

        if (sbMode == StringBuilderMode.ReturnCleared)
            sb.Clear();

        return helpWasRequested;
    }

    public static string GetParseInfo(this ICliParser parser, StringBuilder? sb = null)
    {
        sb ??= new StringBuilder(256);
        sb.Append("Provided arguments: '").AppendJoin("', '", parser.Args).Append('\'').AppendRepeating('\n', 2);

        if (parser.Errors.Count > 0)
        {
            sb.AppendLine("Errors:");
            foreach (var error in parser.Errors)
            {
                sb.AppendLine(error);
            }
        }

        if (parser.InputCommands.Count > 0)
        {
            sb.AppendLine().AppendLine("Commands:");
            foreach (var subcommand in parser.InputCommands)
            {
                sb.AppendLine(subcommand);
            }
        }

        var result = sb.ToString();
        sb.Clear();

        return result;
    }
}