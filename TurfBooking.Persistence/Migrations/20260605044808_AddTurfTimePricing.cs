using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTurfTimePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AfternoonPrice",
                table: "Turfs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DayTimePrice",
                table: "Turfs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NightTimePrice",
                table: "Turfs",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfternoonPrice",
                table: "Turfs");

            migrationBuilder.DropColumn(
                name: "DayTimePrice",
                table: "Turfs");

            migrationBuilder.DropColumn(
                name: "NightTimePrice",
                table: "Turfs");
        }
    }
}
