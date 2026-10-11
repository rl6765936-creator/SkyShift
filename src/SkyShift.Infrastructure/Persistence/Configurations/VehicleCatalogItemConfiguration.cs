using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;

namespace SkyShift.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the catalog table and its deterministic 22-item seed; ids are explicit so each id
/// permanently maps to the frontend asset /catalog/car-NN.webp. Every row has seats and a maximum road
/// speed marked Verified (see CATALOG-MANIFEST.md) or Illustrative, and no row stores a flight
/// specification.
/// </summary>
public class VehicleCatalogItemConfiguration : IEntityTypeConfiguration<VehicleCatalogItem>
{
    private const string IllustrativeNote = " Specifications marked as illustrative are SkyShift sample data, not manufacturer figures.";
    private const string IllustrativeNoteHe = " מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן.";
    private const string Illustration = " The propellers are a SkyShift illustration; the road car does not fly.";
    private const string IllustrationHe = " המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.";

    public void Configure(EntityTypeBuilder<VehicleCatalogItem> builder)
    {
        builder.ToTable("VehicleCatalogItems");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Manufacturer).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ModelName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Seats).IsRequired();
        builder.Property(c => c.SeatsAccuracy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(c => c.MaxRoadSpeedKmh).IsRequired();
        builder.Property(c => c.MaxRoadSpeedAccuracy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(500);
        builder.Property(c => c.DescriptionHe).IsRequired().HasMaxLength(500);
        builder.Property(c => c.ImageUrl).IsRequired().HasMaxLength(260);
        builder.Property(c => c.Identification)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => c.ImageUrl).IsUnique();

        builder.HasData(
            Item(1, "Audi", "Q3 (2nd generation)", 5, SpecificationAccuracy.Illustrative, 210, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Compact SUV based on the Audi Q3.",
                "רכב פנאי קומפקטי המבוסס על Audi Q3."),
            Item(2, "Geely", "Geometry E (uncertain)", 5, SpecificationAccuracy.Illustrative, 150, SpecificationAccuracy.Illustrative, CatalogIdentification.VisualInspiration,
                "Small electric crossover. Visual inspiration only: the exact model is not verified.",
                "קרוסאובר חשמלי קטן. השראה חזותית בלבד: הדגם המדויק לא אומת."),
            Item(3, "Koenigsegg", "Regera", 2, SpecificationAccuracy.Illustrative, 400, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Hybrid hypercar based on the Koenigsegg Regera.",
                "מכונית־על היברידית המבוססת על Koenigsegg Regera."),
            Item(4, "Bugatti", "Chiron Super Sport", 2, SpecificationAccuracy.Illustrative, 440, SpecificationAccuracy.Verified, CatalogIdentification.Identified,
                "Hypercar based on the Bugatti Chiron Super Sport. The 440 km/h road top speed is the manufacturer's electronically limited figure.",
                "מכונית־על המבוססת על Bugatti Chiron Super Sport. מהירות הכביש המרבית של 440 קמ״ש היא הנתון של היצרן, מוגבלת אלקטרונית."),
            Item(5, "Tesla", "Model X", 5, SpecificationAccuracy.Illustrative, 250, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Electric family SUV based on the Tesla Model X.",
                "רכב פנאי משפחתי חשמלי המבוסס על Tesla Model X."),
            Item(6, "BMW", "i Vision Circular (concept)", 4, SpecificationAccuracy.Verified, 180, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Four-seat BMW concept car shown in 2021.",
                "רכב קונספט של BMW בעל ארבעה מושבים שהוצג ב־2021."),
            Item(7, "Mercedes-Benz", "CLE Coupé (camouflaged prototype, uncertain)", 4, SpecificationAccuracy.Illustrative, 250, SpecificationAccuracy.Illustrative, CatalogIdentification.VisualInspiration,
                "Camouflaged coupé prototype. Visual inspiration only: the exact model and variant are not verified.",
                "אב־טיפוס קופה מוסווה. השראה חזותית בלבד: הדגם והגרסה המדויקים לא אומתו."),
            Item(8, "Mercedes-Benz", "Concept CLA Class (concept)", 5, SpecificationAccuracy.Illustrative, 210, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Mercedes-Benz concept saloon.",
                "מכונית קונספט של Mercedes-Benz."),
            Item(9, "Nissan", "N7", 5, SpecificationAccuracy.Illustrative, 170, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Electric saloon based on the Nissan N7.",
                "מכונית סדאן חשמלית המבוססת על Nissan N7."),
            Item(10, "BMW", "Vision Neue Klasse (concept)", 5, SpecificationAccuracy.Illustrative, 200, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "BMW concept saloon.",
                "מכונית קונספט של BMW."),
            Item(11, "BMW", "5 Series (G30, art livery)", 5, SpecificationAccuracy.Illustrative, 250, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Executive saloon based on the BMW 5 Series in an art livery.",
                "מכונית מנהלים המבוססת על BMW סדרה 5 בעיטוף אמנותי."),
            Item(12, "Lancia", "Pu+Ra HPE (concept)", 4, SpecificationAccuracy.Illustrative, 180, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Lancia concept car.",
                "רכב קונספט של Lancia."),
            Item(13, "Bestune", "Xiaoma (uncertain)", 4, SpecificationAccuracy.Illustrative, 100, SpecificationAccuracy.Illustrative, CatalogIdentification.VisualInspiration,
                "Tiny electric city car. Visual inspiration only: the exact model is not verified.",
                "מכונית עירונית חשמלית זעירה. השראה חזותית בלבד: הדגם המדויק לא אומת."),
            Item(14, "Toyota", "Prius Plug-in Hybrid (5th generation)", 5, SpecificationAccuracy.Illustrative, 175, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Plug-in hybrid hatchback based on the Toyota Prius.",
                "האצ'בק היברידי נטען המבוסס על Toyota Prius."),
            Item(15, "Škoda", "Kodiaq (2nd generation, camouflaged prototype)", 5, SpecificationAccuracy.Illustrative, 200, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "SUV prototype of the second-generation Škoda Kodiaq.",
                "אב־טיפוס של רכב הפנאי Škoda Kodiaq מהדור השני."),
            Item(16, "Maserati", "GranCabrio (camouflaged prototype)", 4, SpecificationAccuracy.Illustrative, 300, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Convertible grand tourer prototype of the Maserati GranCabrio.",
                "אב־טיפוס של מכונית הגראנד טורר הפתוחה Maserati GranCabrio."),
            Item(17, "Unidentified", "Hypercar (unidentified)", 2, SpecificationAccuracy.Illustrative, 350, SpecificationAccuracy.Illustrative, CatalogIdentification.VisualInspiration,
                "Low-slung hypercar. Visual inspiration only: the manufacturer and model are not identified.",
                "מכונית־על נמוכה. השראה חזותית בלבד: היצרן והדגם לא זוהו."),
            Item(18, "Mercedes-Benz", "EQB", 5, SpecificationAccuracy.Illustrative, 160, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Compact electric SUV based on the Mercedes-Benz EQB.",
                "רכב פנאי חשמלי קומפקטי המבוסס על Mercedes-Benz EQB."),
            Item(19, "MINI", "Countryman (3rd generation, camouflaged prototype)", 5, SpecificationAccuracy.Verified, 210, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Five-seat SUV prototype of the MINI Countryman.",
                "אב־טיפוס של רכב הפנאי MINI Countryman בעל חמישה מושבים."),
            Item(20, "Chery", "QQ Ice Cream", 4, SpecificationAccuracy.Illustrative, 100, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Tiny electric city car based on the Chery QQ Ice Cream.",
                "מכונית עירונית חשמלית זעירה המבוססת על Chery QQ Ice Cream."),
            Item(21, "MINI", "Cooper E (3-door)", 4, SpecificationAccuracy.Verified, 160, SpecificationAccuracy.Verified, CatalogIdentification.Identified,
                "Electric three-door hatch based on the MINI Cooper E.",
                "האצ'בק חשמלי בן שלוש דלתות המבוסס על MINI Cooper E."),
            Item(22, "Peugeot", "E-3008", 5, SpecificationAccuracy.Illustrative, 170, SpecificationAccuracy.Illustrative, CatalogIdentification.Identified,
                "Electric SUV based on the Peugeot E-3008.",
                "רכב פנאי חשמלי המבוסס על Peugeot E-3008."));
    }

    // Anonymous seed rows keep the entity's private setters untouched.
    private static object Item(int id, string manufacturer, string modelName,
        int seats, SpecificationAccuracy seatsAccuracy, int maxRoadSpeedKmh, SpecificationAccuracy maxRoadSpeedAccuracy,
        CatalogIdentification identification, string description, string descriptionHe)
    {
        var hasIllustrative = seatsAccuracy == SpecificationAccuracy.Illustrative
            || maxRoadSpeedAccuracy == SpecificationAccuracy.Illustrative;

        return new
        {
            Id = id,
            Manufacturer = manufacturer,
            ModelName = modelName,
            Seats = seats,
            SeatsAccuracy = seatsAccuracy,
            MaxRoadSpeedKmh = maxRoadSpeedKmh,
            MaxRoadSpeedAccuracy = maxRoadSpeedAccuracy,
            Description = description + (hasIllustrative ? IllustrativeNote : string.Empty) + Illustration,
            DescriptionHe = descriptionHe + (hasIllustrative ? IllustrativeNoteHe : string.Empty) + IllustrationHe,
            ImageUrl = $"/catalog/car-{id:00}.webp",
            Identification = identification
        };
    }
}
