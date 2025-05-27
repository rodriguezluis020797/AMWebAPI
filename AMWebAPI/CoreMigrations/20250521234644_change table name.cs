#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class changetablename : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            "FK_ResetPassword_Provider_ProviderId",
            "ResetPassword");

        migrationBuilder.DropPrimaryKey(
            "PK_ResetPassword",
            "ResetPassword");

        migrationBuilder.RenameTable(
            "ResetPassword",
            newName: "ResetPasswordRequest");

        migrationBuilder.RenameIndex(
            "IX_ResetPassword_ProviderId",
            table: "ResetPasswordRequest",
            newName: "IX_ResetPasswordRequest_ProviderId");

        migrationBuilder.AddPrimaryKey(
            "PK_ResetPasswordRequest",
            "ResetPasswordRequest",
            "ResetPasswordId");

        migrationBuilder.AddForeignKey(
            "FK_ResetPasswordRequest_Provider_ProviderId",
            "ResetPasswordRequest",
            "ProviderId",
            "Provider",
            principalColumn: "ProviderId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            "FK_ResetPasswordRequest_Provider_ProviderId",
            "ResetPasswordRequest");

        migrationBuilder.DropPrimaryKey(
            "PK_ResetPasswordRequest",
            "ResetPasswordRequest");

        migrationBuilder.RenameTable(
            "ResetPasswordRequest",
            newName: "ResetPassword");

        migrationBuilder.RenameIndex(
            "IX_ResetPasswordRequest_ProviderId",
            table: "ResetPassword",
            newName: "IX_ResetPassword_ProviderId");

        migrationBuilder.AddPrimaryKey(
            "PK_ResetPassword",
            "ResetPassword",
            "ResetPasswordId");

        migrationBuilder.AddForeignKey(
            "FK_ResetPassword_Provider_ProviderId",
            "ResetPassword",
            "ProviderId",
            "Provider",
            principalColumn: "ProviderId",
            onDelete: ReferentialAction.Restrict);
    }
}