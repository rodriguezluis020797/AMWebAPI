#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class makeappoinmentenddatesoptional : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            "EndDate",
            "Appointment",
            "datetime2",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            "EndDate",
            "Appointment",
            "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);
    }
}