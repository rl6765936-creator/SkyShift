using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkyShift.Infrastructure.Migrations
{
    /// <summary>
    /// Adds the vehicle catalog. Schema additions and catalog seed only:
    /// existing Vehicles rows are NOT updated or deleted. Their new columns
    /// start as NULL, so legacy vehicles stay unlinked until an explicit,
    /// reviewed mapping is applied (see CATALOG-MANIFEST.md). No existing
    /// IDs, statuses, timestamps, assignments or flights are touched.
    /// </summary>
    public partial class AddVehicleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogIdentification",
                table: "Vehicles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogItemId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Vehicles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionHe",
                table: "Vehicles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Seats = table.Column<int>(type: "int", nullable: true),
                    MaxRoadSpeedKmh = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionHe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogItems", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "VehicleCatalogItems",
                columns: new[] { "Id", "Description", "DescriptionHe", "Identification", "ImageUrl", "Manufacturer", "MaxRoadSpeedKmh", "ModelName", "Seats" },
                values: new object[,]
                {
                    { 1, "Compact SUV based on the Audi Q3. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי קומפקטי המבוסס על Audi Q3. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-01.webp", "Audi", null, "Q3 (2nd generation)", null },
                    { 2, "Small electric crossover. Visual inspiration only: the exact model is not verified. The propellers are a SkyShift illustration; the road car does not fly.", "קרוסאובר חשמלי קטן. השראה חזותית בלבד: הדגם המדויק לא אומת. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-02.webp", "Geely", null, "Geometry E (uncertain)", null },
                    { 3, "Hybrid hypercar based on the Koenigsegg Regera. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על היברידית המבוססת על Koenigsegg Regera. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-03.webp", "Koenigsegg", null, "Regera", null },
                    { 4, "Hypercar based on the Bugatti Chiron Super Sport. 440 km/h is the manufacturer's electronically limited road top speed. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על המבוססת על Bugatti Chiron Super Sport. 440 קמ״ש היא מהירות הכביש המרבית המוגבלת אלקטרונית לפי היצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-04.webp", "Bugatti", 440, "Chiron Super Sport", null },
                    { 5, "Electric family SUV based on the Tesla Model X. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי משפחתי חשמלי המבוסס על Tesla Model X. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-05.webp", "Tesla", null, "Model X", null },
                    { 6, "Four-seat BMW concept car shown in 2021. A concept has no production road top speed. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של BMW בעל ארבעה מושבים שהוצג ב־2021. לרכב קונספט אין מהירות כביש מרבית של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-06.webp", "BMW", null, "i Vision Circular (concept)", 4 },
                    { 7, "Camouflaged coupé prototype. Visual inspiration only: the exact model and variant are not verified. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס קופה מוסווה. השראה חזותית בלבד: הדגם והגרסה המדויקים לא אומתו. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-07.webp", "Mercedes-Benz", null, "CLE Coupé (camouflaged prototype, uncertain)", null },
                    { 8, "Mercedes-Benz concept saloon. A concept has no production road specifications. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של Mercedes-Benz. לרכב קונספט אין מפרט כביש של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-08.webp", "Mercedes-Benz", null, "Concept CLA Class (concept)", null },
                    { 9, "Electric saloon based on the Nissan N7. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית סדאן חשמלית המבוססת על Nissan N7. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-09.webp", "Nissan", null, "N7", null },
                    { 10, "BMW concept saloon. A concept has no production road specifications. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של BMW. לרכב קונספט אין מפרט כביש של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-10.webp", "BMW", null, "Vision Neue Klasse (concept)", null },
                    { 11, "Executive saloon based on the BMW 5 Series in an art livery. The exact variant is unknown. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית מנהלים המבוססת על BMW סדרה 5 בעיטוף אמנותי. הגרסה המדויקת אינה ידועה. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-11.webp", "BMW", null, "5 Series (G30, art livery)", null },
                    { 12, "Lancia concept car. A concept has no production road specifications. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של Lancia. לרכב קונספט אין מפרט כביש של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-12.webp", "Lancia", null, "Pu+Ra HPE (concept)", null },
                    { 13, "Tiny electric city car. Visual inspiration only: the exact model is not verified. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה. השראה חזותית בלבד: הדגם המדויק לא אומת. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-13.webp", "Bestune", null, "Xiaoma (uncertain)", null },
                    { 14, "Plug-in hybrid hatchback based on the Toyota Prius. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק היברידי נטען המבוסס על Toyota Prius. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-14.webp", "Toyota", null, "Prius Plug-in Hybrid (5th generation)", null },
                    { 15, "SUV prototype of the second-generation Škoda Kodiaq. The production variant is unknown. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי Škoda Kodiaq מהדור השני. גרסת הייצור אינה ידועה. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-15.webp", "Škoda", null, "Kodiaq (2nd generation, camouflaged prototype)", null },
                    { 16, "Convertible grand tourer prototype of the Maserati GranCabrio. The production variant is unknown. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של מכונית הגראנד טורר הפתוחה Maserati GranCabrio. גרסת הייצור אינה ידועה. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-16.webp", "Maserati", null, "GranCabrio (camouflaged prototype)", null },
                    { 17, "Low-slung hypercar. Visual inspiration only: the manufacturer and model are not identified. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על נמוכה. השראה חזותית בלבד: היצרן והדגם לא זוהו. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "VisualInspiration", "/catalog/car-17.webp", "Unidentified", null, "Hypercar (unidentified)", null },
                    { 18, "Compact electric SUV based on the Mercedes-Benz EQB. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי קומפקטי המבוסס על Mercedes-Benz EQB. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-18.webp", "Mercedes-Benz", null, "EQB", null },
                    { 19, "Five-seat SUV prototype of the MINI Countryman. Top speed varies by variant, so it is left unknown. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי MINI Countryman בעל חמישה מושבים. המהירות המרבית משתנה לפי גרסה ולכן אינה מוצגת. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-19.webp", "MINI", null, "Countryman (3rd generation, camouflaged prototype)", 5 },
                    { 20, "Tiny electric city car based on the Chery QQ Ice Cream. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה המבוססת על Chery QQ Ice Cream. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-20.webp", "Chery", null, "QQ Ice Cream", null },
                    { 21, "Electric three-door hatch based on the MINI Cooper E. Seats and road top speed are for the Cooper E variant. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק חשמלי בן שלוש דלתות המבוסס על MINI Cooper E. המושבים ומהירות הכביש המרבית הם של גרסת Cooper E. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-21.webp", "MINI", 160, "Cooper E (3-door)", 4 },
                    { 22, "Electric SUV based on the Peugeot E-3008. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי המבוסס על Peugeot E-3008. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Identified", "/catalog/car-22.webp", "Peugeot", null, "E-3008", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CatalogItemId",
                table: "Vehicles",
                column: "CatalogItemId",
                unique: true,
                filter: "[CatalogItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogItems_ImageUrl",
                table: "VehicleCatalogItems",
                column: "ImageUrl",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleCatalogItems_CatalogItemId",
                table: "Vehicles",
                column: "CatalogItemId",
                principalTable: "VehicleCatalogItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleCatalogItems_CatalogItemId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CatalogItemId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CatalogIdentification",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CatalogItemId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DescriptionHe",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Vehicles");
        }
    }
}
