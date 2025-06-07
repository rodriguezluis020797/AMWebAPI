#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class addproviderreview : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ProviderReviewModel",
            table => new
            {
                ProviderReviewId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                GuidQuery = table.Column<string>("nvarchar(450)", nullable: false),
                ProviderId = table.Column<long>("bigint", nullable: false),
                ReviewText = table.Column<string>("nvarchar(max)", nullable: false),
                Rating = table.Column<decimal>("decimal(18,2)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProviderReviewModel", x => x.ProviderReviewId);
                table.ForeignKey(
                    "FK_ProviderReviewModel_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ProviderReviewModel_GuidQuery",
            "ProviderReviewModel",
            "GuidQuery",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_ProviderReviewModel_ProviderId",
            "ProviderReviewModel",
            "ProviderId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "ProviderReviewModel");
    }
}