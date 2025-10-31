namespace HaidersAPI.Models;

/// <summary>
/// Represents the health status of the database connection
/// </summary>
public class DatabaseHealthStatus
{
    public bool IsHealthy { get; set; }
    public long ResponseTime { get; set; }
    public DateTime CheckTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Error { get; set; }
}