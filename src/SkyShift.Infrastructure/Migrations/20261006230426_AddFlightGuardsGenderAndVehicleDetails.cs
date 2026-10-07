using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightGuardsGenderAndVehicleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop safely before any schema changes. Never delete existing reports.
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM [Flights]
    GROUP BY [UserId], [Date]
    HAVING COUNT_BIG(*) > 1
)
BEGIN
    THROW 51000, 'Duplicate flight reports exist. Review UserId and Date duplicates before applying this migration.', 1;
END;");

            migrationBuilder.DropIndex(
                name: "IX_Flights_UserId_Date",
                table: "Flights");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Vehicles",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxSpeedKmh",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "Vehicles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seats",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_UserId_Date",
                table: "Flights",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Flights_UserId_Date",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MaxSpeedKmh",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Seats",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_UserId_Date",
                table: "Flights",
                columns: new[] { "UserId", "Date" });
        }
    }
}
