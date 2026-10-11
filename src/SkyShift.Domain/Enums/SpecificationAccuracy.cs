namespace SkyShift.Domain.Enums;

/// <summary>
/// How a single catalog specification value (seats, maximum road speed) was
/// obtained. Independent of CatalogIdentification: a correctly identified
/// model can still carry illustrative figures, and neither value implies
/// that the real manufacturer's vehicle can fly.
/// </summary>
public enum SpecificationAccuracy
{
    /// <summary>Confirmed from an official manufacturer or importer document.</summary>
    Verified = 1,

    /// <summary>Plausible sample value chosen for SkyShift; not a manufacturer specification.</summary>
    Illustrative = 2
}
