using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMWebAPI.CoreMigrations
{
    /// <inheritdoc />
    public partial class Modifiedprovidermodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndOfService",
                table: "Provider");

            migrationBuilder.AddColumn<bool>(
                name: "SubscriptionToBeCancelled",
                table: "Provider",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionToBeCancelled",
                table: "Provider");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndOfService",
                table: "Provider",
                type: "datetime2",
                nullable: true);
        }
    }
}
