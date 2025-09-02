namespace DatabaseScriptExecutor.Core.Models;

public class ExecutionResult
{
    private ExecutionResult(){}
    public List<string>? AppliedScripts { get; set; }
    public List<string>? SkippedScripts { get; set; }
    public bool IsSuccess { get; set; }
    public ExcecutionException? Error { get; set; }
    public static ExecutionResult Success(List<string> appliedScripts, List<string> skippedScripts)
    {
        return new ExecutionResult
        {
            AppliedScripts = appliedScripts,
            SkippedScripts = skippedScripts,
            IsSuccess = true,
            Error = null
        };
    }

    public static ExecutionResult Failure(Exception error, List<string>? appliedScripts = null,
        List<string>? skippedScripts = null, string? fileName = null)
    {
        return new ExecutionResult
        {
            AppliedScripts = appliedScripts,
            SkippedScripts = skippedScripts,
            IsSuccess = false,
            Error = new()
            {
                FileName = fileName,
                Exception = error
            }
        };
    }
}

public class ExcecutionException
{
    public string FileName { get; set; }
    public Exception Exception { get; set; }
}
