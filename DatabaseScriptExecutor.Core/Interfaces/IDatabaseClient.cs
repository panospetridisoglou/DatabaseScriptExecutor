using DatabaseScriptExecutor.Core.Models;

namespace DatabaseScriptExecutor.Core.Interfaces;

public interface IDatabaseClient
{
    public Task<ScriptExecutionResult> ExecuteScript(string fileName, string database, string sqlquery);
    public Task<ScriptExecutionResult> InitializeScriptLogTable(string database);
    public Task<bool> IsExecuted(string database, string fileName);
}