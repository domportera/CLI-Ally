using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using CLIAlly;

namespace Example;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] // gets rid of IDE warnings about unused/uninitialized fields
[Serializable] // allows the entire arguments object to be input as json (via file or inline) instead of by individual arguments
internal class MyCommandArgs // note that this can be any type at all - no special attribute required
{
    [Arg(0)] [Description("The way your victim dies")]
    public required string ModeOfDeath;

    [Arg(1), Range(1, 10), DefaultValue(4)] [Description("The number of times your victim dies")]
    public int Quantity; // optional, defaults to 4

    // defaults to false unless [DefaultValue(true)]
    [Arg(2)] [Description("Whether or not they are doomed to die again in the future")]
    public bool Doom; // bools are

    [Arg(3), Path] [Description("Optional path to describe death in further detail")]
    [VerboseDescription("Try using the included file 'details.txt' to see how it works!")]
    public string? DeathTextPath;

    [Arg(4), JsonText]
    [Description("Path to a JSON file identifying your victim. Includes only \"Name\" (strings) and \"Age\" (integer) fields.")]
    [VerboseDescription("An example JSON file is included, called 'Person.json'.\n" + 
                        """
                        The full format for this example is:
                        {
                           "Name": "Some name",
                           "Age": 66
                        }
                        """)]
    public PersonWhoDies? PersonWhoDies;

    // Value types are always required unless a default value is specified, or they are marked with nullable.
    // booleans are always optional unless required, as their exclusion suggests that their value is false.
    [Arg(5)] public int? ExampleUndocumentedArgument;
}