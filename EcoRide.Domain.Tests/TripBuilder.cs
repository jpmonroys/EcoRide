using EcoRide.Domain;

namespace EcoRide.Domain.Tests;

public sealed class TripBuilder
{
    private string _vehicleId = "scooter-abc-123";
    private Location _origin = new(19.4326, -99.1332);

    public TripBuilder WithVehicleId(string vehicleId)
    {
        _vehicleId = vehicleId;
        return this;
    }

    public TripBuilder WithOrigin(Location origin)
    {
        _origin = origin;
        return this;
    }

    public Trip Build() => TripFactory.Create(_vehicleId, _origin);
}
