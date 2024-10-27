namespace CLIAlly;

/// <summary>
/// An interface that represents the parsed command line arguments
/// The implementing classes should ideally take a <see cref="CommandConfiguration"/> and a list of arguments that were
/// passed to the application
/// </summary>
public interface ICliParser
{
    /// <summary>
    /// A list of errors that occurred during parsing - if this is not empty, you should probably print them to the console
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// A list of options that are associated with a command - like "git push --force"
    /// </summary>
    public IReadOnlyList<InputCommand> InputCommands { get; }

    /// <summary>
    /// A command is included here if the user requested help for a specific command
    /// </summary>
    public IReadOnlyList<InputCommand> CommandHelpRequested { get; }

    /// <summary>
    /// True if the user requested help for the application as a whole
    /// </summary>
    public bool AppHelpRequested { get; }

    /// <summary>
    /// The configuration used to parse the command line arguments
    /// </summary>
    public CommandConfiguration Commands { get; }

    /// <summary>
    /// The original arguments that were passed to the application
    /// </summary>
    public IReadOnlyList<string> Args { get; }
}