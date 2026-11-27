namespace EcoRide.Domain;

public sealed record Location
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Location(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }
}
