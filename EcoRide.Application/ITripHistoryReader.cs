namespace EcoRide.Application;

public interface ITripHistoryReader
{
    Task<IReadOnlyList<TripHistoryDto>> GetAsync(GetTripHistoryQuery query, CancellationToken cancellationToken = default);
}
