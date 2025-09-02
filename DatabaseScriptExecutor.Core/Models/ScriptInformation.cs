namespace DatabaseScriptExecutor.Core.Models;

public class ScriptInformation
{
    public required DateTime CreationDate { get; set; }
    public required string TargetDatabase { get; set; }
    public string? Creator { get; set; }
    public string? TargetTable { get; set; }
    public required string Script { get; set; }
    public required string FileName { get; set; }
}