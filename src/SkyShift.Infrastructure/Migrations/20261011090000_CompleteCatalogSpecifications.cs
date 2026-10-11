using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyShift.Infrastructure.Migrations
{
    /// <summary>
    /// Completes catalog specifications: every catalog item gets seats and a
    /// maximum road speed, each flagged Verified or Illustrative, and refreshed
    /// EN/HE descriptions. Only VehicleCatalogItems rows are updated.
    ///
    /// Existing Vehicles rows are NOT updated: two nullable accuracy columns are
    /// added and left NULL, so fleet vehicle data (including any vehicle already
    /// created from the catalog) is preserved exactly as it is.
    ///
    /// Order matters: catalog rows are filled before the columns become NOT NULL.
    /// </summary>
    public partial class CompleteCatalogSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaxRoadSpeedAccuracy",
                table: "Vehicles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatsAccuracy",
                table: "Vehicles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxRoadSpeedAccuracy",
                table: "VehicleCatalogItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatsAccuracy",
                table: "VehicleCatalogItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Compact SUV based on the Audi Q3. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי קומפקטי המבוסס על Audi Q3. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 210, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Small electric crossover. Visual inspiration only: the exact model is not verified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "קרוסאובר חשמלי קטן. השראה חזותית בלבד: הדגם המדויק לא אומת. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 150, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Hybrid hypercar based on the Koenigsegg Regera. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על היברידית המבוססת על Koenigsegg Regera. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 400, 2, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Hypercar based on the Bugatti Chiron Super Sport. The 440 km/h road top speed is the manufacturer's electronically limited figure. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על המבוססת על Bugatti Chiron Super Sport. מהירות הכביש המרבית של 440 קמ״ש היא הנתון של היצרן, מוגבלת אלקטרונית. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Verified", 440, 2, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Electric family SUV based on the Tesla Model X. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי משפחתי חשמלי המבוסס על Tesla Model X. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 250, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Four-seat BMW concept car shown in 2021. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של BMW בעל ארבעה מושבים שהוצג ב־2021. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 180, 4, "Verified" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Camouflaged coupé prototype. Visual inspiration only: the exact model and variant are not verified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס קופה מוסווה. השראה חזותית בלבד: הדגם והגרסה המדויקים לא אומתו. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 250, 4, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Mercedes-Benz concept saloon. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של Mercedes-Benz. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 210, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Electric saloon based on the Nissan N7. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית סדאן חשמלית המבוססת על Nissan N7. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 170, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "BMW concept saloon. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של BMW. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 200, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Executive saloon based on the BMW 5 Series in an art livery. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית מנהלים המבוססת על BMW סדרה 5 בעיטוף אמנותי. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 250, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Lancia concept car. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של Lancia. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 180, 4, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Tiny electric city car. Visual inspiration only: the exact model is not verified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה. השראה חזותית בלבד: הדגם המדויק לא אומת. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 100, 4, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Plug-in hybrid hatchback based on the Toyota Prius. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק היברידי נטען המבוסס על Toyota Prius. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 175, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "SUV prototype of the second-generation Škoda Kodiaq. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי Škoda Kodiaq מהדור השני. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 200, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Convertible grand tourer prototype of the Maserati GranCabrio. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של מכונית הגראנד טורר הפתוחה Maserati GranCabrio. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 300, 4, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Low-slung hypercar. Visual inspiration only: the manufacturer and model are not identified. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על נמוכה. השראה חזותית בלבד: היצרן והדגם לא זוהו. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 350, 2, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Compact electric SUV based on the Mercedes-Benz EQB. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי קומפקטי המבוסס על Mercedes-Benz EQB. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 160, 5, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Five-seat SUV prototype of the MINI Countryman. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי MINI Countryman בעל חמישה מושבים. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 210, 5, "Verified" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Tiny electric city car based on the Chery QQ Ice Cream. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה המבוססת על Chery QQ Ice Cream. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 100, 4, "Illustrative" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Electric three-door hatch based on the MINI Cooper E. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק חשמלי בן שלוש דלתות המבוסס על MINI Cooper E. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Verified", 160, 4, "Verified" });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedAccuracy", "MaxRoadSpeedKmh", "Seats", "SeatsAccuracy" },
                values: new object[] { "Electric SUV based on the Peugeot E-3008. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי המבוסס על Peugeot E-3008. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", "Illustrative", 170, 5, "Illustrative" });

            migrationBuilder.AlterColumn<int>(
                name: "Seats",
                table: "VehicleCatalogItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxRoadSpeedKmh",
                table: "VehicleCatalogItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeatsAccuracy",
                table: "VehicleCatalogItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaxRoadSpeedAccuracy",
                table: "VehicleCatalogItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Seats",
                table: "VehicleCatalogItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "MaxRoadSpeedKmh",
                table: "VehicleCatalogItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "SeatsAccuracy",
                table: "VehicleCatalogItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "MaxRoadSpeedAccuracy",
                table: "VehicleCatalogItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: false);

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Compact SUV based on the Audi Q3. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי קומפקטי המבוסס על Audi Q3. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Small electric crossover. Visual inspiration only: the exact model is not verified. The propellers are a SkyShift illustration; the road car does not fly.", "קרוסאובר חשמלי קטן. השראה חזותית בלבד: הדגם המדויק לא אומת. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Hybrid hypercar based on the Koenigsegg Regera. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על היברידית המבוססת על Koenigsegg Regera. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Hypercar based on the Bugatti Chiron Super Sport. 440 km/h is the manufacturer's electronically limited road top speed. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על המבוססת על Bugatti Chiron Super Sport. 440 קמ״ש היא מהירות הכביש המרבית המוגבלת אלקטרונית לפי היצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", 440, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Electric family SUV based on the Tesla Model X. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי משפחתי חשמלי המבוסס על Tesla Model X. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Four-seat BMW concept car shown in 2021. A concept has no production road top speed. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של BMW בעל ארבעה מושבים שהוצג ב־2021. לרכב קונספט אין מהירות כביש מרבית של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, 4 });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Camouflaged coupé prototype. Visual inspiration only: the exact model and variant are not verified. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס קופה מוסווה. השראה חזותית בלבד: הדגם והגרסה המדויקים לא אומתו. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Mercedes-Benz concept saloon. A concept has no production road specifications. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של Mercedes-Benz. לרכב קונספט אין מפרט כביש של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Electric saloon based on the Nissan N7. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית סדאן חשמלית המבוססת על Nissan N7. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "BMW concept saloon. A concept has no production road specifications. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית קונספט של BMW. לרכב קונספט אין מפרט כביש של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Executive saloon based on the BMW 5 Series in an art livery. The exact variant is unknown. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית מנהלים המבוססת על BMW סדרה 5 בעיטוף אמנותי. הגרסה המדויקת אינה ידועה. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Lancia concept car. A concept has no production road specifications. The propellers are a SkyShift illustration; the road car does not fly.", "רכב קונספט של Lancia. לרכב קונספט אין מפרט כביש של דגם ייצור. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Tiny electric city car. Visual inspiration only: the exact model is not verified. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה. השראה חזותית בלבד: הדגם המדויק לא אומת. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Plug-in hybrid hatchback based on the Toyota Prius. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק היברידי נטען המבוסס על Toyota Prius. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "SUV prototype of the second-generation Škoda Kodiaq. The production variant is unknown. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי Škoda Kodiaq מהדור השני. גרסת הייצור אינה ידועה. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Convertible grand tourer prototype of the Maserati GranCabrio. The production variant is unknown. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של מכונית הגראנד טורר הפתוחה Maserati GranCabrio. גרסת הייצור אינה ידועה. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Low-slung hypercar. Visual inspiration only: the manufacturer and model are not identified. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית־על נמוכה. השראה חזותית בלבד: היצרן והדגם לא זוהו. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Compact electric SUV based on the Mercedes-Benz EQB. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי קומפקטי המבוסס על Mercedes-Benz EQB. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Five-seat SUV prototype of the MINI Countryman. Top speed varies by variant, so it is left unknown. The propellers are a SkyShift illustration; the road car does not fly.", "אב־טיפוס של רכב הפנאי MINI Countryman בעל חמישה מושבים. המהירות המרבית משתנה לפי גרסה ולכן אינה מוצגת. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, 5 });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Tiny electric city car based on the Chery QQ Ice Cream. The propellers are a SkyShift illustration; the road car does not fly.", "מכונית עירונית חשמלית זעירה המבוססת על Chery QQ Ice Cream. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Electric three-door hatch based on the MINI Cooper E. Seats and road top speed are for the Cooper E variant. The propellers are a SkyShift illustration; the road car does not fly.", "האצ'בק חשמלי בן שלוש דלתות המבוסס על MINI Cooper E. המושבים ומהירות הכביש המרבית הם של גרסת Cooper E. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", 160, 4 });

            migrationBuilder.UpdateData(
                table: "VehicleCatalogItems",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Description", "DescriptionHe", "MaxRoadSpeedKmh", "Seats" },
                values: new object[] { "Electric SUV based on the Peugeot E-3008. The propellers are a SkyShift illustration; the road car does not fly.", "רכב פנאי חשמלי המבוסס על Peugeot E-3008. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.", null, null });

            migrationBuilder.DropColumn(
                name: "MaxRoadSpeedAccuracy",
                table: "VehicleCatalogItems");

            migrationBuilder.DropColumn(
                name: "SeatsAccuracy",
                table: "VehicleCatalogItems");

            migrationBuilder.DropColumn(
                name: "MaxRoadSpeedAccuracy",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SeatsAccuracy",
                table: "Vehicles");
        }
    }
}
