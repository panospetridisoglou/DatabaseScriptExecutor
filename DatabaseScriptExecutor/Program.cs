using DatabaseScriptExecutor.Core.Integrations;
using DatabaseScriptExecutor.Core.Interfaces;
using DatabaseScriptExecutor.Core.Models;
using Newtonsoft.Json.Linq;

namespace DatabaseScriptExecutor;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        
        var config = await File.ReadAllTextAsync("appsettings.json");
        var jobject = JObject.Parse(config);
        var databaseConfigurationObject = jobject["databaseConfiguration"];
        var databaseConfiguration = databaseConfigurationObject.ToObject<DatabaseConfiguration>();
        if (databaseConfiguration == null)
        {
            Console.WriteLine("appsettings.json not found.");
            return 1;
        }
        
        
        if (args.Length == 0)
        {
            Console.WriteLine("Please enter a path with all the required scripts.");
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
                Console.WriteLine($"Failed to initialize script log table for {record.database}. Error: {result.ErrorMessage}");
                return 1;
            }
        }

        foreach (var file in files.Order())
        {
            Console.ResetColor();
            var filename = Path.GetFileName(file);
            Console.Write($"{filename}");
            Console.Write(" . ");
            var script = await File.ReadAllTextAsync(file);
            var lines = script.Split('\n').Take(5);
            var creationDate = lines?.FirstOrDefault(x => x.Contains("Creation Date:"))?.Split("Creation Date:")?.Last()?.Trim();
            var targetDatabase = lines?.FirstOrDefault(x => x.Contains("Target Database:"))?.Split("Target Database:")?.Last()?.Trim();
            var targetTable = lines?.FirstOrDefault(x => x.Contains("Target Table:"))?.Split("Target Table:").Last().Trim();
            var creator = lines?.FirstOrDefault(x => x.Contains("Creator:"))?.Split("Creator:").Last().Trim();
            Console.Write(" . ");
            if (targetDatabase == null)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed!\n Script doesnt contain the target database info");
                Console.ResetColor();
                return 1;
            }

            var result = await client.ExecuteScript(filename,targetDatabase,script);
            Console.Write(" . ");
            if (!result.IsSuccess)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed!\n Error: {result.ErrorMessage}");
                Console.ResetColor();
                return 1;
            }

            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success");
            Console.ResetColor();

        }

        Console.WriteLine("All scripts executed successfully.");
        return 0;
    }
}