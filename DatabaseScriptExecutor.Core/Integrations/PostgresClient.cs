using System.Data;
using System.Data.Common;
using DatabaseScriptExecutor.Core.Interfaces;
using DatabaseScriptExecutor.Core.Models;
using Npgsql;

namespace DatabaseScriptExecutor.Core.Integrations;

public class PostgresClient : IDatabaseClient
{
    private Dictionary<string, string> connectionStrings { get; set; }

    public PostgresClient(DatabaseConfiguration databaseConfiguration)
    {
        connectionStrings = [];
        foreach (var conn in databaseConfiguration.databaseConnections)
        {
            connectionStrings.Add(conn.database, conn.connectionString);
        }
    }

    public Task<ScriptExecutionResult> InitializeScriptLogTable(string database)
    {
        var createtable = """
                            CREATE TABLE IF NOT EXISTS script_log
                            (
                                file_name VARCHAR(255),
                                execution_time TIMESTAMP
                            );
                            ALTER TABLE script_log ADD COLUMN IF NOT EXISTS creation_date TIMESTAMP;
                          """;
        return ExecuteScript(new ()
        {
            TargetDatabase = database,
            Script = createtable,
            FileName = "script_log_init.sql",
            CreationDate = DateTime.MinValue
        });
    }

    public async Task<bool> IsExecuted(ScriptInformation script)
    {
        NpgsqlConnection connection = new(connectionStrings[script.TargetDatabase]);
        await connection.OpenAsync();
        NpgsqlCommand? sql = new();
        sql.Connection = connection;

        sql.CommandText += $"""
                              SELECT 1 FROM script_log WHERE file_name = '{script.FileName}' AND creation_date = '{script.CreationDate:yyyy-MM-dd}'
                            """;
        var result = false;
        var sqlReader = await sql.ExecuteReaderAsync();
        if (await sqlReader.ReadAsync())
        {
            result = true;
        }
        else
        {
            result = false;
        }

        await sqlReader.CloseAsync();
        await connection.CloseAsync();
        return result;
    }

    public async Task<ScriptExecutionResult> ExecuteScript(ScriptInformation script)
    {
        NpgsqlConnection connection = new(connectionStrings[script.TargetDatabase]);
        try
        {
            await connection.OpenAsync();
            NpgsqlCommand? sql = new();
            sql.Connection = connection;

            sql.CommandText = script.Script;
            sql.CommandText += $"""
                                  INSERT INTO script_log (file_name, execution_time,creation_date)
                                  VALUES ('{script.FileName}', NOW()),'{script.CreationDate:yyyy-MM-dd}');
                                """;
            var result = await sql.ExecuteNonQueryAsync();
            await connection.CloseAsync();
            return ScriptExecutionResult.Success(script.FileName);
        }
        catch (DbException e)
        {
            await connection.CloseAsync();
            return ScriptExecutionResult.Failure(script.FileName, e);
        }
    }
}