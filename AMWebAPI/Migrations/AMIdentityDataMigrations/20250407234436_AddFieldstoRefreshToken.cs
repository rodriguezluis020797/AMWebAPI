using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.Migrations.AMIdentityDataMigrations
{
    /// <inheritdoc />
    public partial class AddFieldstoRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FingerPrint",
                table: "RefreshToken",
                newName: "UserAgent");

            migrationBuilder.AddColumn<string>(
                name: "IPAddress",
                table: "RefreshToken",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "RefreshToken",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "RefreshToken",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IPAddress",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "RefreshToken",
                newName: "FingerPrint");
        }
    }
}
