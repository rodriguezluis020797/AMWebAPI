#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class UpdateModels : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "AddressLine1",
            "Provider",
            "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            "AddressLine2",
            "Provider",
            "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "City",
            "Provider",
            "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            "ZipCode",
            "Provider",
            "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "AddressLine1",
            "Provider");

        migrationBuilder.DropColumn(
            "AddressLine2",
            "Provider");

        migrationBuilder.DropColumn(
            "City",
            "Provider");

        migrationBuilder.DropColumn(
            "ZipCode",
            "Provider");
    }
}