using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.Migrations.AMIdentityDataMigrations
{
    /// <inheritdoc />
    public partial class UpdateRefreshTokenModelAddFingerPrint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FingerPrint",
                table: "RefreshToken",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FingerPrint",
                table: "RefreshToken");
        }
    }
}
