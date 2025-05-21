#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class AddServiceandAppointmentnavigation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            "IX_Appointment_ServiceId",
            "Appointment",
            "ServiceId");

        migrationBuilder.AddForeignKey(
            "FK_Appointment_Service_ServiceId",
            "Appointment",
            "ServiceId",
            "Service",
            principalColumn: "ServiceId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            "FK_Appointment_Service_ServiceId",
            "Appointment");

        migrationBuilder.DropIndex(
            "IX_Appointment_ServiceId",
            "Appointment");
    }
}