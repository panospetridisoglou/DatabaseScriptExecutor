using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

    private async Task<List<ScriptInformation>> GetScriptInformationOrderedAsync(List<string> files)
    {
        List<ScriptInformation> scripts = [];
        foreach (var file in files)
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
                var ex = new ValidationException("Script doesnt contain the target database info",
                    new Exception("filename"));
                throw ex;
            }

            if (creationDate == null)
            {
                log += " Failed!\n Script doesnt contain the creation date info";
                _logger.LogError(log);
                var ex = new ValidationException("Script doesnt contain the creation date info",
                    new Exception("filename"));
                throw ex;
            }

            var dateTime = DateTime.ParseExact(creationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            scripts.Add(new ScriptInformation
            {
                FileName = filename,
                CreationDate = dateTime,
                TargetDatabase = targetDatabase,
                Creator = creator,
                TargetTable = targetTable,
                Script = script
            });
            _logger.LogInformation($"Found file {file}");

        }

        return scripts.OrderBy(x => x.CreationDate).ToList();
    }

    public async Task<ExecutionResult> ExecuteScripts(string path)
    {
        var files = Directory.GetFiles(path, "*.sql", SearchOption.AllDirectories).ToList();
        var client = new PostgresClient(_databaseConfiguration);
        foreach (var record in _databaseConfiguration.databaseConnections)
        {
            _logger.LogInformation($"Initializing script log table for {record.database}");
            var result = await client.InitializeScriptLogTable(record.database);
            if (!result.IsSuccess)
            {
                _logger.LogCritical(
                    $"Failed to initialize script log table for {record.database}. Error: {result.Error?.Message}");
                return ExecutionResult.Failure(result.Error!);
            }
        }

        List<ScriptInformation> scriptInformations = [];
        try
        {
            scriptInformations = await GetScriptInformationOrderedAsync(files);
        }
        catch (Exception e)
        {
            return ExecutionResult.Failure(e, _appliedScripts, _skippedScripts, e.InnerException!.Message);
        }

        foreach (var file in scriptInformations)
        {
            var log = $"{file.FileName}";
            var targetDatabase = file.TargetDatabase;
            var filename = file.FileName;
            var script = file.Script;
            var exists = await client.IsExecuted(file);
            if (exists)
            {
                log += " Skipped!\nScript already executed";
                _logger.LogDebug(log);
                _skippedScripts.Add(filename);
                continue;
            }
            _logger.LogInformation($"Executing script {filename}");
            var result = await client.ExecuteScript(file);
            if (!result.IsSuccess)
            {
                log += $" Failed!\nError: {result.Error?.Message}";
                _logger.LogError(log);
                return ExecutionResult.Failure(result.Error!, _appliedScripts, _skippedScripts, filename);
            }

            _appliedScripts.Add(filename);
            _logger.LogInformation(log + " Success!");
        }

        _logger.LogInformation("All scripts executed successfully.");
        return ExecutionResult.Success(_appliedScripts, _skippedScripts);
    }
}