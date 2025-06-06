#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addProviderAlert : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ProviderAlert",
            table => new
            {
                ProviderAlertId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                Alert = table.Column<string>("nvarchar(max)", nullable: false),
                AlertAfterDate = table.Column<DateTime>("datetime2", nullable: false),
                Acknowledged = table.Column<bool>("bit", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProviderAlert", x => x.ProviderAlertId);
                table.ForeignKey(
                    "FK_ProviderAlert_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ProviderAlert_ProviderId",
            "ProviderAlert",
            "ProviderId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "ProviderAlert");
    }
}