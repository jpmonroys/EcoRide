namespace EcoRide.Domain;

public static class TripFactory
{
    public static Trip Create(string vehicleId, Location origin)
        => new(Guid.NewGuid(), vehicleId, origin);
}
