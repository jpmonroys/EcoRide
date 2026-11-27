using EcoRide.Application;
using EcoRide.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoRide.Infrastructure;

public sealed class TripWriteRepository(EcoRideDbContext db) : ITripWriteRepository
{
    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        db.Trips.Add(trip);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Trip?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Trips.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
