using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cateredByLetsuwi.Migrations
{
    /// <inheritdoc />
    public partial class SyncBookingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Bookings");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
