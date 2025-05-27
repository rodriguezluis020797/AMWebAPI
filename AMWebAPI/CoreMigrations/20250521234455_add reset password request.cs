#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addresetpasswordrequest : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ResetPassword",
            table => new
            {
                ResetPasswordId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                QueryGuid = table.Column<string>("nvarchar(max)", nullable: false),
                Reset = table.Column<bool>("bit", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                UpdateDate = table.Column<DateTime>("datetime2", nullable: true),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResetPassword", x => x.ResetPasswordId);
                table.ForeignKey(
                    "FK_ResetPassword_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ResetPassword_ProviderId",
            "ResetPassword",
            "ProviderId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "ResetPassword");
    }
}