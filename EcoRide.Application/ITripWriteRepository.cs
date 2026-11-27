using EcoRide.Domain;

namespace EcoRide.Application;

public interface ITripWriteRepository
{
    Task AddAsync(Trip trip, CancellationToken cancellationToken = default);
    Task<Trip?> FindAsync(Guid id, CancellationToken cancellationToken = default);
}
