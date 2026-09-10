namespace SkyShift.Application.DTOs;

public class ManagerDashboardResponse
{
    public int TotalUsers { get; set; }
    public int TotalFlights { get; set; }
    public int ActiveAssignments { get; set; }

    public VehicleStatusBreakdown VehiclesByStatus { get; set; } = new();

    // InUse / (Total - Retired), 0 when the denominator is 0.
    public double CurrentUtilizationPercentage { get; set; }

    // % of registered Users who have ever created at least one
    // VehicleAssignment (active or historical).
    public double EngagementRatePercentage { get; set; }

    // Existing, approved LotteryEligibilityService.CalculateCompletionPercentage,
    // averaged across Users who currently have an active VehicleAssignment.
    // Still based on Flight.Date, exactly as the existing, approved
    // per-user Dashboard computes it -- NOT changed to CreatedAt here.
    public double ComplianceRatePercentage { get; set; }
}
