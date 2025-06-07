#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class modifyproviderbillingmodel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            "ReviewText",
            "ProviderReviewModel",
            "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<decimal>(
            "Rating",
            "ProviderReviewModel",
            "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            "ReviewText",
            "ProviderReviewModel",
            "nvarchar(max)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            "Rating",
            "ProviderReviewModel",
            "decimal(18,2)",
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);
    }
}