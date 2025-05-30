#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class Modifiedprovidermodel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "EndOfService",
            "Provider");

        migrationBuilder.AddColumn<bool>(
            "SubscriptionToBeCancelled",
            "Provider",
            "bit",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "SubscriptionToBeCancelled",
            "Provider");

        migrationBuilder.AddColumn<DateTime>(
            "EndOfService",
            "Provider",
            "datetime2",
            nullable: true);
    }
}