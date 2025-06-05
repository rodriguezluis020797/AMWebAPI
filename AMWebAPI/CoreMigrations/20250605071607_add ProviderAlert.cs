using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.CoreMigrations
{
    /// <inheritdoc />
    public partial class addProviderAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderAlert",
                columns: table => new
                {
                    ProviderAlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Alert = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlertAfterDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Acknowledged = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderAlert", x => x.ProviderAlertId);
                    table.ForeignKey(
                        name: "FK_ProviderAlert_Provider_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Provider",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAlert_ProviderId",
                table: "ProviderAlert",
                column: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderAlert");
        }
    }
}
