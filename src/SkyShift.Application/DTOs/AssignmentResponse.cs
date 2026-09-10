namespace SkyShift.Application.DTOs;

public class AssignmentResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Computed at mapping time via VehicleAssignment.IsActive(DateTime.UtcNow)
    // -- the existing Domain method, not re-derived independently.
    public bool IsActive { get; set; }
}
