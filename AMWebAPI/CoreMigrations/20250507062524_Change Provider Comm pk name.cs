#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class ChangeProviderCommpkname : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            "CommunicationId",
            "ProviderCommunication",
            "ProviderCommunicationId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            "ProviderCommunicationId",
            "ProviderCommunication",
            "CommunicationId");
    }
}