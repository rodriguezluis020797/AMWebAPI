using AMData.Models.CoreModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.DataServices
{
    public class AMCoreData : DbContext
    {
        private readonly IConfiguration _configuration;

        public AMCoreData(DbContextOptions<AMCoreData> options, IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(_configuration.GetConnectionString("CoreConnectionString"), b => b.MigrationsAssembly("AMWebAPI"));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Session Action
            modelBuilder.Entity<SessionActionModel>()
                .HasKey(x => x.SessionActionId);
            #endregion

            #region Session
            modelBuilder.Entity<SessionModel>()
                .HasKey(x => x.SessionId);
            modelBuilder.Entity<SessionModel>()
                .HasMany(u => u.SessionActions)
                .WithOne(s => s.Session)
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region UserCommunication
            modelBuilder.Entity<UserCommunicationModel>()
                .HasKey(x => x.CommunicationId);
            #endregion

            #region User
            modelBuilder.Entity<UserModel>()
                .HasKey(x => x.UserId);
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
            #endregion
        }

        public DbSet<SessionActionModel> SessionActions { get; set; }
        public DbSet<SessionModel> Sessions { get; set; }
        public DbSet<UserCommunicationModel> UserCommunications { get; set; }
        public DbSet<UserModel> Users { get; set; }
    }
}
