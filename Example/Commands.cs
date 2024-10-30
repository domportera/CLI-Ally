using CLIAlly;

namespace Example;

internal static class MyClassWithCommands
{
    /// <returns>Returning <see cref="ExitCodeInfo"/> is optional, and can be used to exit the application with more
    /// detailed information. </returns>
    [Command(IsDefaultCommand = true)]
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
}

[Serializable]
internal class PersonWhoDies
{
    public string Name = "An unnamed stranger";
    public int Age = 42;
}