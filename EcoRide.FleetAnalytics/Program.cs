using EcoRide.FleetAnalytics;
using EcoRide.FleetAnalytics.Grpc;
using MongoDB.Driver;

namespace EcoRide.FleetAnalytics;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureEndpointDefaults(endpoint =>
                endpoint.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
        });

        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<ApiTokenInterceptor>();
        });
        builder.Services.AddSingleton<ApiTokenInterceptor>();
        builder.Services.AddSingleton<TelemetryMaintenancePlugin>();

        var mongoConnection = builder.Configuration["MONGO_CONNECTION_STRING"]
            ?? "mongodb://localhost:27018";
        builder.Services.AddSingleton(_ =>
        {
            var client = new MongoClient(mongoConnection);
            return client.GetDatabase("ecoride-fleet").GetCollection<TelemetryDocument>("telemetry");
        });

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();
        if (app.Environment.IsDevelopment()) app.MapOpenApi();

        app.UseAuthorization();

        app.MapGrpcService<FleetServiceImpl>();

        app.MapPost("/telemetry", async (
            TelemetryDocument document,
            IMongoCollection<TelemetryDocument> telemetry,
            CancellationToken cancellationToken) =>
        {
            await telemetry.InsertOneAsync(document, cancellationToken: cancellationToken);
            return Results.Created($"/telemetry/{document.VehicleId}", document);
        }).WithTags("Telemetry");

        app.MapGet("/telemetry/{vehicleId}/diagnosis", async (
            string vehicleId,
            IMongoCollection<TelemetryDocument> telemetry,
            TelemetryMaintenancePlugin maintenancePlugin) =>
        {
            var records = await telemetry.Find(x => x.VehicleId == vehicleId)
                .SortByDescending(x => x.CapturedAt)
                .Limit(20)
                .ToListAsync();

            if (records.Count == 0) return Results.NotFound();

            var minimumBattery = records.Min(x => x.BatteryPercentage);
            var impactEvents = records.Count(x => x.GForce > 2.1);
            var diagnosis = maintenancePlugin.Diagnose(minimumBattery, impactEvents);

            return Results.Ok(new
            {
                VehicleId = vehicleId,
                RecordsAnalyzed = records.Count,
                ImpactEvents = impactEvents,
                MinimumBattery = minimumBattery,
                Diagnosis = diagnosis,
                Recommendation = minimumBattery < 15 || impactEvents > 0
                    ? "Suspender uso inmediato y realizar revisión técnica."
                    : "No requiere intervención inmediata."
            });
        }).WithTags("Telemetry");

        app.Run();
    }
}
