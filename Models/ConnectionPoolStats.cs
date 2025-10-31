namespace HaidersAPI.Models;

/// <summary>
/// Represents connection pool statistics
/// </summary>
public class ConnectionPoolStats
{
    public int TotalConnections { get; set; }
    public int ActiveSessions { get; set; }
    public int ActiveRequests { get; set; }
    public DateTime CheckTime { get; set; }
    public string? Message { get; set; }
}