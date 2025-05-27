#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class Addverifyemailmodel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "VerifyProviderEMailRequest",
            table => new
            {
                VerifyProviderEMailRequestId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                QueryGuid = table.Column<string>("nvarchar(max)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VerifyProviderEMailRequest", x => x.VerifyProviderEMailRequestId);
                table.ForeignKey(
                    "FK_VerifyProviderEMailRequest_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_VerifyProviderEMailRequest_ProviderId",
            "VerifyProviderEMailRequest",
            "ProviderId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "VerifyProviderEMailRequest");
    }
}