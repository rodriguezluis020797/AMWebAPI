#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class Addprovidermodelmodifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "BusinessName",
            "Provider",
            "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            "IsActive",
            "Provider",
            "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            "TrialEndDate",
            "Provider",
            "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "BusinessName",
            "Provider");

        migrationBuilder.DropColumn(
            "IsActive",
            "Provider");

        migrationBuilder.DropColumn(
            "TrialEndDate",
            "Provider");
    }
}