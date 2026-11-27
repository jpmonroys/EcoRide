using EcoRide.FleetAnalytics.Grpc;
using Grpc.Core;
using MongoDB.Driver;

namespace EcoRide.FleetAnalytics;

public sealed class FleetServiceImpl(
    IMongoCollection<TelemetryDocument> telemetry,
    ILogger<FleetServiceImpl> logger) : FleetService.FleetServiceBase
{
    public override async Task<LockVehicleResponse> LockVehicle(LockVehicleRequest request, ServerCallContext context)
    {
        var filter = Builders<TelemetryDocument>.Filter.Eq(x => x.VehicleId, request.VehicleId);
        var update = Builders<TelemetryDocument>.Update.Set(x => x.Locked, true)
            .Set(x => x.CapturedAt, DateTimeOffset.UtcNow);

        await telemetry.UpdateManyAsync(filter, update);
        logger.LogInformation("Received gRPC Call: /FleetService/LockVehicle");
        logger.LogInformation("Vehicle '{VehicleId}' locked in MongoDB due to {Reason}", request.VehicleId, request.Reason);

        return new LockVehicleResponse
        {
            Locked = true,
            Message = $"Vehicle {request.VehicleId} locked."
        };
    }
}
