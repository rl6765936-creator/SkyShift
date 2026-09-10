namespace SkyShift.Application.DTOs;

// No Data Annotations: any DateTime value is syntactically valid here: a
// business rule such as "not in the future" belongs to the Use Case /
// Domain, not to API-boundary input validation.
public class RecordFlightRequest
{
    public DateTime Date { get; set; }
}
