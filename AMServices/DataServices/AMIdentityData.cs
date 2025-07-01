using AMData.Models.IdentityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.DataServices;

/// <summary>
///     Entity Framework Core context for Identity-related data.
/// </summary>
public class AMIdentityData(DbContextOptions<AMIdentityData> options, IConfiguration configuration)
    : DbContext(options)
{
    /// <summary>
    ///     Configures the database connection using the Identity connection string.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("IdentityConnectionString"),
            sql => sql.MigrationsAssembly("AMWebAPI")
        );
    }

    public async Task ReseedIdentitiesAsync()
    {
        var tableIdMappings = new Dictionary<string, string>
        {
            { "Password", "PasswordId" },
            { "RefreshToken", "RefreshTokenId" }
        };

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

    #region DbSets

    public DbSet<PasswordModel> Passwords { get; set; }
    public DbSet<RefreshTokenModel> RefreshTokens { get; set; }

    #endregion
}