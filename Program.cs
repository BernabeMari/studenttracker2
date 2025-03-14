using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentTracker.Data;
using StudentTracker.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add DbContext with retry logic
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
        };
    });

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var retryCount = 0;
        const int maxRetries = 3;
        const int retryDelaySeconds = 5;

        while (retryCount < maxRetries)
        {
            try
            {
                if (!context.Database.CanConnect())
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Waiting for database connection...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                    retryCount++;
                    continue;
                }

                // Skip database creation and directly apply migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync();
                }
                break;
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, $"An error occurred while connecting to the database (Attempt {retryCount + 1}/{maxRetries})");
                
                if (retryCount >= maxRetries - 1)
                    throw;

                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<TrackingHub>("/trackingHub");

// Serve default page
app.MapFallbackToFile("index.html");

app.Run(); 