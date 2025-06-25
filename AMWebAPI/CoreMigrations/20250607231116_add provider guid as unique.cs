#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addproviderguidasunique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "ProviderGuid",
            "Provider",
            "nvarchar(450)",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Provider_ProviderGuid",
            "Provider",
            "ProviderGuid",
            unique: true,
            filter: "[ProviderGuid] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            "IX_Provider_ProviderGuid",
            "Provider");

        migrationBuilder.DropColumn(
            "ProviderGuid",
            "Provider");
    }
}