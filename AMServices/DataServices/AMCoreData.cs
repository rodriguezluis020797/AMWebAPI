using AMData.Models.CoreModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.DataServices
{
    public class AMCoreData : DbContext
    {
        private readonly IConfiguration _configuration;

        public AMCoreData(DbContextOptions<AMCoreData> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        { optionsBuilder.UseSqlServer(
                    _configuration.GetConnectionString("CoreConnectionString"),
                    sqlOptions => sqlOptions.MigrationsAssembly("AMWebAPI")
                );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureClientModel(modelBuilder);
            ConfigureUserModel(modelBuilder);
            ConfigureUserCommunicationModel(modelBuilder);
            ConfigureSessionModel(modelBuilder);
            ConfigureSessionActionModel(modelBuilder);
        }

        private static void ConfigureClientModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientModel>()
                .HasKey(x => x.ClientId);

            modelBuilder.Entity<ProviderModel>()
                .HasMany(u => u.Clients)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureUserModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProviderModel>()
                .HasKey(u => u.Provider);

            modelBuilder.Entity<ProviderModel>()
                .HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProviderModel>()
                .HasMany(u => u.Communications)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureUserCommunicationModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCommunicationModel>()
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

        public DbSet<ClientModel> Clients { get; init; }
        public DbSet<SessionActionModel> SessionActions { get; init; }
        public DbSet<SessionModel> Sessions { get; init; }
        public DbSet<UserCommunicationModel> UserCommunications { get; init; }
        public DbSet<ProviderModel> Users { get; init; }
    }
}