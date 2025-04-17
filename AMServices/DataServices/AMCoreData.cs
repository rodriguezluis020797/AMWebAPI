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

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(
                _configuration.GetConnectionString("CoreConnectionString"),
                sqlOptions => sqlOptions.MigrationsAssembly("AMWebAPI")
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureSessionActionModel(modelBuilder);
            ConfigureSessionModel(modelBuilder);
            ConfigureUserCommunicationModel(modelBuilder);
            ConfigureUserModel(modelBuilder);
        }

        private static void ConfigureSessionActionModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionActionModel>()
                .HasKey(sa => sa.SessionActionId);
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

        private static void ConfigureUserCommunicationModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCommunicationModel>()
                .HasKey(uc => uc.CommunicationId);
        }

        private static void ConfigureUserModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<UserModel>()
                .HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModel>()
                .HasMany(u => u.Communications)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<SessionActionModel> SessionActions { get; set; }
        public DbSet<SessionModel> Sessions { get; set; }
        public DbSet<UserCommunicationModel> UserCommunications { get; set; }
        public DbSet<UserModel> Users { get; set; }
    }
}