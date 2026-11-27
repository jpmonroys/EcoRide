using EcoRide.Application;
using EcoRide.Infrastructure;
using EcoRide.FleetAnalytics.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace EcoRide.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        var connectionString = builder.Configuration.GetConnectionString("ecoride")
            ?? "Server=localhost,1434;Database=EcoRide;User Id=sa;Password=EcoRide!2026;TrustServerCertificate=true";

        builder.Services.AddDbContext<EcoRideDbContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddScoped<ITripWriteRepository, TripWriteRepository>();
        builder.Services.AddScoped<ITripHistoryReader>(_ => new TripHistoryReader(connectionString));
        builder.Services.AddScoped<StartTripCommandHandler>();

        builder.Services.AddGrpcClient<FleetService.FleetServiceClient>(options =>
        {
            options.Address = new Uri(builder.Configuration["FLEET_GRPC_URL"] ?? "https://localhost:7188");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EcoRideDbContext>();
            db.Database.Migrate();
        }

        app.MapDefaultEndpoints();
        if (app.Environment.IsDevelopment()) app.MapOpenApi();

        app.UseAuthorization();

        app.MapPost("/trips/start", async (
            StartTripCommand command,
            StartTripCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var trip = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/trips/{trip.Id}", new
            {
                trip.Id,
                trip.VehicleId,
                trip.Status,
                trip.StartedAt
            });
        }).WithName("StartTrip").WithTags("Trips");

        app.MapGet("/trips/history", async (
            ITripHistoryReader reader,
            string? vehicleId,
            CancellationToken cancellationToken) =>
            Results.Ok(await reader.GetAsync(new GetTripHistoryQuery(vehicleId), cancellationToken)))
            .WithName("GetTripHistory").WithTags("Trips");

        app.MapPost("/trips/{tripId:guid}/finish", async (
            Guid tripId,
            LocationRequest location,
            EcoRideDbContext db,
            FleetService.FleetServiceClient fleetClient,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            var trip = await db.Trips.FindAsync([tripId], cancellationToken);
            if (trip is null) return Results.NotFound();

            trip.Finish(new EcoRide.Domain.Location(location.Latitude, location.Longitude));
            await db.SaveChangesAsync(cancellationToken);

            if (location.BatteryPercentage is < 15)
            {
                var headers = new Metadata
                {
                    { "x-api-token", configuration["GRPC_API_TOKEN"] ?? "eco-ride-demo-token" }
                };
                await fleetClient.LockVehicleAsync(new LockVehicleRequest
                {
                    VehicleId = trip.VehicleId,
                    Reason = $"Battery alert ({location.BatteryPercentage:P0})"
                }, headers);
            }

            return Results.Ok(trip);
        }).WithName("FinishTrip").WithTags("Trips");

        app.Run();
    }
}

public sealed record LocationRequest(double Latitude, double Longitude, double? BatteryPercentage);
