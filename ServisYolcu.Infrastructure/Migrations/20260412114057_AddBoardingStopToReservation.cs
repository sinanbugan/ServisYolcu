using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServisYolcu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardingStopToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BoardingStopId",
                table: "Reservations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BoardingStopId",
                table: "Reservations",
                column: "BoardingStopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Stops_BoardingStopId",
                table: "Reservations",
                column: "BoardingStopId",
                principalTable: "Stops",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Stops_BoardingStopId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_BoardingStopId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "BoardingStopId",
                table: "Reservations");
        }
    }
}
