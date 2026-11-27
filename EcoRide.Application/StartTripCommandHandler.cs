using EcoRide.Domain;

namespace EcoRide.Application;

public sealed class StartTripCommandHandler(ITripWriteRepository repository)
{
    public async Task<Trip> HandleAsync(StartTripCommand command, CancellationToken cancellationToken = default)
    {
        var trip = TripFactory.Create(command.VehicleId, new Location(command.Latitude, command.Longitude));
        trip.Start();
        await repository.AddAsync(trip, cancellationToken);
        return trip;
    }
}
