#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.IdentityMigrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Password",
            table => new
            {
                PasswordId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                Temporary = table.Column<bool>("bit", nullable: false),
                HashedPassword = table.Column<string>("nvarchar(max)", nullable: false),
                Salt = table.Column<string>("nvarchar(max)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_Password", x => x.PasswordId); });

        migrationBuilder.CreateTable(
            "RefreshToken",
            table => new
            {
                RefreshTokenId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                Token = table.Column<string>("nvarchar(max)", nullable: false),
                IPAddress = table.Column<string>("nvarchar(max)", nullable: false),
                UserAgent = table.Column<string>("nvarchar(max)", nullable: false),
                Platform = table.Column<string>("nvarchar(max)", nullable: false),
                Language = table.Column<string>("nvarchar(max)", nullable: false),
                ExpiresDate = table.Column<DateTime>("datetime2", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_RefreshToken", x => x.RefreshTokenId); });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "Password");

        migrationBuilder.DropTable(
            "RefreshToken");
    }
}