using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CLIAlly;

/// <summary>
/// Reference implementation of <see cref="ICliParser"/> that parses command line arguments, attempting to
/// follow the POSIX/GNU conventions for command line arguments while also allowing for flexibility
/// </summary>
public sealed class CommandLineParser : ICliParser
{
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<InputCommand> InputCommands { get; }
    private IReadOnlyDictionary<InputCommand, object?> _jsonParsedArgs;
    public IReadOnlyList<InputCommand> CommandHelpRequested { get; }
    public bool AppHelpRequested { get; private set; }
    public bool AppVersionRequested { get; private set; }
    public CommandConfiguration Commands { get; }

    public IReadOnlyList<string> Args { get; }


    private readonly record struct RuntimeCommand(InputCommand Command, MethodInfo Method, object? Params);

    public readonly record struct ReservedOption(
        ReservedOptionType Type,
        string LongName,
        string[] ShortNames,
        AppliesTo AppliesTo);

    public static IReadOnlyCollection<ReservedOption> ReservedArgs => _reservedArgs;

    private static readonly ReservedOption[] _reservedArgs =
    [
        new(ReservedOptionType.Help, "--help", ["-h"], AppliesTo.Application | AppliesTo.Command),
        new(ReservedOptionType.Version, "--version", ["-v"], AppliesTo.Application),
        new(ReservedOptionType.Json, "--json", ["-j"], AppliesTo.Command),
    ];

    public enum ReservedOptionType
    {
        Help,
        Version,
        Json
    }

    [Flags]
    public enum AppliesTo
    {
        Application = 1,
        Command = 2
    }

    public static CommandLineParser FromArgs(string[] args, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var commands = ArgsReflector.GetCommandInfo(type);
        var config = new CommandConfiguration(commands);
        return new CommandLineParser(args, config);
    }

    public static CommandLineParser FromArgs(string[] args, params Type[] typesContainingCommands)
    {
        if (typesContainingCommands is null || typesContainingCommands.Length == 0)
            throw new ArgumentException("At least one type must be provided", nameof(typesContainingCommands));

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (typesContainingCommands.Any(type => type is null))
            throw new ArgumentException("Provided types cannot be null", nameof(typesContainingCommands));


        List<CommandInfo> allCommands = [];
        foreach (var type in typesContainingCommands)
        {
            var commands = ArgsReflector.GetCommandInfo(type);
            allCommands.AddRange(commands);
        }

        var config = new CommandConfiguration(allCommands);
        return new CommandLineParser(args, config);
    }

