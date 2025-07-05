using System.Text;
using AMServices.CoreServices;
using AMServices.DataServices;
using AMServices.IdentityServices;
using AMServices.PaymentEngineServices;
using AMTools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using IdentityService = AMServices.IdentityServices.IdentityService;

namespace AMWebAPI;

public class Program
{
    //static readonly string basePath = AppContext.BaseDirectory;
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseWindowsService();
        ConfigureConfiguration(builder);
        ConfigureServices(builder);
        ConfigureEnvironmentLogging(builder);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AMCoreData>();
            dbContext.ReseedIdentitiesAsync().GetAwaiter().GetResult();
        }

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AMIdentityData>();
            dbContext.ReseedIdentitiesAsync().GetAwaiter().GetResult();
        }

        ConfigureMiddleware(app);

        app.Run();
    }

    private static void ConfigureConfiguration(WebApplicationBuilder builder)
    {
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            //.SetBasePath(basePath)
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", false, true)
            .AddEnvironmentVariables();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

        // Auth & JWT
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["JWToken"];
                        if (!string.IsNullOrEmpty(token)) context.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // Core Services
        builder.Services.AddScoped<IAppointmentService, AppointmentService>();
        builder.Services.AddScoped<ISystemStatusService, SystemStatusService>();
        builder.Services.AddScoped<IProviderService, ProviderService>();
        builder.Services.AddScoped<IServiceService, ServiceService>();
        builder.Services.AddScoped<IClientService, ClientService>();
        builder.Services.AddScoped<IMetricsService, MetricsService>();

        // Data Services
        builder.Services.AddDbContext<AMCoreData>(options =>
            options.UseSqlServer(config.GetConnectionString("CoreConnectionString"))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuting)));

        builder.Services.AddDbContext<AMIdentityData>(options =>
            options.UseSqlServer(config.GetConnectionString("IdentityConnectionString"))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuting)));

        // Identity Services
        builder.Services.AddScoped<IIdentityService, IdentityService>();

        //PaymentEngineServices
        builder.Services.AddScoped<IProviderBillingService, StripeProviderBillingService>();
    }

    private static void ConfigureEnvironmentLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);

        builder.Services.AddSingleton<IAMLogger, AMDevLogger>();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowAngular");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}