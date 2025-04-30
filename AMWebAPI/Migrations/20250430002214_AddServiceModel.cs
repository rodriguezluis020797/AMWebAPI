using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    ServiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowClientScheduling = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_Service_Provider_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Provider",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UpdateProviderEMailRequest_ProviderId",
                table: "UpdateProviderEMailRequest",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_ProviderId",
                table: "Service",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateProviderEMailRequest_Provider_ProviderId",
                table: "UpdateProviderEMailRequest",
                column: "ProviderId",
                principalTable: "Provider",
                principalColumn: "ProviderId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UpdateProviderEMailRequest_Provider_ProviderId",
                table: "UpdateProviderEMailRequest");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropIndex(
                name: "IX_UpdateProviderEMailRequest_ProviderId",
                table: "UpdateProviderEMailRequest");
        }
    }
}
