using DatabaseScriptExecutor.Core.Integrations;
using DatabaseScriptExecutor.Core.Interfaces;
using DatabaseScriptExecutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DatabaseScriptExecutor;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        args = ["""G:\C# Projects\EnergyHub.Shared\Sql Scripts\Functions"""];
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("DatabaseScriptExecutor");

        var config = await File.ReadAllTextAsync("appsettings.json");
        var jobject = JObject.Parse(config);
        var databaseConfigurationObject = jobject["databaseConfiguration"];
        var databaseConfiguration = databaseConfigurationObject?.ToObject<DatabaseConfiguration>();
        if (databaseConfiguration == null)
        {
            logger.LogCritical("appsettings.json not found.");
            return 1;
        }


        if (args.Length == 0)
        {
            logger.LogCritical("Please enter a path with all the required scripts.");
            return 1;
        }

        var path = args[0];

        var files = Directory.GetFiles(path, "*.sql", SearchOption.AllDirectories);


        IDatabaseClient client = new PostgresClient(databaseConfiguration);
        foreach (var record in databaseConfiguration.databaseConnections)
        {
            var result = await client.InitializeScriptLogTable(record.database);
            if (!result.IsSuccess)
            {
                logger.LogCritical(
                    $"Failed to initialize script log table for {record.database}. Error: {result.ErrorMessage}");
                return 1;
            }
        }

        foreach (var file in files.Order())
        {
            var filename = Path.GetFileName(file);
            var log = $"{filename}";
            var script = await File.ReadAllTextAsync(file);
            var lines = script.Split('\n').Take(5);
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
                logger.LogError(log);
                return 1;
            }
            var exists = await client.IsExecuted(targetDatabase, filename);
            if (exists)
            {
                log += " Skipped!\nScript already executed";
                logger.LogDebug(log);
                continue;
            }

            var result = await client.ExecuteScript(filename, targetDatabase, script);
            if (!result.IsSuccess)
            {
                log += $" Failed!\nError: {result.ErrorMessage}";
                logger.LogError(log);
                return 1;
            }

            logger.LogInformation(log+" Success!");
        }

        logger.LogInformation("All scripts executed successfully.");
        return 0;
    }
}