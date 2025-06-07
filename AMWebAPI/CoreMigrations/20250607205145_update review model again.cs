#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class updatereviewmodelagain : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            "ReviewText",
            "ProviderReview",
            "nvarchar(max)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            "Rating",
            "ProviderReview",
            "decimal(18,2)",
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);

        migrationBuilder.AddColumn<bool>(
            "Submitted",
            "ProviderReview",
            "bit",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "Submitted",
            "ProviderReview");

        migrationBuilder.AlterColumn<string>(
            "ReviewText",
            "ProviderReview",
            "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<decimal>(
            "Rating",
            "ProviderReview",
            "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)");
    }
}