using AMData.Models.CoreModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.DataServices;

public class AMCoreData : DbContext
{
    private readonly IConfiguration _configuration;

    public AMCoreData(DbContextOptions<AMCoreData> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<AppointmentModel> Appointments { get; init; }
    public DbSet<ClientModel> Clients { get; init; }
    public DbSet<ClientCommunicationModel> ClientCommunications { get; init; }
    public DbSet<ProviderBillingModel> ProviderBillings { get; init; }
    public DbSet<ProviderCommunicationModel> ProviderCommunications { get; init; }
    public DbSet<ProviderModel> Providers { get; init; }
    public DbSet<ServiceModel> Services { get; init; }
    public DbSet<SessionActionModel> SessionActions { get; init; }
    public DbSet<SessionModel> Sessions { get; init; }
    public DbSet<UpdateProviderEMailRequestModel> UpdateProviderEMailRequests { get; init; }
    public DbSet<VerifyProviderEMailRequestModel> VerifyProviderEMailRequests { get; init; }
    public DbSet<ResetPasswordRequestModel> ResetPasswordRequests { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            _configuration.GetConnectionString("CoreConnectionString"),
            sqlOptions => sqlOptions.MigrationsAssembly("AMWebAPI")
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAppointmentModel(modelBuilder);
        ConfigureClientModel(modelBuilder);
        ConfigureClientCommunicationModel(modelBuilder);
        ConfigureProviderCommunicationModel(modelBuilder);
        ConfigureProviderModel(modelBuilder);
        ConfigureServiceModel(modelBuilder);
        ConfigureSessionActionModel(modelBuilder);
        ConfigureSessionModel(modelBuilder);
        ConfigureUpdateProviderEMailRequestModel(modelBuilder);
        ConfigureProviderBillingModel(modelBuilder);
        ConffigureVerifyProviderEMailRequestModel(modelBuilder);
        ConffigureResetPasswordRequestsModel(modelBuilder);

        foreach (var foreignKey in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private void ConffigureResetPasswordRequestsModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResetPasswordRequestModel>()
            .HasKey(x => x.ResetPasswordId);
    }

    private void ConffigureVerifyProviderEMailRequestModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VerifyProviderEMailRequestModel>()
            .HasKey(x => x.VerifyProviderEMailRequestId);
    }

    private static void ConfigureProviderBillingModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderBillingModel>()
            .HasKey(s => s.ProviderBillingId);
    }

    private static void ConfigureAppointmentModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentModel>()
            .HasKey(s => s.AppointmentId);

        modelBuilder.Entity<AppointmentModel>()
            .HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId);
    }

    private static void ConfigureClientModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientModel>()
            .HasKey(x => x.ClientId);

        modelBuilder.Entity<ClientModel>()
            .HasMany(u => u.Appointments)
            .WithOne(c => c.Client)
            .HasForeignKey(c => c.ClientId);

        modelBuilder.Entity<ClientModel>()
            .HasMany(u => u.Communications)
            .WithOne(c => c.Client)
            .HasForeignKey(c => c.ClientId);
    }

    private static void ConfigureClientCommunicationModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientCommunicationModel>()
            .HasKey(x => x.ClientCommunicationId);
    }

    private static void ConfigureProviderModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderModel>()
            .HasKey(u => u.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Sessions)
            .WithOne(s => s.Provider)
            .HasForeignKey(s => s.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Communications)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Services)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.UpdateProviderEMailRequests)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Appointments)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Clients)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(x => x.ProviderBillings)
            .WithOne(x => x.Provider)
            .HasForeignKey(x => x.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasIndex(x => x.PayEngineId)
            .IsUnique();

        modelBuilder.Entity<ProviderModel>()
            .HasOne(x => x.VerifyProviderEMailRequest)
            .WithOne(x => x.Provider)
            .HasForeignKey<VerifyProviderEMailRequestModel>(x => x.ProviderId);
        
        modelBuilder.Entity<ProviderModel>()
            .HasMany(x => x.ResetPasswordRequests)
            .WithOne(x => x.Provider)
            .HasForeignKey(x => x.ProviderId);
    }

    private static void ConfigureServiceModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceModel>()
            .HasKey(s => s.ServiceId);

        modelBuilder.Entity<ServiceModel>()
            .Property(i => i.Price)
            .HasColumnType("decimal(18,2)");
    }

    private static void ConfigureProviderCommunicationModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderCommunicationModel>()
            .HasKey(uc => uc.ProviderCommunicationId);
    }

    private static void ConfigureSessionModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionModel>()
            .HasKey(s => s.SessionId);

        modelBuilder.Entity<SessionModel>()
            .HasMany(s => s.SessionActions)
            .WithOne(sa => sa.Session)
            .HasForeignKey(sa => sa.SessionId);
    }

    private static void ConfigureSessionActionModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionActionModel>()
            .HasKey(sa => sa.SessionActionId);
    }

    private static void ConfigureUpdateProviderEMailRequestModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UpdateProviderEMailRequestModel>()
            .HasKey(x => x.UpdateProviderEMailRequestId);
    }

    public async Task ReseedIdentitiesAsync()
    {
        var tableIdMappings = new Dictionary<string, string>
        {
            { "Appointment", "AppointmentId" },
            { "Client", "ClientId" },
            { "ProviderCommunication", "ProviderCommunicationId" },
            { "Provider", "ProviderId" },
            { "Service", "ServiceId" },
            { "SessionAction", "SessionActionId" },
            { "Session", "SessionId" },
            { "UpdateProviderEMailRequest", "UpdateProviderEMailRequestId" },
            { "ClientCommunication", "ClientCommunicationId" },
            { "VerifyProviderEMailRequest", "VerifyProviderEMailRequestId" }
        };

        try
        {
            foreach (var kvp in tableIdMappings)
            {
                var tableName = kvp.Key;
                var columnName = kvp.Value;

                var sql = $@"
            DECLARE @max INT;
            SELECT @max = ISNULL(MAX([{columnName}]), 0) FROM [{tableName}];
            DBCC CHECKIDENT ('{tableName}', RESEED, @max);";

                await Database.ExecuteSqlRawAsync(sql);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}