using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ServisYolcu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnTripSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut tüm seferler ve aylık abonelikler gidiştir (Direction = 0).
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "Trips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "MonthlyReservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BoardingStopId",
                table: "MonthlyReservations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "NotificationLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReferenceDate",
                table: "NotificationLogs",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReturnDayChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PassengerId = table.Column<int>(type: "integer", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: true),
                    BoardingStopId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnDayChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnDayChoices_Stops_BoardingStopId",
                        column: x => x.BoardingStopId,
                        principalTable: "Stops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReturnDayChoices_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnDayChoices_Users_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Direction",
                table: "Trips",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReservations_BoardingStopId",
                table: "MonthlyReservations",
                column: "BoardingStopId");

            // Bir yolcunun bir ay için yalnızca tek bir dönüş şablonu olabilir; aksi hâlde
            // ReturnAttendanceResolver hangi şablonun geçerli olduğunu bilemez. Filtre sayesinde
            // mevcut gidiş satırları (Direction = 0) bu kısıttan etkilenmez.
            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReservations_ReturnTemplate_Unique",
                table: "MonthlyReservations",
                columns: new[] { "PassengerId", "Year", "Month" },
                unique: true,
                filter: "\"Direction\" = 1");

            // Dönüş hatırlatması sefer+gün başına tek kez. ReferenceDate yalnızca
            // hatırlatmalarda dolduğundan diğer bildirim tipleri kısıttan etkilenmez.
            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_ReturnReminder_Unique",
                table: "NotificationLogs",
                columns: new[] { "Type", "TripId", "ReferenceDate" },
                unique: true,
                filter: "\"ReferenceDate\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnDayChoices_BoardingStopId",
                table: "ReturnDayChoices",
                column: "BoardingStopId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnDayChoices_PassengerId_Date",
                table: "ReturnDayChoices",
                columns: new[] { "PassengerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnDayChoices_TripId_Date",
                table: "ReturnDayChoices",
                columns: new[] { "TripId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyReservations_Stops_BoardingStopId",
                table: "MonthlyReservations",
                column: "BoardingStopId",
                principalTable: "Stops",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyReservations_Stops_BoardingStopId",
                table: "MonthlyReservations");

            migrationBuilder.DropTable(
                name: "ReturnDayChoices");

            migrationBuilder.DropIndex(
                name: "IX_Trips_Direction",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyReservations_ReturnTemplate_Unique",
                table: "MonthlyReservations");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyReservations_BoardingStopId",
                table: "MonthlyReservations");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_ReturnReminder_Unique",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "ReferenceDate",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "BoardingStopId",
                table: "MonthlyReservations");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "MonthlyReservations");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Trips");
        }
    }
}
