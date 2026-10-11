namespace SkyShift.Domain.Enums;

/// <summary>
/// How confidently a catalog item's road-car model was identified from its
/// (edited) image. Neither value claims the road car can fly: the propellers
/// in every catalog image are a SkyShift illustration.
/// </summary>
public enum CatalogIdentification
{
    /// <summary>Model recognised from visible badges, plates or distinctive design.</summary>
    Identified = 1,

    /// <summary>Uncertain match. Shown as visual inspiration only, without road specifications.</summary>
    VisualInspiration = 2
}
