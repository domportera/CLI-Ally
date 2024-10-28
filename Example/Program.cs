namespace Example;
using CLIAlly;
using NetJsonAOT;

internal static class Program
{
    private static int Main(string[] args)
    {
        // necessary for AOT only when using JSON serialization/deserialization
        ArgsReflector.SetAotJsonTypes(RuntimeJson.JsonSerializerOptions);
        
        // parse command line arguments
        var parser = CommandLineParser.FromArgs(args, typeof(MyClassWithCommands));
        
        // see what was input by the user, and the resulting commands or errors that were parsed
        var info = parser.GetParseInfo();
        Console.WriteLine(info);
        
        // automatic help printing
        if (parser.PrintHelpIfRequested())
        {
            // exit application
            return 0;
        }

        // run the commands
        var exitCodeInfo = parser.TryInvokeCommands();

        // if there are any failures, print them to the console
        if (exitCodeInfo.ExitCode != 0)
        {
            PrintErrorToConsole($"Error: {exitCodeInfo.Message}");
        }
        
        return exitCodeInfo.ExitCode;
    }

    private static void PrintErrorToConsole(string error)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ForegroundColor = originalColor;
    }
}