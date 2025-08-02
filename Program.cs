using Microsoft.EntityFrameworkCore;
using NeshanGeocodingApi.Data;
using NeshanGeocodingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    // Force HTTPS redirection in production
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // Add this line to serve static files
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
