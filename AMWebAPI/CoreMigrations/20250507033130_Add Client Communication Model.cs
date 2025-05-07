#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class AddClientCommunicationModel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ClientCommunication",
            table => new
            {
                ClientCommunicationId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ClientId = table.Column<long>("bigint", nullable: false),
                Message = table.Column<string>("nvarchar(max)", nullable: false),
                SendAfter = table.Column<DateTime>("datetime2", nullable: false),
                Sent = table.Column<bool>("bit", nullable: false),
                AttemptOne = table.Column<DateTime>("datetime2", nullable: true),
                AttemptTwo = table.Column<DateTime>("datetime2", nullable: true),
                AttemptThree = table.Column<DateTime>("datetime2", nullable: true),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientCommunication", x => x.ClientCommunicationId);
                table.ForeignKey(
                    "FK_ClientCommunication_Client_ClientId",
                    x => x.ClientId,
                    "Client",
                    "ClientId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ClientCommunication_ClientId",
            "ClientCommunication",
            "ClientId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "ClientCommunication");
    }
}