#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class MakepayEngineIdnullable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            "IX_Provider_PayEngineId",
            "Provider");

        migrationBuilder.AlterColumn<string>(
            "PayEngineId",
            "Provider",
            "nvarchar(450)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.CreateIndex(
            "IX_Provider_PayEngineId",
            "Provider",
            "PayEngineId",
            unique: true,
            filter: "[PayEngineId] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            "IX_Provider_PayEngineId",
            "Provider");

        migrationBuilder.AlterColumn<string>(
            "PayEngineId",
            "Provider",
            "nvarchar(450)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(450)",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            "IX_Provider_PayEngineId",
            "Provider",
            "PayEngineId",
            unique: true);
    }
}