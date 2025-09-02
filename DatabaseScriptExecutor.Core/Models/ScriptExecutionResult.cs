namespace DatabaseScriptExecutor.Core.Models;

public class ScriptExecutionResult
{
    private ScriptExecutionResult(){}
    public string ScriptName { get; set; }
    public bool IsSuccess { get; set; }
    public Exception? Error { get; set; }
    public DateTime ExecutionTime { get; set; } = DateTime.Now;
    public static ScriptExecutionResult Success(string scriptName) => new(){ ScriptName = scriptName, IsSuccess = true };
    public static ScriptExecutionResult Failure(string scriptName, Exception errorMessage) => new(){ ScriptName = scriptName, IsSuccess = false, Error = errorMessage };
}