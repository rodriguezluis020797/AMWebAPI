#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class removeSubscriptionEndedfromprovidermodel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "SubscriptionToBeCancelled",
            "Provider");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            "SubscriptionToBeCancelled",
            "Provider",
            "bit",
            nullable: false,
            defaultValue: false);
    }
}