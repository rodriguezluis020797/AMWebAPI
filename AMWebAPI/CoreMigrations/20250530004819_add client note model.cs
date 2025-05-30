#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addclientnotemodel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ClientNote",
            table => new
            {
                ClientNoteId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ClientId = table.Column<long>("bigint", nullable: false),
                Note = table.Column<string>("nvarchar(max)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                UpdateDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientNote", x => x.ClientNoteId);
                table.ForeignKey(
                    "FK_ClientNote_Client_ClientId",
                    x => x.ClientId,
                    "Client",
                    "ClientId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ClientNote_ClientId",
            "ClientNote",
            "ClientId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "ClientNote");
    }
}