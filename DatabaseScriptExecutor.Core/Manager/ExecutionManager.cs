using System.ComponentModel.DataAnnotations;
using DatabaseScriptExecutor.Core.Integrations;
using DatabaseScriptExecutor.Core.Interfaces;
using DatabaseScriptExecutor.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatabaseScriptExecutor.Core.Manager;

public class ExecutionManager : IExecutionManager
{
    private readonly DatabaseConfiguration _databaseConfiguration;
    private readonly ILogger<ExecutionManager> _logger;
    private readonly List<string> _appliedScripts = [];
    private readonly List<string> _skippedScripts = [];
    public ExecutionManager(IOptions<DatabaseConfiguration> databaseConfiguration,
        ILogger<ExecutionManager> logger)
    {
        _databaseConfiguration = databaseConfiguration.Value;
        _logger = logger;
    }

    public async Task<ExecutionResult> ExecuteScripts(string path)
    {
        var files = Directory.GetFiles(path, "*.sql", SearchOption.AllDirectories);
        var client = new PostgresClient(_databaseConfiguration);
        foreach (var record in _databaseConfiguration.databaseConnections)
        {
            var result = await client.InitializeScriptLogTable(record.database);
            if (!result.IsSuccess)
            {
                _logger.LogCritical(
                    $"Failed to initialize script log table for {record.database}. Error: {result.Error?.Message}");
                return ExecutionResult.Failure(result.Error!);
            }
        }
        
        foreach (var file in files.Order())
        {
            var filename = Path.GetFileName(file);
            var log = $"{filename}";
            var script = await File.ReadAllTextAsync(file);
            var lines = script.Split('\n').Take(5).ToList();
            var creationDate = lines?.FirstOrDefault(x => x.Contains("Creation Date:"))?.Split("Creation Date:")?.Last()
                ?.Trim();
            var targetDatabase = lines?.FirstOrDefault(x => x.Contains("Target Database:"))?.Split("Target Database:")
                ?.Last()?.Trim();
            var targetTable = lines?.FirstOrDefault(x => x.Contains("Target Table:"))?.Split("Target Table:").Last()
                .Trim();
            var creator = lines?.FirstOrDefault(x => x.Contains("Creator:"))?.Split("Creator:").Last().Trim();
            if (targetDatabase == null)
            {
                log += " Failed!\n Script doesnt contain the target database info";
                _logger.LogError(log);
                return ExecutionResult.Failure(new ValidationException("Script doesnt contain the target database info"),_appliedScripts,_skippedScripts,filename);
            }
            var exists = await client.IsExecuted(targetDatabase, filename);
            if (exists)
            {
                log += " Skipped!\nScript already executed";
                _logger.LogDebug(log);
                _skippedScripts.Add(filename);
                continue;
            }

            var result = await client.ExecuteScript(filename, targetDatabase, script);
            if (!result.IsSuccess)
            {
                log += $" Failed!\nError: {result.Error?.Message}";
                _logger.LogError(log);
                return ExecutionResult.Failure(result.Error!,_appliedScripts,_skippedScripts,filename);
            }
            _appliedScripts.Add(filename);
            _logger.LogInformation(log+" Success!");
        }

        _logger.LogInformation("All scripts executed successfully.");
        return ExecutionResult.Success(_appliedScripts, _skippedScripts);
    }
}