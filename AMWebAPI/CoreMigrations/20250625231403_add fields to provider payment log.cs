#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addfieldstoproviderpaymentlog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            "SMSCount",
            "ProviderLogPayment",
            "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<long>(
            "TotalPrice",
            "ProviderLogPayment",
            "bigint",
            nullable: false,
            defaultValue: 0L);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "SMSCount",
            "ProviderLogPayment");

        migrationBuilder.DropColumn(
            "TotalPrice",
            "ProviderLogPayment");
    }
}