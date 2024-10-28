namespace CLIAlly;

public readonly struct ExitCodeInfo
{
    public const int InvalidArgsCode = 2;
    public const int FailureCode = 1;
    public const int SuccessCode = 0;

    public ExitCodeInfo(int exitCode, string? message)
    {
        ExitCode = exitCode;
        message ??= "";
        Message = message;
    }
    
    public static ExitCodeInfo FromInvalidArgs(string errorMessage) => new(InvalidArgsCode, errorMessage);
    public static ExitCodeInfo FromInvalidArgs(params string[] args) => FromInvalidArgs("Invalid arguments provided: " + string.Join(", ", args));
    public static ExitCodeInfo FromFailure(string errorMessage) => new(FailureCode, errorMessage);
    public static ExitCodeInfo FromException(string? message, Exception ex) => new(FailureCode, $"{message ?? "Unknown exception occurred"}: {ex.Message}\n{ex.StackTrace}");
    public static ExitCodeInfo FromSuccess(string? message = null) => new(SuccessCode, message!);

    public readonly int ExitCode;
    public readonly string Message;
    public bool IsSuccess => ExitCode == SuccessCode;

    // implicit conversion to bool
    public static implicit operator bool(ExitCodeInfo result) => result.ExitCode == SuccessCode;

    // implicit conversion to int
    public static implicit operator int(ExitCodeInfo result) => result.ExitCode;

    public override string ToString() => $"{ExitCode}: {Message}";
}