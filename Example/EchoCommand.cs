using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CLIAlly;

namespace Example;

public class EchoCommand
{
    [Command]
    [Description("Prints text to the console")]
    public static ExitCodeInfo Echo(EchoArgs args)
    {
        Printer.WriteLine(args.Text);
        return ExitCodeInfo.FromSuccess();
    }
}

public class EchoArgs
{
    [Arg(0)] [Description("Says something")]
    public string Text;
}