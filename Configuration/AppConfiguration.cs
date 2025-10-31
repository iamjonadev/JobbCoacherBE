using System.Reflection;

namespace HaidersAPI.Configuration;

/// <summary>
/// Basic application configuration for connection strings and keys
/// </summary>
public class AppConfiguration
{
    public required string DefaultConnection { get; set; }
    public required string PasswordKey { get; set; }
    public required string TokenKey { get; set; }
}

/// <summary>
/// Helper for loading environment variables from .env file
/// </summary>
public static class ConfigurationHelper
{
    public static void LoadFromEnvFile(IConfiguration configuration)
    {
        try
        {
            // Get the directory where the assembly is located
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            
            // Look for .env file in the project root (go up from bin/Debug/net8.0)
            var projectRoot = Directory.GetParent(assemblyDirectory!)?.Parent?.Parent?.FullName;
            var envFilePath = Path.Combine(projectRoot!, ".env");
            
            if (!File.Exists(envFilePath))
            {
                // Fallback: look in current working directory
                envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            }
            
            if (File.Exists(envFilePath))
            {
                Console.WriteLine($"Loading .env from: {envFilePath}");
                LoadEnvFile(envFilePath);
            }
            else
            {
                Console.WriteLine($"No .env file found at {envFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading .env file: {ex.Message}");
        }
    }
    
    private static void LoadEnvFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("##"))
                continue;
                
            var equalIndex = line.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = line[..equalIndex].Trim();
                var value = line[(equalIndex + 1)..].Trim();
                
                // Set environment variables
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}