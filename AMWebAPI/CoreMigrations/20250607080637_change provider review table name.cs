#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class changeproviderreviewtablename : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            "FK_ProviderReviewModel_Client_ClientId",
            "ProviderReviewModel");

        migrationBuilder.DropForeignKey(
            "FK_ProviderReviewModel_Provider_ProviderId",
            "ProviderReviewModel");

        migrationBuilder.DropPrimaryKey(
            "PK_ProviderReviewModel",
            "ProviderReviewModel");

        migrationBuilder.RenameTable(
            "ProviderReviewModel",
            newName: "ProviderReview");

        migrationBuilder.RenameIndex(
            "IX_ProviderReviewModel_ProviderId",
            table: "ProviderReview",
            newName: "IX_ProviderReview_ProviderId");

        migrationBuilder.RenameIndex(
            "IX_ProviderReviewModel_GuidQuery",
            table: "ProviderReview",
            newName: "IX_ProviderReview_GuidQuery");

        migrationBuilder.RenameIndex(
            "IX_ProviderReviewModel_ClientId",
            table: "ProviderReview",
            newName: "IX_ProviderReview_ClientId");

        migrationBuilder.AddPrimaryKey(
            "PK_ProviderReview",
            "ProviderReview",
            "ProviderReviewId");

        migrationBuilder.AddForeignKey(
            "FK_ProviderReview_Client_ClientId",
            "ProviderReview",
            "ClientId",
            "Client",
            principalColumn: "ClientId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ProviderReview_Provider_ProviderId",
            "ProviderReview",
            "ProviderId",
            "Provider",
            principalColumn: "ProviderId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            "FK_ProviderReview_Client_ClientId",
            "ProviderReview");

        migrationBuilder.DropForeignKey(
            "FK_ProviderReview_Provider_ProviderId",
            "ProviderReview");

        migrationBuilder.DropPrimaryKey(
            "PK_ProviderReview",
            "ProviderReview");

        migrationBuilder.RenameTable(
            "ProviderReview",
            newName: "ProviderReviewModel");

        migrationBuilder.RenameIndex(
            "IX_ProviderReview_ProviderId",
            table: "ProviderReviewModel",
            newName: "IX_ProviderReviewModel_ProviderId");

        migrationBuilder.RenameIndex(
            "IX_ProviderReview_GuidQuery",
            table: "ProviderReviewModel",
            newName: "IX_ProviderReviewModel_GuidQuery");

        migrationBuilder.RenameIndex(
            "IX_ProviderReview_ClientId",
            table: "ProviderReviewModel",
            newName: "IX_ProviderReviewModel_ClientId");

        migrationBuilder.AddPrimaryKey(
            "PK_ProviderReviewModel",
            "ProviderReviewModel",
            "ProviderReviewId");

        migrationBuilder.AddForeignKey(
            "FK_ProviderReviewModel_Client_ClientId",
            "ProviderReviewModel",
            "ClientId",
            "Client",
            principalColumn: "ClientId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ProviderReviewModel_Provider_ProviderId",
            "ProviderReviewModel",
            "ProviderId",
            "Provider",
            principalColumn: "ProviderId",
            onDelete: ReferentialAction.Restrict);
    }
}