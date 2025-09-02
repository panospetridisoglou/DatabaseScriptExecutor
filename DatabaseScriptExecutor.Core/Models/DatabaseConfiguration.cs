namespace DatabaseScriptExecutor.Core.Models;


public class DatabaseConfiguration
{
    public string                   databaseProvider    { get; set; }
    public List<DatabaseConnection> databaseConnections { get; set; }
    public int                      DatabaseRetries     { get; set; }
    public int                      DatabaseRetryDelay  { get; set; }
}
public class DatabaseConnection
{
    public string database         { get; set; }
    public string connectionString { get; set; }
}