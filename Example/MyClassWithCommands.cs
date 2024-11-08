using System.ComponentModel;
using CLIAlly;

namespace Example;

internal static class MyClassWithCommands
{
    /// <returns>Returning <see cref="ExitCodeInfo"/> is optional, and can be used to exit the application with more
    /// detailed information. </returns>
    [Command(IsDefaultCommand = true, Name = "Tragedy")]
    [Description("This is the default command, and will be executed if no other command is found. " +
                 "It is unfortunately very, very sad :(")]
    public static ExitCodeInfo MyCommand(MyCommandArgs args)
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
            $"They have unfortunately died {args.Quantity} {(args.Quantity == 1 ? "time" : "times")}. " +
            $"They are {
                (args.Doom
                    ? "doomed to die again."
                    : "granted 400 gold pieces for their trouble.")
            }";

        return ExitCodeInfo.FromSuccess(output);
    }

    [Command(Name="Judge")]
    [Description("Judges you for your inputs.")]
    public static ExitCodeInfo MyOtherCommand(MyCommandArgs args)
    {
        var person = args.PersonWhoDies ?? new PersonWhoDies();

        if (args.DeathTextPath != null)
        {
            Printer.WriteLine("You've chosen to add some details, eh? You must think you're some kind of story teller.");
            Printer.WriteLine("Let me read... Fetching from '{0}'...", args.DeathTextPath);
            
            var comment = File.Exists(args.DeathTextPath) 
                ? "Judging by the first few words, I think I'd rather not."
                : "I can't even find the file... I'm probably better off.";
            
            Printer.WriteLine(comment);
        }

        if (args.Quantity > 1)
        {
            Printer.WriteLine("You've chosen to kill {0} {1} times, eh? I'm not sure I'd be able to handle that, tough guy", person.Name, args.Quantity);
        }
        
        Printer.WriteLine($"Oh, you're trying kill {person.Name} by {args.ModeOfDeath}??? You sick f-");

        Printer.WriteLine(args.Doom ? "And you're cruel too..." : "At least you have some mercy.");

        return ExitCodeInfo.FromSuccess(); 
    }
}