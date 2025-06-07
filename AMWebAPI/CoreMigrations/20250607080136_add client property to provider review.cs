#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addclientpropertytoproviderreview : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            "ClientId",
            "ProviderReviewModel",
            "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_ProviderReviewModel_ClientId",
            "ProviderReviewModel",
            "ClientId");

        migrationBuilder.AddForeignKey(
            "FK_ProviderReviewModel_Client_ClientId",
            "ProviderReviewModel",
            "ClientId",
            "Client",
            principalColumn: "ClientId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            "FK_ProviderReviewModel_Client_ClientId",
            "ProviderReviewModel");

        migrationBuilder.DropIndex(
            "IX_ProviderReviewModel_ClientId",
            "ProviderReviewModel");

        migrationBuilder.DropColumn(
            "ClientId",
            "ProviderReviewModel");
    }
}