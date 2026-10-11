using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkyShift.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:skyshift_case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, collation: "skyshift_case_insensitive"),
                    AverageDailyTrips = table.Column<int>(type: "integer", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "User")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    SeatsAccuracy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaxRoadSpeedKmh = table.Column<int>(type: "integer", nullable: false),
                    MaxRoadSpeedAccuracy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionHe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    Identification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CatalogItemId = table.Column<int>(type: "integer", nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescriptionHe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CatalogIdentification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SeatsAccuracy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaxRoadSpeedAccuracy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaxSpeedKmh = table.Column<int>(type: "integer", nullable: true),
                    Seats = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPersonalExperimental = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleCatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "VehicleCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    VehicleId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flights_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    VehicleId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "VehicleCatalogItems",
                columns: new[] { "Id", "Description", "DescriptionHe", "Identification", "ImageUrl", "Manufacturer", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "ModelName", "Seats", "SeatsAccuracy" },
                values: new object[,]
                {
                    { 1, "Compact SUV based on the Audi Q3. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי קומפקטי המבוסס על Audi Q3. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-01.webp", "Audi", "Illustrative", 210, "Q3 (2nd generation)", 5, "Illustrative" },
                    { 2, "Small electric crossover. Visual inspiration only: the exact model is not verified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "קרוסאובר חשמלי קטן. השראה חזותית בלבד: הדגם המדויק לא אומת. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-02.webp", "Geely", "Illustrative", 150, "Geometry E (uncertain)", 5, "Illustrative" },
                    { 3, "Hybrid hypercar based on the Koenigsegg Regera. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על היברידית המבוססת על Koenigsegg Regera. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-03.webp", "Koenigsegg", "Illustrative", 400, "Regera", 2, "Illustrative" },
                    { 4, "Hypercar based on the Bugatti Chiron Super Sport. The 440 km/h road top speed is the manufacturer's electronically limited figure. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על המבוססת על Bugatti Chiron Super Sport. מהירות הכביש המרבית של 440 קמ״ש היא הנתון של היצרן, מוגבלת אלקטרונית. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-04.webp", "Bugatti", "Verified", 440, "Chiron Super Sport", 2, "Illustrative" },
                    { 5, "Electric family SUV based on the Tesla Model X. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי משפחתי חשמלי המבוסס על Tesla Model X. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-05.webp", "Tesla", "Illustrative", 250, "Model X", 5, "Illustrative" },
                    { 6, "Four-seat BMW concept car shown in 2021. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של BMW בעל ארבעה מושבים שהוצג ב־2021. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-06.webp", "BMW", "Illustrative", 180, "i Vision Circular (concept)", 4, "Verified" },
                    { 7, "Camouflaged coupé prototype. Visual inspiration only: the exact model and variant are not verified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס קופה מוסווה. השראה חזותית בלבד: הדגם והגרסה המדויקים לא אומתו. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-07.webp", "Mercedes-Benz", "Illustrative", 250, "CLE Coupé (camouflaged prototype, uncertain)", 4, "Illustrative" },
                    { 8, "Mercedes-Benz concept saloon. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של Mercedes-Benz. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-08.webp", "Mercedes-Benz", "Illustrative", 210, "Concept CLA Class (concept)", 5, "Illustrative" },
                    { 9, "Electric saloon based on the Nissan N7. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית סדאן חשמלית המבוססת על Nissan N7. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-09.webp", "Nissan", "Illustrative", 170, "N7", 5, "Illustrative" },
                    { 10, "BMW concept saloon. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של BMW. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-10.webp", "BMW", "Illustrative", 200, "Vision Neue Klasse (concept)", 5, "Illustrative" },
                    { 11, "Executive saloon based on the BMW 5 Series in an art livery. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית מנהלים המבוססת על BMW סדרה 5 בעיטוף אמנותי. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-11.webp", "BMW", "Illustrative", 250, "5 Series (G30, art livery)", 5, "Illustrative" },
                    { 12, "Lancia concept car. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של Lancia. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-12.webp", "Lancia", "Illustrative", 180, "Pu+Ra HPE (concept)", 4, "Illustrative" },
                    { 13, "Tiny electric city car. Visual inspiration only: the exact model is not verified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה. השראה חזותית בלבד: הדגם המדויק לא אומת. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-13.webp", "Bestune", "Illustrative", 100, "Xiaoma (uncertain)", 4, "Illustrative" },
                    { 14, "Plug-in hybrid hatchback based on the Toyota Prius. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק היברידי נטען המבוסס על Toyota Prius. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-14.webp", "Toyota", "Illustrative", 175, "Prius Plug-in Hybrid (5th generation)", 5, "Illustrative" },
                    { 15, "SUV prototype of the second-generation Škoda Kodiaq. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי Škoda Kodiaq מהדור השני. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-15.webp", "Škoda", "Illustrative", 200, "Kodiaq (2nd generation, camouflaged prototype)", 5, "Illustrative" },
                    { 16, "Convertible grand tourer prototype of the Maserati GranCabrio. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של מכונית הגראנד טורר הפתוחה Maserati GranCabrio. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-16.webp", "Maserati", "Illustrative", 300, "GranCabrio (camouflaged prototype)", 4, "Illustrative" },
                    { 17, "Low-slung hypercar. Visual inspiration only: the manufacturer and model are not identified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על נמוכה. השראה חזותית בלבד: היצרן והדגם לא זוהו. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-17.webp", "Unidentified", "Illustrative", 350, "Hypercar (unidentified)", 2, "Illustrative" },
                    { 18, "Compact electric SUV based on the Mercedes-Benz EQB. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי קומפקטי המבוסס על Mercedes-Benz EQB. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-18.webp", "Mercedes-Benz", "Illustrative", 160, "EQB", 5, "Illustrative" },
                    { 19, "Five-seat SUV prototype of the MINI Countryman. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי MINI Countryman בעל חמישה מושבים. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-19.webp", "MINI", "Illustrative", 210, "Countryman (3rd generation, camouflaged prototype)", 5, "Verified" },
                    { 20, "Tiny electric city car based on the Chery QQ Ice Cream. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה המבוססת על Chery QQ Ice Cream. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-20.webp", "Chery", "Illustrative", 100, "QQ Ice Cream", 4, "Illustrative" },
                    { 21, "Electric three-door hatch based on the MINI Cooper E. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק חשמלי בן שלוש דלתות המבוסס על MINI Cooper E. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-21.webp", "MINI", "Verified", 160, "Cooper E (3-door)", 4, "Verified" },
                    { 22, "Electric SUV based on the Peugeot E-3008. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי המבוסס על Peugeot E-3008. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-22.webp", "Peugeot", "Illustrative", 170, "E-3008", 5, "Illustrative" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_UserId_Date",
                table: "Flights",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_VehicleId",
                table: "Flights",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_UserId",
                table: "VehicleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_VehicleId",
                table: "VehicleAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogItems_ImageUrl",
                table: "VehicleCatalogItems",
                column: "ImageUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CatalogItemId",
                table: "Vehicles",
                column: "CatalogItemId",
                unique: true,
                filter: "\"CatalogItemId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "VehicleAssignments");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleCatalogItems");
        }
    }
}
