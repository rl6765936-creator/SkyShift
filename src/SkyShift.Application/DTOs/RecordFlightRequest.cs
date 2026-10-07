namespace SkyShift.Application.DTOs;

public class RecordFlightRequest
{
    // ISO 8601 UTC, e.g. 2026-10-06T15:30:00Z. Trial validation runs in Domain.
    public DateTime Date { get; set; }
}
