using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Communication",
                columns: table => new
                {
                    CommunicationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SendAfter = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptOne = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptTwo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptThree = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communication", x => x.CommunicationId);
                    table.ForeignKey(
                        name: "FK_Communication_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Communication_UserId",
                table: "Communication",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Communication");
        }
    }
}
