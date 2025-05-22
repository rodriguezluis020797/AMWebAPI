using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.CoreMigrations
{
    /// <inheritdoc />
    public partial class changetablename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResetPassword_Provider_ProviderId",
                table: "ResetPassword");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResetPassword",
                table: "ResetPassword");

            migrationBuilder.RenameTable(
                name: "ResetPassword",
                newName: "ResetPasswordRequest");

            migrationBuilder.RenameIndex(
                name: "IX_ResetPassword_ProviderId",
                table: "ResetPasswordRequest",
                newName: "IX_ResetPasswordRequest_ProviderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResetPasswordRequest",
                table: "ResetPasswordRequest",
                column: "ResetPasswordId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResetPasswordRequest_Provider_ProviderId",
                table: "ResetPasswordRequest",
                column: "ProviderId",
                principalTable: "Provider",
                principalColumn: "ProviderId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResetPasswordRequest_Provider_ProviderId",
                table: "ResetPasswordRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResetPasswordRequest",
                table: "ResetPasswordRequest");

            migrationBuilder.RenameTable(
                name: "ResetPasswordRequest",
                newName: "ResetPassword");

            migrationBuilder.RenameIndex(
                name: "IX_ResetPasswordRequest_ProviderId",
                table: "ResetPassword",
                newName: "IX_ResetPassword_ProviderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResetPassword",
                table: "ResetPassword",
                column: "ResetPasswordId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResetPassword_Provider_ProviderId",
                table: "ResetPassword",
                column: "ProviderId",
                principalTable: "Provider",
                principalColumn: "ProviderId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
