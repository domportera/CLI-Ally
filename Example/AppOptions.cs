using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using CLIAlly;

namespace Example;

internal static class MyClassWithCommands
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <returns>Returning <see cref="ExitCodeInfo"/> is optional, and can be used to exit the application with more detailed information.
    /// There's little reason not to do this. </returns>
    [Command(IsDefaultCommand = true)]
    public static ExitCodeInfo MyCommand(CommandArgs args)
    {
        var person = args.PersonWhoDies ?? new PersonWhoDies();

        var additionalDeathText = "";
        if (args.DeathTextPath != null)
        {
            try
            {
                var text = File.ReadAllText(args.DeathTextPath);
                additionalDeathText = text;
            }
            catch (Exception e)
            {
                return ExitCodeInfo.FromException("Failed to read death text", e);
            }
        }

        var output =
            $"{person.Name} has died of {args.ModeOfDeath} at the age of {person.Age}. {additionalDeathText}\n" +
            $"They have unfortunately done so {args.Quantity} times. " +
            $"They are {
                (args.Doom
                    ? "doomed to die again."
                    : "granted 400 gold pieces for their trouble.")
            }";

        return ExitCodeInfo.FromSuccess(output);
    }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes
    .PublicFields)] // gets rid of IDE warnings about unused/uninitialized fields
[Serializable] // allows the entire arguments object to be input as json (via file or inline) instead of by individual arguments
internal class CommandArgs
{
    [Arg(0)] [Description("The way your victim dies")]
    public required string ModeOfDeath;

    [Arg(1), Range(1, 10), DefaultValue(4)] [Description("The number of times your victim dies")]
    public int Quantity; // optional, defaults to 4

    // defaults to false unless [defaultvalue(true)]
    [Arg(2)] [Description("Whether or not they are doomed to die again in the future")]
    public bool Doom; // bools are

    [Arg(3), Path] [Description("Optional path to describe death in further detail")]
    public string? DeathTextPath;

    [Arg(4), JsonPath]
    [Description(
        "Path to a JSON file identifying your victim. Includes only \"Name\" (strings) and \"Age\" (integer) fields.")]
    public PersonWhoDies? PersonWhoDies;
}

[Serializable]
internal class PersonWhoDies
{
    public string Name = "John Doe";
    public int Age = 42;
}