using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServisYolcu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterRollback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MonthlyReservations_PassengerId",
                table: "MonthlyReservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReservations_PassengerId",
                table: "MonthlyReservations",
                column: "PassengerId");
        }
    }
}
