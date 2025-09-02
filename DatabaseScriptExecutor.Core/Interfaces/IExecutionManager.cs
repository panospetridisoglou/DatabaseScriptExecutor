using DatabaseScriptExecutor.Core.Models;

namespace DatabaseScriptExecutor.Core.Interfaces;

public interface IExecutionManager
{
    public Task<ExecutionResult> ExecuteScripts(string database);
}