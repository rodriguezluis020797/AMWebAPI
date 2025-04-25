using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateProviderEMailRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UpdateProviderEMailRequest",
                columns: table => new
                {
                    UpdateProviderEMailRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    NewEMail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateProviderEMailRequest", x => x.UpdateProviderEMailRequestId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateProviderEMailRequest");
        }
    }
}
