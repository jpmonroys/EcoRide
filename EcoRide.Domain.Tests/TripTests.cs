using EcoRide.Domain;

namespace EcoRide.Domain.Tests;

public class TripTests
{
    [Fact]
    public void Start_changes_status_and_sets_start_time()
    {
        var trip = new TripBuilder().Build();

        trip.Start();

        Assert.Equal(TripStatus.Started, trip.Status);
        Assert.NotNull(trip.StartedAt);
    }

    [Fact]
    public void Start_rejects_a_second_start()
    {
        var trip = new TripBuilder().Build();
        trip.Start();

        var action = () => trip.Start();

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Location_rejects_out_of_range_coordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Location(91, 0));
    }
}
