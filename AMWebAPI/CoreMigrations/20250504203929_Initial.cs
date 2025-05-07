#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AMWebAPI.CoreMigrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Provider",
            table => new
            {
                ProviderId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FirstName = table.Column<string>("nvarchar(max)", nullable: false),
                MiddleName = table.Column<string>("nvarchar(max)", nullable: true),
                LastName = table.Column<string>("nvarchar(max)", nullable: false),
                EMail = table.Column<string>("nvarchar(max)", nullable: false),
                EMailVerified = table.Column<bool>("bit", nullable: false),
                CountryCode = table.Column<int>("int", nullable: false),
                StateCode = table.Column<int>("int", nullable: false),
                TimeZoneCode = table.Column<int>("int", nullable: false),
                AccessGranted = table.Column<bool>("bit", nullable: false),
                LastLogindate = table.Column<DateTime>("datetime2", nullable: true),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                UpdateDate = table.Column<DateTime>("datetime2", nullable: true),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_Provider", x => x.ProviderId); });

        migrationBuilder.CreateTable(
            "Client",
            table => new
            {
                ClientId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                FirstName = table.Column<string>("nvarchar(max)", nullable: false),
                MiddleName = table.Column<string>("nvarchar(max)", nullable: true),
                LastName = table.Column<string>("nvarchar(max)", nullable: false),
                PhoneNumber = table.Column<string>("nvarchar(max)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                UpdateDate = table.Column<DateTime>("datetime2", nullable: true),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Client", x => x.ClientId);
                table.ForeignKey(
                    "FK_Client_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProviderCommunication",
            table => new
            {
                CommunicationId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
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
                table.PrimaryKey("PK_ProviderCommunication", x => x.CommunicationId);
                table.ForeignKey(
                    "FK_ProviderCommunication_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Service",
            table => new
            {
                ServiceId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                Name = table.Column<string>("nvarchar(max)", nullable: false),
                Description = table.Column<string>("nvarchar(max)", nullable: true),
                AllowClientScheduling = table.Column<bool>("bit", nullable: false),
                Price = table.Column<decimal>("decimal(18,2)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Service", x => x.ServiceId);
                table.ForeignKey(
                    "FK_Service_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Session",
            table => new
            {
                SessionId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Session", x => x.SessionId);
                table.ForeignKey(
                    "FK_Session_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "UpdateProviderEMailRequest",
            table => new
            {
                UpdateProviderEMailRequestId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProviderId = table.Column<long>("bigint", nullable: false),
                QueryGuid = table.Column<string>("nvarchar(max)", nullable: false),
                NewEMail = table.Column<string>("nvarchar(max)", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UpdateProviderEMailRequest", x => x.UpdateProviderEMailRequestId);
                table.ForeignKey(
                    "FK_UpdateProviderEMailRequest_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Appointment",
            table => new
            {
                AppointmentId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ServiceId = table.Column<long>("bigint", nullable: false),
                ClientId = table.Column<long>("bigint", nullable: false),
                ProviderId = table.Column<long>("bigint", nullable: false),
                StartDate = table.Column<DateTime>("datetime2", nullable: false),
                EndDate = table.Column<DateTime>("datetime2", nullable: false),
                Notes = table.Column<string>("nvarchar(max)", nullable: true),
                Status = table.Column<int>("int", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false),
                UpdateDate = table.Column<DateTime>("datetime2", nullable: true),
                DeleteDate = table.Column<DateTime>("datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointment", x => x.AppointmentId);
                table.ForeignKey(
                    "FK_Appointment_Client_ClientId",
                    x => x.ClientId,
                    "Client",
                    "ClientId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Appointment_Provider_ProviderId",
                    x => x.ProviderId,
                    "Provider",
                    "ProviderId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SessionAction",
            table => new
            {
                SessionActionId = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SessionId = table.Column<long>("bigint", nullable: false),
                SessionAction = table.Column<int>("int", nullable: false),
                CreateDate = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SessionAction", x => x.SessionActionId);
                table.ForeignKey(
                    "FK_SessionAction_Session_SessionId",
                    x => x.SessionId,
                    "Session",
                    "SessionId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_Appointment_ClientId",
            "Appointment",
            "ClientId");

        migrationBuilder.CreateIndex(
            "IX_Appointment_ProviderId",
            "Appointment",
            "ProviderId");

        migrationBuilder.CreateIndex(
            "IX_Client_ProviderId",
            "Client",
            "ProviderId");

        migrationBuilder.CreateIndex(
            "IX_ProviderCommunication_ProviderId",
            "ProviderCommunication",
            "ProviderId");

        migrationBuilder.CreateIndex(
            "IX_Service_ProviderId",
            "Service",
            "ProviderId");

        migrationBuilder.CreateIndex(
            "IX_Session_ProviderId",
            "Session",
            "ProviderId");

        migrationBuilder.CreateIndex(
            "IX_SessionAction_SessionId",
            "SessionAction",
            "SessionId");

        migrationBuilder.CreateIndex(
            "IX_UpdateProviderEMailRequest_ProviderId",
            "UpdateProviderEMailRequest",
            "ProviderId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "Appointment");

        migrationBuilder.DropTable(
            "ProviderCommunication");

        migrationBuilder.DropTable(
            "Service");

        migrationBuilder.DropTable(
            "SessionAction");

        migrationBuilder.DropTable(
            "UpdateProviderEMailRequest");

        migrationBuilder.DropTable(
            "Client");

        migrationBuilder.DropTable(
            "Session");

        migrationBuilder.DropTable(
            "Provider");
    }
}