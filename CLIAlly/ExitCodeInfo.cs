namespace CLIAlly;

public readonly struct ExitCodeInfo
{
    public static readonly ExitCodeInfo Success = new(0, null!);

    public const int InvalidArgsCode = 2;
    public const int FailureCode = 1;

    public ExitCodeInfo(int exitCode, string errorMessage)
    {
        ExitCode = exitCode;

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            if (exitCode != 0)
                throw new ArgumentNullException(nameof(errorMessage));

            errorMessage = "Success";
        }

        ErrorMessage = errorMessage;
    }
    
    public static ExitCodeInfo FromInvalidArgs(string errorMessage) => new(InvalidArgsCode, errorMessage);
    public static ExitCodeInfo FromInvalidArgs(params string[] args) => FromInvalidArgs("Invalid arguments provided: " + string.Join(", ", args));
    public static ExitCodeInfo FromFailure(string errorMessage) => new(FailureCode, errorMessage);
    public static ExitCodeInfo FromException(string? message, Exception ex) => new(FailureCode, $"{message ?? "Unknown exception occurred"}: {ex.Message}\n{ex.StackTrace}");

    public readonly int ExitCode;
    public readonly string ErrorMessage;

    // implicit conversion to bool
    public static implicit operator bool(ExitCodeInfo result) => result.ExitCode == 0;

    // implicit conversion to int
    public static implicit operator int(ExitCodeInfo result) => result.ExitCode;

    public override string ToString() => $"{ExitCode}: {ErrorMessage}";
}