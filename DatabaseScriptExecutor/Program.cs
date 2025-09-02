using System.Net.Mime;
using DatabaseScriptExecutor.Core.Integrations;
using DatabaseScriptExecutor.Core.Interfaces;
using DatabaseScriptExecutor.Core.Manager;
using DatabaseScriptExecutor.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DatabaseScriptExecutor;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Write("Please enter a path with all the required scripts.");
            return 1;
        }
        
        ServiceCollection services = new();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        services.Configure<DatabaseConfiguration>(configuration.GetSection("databaseConfiguration"));
        services.AddLogging(builder => builder.AddConsole().AddConfiguration(configuration));
        services.AddSingleton<IExecutionManager,ExecutionManager>();
        services.AddSingleton<IConfiguration>(configuration);
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<IExecutionManager>();
        var result = await app.ExecuteScripts(args[0]);
        return result.IsSuccess ? 0 : 1;
    }
}