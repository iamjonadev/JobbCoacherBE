using HaidersAPI.Configuration;
using HaidersAPI.Data;
using HaidersAPI.Helpers;
using HaidersAPI.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using HaidersAPI.Models.Configuration;
using HaidersAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from .env file
HaidersAPI.Helpers.ConfigurationHelper.LoadFromEnvFile(builder.Configuration);

// Configure connection string from environment
var defaultConnection = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
if (!string.IsNullOrEmpty(defaultConnection))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
}

var passwordKey = Environment.GetEnvironmentVariable("PASSWORD_KEY");
if (!string.IsNullOrEmpty(passwordKey))
{
    builder.Configuration["Authentication:PasswordKey"] = passwordKey;
}

var tokenKey = Environment.GetEnvironmentVariable("TOKEN_KEY");
if (!string.IsNullOrEmpty(tokenKey))
{
    builder.Configuration["Authentication:JwtSecret"] = tokenKey;
}

// Configure strongly typed configuration options
builder.Services.Configure<AuthenticationOptions>(
    builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<SwedishTaxOptions>(
    builder.Configuration.GetSection(SwedishTaxOptions.SectionName));
builder.Services.Configure<CompanyOptions>(
    builder.Configuration.GetSection(CompanyOptions.SectionName));
builder.Services.Configure<MailOptions>(
    builder.Configuration.GetSection(MailOptions.SectionName));
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<ComplianceOptions>(
    builder.Configuration.GetSection(ComplianceOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("TOKEN_KEY");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    // Add Authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdoteamOwner", policy => 
            policy.RequireRole("Admin", "Owner", "AdoteamOwner"));
        options.AddPolicy("AdoteamUser", policy => 
            policy.RequireRole("Admin", "Owner", "AdoteamOwner", "AdoteamAccountant", "AdoteamClient"));
        options.AddPolicy("AdoteamViewer", policy => 
            policy.RequireRole("Admin", "Owner", "AdoteamOwner", "AdoteamAccountant", "AdoteamClient", "AdoteamAuditor"));
    });
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdoteamPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://localhost:7001" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Database Context
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured. Check your .env file and appsettings.json");
    }
    return new SqlConnection(connectionString);
});

// Register database services following InventoryBE patterns
builder.Services.AddScoped<AdoteamSQL>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AdoteamDataContext>();

// Register core services
builder.Services.AddScoped<AdoteamAuthHelper>();
builder.Services.AddScoped<ContactEmailService>();

// Register HaidersGraphMailService with proper error handling
builder.Services.AddScoped<HaidersGraphMailService>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<HaidersGraphMailService>>();
    
    try
    {
        return new HaidersGraphMailService(config, logger);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "HaidersGraphMailService initialization failed - mail features will be disabled");
        return null!;
    }
});

builder.Services.AddHttpContextAccessor();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Security middlewares (order is important!)
app.UseGlobalExceptionHandling();
app.UseSecurityHeaders();
app.UseRateLimiting();
app.UseIPWhitelist();

app.UseHttpsRedirection();
app.UseCors("AdoteamPolicy");

// Authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom permission middleware
app.UsePermissionMiddleware();

// Audit logging (should be after auth to capture user info)
app.UseAuditLogging();

app.UseRouting();
app.MapControllers();

// Test endpoint to verify configuration
app.MapGet("/api/config/test", (IConfiguration config
    , AdoteamDataContext dbContext
    ) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    var hasConnection = !string.IsNullOrEmpty(connectionString);
    
    return Results.Ok(new
    {
        ConfigurationLoaded = true,
        HasDatabaseConnection = hasConnection,
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTime.UtcNow,
        ConnectionStringLength = connectionString?.Length ?? 0
    });
})
.WithName("TestConfiguration")
.WithTags("Configuration");

// Database test endpoint
app.MapGet("/api/database/test", async (AdoteamDataContext dbContext) =>
{
    try
    {
        var healthStatus = await dbContext.CheckDatabaseHealthAsync();
        return Results.Ok(healthStatus);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
})
.WithName("TestDatabase")
.WithTags("Database");

app.Run();
