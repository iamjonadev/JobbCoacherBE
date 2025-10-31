using dotenv.net;

namespace HaidersAPI.Helpers;

public static class ConfigurationHelper
{
    public static void LoadFromEnvFile(IConfigurationBuilder configurationBuilder)
    {
        try
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            Console.WriteLine($"Loading .env from: {envPath}");
            
            if (File.Exists(envPath))
            {
                DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envPath }));
                Console.WriteLine("✅ .env file loaded successfully");
                
                // Debug: Show some environment variables
                var mailFrom = Environment.GetEnvironmentVariable("MAIL_FROM");
                Console.WriteLine($"MAIL_FROM from env: {mailFrom ?? "NULL"}");
                
                var tenantId = Environment.GetEnvironmentVariable("AZUREAD_TENANTID");
                Console.WriteLine($"AZUREAD_TENANTID from env: {tenantId ?? "NULL"}");
            }
            else
            {
                Console.WriteLine($"❌ .env file not found at: {envPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading .env file: {ex.Message}");
        }
    }
}