using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServisYolcu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReverseRouteLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReverse",
                table: "Routes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReverseRouteId",
                table: "Routes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ReverseRouteId",
                table: "Routes",
                column: "ReverseRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Routes_ReverseRouteId",
                table: "Routes",
                column: "ReverseRouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Routes_ReverseRouteId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ReverseRouteId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "IsReverse",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ReverseRouteId",
                table: "Routes");
        }
    }
}
