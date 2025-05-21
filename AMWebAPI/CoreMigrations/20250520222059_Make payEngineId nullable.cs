using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.CoreMigrations
{
    /// <inheritdoc />
    public partial class MakepayEngineIdnullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Provider_PayEngineId",
                table: "Provider");

            migrationBuilder.AlterColumn<string>(
                name: "PayEngineId",
                table: "Provider",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Provider_PayEngineId",
                table: "Provider",
                column: "PayEngineId",
                unique: true,
                filter: "[PayEngineId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Provider_PayEngineId",
                table: "Provider");

            migrationBuilder.AlterColumn<string>(
                name: "PayEngineId",
                table: "Provider",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provider_PayEngineId",
                table: "Provider",
                column: "PayEngineId",
                unique: true);
        }
    }
}
