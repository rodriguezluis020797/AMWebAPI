
using AMWebAPI.Services.CoreServices;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;
using Microsoft.EntityFrameworkCore;

namespace AMWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            var config = builder.Configuration;

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AMCoreData>(options => options.UseSqlServer(config.GetConnectionString("CoreConnectionString")), ServiceLifetime.Singleton);
            builder.Services.AddSingleton<IUserService, UserService>();

            switch (builder.Environment.EnvironmentName)
            {
                case "Development":
                    builder.Services.AddSingleton<IAMLogger, AMDevLogger>();
                    break;
                default:
                    throw new ArgumentException(builder.Environment.EnvironmentName);
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
