using AMTools.Tools;
using AMWebAPI.Services.CoreServices;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Services.IdentityServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AMWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureConfiguration(builder);
            ConfigureServices(builder);
            ConfigureEnvironmentLogging(builder);

            var app = builder.Build();
            ConfigureMiddleware(app);

            app.Run();
        }

        private static void ConfigureConfiguration(WebApplicationBuilder builder)
        {
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var config = builder.Configuration;

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
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }
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
            builder.Services.AddScoped<ICommunicationService, CommunicationService>();
            builder.Services.AddScoped<ISystemStatusService, SystemStatusService>();
            builder.Services.AddScoped<IProviderService, ProviderService>();

            // Data Services
            builder.Services.AddDbContext<AMCoreData>(options =>
                options.UseSqlServer(config.GetConnectionString("CoreConnectionString")), ServiceLifetime.Scoped);
            builder.Services.AddDbContext<AMIdentityData>(options =>
                options.UseSqlServer(config.GetConnectionString("IdentityConnectionString")), ServiceLifetime.Scoped);

            // Identity Services
            builder.Services.AddScoped<IIdentityService, IdentityService>();
        }

        private static void ConfigureEnvironmentLogging(WebApplicationBuilder builder)
        {
            switch (builder.Environment.EnvironmentName)
            {
                case "Development":
                    builder.Services.AddSingleton<IAMLogger, AMDevLogger>();
                    break;
                default:
                    throw new ArgumentException($"Unknown environment: {builder.Environment.EnvironmentName}");
            }
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
}