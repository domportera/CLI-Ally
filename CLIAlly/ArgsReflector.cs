using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace CLIAlly;

public class DynamicMethodInfo(MethodInfo method, Type? parameters, in MethodAttributes attributes)
{
    public readonly MethodInfo Method = method;
    public readonly Type? Parameters = parameters;
    public readonly MethodAttributes Attributes = attributes;
}

public class ArgsReflector
{
    // warning : this will hold on to types unless they are explicitly removed
    private static IReadOnlyDictionary<Type, JsonSerializerOptions> _jsonOptions = new Dictionary<Type, JsonSerializerOptions>();
    
    public static void SetAotJsonTypes(IReadOnlyDictionary<Type, JsonSerializerOptions> typeDict)
    {
        _jsonOptions = typeDict;
    }
    
    public static CommandConfiguration GetCommandConfiguration(Type typeContainingCommandMethods,
        bool useInheritedMethods = true)
    {
        var commandInfo = GetCommandInfo(typeContainingCommandMethods, useInheritedMethods);
        return new CommandConfiguration(commandInfo);
    }

    public static IReadOnlyList<CommandInfo> GetCommandInfo(Type typeContainingCommandMethods,
        bool useInheritedMethods = true)
    {
        var methodBindingFlags =
            useInheritedMethods ? BindingFlags.DeclaredOnly | BindingFlags.Static : BindingFlags.Static;
        
        methodBindingFlags |= BindingFlags.Public | BindingFlags.NonPublic;

        var methods = typeContainingCommandMethods.GetMethods(methodBindingFlags);

        List<CommandInfo> commands = [];
        List<string> takenNames = new();
        MethodInfo? defaultCommand = null;

        foreach (var method in methods)
        {
            var attributes = new MethodAttributes(method, useInheritedMethods);
            var commandAttribute = attributes.Command;

            if (commandAttribute is null)
                continue;

            var parameters = method.GetParameters();

            if (parameters.Length > 1)
            {
                throw new ArgumentException(
                    $"Command method '{method.Name}' must have exactly one parameter of an args type");
            }

            IReadOnlyList<OptionInfo>? options = null;
            Type? argsType = null;
            if (parameters.Length != 0)
            {
                argsType = parameters[0].ParameterType;
                options = GetOptionInfo(argsType);
            }

            var longName = GetLongName(commandAttribute, method, takenNames, out var isCaseSensitive);
            var isDefaultCommand = commandAttribute.IsDefaultCommand || methods.Length == 1;
            if (isDefaultCommand)
            {
                if (defaultCommand != null)
                    throw new ArgumentException("Only one default command can be specified");
                defaultCommand = method;
            }

            var command = new CommandInfo(
                name: longName,
                description: attributes.Description?.Description,
                furtherInformation: attributes.DetailedDescription?.Description,
                options,
                isDefaultCommand: isDefaultCommand,
                isCaseSensitive: isCaseSensitive,
                methodInfo: new DynamicMethodInfo(method, argsType, attributes));

            commands.Add(command);
        }

        return commands;
    }

    private static readonly Dictionary<Type, IReadOnlyList<OptionInfo>> OptionInfoByType = new();

    private static IReadOnlyList<OptionInfo> GetOptionInfo(Type argsType)
    {
        if (OptionInfoByType.TryGetValue(argsType, out var cachedOptions))
            return cachedOptions;

        var fields = argsType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new FieldAttributes(x))
            .Where(x => x.ArgumentDef != null)
            .OrderBy(x => x.ArgumentDef!.Order)
            .ToArray();

        var options = new OptionInfo[fields.Length];
        var nullabilityInfoContext = new NullabilityInfoContext();
        OptionValidator validator = TryParse;
        OptionValidator jsonValidator = TryParseJson;
        OptionValidator pathValidator = TryParsePath;
        List<string> takenNames = [];
        List<char> takenShorts = [];

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            var fieldType = field.FieldType;
            var argumentDef = field.ArgumentDef!;

            if (argumentDef.Order != index)
            {
                throw new ArgumentException(
                    $"Argument order must be consecutive, starting at 0. You are missing index {index}");
            }

            var longName = GetLongName(argumentDef, field.FieldInfo, takenNames, out var isLongNameCaseSensitive);
            var shortName = GetShortName(argumentDef, longName,
                out var isShortOptCaseSensitive);

            var nullabilityInfo = nullabilityInfoContext.Create(field.FieldInfo);
            var nullability = nullabilityInfo.ReadState;

            var isBoolean = fieldType == typeof(bool);

            OptionValidator myValidator;
            if (field.JsonPath != null)
                myValidator = jsonValidator;
            else if (field.Path != null)
                myValidator = pathValidator;
            else
                myValidator = validator;

