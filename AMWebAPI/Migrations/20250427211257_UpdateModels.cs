using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeZone",
                table: "Provider",
                newName: "TimeZoneCode");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Provider",
                newName: "StateCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeZoneCode",
                table: "Provider",
                newName: "TimeZone");

            migrationBuilder.RenameColumn(
                name: "StateCode",
                table: "Provider",
                newName: "State");
        }
    }
}
