using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.CoreMigrations
{
    /// <inheritdoc />
    public partial class Addverifyemailmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerifyProviderEMailRequest",
                columns: table => new
                {
                    VerifyProviderEMailRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    QueryGuid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifyProviderEMailRequest", x => x.VerifyProviderEMailRequestId);
                    table.ForeignKey(
                        name: "FK_VerifyProviderEMailRequest_Provider_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Provider",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerifyProviderEMailRequest_ProviderId",
                table: "VerifyProviderEMailRequest",
                column: "ProviderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifyProviderEMailRequest");
        }
    }
}
