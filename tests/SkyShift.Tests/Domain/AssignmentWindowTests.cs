using SkyShift.Domain.Entities;
using Xunit;

namespace SkyShift.Tests.Domain;

/// <summary>
/// The trial-window rule lives in the Domain once: IsActive (in memory) and its
/// query forms ActiveAt / EndedBefore (used by repositories) must agree exactly.
/// </summary>
public class AssignmentWindowTests
{
    [Fact]
    public void ActiveAt_AgreesWithIsActive_OnAndAroundEveryBoundary_AndEndedBeforeNeverOverlaps()
    {
        var start = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        var assignment = VehicleAssignment.Create(1, 2, start);
        var activeAt = (DateTime t) => VehicleAssignment.ActiveAt(t).Compile()(assignment);
        var endedBefore = (DateTime t) => VehicleAssignment.EndedBefore(t).Compile()(assignment);

        var instants = new[]
        {
            start.AddTicks(-1), start, start.AddTicks(1), start.AddDays(10),
            assignment.EndDate.AddTicks(-1), assignment.EndDate, assignment.EndDate.AddTicks(1), assignment.EndDate.AddDays(30)
        };

        foreach (var t in instants)
        {
            Assert.Equal(assignment.IsActive(t), activeAt(t));
            Assert.False(activeAt(t) && endedBefore(t)); // an assignment is never both active and ended
            if (t >= start) Assert.True(activeAt(t) || endedBefore(t)); // after it starts, it is exactly one of them
        }

        Assert.True(activeAt(start));
        Assert.True(activeAt(assignment.EndDate));
        Assert.False(activeAt(assignment.EndDate.AddTicks(1)));
        Assert.True(endedBefore(assignment.EndDate.AddTicks(1)));
    }
}
