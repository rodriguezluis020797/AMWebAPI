using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.CoreMigrations
{
    /// <inheritdoc />
    public partial class addproviderguidasunique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderGuid",
                table: "Provider",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provider_ProviderGuid",
                table: "Provider",
                column: "ProviderGuid",
                unique: true,
                filter: "[ProviderGuid] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Provider_ProviderGuid",
                table: "Provider");

            migrationBuilder.DropColumn(
                name: "ProviderGuid",
                table: "Provider");
        }
    }
}
