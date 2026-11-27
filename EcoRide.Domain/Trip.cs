namespace EcoRide.Domain;

public sealed class Trip
{
    private Trip() { }

    internal Trip(Guid id, string vehicleId, Location origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicleId);
        Id = id;
        VehicleId = vehicleId;
        Origin = origin;
        Status = TripStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string VehicleId { get; private set; } = string.Empty;
    public Location Origin { get; private set; } = null!;
    public Location? Destination { get; private set; }
    public TripStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    public void Start()
    {
        if (Status != TripStatus.Draft)
            throw new InvalidOperationException("Only draft trips can be started.");

        Status = TripStatus.Started;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Finish(Location destination)
    {
        if (Status != TripStatus.Started)
            throw new InvalidOperationException("Only started trips can be finished.");

        Destination = destination;
        Status = TripStatus.Finished;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}