    private CommandLineParser(string[] args, CommandConfiguration commands)
    {
        // populate our fields
        var quantityArgs = args.Length;

        var myArgs = new string[quantityArgs];
        Array.Copy(args, myArgs, quantityArgs); // defensive copy
        Args = myArgs;

        List<InputCommand> inputCommands = [];
        List<InputCommand> commandHelpRequested = [];
        Dictionary<InputCommand, object?> jsonCommands = [];

        InputCommands = inputCommands;
        CommandHelpRequested = commandHelpRequested;
        Commands = commands;
        _jsonParsedArgs = jsonCommands;

        InputCommand? currentCommand = null;
        InputOption? currentOption = null;
        bool canBeOrderedArguments = true;
        int orderedArgumentIndex = 0;
        bool canUseDefaultCommand = true;

        // parsing time

        List<OptionInfo> availableOptions = [];

        for (int i = 0; i < quantityArgs; i++)
        {
            var arg = args[i];

            // handle json arguments
            if (currentCommand is { JsonRequested: true })
            {
                if (!TryHandleJson(arg))
                    return;

                continue;
            }

            var couldBeLongOption = arg.StartsWith("--");
            var couldBeShortOption = !couldBeLongOption && arg.StartsWith('-');

            if ((couldBeLongOption || couldBeShortOption) && currentCommand == null)
            {
                if (TryHandleReservedOptions(arg, null, couldBeLongOption))
                {
                    // application-level reserved option was triggered - we're done
                    return;
                }

                if (!canUseDefaultCommand || commands.DefaultCommand == null)
                {
                    // we don't have a command and can't assume one
                    _errors.Add($"Unknown argument '{arg}'");
                    return;
                }

                AddCommand(commands.DefaultCommand);

                // check for reserved options now that we have a command,
                // that way any reserved options won't be mistaken for a string argument
                if (TryHandleReservedOptions(arg, currentCommand, couldBeLongOption))
                {
                    // logic is correct but this isn't matching like it should 
                    continue;
                }
            }

            // is this a long option?
            if (couldBeLongOption)
            {
                Debug.Assert(currentCommand != null);

                // long args must be at least 4 chars including the '--'
                if (arg.Length > 3 && TryGetLongOption(arg, availableOptions, out var opt))
                {
                    // this is a command option
                    AddOption(opt, currentCommand.OptionsInternal);
                }
                else if (!TryHandleReservedOptions(arg, currentCommand, true) &&
                         !TryAddArgument(arg)) // string argument e.g. "--!foo!!--bA~~r--"
                {
                    HandleCommandError(arg, "Unrecognized argument");
                    return;
                }
            }

            // is this a short option?
            else if (couldBeShortOption)
            {
                Debug.Assert(currentCommand != null);

                // if it's just a single dash, it's not a short option
                // but we allow lengths > 2 to be a series of short options (e.g. git clean -fxd)
                string? shortOptionError = null;
                if (arg.Length > 1 && TryGetShortOptions(arg, availableOptions, out var opts, out shortOptionError))
                {
                    AddShortOptions(opts, currentCommand.OptionsInternal);
                }
                else if (!TryHandleReservedOptions(arg, currentCommand, false) &&
                         !TryAddArgument(arg)) // string argument e.g. "-foo-bar!!!"
                {
                    HandleCommandError(arg, shortOptionError ?? "Unrecognized argument");
                    return;
                }
            }
            else // it's a command or option argument
            {
                if (commands.TryGetCommand(arg, out var matchingCommand))
                {
                    AddCommand(matchingCommand);
                    continue;
                }

                if (!TryAddArgument(arg))
                    return;
            }
        }

        return;

        // Welcome to Local Method Hell.
        // The non-static methods have a good reason to be here.
        // The static ones have an o-k reason to be here - they just weren't and shouldn't be used elsewhere.
        // Just pretend this constructor is a class with a bunch of private methods.
        void HandleCommandError(string arg, string errorMessage)
        {
            var error = $"Error with argument '{arg}': " + errorMessage;
            currentCommand.AddError(error);


            var fullError = $"Command '{currentCommand.CommandInfo.Name}' --- {error}";
            _errors.Add(fullError);

            currentCommand = null;
            currentOption = null;
        }

        void AddCommand(CommandInfo cmd)
        {
            var newCommand = new InputCommand(cmd);
            inputCommands.Add(newCommand);
            currentCommand = newCommand;
            currentOption = null; // new subcommand, so we don't have an option yet
            canBeOrderedArguments = true;
            orderedArgumentIndex = 0;
            availableOptions.Clear();
            availableOptions.AddRange(cmd.Options);

            canUseDefaultCommand = false;
        }

        void AddShortOptions(IReadOnlyList<OptionInfo> opts, IList<InputOption> optionsList)
        {
            if (opts.Count == 0)
                throw new ArgumentException("No options provided");

            for (int x = 0; x < opts.Count; x++)
            {
                AddOption(opts[x], optionsList);
            }

            // only the last option in a short option list (e.g. -fxd, -fx, -f) can accept an argument
            // this is achieved automatically because the last option applied to AddOption will be the new currentOption
        }

        void AddOption(OptionInfo optionInfo, IList<InputOption> optionsList)
        {
            var option = new InputOption(optionInfo);
            currentOption = option;
            optionsList.Add(option);

            if (!canBeOrderedArguments)
                return;

            if (option.OptionInfo.Order == orderedArgumentIndex)
            {
                ++orderedArgumentIndex;
            }
            else
            {
                canBeOrderedArguments = false;
            }
        }

        static bool TryGetLongOption(string s, IList<OptionInfo> availableOptions,
            [NotNullWhen(true)] out OptionInfo? foundOption)
        {
            var attemptedOption = s.AsSpan()[2..]; // remove the '--'
            var length = attemptedOption.Length;

            for (var index = 0; index < availableOptions.Count; index++)
            {
                var option = availableOptions[index];
                var optName = option.LongName.AsSpan();
                if (length != optName.Length)
                    continue;

                var caseSensitive = option.IsLongNameCaseSensitive;

                bool matched = true;
                if (caseSensitive)
                {
                    for (int i = 0; i < length; i++)
                    {
                        if (optName[i] == attemptedOption[i])
                            continue;
                        matched = false;
                        break;
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        if (char.ToLower(optName[i]) == char.ToLower(attemptedOption[i]))
                            continue;
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                    continue;

                availableOptions.RemoveAt(index);
                foundOption = option;
                return true;
            }

            foundOption = null;
            return false;
        }

        static bool TryGetShortOptions(string s, IList<OptionInfo> availableOptions,
            [NotNullWhen(true)] out List<OptionInfo>? foundOptions, [NotNullWhen(false)] out string? error)
        {
            if (s.Length == 1)
            {
                error = $"invalid option '{s}' is not complete";
                foundOptions = null;
                return false;
            }

            if (availableOptions.Count == 0)
            {
                error = "No remaining options available for current command";
                foundOptions = null;
                return false;
            }

            var attemptedOption = s.AsSpan()[1..]; // remove the '-'

            foundOptions = null;

            foreach (var c in attemptedOption) // for each char in the short option - could be something like `-fxd`
            {
                bool found = false;
                for (var index = availableOptions.Count - 1; index >= 0; index--)
                {
                    var option = availableOptions[index];

                    if (option.ShortNames.Length == 0)
                        continue;

                    foreach (var shortName in option.ShortNames)
                    {
                        if (shortName != c) continue;

                        // found it
                        foundOptions ??= new List<OptionInfo>();
                        foundOptions.Add(option);
                        found = true;
                        availableOptions.RemoveAt(index);
                        break;
                    }
                }

                if (!found)
                {
                    error = $"Unknown short option provided: '{c}'";
                    return false;
                }
            }

            error = null;
            Debug.Assert(foundOptions != null && foundOptions.Count == attemptedOption.Length);
            return true;
        }

        bool TryAddArgument(string arg)
        {
            if (currentCommand == null)
                return false;

            if (currentOption == null && canBeOrderedArguments)
            {
                var options = currentCommand!.CommandInfo.Options;
                if (orderedArgumentIndex < options.Count)
                {
                    var option = options[orderedArgumentIndex];
                    AddOption(option, currentCommand.OptionsInternal);
                }
                else
                {
                    // indicates an out of range argument
                    canBeOrderedArguments = false;
                }
            }

            if (currentOption != null)
            {
                if (currentOption.TrySetValue(arg, out var errorReason))
                {
                    // we set the current option's value - we're done with it
                    currentOption = null;
                    return true;
                }

                _errors.Add($"Invalid value '{arg}' provided to option '{currentOption.OptionInfo.LongName} " +
                            $"({string.Join(", ", currentOption.OptionInfo.ShortNames)})': {errorReason}");
                return false;
            }


            _errors.Add($"Unknown command or argument '{arg}'");
            return false;
        }

        bool TryHandleReservedOptions(string s, InputCommand? cmd, bool isLongOption)
        {
            var reservedArgs = cmd == null
                ? _reservedArgs.Where(arg => arg.AppliesTo.HasFlag(AppliesTo.Application))
                : _reservedArgs.Where(arg => arg.AppliesTo.HasFlag(AppliesTo.Command));

            ReservedOption? triggeredArg = null;
            foreach (var reservedArg in reservedArgs)
            {
                if (isLongOption)
                {
                    if (s == reservedArg.LongName)
                    {
                        triggeredArg = reservedArg;
                        break;
                    }
                }
                else
                {
                    foreach (var shortName in reservedArg.ShortNames)
                    {
                        if (s == shortName)
                        {
                            triggeredArg = reservedArg;
                            break;
                        }
                    }
                }
            }

            if (triggeredArg == null)
            {
                return false;
            }

            switch (triggeredArg.Value.Type)
            {
                case ReservedOptionType.Help:
                {
                    if (cmd == null)
                        AppHelpRequested = true;
                    else
                        commandHelpRequested.Add(cmd);

                    return true;
                }
                case ReservedOptionType.Version:
                {
                    if (cmd != null)
                    {
                        throw new Exception(
                            "This is a bug - version should only be requested at the application level");
                    }

                    if (inputCommands.Count > 0)
                    {
                        //"Version cannot be requested with other commands present";
                        return false;
                    }

                    AppVersionRequested = true;
                    return true;
                }
                case ReservedOptionType.Json:
                {
                    if (cmd == null)
                    {
                        throw new Exception("This is a bug - json should only be requested at the command level");
                    }

                    // Json-based command arguments cannot be used with other options so we ignore it
                    if (cmd.Options.Count != 0)
                    {
                        return false;
                    }

                    // if the command doesnt accept any options, it certainly doesn't accept json
                    if (cmd.CommandInfo.AcceptsOption)
                    {
                        cmd.JsonRequested = true;
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        bool TryHandleJson(string arg)
        {
            var type = currentCommand.CommandInfo.MethodInfo.Parameters;
            if (type == null)
            {
                HandleCommandError(arg, "Command does not accept any parameters");
                return false;
            }

            if (ArgsReflector.TryParseJson(arg, type, out var parsed, out var failureReason))
            {
                jsonCommands.Add(currentCommand, parsed);
                currentCommand = null;
                currentOption = null;
            }
            else
            {
                HandleCommandError(arg, $"Error parsing JSON: {failureReason}");
                return false;
            }

            return true;
        }
    }

    public ExitCodeInfo TryInvokeCommands(bool writeOutputToConsole = true)
    {
        if (InputCommands.Count == 0)
        {
            ExitCodeInfo.FromInvalidArgs("No commands provided");
        }

        if (Errors.Count > 0)
        {
            var errors = string.Join('\n', Errors);
            return ExitCodeInfo.FromInvalidArgs(errors);
        }

        List<RuntimeCommand> runtimeCommands = [];
        foreach (var command in InputCommands)
        {
            if (_jsonParsedArgs.TryGetValue(command, out var jsonResult))
            {
                if (jsonResult is null)
                {
                    return ExitCodeInfo.FromInvalidArgs();
                }

                // todo - check that the json result has all [Required] fields filled
                runtimeCommands.Add(new RuntimeCommand(command, command.CommandInfo.MethodInfo.Method, jsonResult));
                continue;
            }

            // check if the command is fully complete and valid
            if (!command.IsValid())
            {
                var errors = string.Join('\n', command.Errors);
                return ExitCodeInfo.FromInvalidArgs(errors);
            }

            var dynamicMethodInfo = command.CommandInfo.MethodInfo;
            var method = dynamicMethodInfo.Method;

            var paramsType = dynamicMethodInfo.Parameters;

            if (paramsType == null)
            {
                if (command.Options.Count > 0)
                {
                    return ExitCodeInfo.FromInvalidArgs("Command does not accept any options but we received some?");
                }

                runtimeCommands.Add(new RuntimeCommand(command, method, null));
                continue;
            }

            object? argsInstance;
            try
            {
                argsInstance = Activator.CreateInstance(paramsType);
                if (argsInstance == null)
                {
                    return ExitCodeInfo.FromFailure(
                        $"Error creating instance of type '{paramsType.Name}': result was null.");
                }
            }
            catch (Exception e)
            {
                var errors = $"Error creating instance of type '{paramsType.Name}': {e.Message}";
                return ExitCodeInfo.FromFailure(errors);
            }

            foreach (var inputOption in command.Options)
            {
                var optionInfo = inputOption.OptionInfo;
                var fieldInfo = optionInfo.FieldInfo;

                if (!inputOption.TryGetValue(out var value))
                {
                    var errors = $"Error getting value for option '{optionInfo.LongName}': {value}";
                    return ExitCodeInfo.FromFailure(errors);
                }

                try
                {
                    fieldInfo.SetValue(argsInstance, value);
                }
                catch (Exception e)
                {
                    return ExitCodeInfo.FromException($"Error setting value for option '{optionInfo.LongName}'", e);
                }
            }

            runtimeCommands.Add(new RuntimeCommand(command, method, argsInstance));
        }

        StringBuilder output = new();
        // actually execute commands
        foreach (var runtimeCommand in runtimeCommands)
        {
            var method = runtimeCommand.Method;
            var args = runtimeCommand.Params;
            try
            {
                var result = method.Invoke(null, args == null ? null : [args]);
                switch (result)
                {
                    case ExitCodeInfo exitCode:
                    {
                        if (writeOutputToConsole && !string.IsNullOrEmpty(exitCode.Message))
                        {
                            Printer.WriteLine(exitCode.Message);
                        }

                        if (!exitCode.IsSuccess)
                        {
                            return exitCode;
                        }

                        break;
                    }
                    case string str:
                    {
                        if (writeOutputToConsole)
                        {
                            Printer.WriteLine(str);
                        }

                        output.Append(str);
                        break;
                    }
                    case not null:
                    {
                        var str = result.ToString();
                        if (writeOutputToConsole)
                        {
                            Printer.WriteLine(str);
                        }

                        output.Append(str);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                var error = $"Error invoking command {runtimeCommand.Command.CommandInfo.Name} " +
                            $"via method '{method.Name}'";
                return ExitCodeInfo.FromException(error, e);
            }
        }

        return ExitCodeInfo.FromSuccess(output.ToString());
    }
}