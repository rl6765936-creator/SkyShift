namespace SkyShift.Application.DTOs;

// One point of the Manager activity trend. Date here is the calendar day
// bucket derived from Flight.CreatedAt (the trusted server timestamp) --
// never from Flight.Date.
public class DailyFlightCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
