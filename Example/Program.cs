namespace Example;
using CLIAlly;
using NetJsonAOT;

internal static class Program
{
    private static int Main(string[] args)
    {
        // necessary for AOT only when using JSON serialization/deserialization
        // NetJsonAOT is not necessary - just know it accepts JsonSerializerOptions!
        // this step can be ignored for non-AOT builds, and normal System.Net.Json rules apply
        ArgsReflector.SetAotJsonTypes(RuntimeJson.JsonSerializerOptions);
        
        // parse command line arguments
        var parser = CommandLineParser.FromArgs(args, typeof(MyClassWithCommands), typeof(EchoCommand));
        
        // Optional: see what was input by the user, and the resulting commands or errors that were parsed
        var info = parser.GetParseInfo();
        Console.WriteLine(info);
        
        // automatic help and version printing
        if (parser.PrintHelpIfRequested())
        {
            // exit application
            return 0;
        }
        
        // run the commands
        var exitCodeInfo = parser.TryInvokeCommands(writeOutputToConsole: true);

        // if there are any failures, print them to the console
        if (exitCodeInfo.ExitCode != ExitCodeInfo.SuccessCode)
        {
            PrintErrorToConsole($"Error: {exitCodeInfo.Message}");
        }
        
        return exitCodeInfo.ExitCode;
    }

    private static void PrintErrorToConsole(string error)
    {
        // Printer helper class that allows you to fearlessly print ANSI-formatted strings colored strings without
        // worrying about inter-process communication getting confused - if output of this application is redirected
        // (e.g. to a file, pipe, or run from another application), this class will strip ANSI color codes from the
        // output.
        Printer.WriteLine(error);
    }
}