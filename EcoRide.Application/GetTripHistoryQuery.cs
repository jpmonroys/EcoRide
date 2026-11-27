namespace EcoRide.Application;

public sealed record GetTripHistoryQuery(string? VehicleId = null);

public sealed record TripHistoryDto(Guid Id, string VehicleId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt);
