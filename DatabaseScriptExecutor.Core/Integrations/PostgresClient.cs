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
                          """;
        return ExecuteScript("", database, createtable);
    }

    public async Task<bool> IsExecuted(string database, string fileName)
    {
        NpgsqlConnection connection = new(connectionStrings[database]);
        await connection.OpenAsync();
        NpgsqlCommand? sql = new();
        sql.Connection = connection;

        sql.CommandText += $"""
                              SELECT 1 FROM script_log WHERE file_name = '{fileName}'
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

    public async Task<ScriptExecutionResult> ExecuteScript(string fileName, string database, string sqlquery)
    {
        NpgsqlConnection connection = new(connectionStrings[database]);
        try
        {
            await connection.OpenAsync();
            NpgsqlCommand? sql = new();
            sql.Connection = connection;

            sql.CommandText = sqlquery;
            sql.CommandText += $"""
                                  INSERT INTO script_log (file_name, execution_time)
                                  VALUES ('{fileName}', NOW());
                                """;
            var result = await sql.ExecuteNonQueryAsync();
            await connection.CloseAsync();
            return ScriptExecutionResult.Success(fileName);
        }
        catch (DbException e)
        {
            await connection.CloseAsync();
            return ScriptExecutionResult.Failure(fileName, e);
        }
    }
}