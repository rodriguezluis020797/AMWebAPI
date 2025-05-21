#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class Addpayenginemodelsandfields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "PayEngineId",
            "Provider",
            "nvarchar(450)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            "ProviderBilling",
            table => new
            {
                ProviderBillingId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                Amount = table.Column<long>("bigint", nullable: false),
                DiscountAmount = table.Column<long>("bigint", nullable: false),
                DueDate = table.Column<DateTime>("datetime2", nullable: false),
                PaidDate = table.Column<DateTime>("datetime2", nullable: true),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                UpdateDate = table.Column<DateTime>("datetime2", nullable: true),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProviderBilling", x => x.ProviderBillingId);
                table.ForeignKey(
                    "FK_ProviderBilling_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_Provider_PayEngineId",
            "Provider",
            "PayEngineId",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_ProviderBilling_ProviderId",
            "ProviderBilling",
            "ProviderId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "ProviderBilling");

        migrationBuilder.DropIndex(
            "IX_Provider_PayEngineId",
            "Provider");

        migrationBuilder.DropColumn(
            "PayEngineId",
            "Provider");
    }
}