using AMData.Models.IdentityModels;
using Microsoft.EntityFrameworkCore;

namespace AMWebAPI.Services.DataServices
{
    public class AMIdentityData : DbContext
    {
        private readonly IConfiguration _configuration;

        public AMIdentityData(DbContextOptions<AMIdentityData> options, IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(_configuration.GetConnectionString("IdentityConnectionString"));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
        public DbSet<PasswordModel> Passwords { get; set; }
        public DbSet<RefreshTokenModel> RefreshTokens { get; set; }
    }
}
