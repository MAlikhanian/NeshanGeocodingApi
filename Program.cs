using Microsoft.EntityFrameworkCore;
using NeshanGeocodingApi.Data;
using NeshanGeocodingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTPS in Docker environment
if (builder.Environment.EnvironmentName == "Docker")
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080); // HTTP
        
        // Only configure HTTPS if certificate file exists and is valid
        var certPath = "/app/certs/aspnetcore.pfx";
        if (File.Exists(certPath) && new FileInfo(certPath).Length > 0)
        {
            try
            {
                options.ListenAnyIP(8443, listenOptions =>
                {
                    listenOptions.UseHttps(certPath, "YourSecurePassword123!");
                }); // HTTPS
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure HTTPS due to certificate issue: {ex.Message}");
                Console.WriteLine("Application will run in HTTP-only mode.");
            }
        }
        else
        {
            Console.WriteLine("Warning: SSL certificate not found or invalid. Application will run in HTTP-only mode.");
        }
    });
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add Entity Framework - use SQLite for Docker, SQL Server for local development
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.EnvironmentName == "Docker" || 
    (connectionString != null && connectionString.Contains("Data Source")))
{
    // Use SQLite for Docker environment
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    // Use SQL Server for local development
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Add HttpClient for Neshan API
builder.Services.AddHttpClient();

// Add custom services
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<RateLimitService>();
builder.Services.AddScoped<NeshanGeocodingService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddSingleton<LiveLogService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Force HTTPS redirection in production and Docker
    app.UseHttpsRedirection();
}

// Enable HTTPS redirection for Docker environment as well
if (app.Environment.EnvironmentName == "Docker")
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // Add this line to serve static files
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Database initialization completed successfully");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Database initialization failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}

app.Run();
