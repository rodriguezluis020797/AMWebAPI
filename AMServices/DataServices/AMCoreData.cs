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

    public DbSet<ClientModel> Clients { get; init; }

    public DbSet<ProviderCommunicationModel> ProviderCommunications { get; init; }
    public DbSet<ProviderModel> Providers { get; init; }
    public DbSet<ServiceModel> Services { get; init; }
    public DbSet<SessionActionModel> SessionActions { get; init; }
    public DbSet<SessionModel> Sessions { get; init; }
    public DbSet<UpdateProviderEMailRequestModel> UpdateProviderEMailRequests { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            _configuration.GetConnectionString("CoreConnectionString"),
            sqlOptions => sqlOptions.MigrationsAssembly("AMWebAPI")
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureClientModel(modelBuilder);
        ConfigureProviderCommunicationModel(modelBuilder);
        ConfigureProviderModel(modelBuilder);
        ConfigureServiceModel(modelBuilder);
        ConfigureSessionActionModel(modelBuilder);
        ConfigureSessionModel(modelBuilder);
        ConfigureUpdateProviderEMailRequestModel(modelBuilder);
    }

    private static void ConfigureClientModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientModel>()
            .HasKey(x => x.ClientId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Clients)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProviderModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderModel>()
            .HasKey(u => u.ProviderId);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Sessions)
            .WithOne(s => s.Provider)
            .HasForeignKey(s => s.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Communications)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.Services)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderModel>()
            .HasMany(u => u.UpdateProviderEMailRequests)
            .WithOne(c => c.Provider)
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
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
            .HasKey(uc => uc.CommunicationId);
    }

    private static void ConfigureSessionModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionModel>()
            .HasKey(s => s.SessionId);

        modelBuilder.Entity<SessionModel>()
            .HasMany(s => s.SessionActions)
            .WithOne(sa => sa.Session)
            .HasForeignKey(sa => sa.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
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
}