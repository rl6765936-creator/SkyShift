using System.ComponentModel.DataAnnotations;

namespace SkyShift.Application.DTOs;

/// <summary>
/// The Manager selects an unused catalog model, and the server derives all vehicle metadata
/// (manufacturer, model, road speed, seats, descriptions, image path) from it. There are deliberately
/// no metadata properties, so fields sent by an older client are ignored and can never override catalog
/// values.
/// </summary>
public class CreateVehicleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CatalogItemId must reference a catalog model.")]
    public int CatalogItemId { get; set; }

    public bool IsPersonalExperimental { get; set; }
}
