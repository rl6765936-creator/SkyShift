namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Expected conflict when a Manager tries to delete a fleet Vehicle that must be
/// kept. Maps to 409 with a code per reason (bilingual text in the API layer).
/// </summary>
public sealed class VehicleDeletionNotAllowedException : DomainException
{
    public const string NotAvailableCode = "vehicle_delete_not_available";
    public const string HasHistoryCode = "vehicle_delete_has_history";
    public const string ChangedConcurrentlyCode = "vehicle_delete_conflict";

    private VehicleDeletionNotAllowedException(int vehicleId, string code, string message)
        : base(message)
    {
        VehicleId = vehicleId;
        Code = code;
    }

    public int VehicleId { get; }
    public string Code { get; }

    public static VehicleDeletionNotAllowedException NotAvailable(int vehicleId, Enums.VehicleStatus status) =>
        new(vehicleId, NotAvailableCode, $"Vehicle {vehicleId} cannot be deleted because its status is {status}. Only an Available vehicle can be deleted.");

    public static VehicleDeletionNotAllowedException HasHistory(int vehicleId) =>
        new(vehicleId, HasHistoryCode, $"Vehicle {vehicleId} has assignment or flight history and cannot be deleted. Retire it instead.");

    public static VehicleDeletionNotAllowedException ChangedConcurrently(int vehicleId) =>
        new(vehicleId, ChangedConcurrentlyCode, $"Vehicle {vehicleId} was changed by another request. Refresh and try again.");
}