            char[] shortNames = [];
            if (shortName is not null)
            {
                shortNames = isShortOptCaseSensitive
                    ? [shortName.Value]
                    : [char.ToLowerInvariant(shortName.Value), char.ToUpperInvariant(shortName.Value)];
            }

            object? defaultValue;

            if (field.DefaultValue != null)
            {
                 defaultValue = field.DefaultValue.Value;
            }
            else
            {
                defaultValue = fieldType.IsValueType ? Activator.CreateInstance(fieldType) : null;
            }

            if (defaultValue == null)
            {
                if(fieldType.IsValueType && nullability == NullabilityState.NotNull)
                    throw new ArgumentException($"Field {field.FieldInfo.Name} is not nullable, but has a null default value");
            }
            else if(!fieldType.IsInstanceOfType(defaultValue))
            {
                throw new ArgumentException($"Default value for {field.FieldInfo.Name} is not a compatible type. Expected {fieldType}, got {defaultValue.GetType()}");
            }

            options[index] = new OptionInfo(
                longName: longName,
                shortNames: shortNames,
                description: field.Description?.Description,
                furtherInformation: field.DetailedDescription?.Description,
                required: field.Required != null || (nullability != NullabilityState.Nullable && !isBoolean) ||
                          field.ExplicitBool != null,
                requiredAttribute: field.Required,
                validator: myValidator,
                type: fieldType,
                isLongNameCaseSensitive: isLongNameCaseSensitive,
                defaultValue: defaultValue,
                order: index,
                fieldInfo: field.FieldInfo,
                nullabilityInfo: nullabilityInfo
            );
        }

        OptionInfoByType[argsType] = options;
        return options;


        char? GetShortName(ArgAttribute argAttribute, string longName, out bool isShortOptCaseSensitive)
        {
            char? shortName = argAttribute.ShortOpt;
            isShortOptCaseSensitive = argAttribute.ShortNameCaseSensitive;

            if (shortName is null && !argAttribute.DisableShortOpt)
            {
                for (int index = 0; index < longName.Length; index++)
                {
                    var possibleChar = longName[index];
                    if (!char.IsLetterOrDigit(possibleChar))
                        continue;

                    if (takenShorts.Contains(possibleChar))
                        continue;

                    shortName = possibleChar;
                    break;
                }

                if (shortName is null)
                {
                    throw new ArgumentException($"Could not find an available short option for option '{longName}'. " +
                                                $"Try specifying a one in the {nameof(ArgAttribute)}.");
                }
            }

            if (shortName == null)
                return shortName;

            if (isShortOptCaseSensitive)
            {
                if (takenShorts.Contains(shortName.Value))
                {
                    ThrowAlreadyTakenException(shortName.Value);
                }

                takenShorts.Add(shortName.Value);
            }
            else
            {
                var lower = char.ToLowerInvariant(shortName.Value);
                var upper = char.ToUpperInvariant(shortName.Value);
                if (takenShorts.Contains(lower))
                {
                    ThrowAlreadyTakenException(lower);
                }

                if (takenShorts.Contains(upper))
                {
                    ThrowAlreadyTakenException(upper);
                }

                takenShorts.Add(upper);
                takenShorts.Add(lower);
            }

            void ThrowAlreadyTakenException(char c)
            {
                throw new ArgumentException(
                    $"Short option '{c}' is already taken ({longName}). Try specifying a one in the {nameof(ArgAttribute)}.");
            }

            return shortName;
        }
    }

    private static bool TryParsePath(string? value, Type type, [NotNullWhen(true)] out object? result, [NotNullWhen(false)] out string? reason)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default!;
            reason = "Path is empty";
            return false;
        }

        try
        {
            result = Path.GetFullPath(value);
            reason = null;
            return true;
        }
        catch (Exception e)
        {
            result = null;
            reason = $"Could not derive full path from '{value}' - exception thrown: {e.Message}";
            return false;
        }
    }

    /// <summary>
    /// A simple-stupid class just made to hold all the attributes.
    /// No logic other than the most basic type checking is performed here - it is simply a data container.
    /// </summary>
    private class FieldAttributes
    {
        public FieldInfo FieldInfo { get; }
        public Type FieldType => FieldInfo.FieldType;
        public readonly DescriptionAttribute? Description;
        public readonly VerboseDescriptionAttribute? DetailedDescription;
        public readonly DefaultValueAttribute? DefaultValue;
        public readonly PathAttribute? Path;
        public readonly JsonTextAttribute? JsonPath;
        public readonly ArgAttribute? ArgumentDef;
        public readonly RequiredAttribute? Required;
        public readonly ExplicitBoolAttribute? ExplicitBool;

        public FieldAttributes(FieldInfo field)
        {
            FieldInfo = field;
            var attributes = field.GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case DescriptionAttribute d:
                        Description = d;
                        continue;
                    case VerboseDescriptionAttribute dd:
                        DetailedDescription = dd;
                        continue;
                    case PathAttribute p:
                        Path = p;
                        continue;
                    case JsonTextAttribute jp:
                        JsonPath = jp;
                        continue;
                    case ArgAttribute n:
                        ArgumentDef = n;
                        continue;
                    case RequiredAttribute r:
                        Required = r;
                        continue;
                    case DefaultValueAttribute dv:
                        DefaultValue = dv;
                        continue;
                    case ExplicitBoolAttribute eb:
                        ExplicitBool = eb;
                        continue;
                }
            }

            var fieldType = field.FieldType;
            var isBoolean = fieldType == typeof(bool);

            if (ExplicitBool is not null && !isBoolean)
            {
                throw new ArgumentException($"{nameof(ExplicitBoolAttribute)} can only be applied to boolean fields");
            }

            // validate attributes
            if (JsonPath is not null)
            {
                if (fieldType.IsValueType)
                {
                    throw new ArgumentException($"{nameof(JsonTextAttribute)} can only be applied to reference types");
                }
            }

            if (Path is not null && fieldType != typeof(string))
            {
                throw new ArgumentException($"{nameof(PathAttribute)} can only be applied to string fields");
            }
        }
    }

    private static readonly string[] ReservedNames =
    {
        "help",
        "h",
        "true",
        "false",
    };

    internal static bool TryParseJson(string? s, Type type, [NotNullWhen(true)] out object? value, [NotNullWhen(false)] out string? reason)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            value = default!;
            reason = "JSON is empty";
            return false;
        }
        
        // if it's a file path, read the file. otherwise we assume it's a json string
        if(TryParsePath(s, typeof(string), out var resolvedPath, out _))
        {
            try
            {
                s = File.ReadAllText((string)resolvedPath);
                Console.WriteLine(s);
            }
            catch (Exception e)
            {
                value = default!;
                reason = $"Could not read file at '{resolvedPath}' - exception thrown: {e.Message}";
                return false;
            }
        }

        _jsonOptions.TryGetValue(type, out var options);
        
        try
        {
            value = JsonSerializer.Deserialize(s, type, options);

            if (value == null)
            {
                reason = $"Could not convert {s} to {type.Name}";
                return false;
            }

            reason = null;
            return true;
        }
        catch (JsonException e)
        {
            value = default!;
            reason = $"Could not convert json to {type.Name} - exception thrown: {e.Message}.\n'{s}'";
            return false;
        }
    }

    private static bool TryParse(string? s, Type type, [NotNullWhen(true)] out object? value, [NotNullWhen(false)] out string? reason)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            value = default!;
            reason = "Value is empty";
            return false;
        }

        try
        {
            value = TypeDescriptor.GetConverter(type).ConvertFromString(s);
            if (value == null)
            {
                reason = $"Could not convert {s} to {type.Name}";
                return false;
            }

            reason = null;
            return true;
        }
        catch (Exception e)
        {
            value = default!;
            reason = $"Could not convert {s} to {type.Name} - exception thrown: {e.Message}";
            return false;
        }
    }

    static string GetLongName(INameAttribute nameAttribute, MemberInfo member, List<string> takenNames,
        out bool isLongNameCaseSensitive)
    {
        var longName = nameAttribute.Name ?? member.Name;
        isLongNameCaseSensitive = nameAttribute.NameCaseSensitive;

        if (!IsNameValid(longName, takenNames, out var reason))
        {
            throw new ArgumentException($"Invalid option name '{longName}': {reason}");
        }

        return longName;


        static bool IsNameValid(string name, List<string> takenNames, [NotNullWhen(false)] out string? reason)
        {
            if (name.Length <= 1)
            {
                reason = "Name must be at least 2 characters long";
                return false;
            }

            var nameLower = name.ToLowerInvariant();
            if (takenNames.Contains(nameLower))
            {
                reason = "Name is already taken";
                return false;
            }

            if (ReservedNames.Contains(nameLower))
            {
                reason = "Name is reserved";
                return false;
            }

            takenNames.Add(nameLower);
            reason = null;
            return true;
        }
    }
}

public class MethodAttributes
{
    public readonly CommandAttribute? Command;
    public readonly DescriptionAttribute? Description;
    public readonly VerboseDescriptionAttribute? DetailedDescription;

    public MethodAttributes(MethodInfo method, bool inheritAttributes)
    {
        var attributes = method.GetCustomAttributes(inheritAttributes);
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case CommandAttribute c:
                    Command = c;
                    continue;
                case DescriptionAttribute d:
                    Description = d;
                    continue;
                case VerboseDescriptionAttribute dd:
                    DetailedDescription = dd;
                    continue;
            }
        }
    }
}