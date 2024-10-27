using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CLIAlly;

namespace CommandLineParserDemo;


internal static class MyClassWithCommands
{
    [Command(IsDefaultCommand = true)]
    public static void MyCommand(CommandArgs args)
    {
        var person = args.PersonWhoDies ?? new();

        Console.WriteLine($"{person.Name} has died of {args.ModeOfDeath} at the age of {person.Age}. " +
                          $"They have unfortunately died {args.Quantity} times. " +
                          $"You are {
                              (args.Doom 
                                  ? "doomed to die again." 
                                  : "granted 400 gold pieces for your trouble.")
                          }");
    }
}

internal class CommandArgs
{
    [Arg(0)]
    public required string ModeOfDeath;
    
    [Arg(1), Range(1, 10), DefaultValue(4)]
    public int Quantity; // optional, defaults to 4

    // defaults to false unless [defaultvalue(true)]
    [Arg(2)]
    public bool Doom; // bools are

    [Arg(3), Path]
    public string? DeathTextPath;
    
    [Arg(4), JsonPath]
    public PersonWhoDies? PersonWhoDies; 
}

[Serializable]
internal class PersonWhoDies
{
    public PersonWhoDies()
    {
        Console.WriteLine(typeof(PersonWhoDiesJsonContext).FullName);
    }
    [JsonInclude]
    public string Name = "John Doe";
    [JsonInclude]
    public int Age = 42;
}