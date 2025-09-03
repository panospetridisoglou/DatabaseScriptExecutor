using DatabaseScriptExecutor.Core.Models;

namespace DatabaseScriptExecutor.Core.Interfaces;

public interface IDatabaseClient
{
    public Task<ScriptExecutionResult> ExecuteScript(ScriptInformation script);
    public Task<ScriptExecutionResult> InitializeScriptLogTable(string database);
    public Task<bool> IsExecuted(ScriptInformation script);
}