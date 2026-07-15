using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ServisYolcu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundDayChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboundDayChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PassengerId = table.Column<int>(type: "integer", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    BoardingStopId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundDayChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundDayChoices_Stops_BoardingStopId",
                        column: x => x.BoardingStopId,
                        principalTable: "Stops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OutboundDayChoices_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboundDayChoices_Users_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundDayChoices_BoardingStopId",
                table: "OutboundDayChoices",
                column: "BoardingStopId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundDayChoices_PassengerId_Date",
                table: "OutboundDayChoices",
                columns: new[] { "PassengerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundDayChoices_TripId_Date",
                table: "OutboundDayChoices",
                columns: new[] { "TripId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboundDayChoices");
        }
    }
}
