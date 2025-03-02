using AMWebAPI.Models.CoreModels;
using Microsoft.EntityFrameworkCore;

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
            options.UseSqlServer(_configuration.GetConnectionString("CoreConnectionString"));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>()
                .HasKey(x => x.UserId);
        }
        public DbSet<UserModel> Users { get; set; }
    }
}
