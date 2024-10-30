
namespace CLIAlly;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute, INameAttribute
{
    public string? Name { get; init; }
    public bool NameCaseSensitive { get; init; } = false;
    public bool IsDefaultCommand { get; init; } = false;
}

[AttributeUsage(AttributeTargets.Field)] // todo: not actually implemented?
public class ExplicitBoolAttribute : Attribute; // requires that a boolean argument be explicitly set to "true" or "false"


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class VerboseDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

[AttributeUsage(AttributeTargets.Field)]
public class PathAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public class JsonTextAttribute : Attribute;


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
public class ArgAttribute(int order) : Attribute, INameAttribute
{
    public string? Name { get; init; }
    public bool NameCaseSensitive { get; init; } = false;
    public bool ShortNameCaseSensitive { get; init; } = false;
    
    public int Order { get; init; } = order;
    
    public char? ShortOpt { get; init; }
    
    public bool DisableShortOpt { get; init; } = false;
}

internal interface INameAttribute
{
    public string? Name { get; }
    public bool NameCaseSensitive { get; }
}