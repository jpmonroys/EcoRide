var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql", port: 1434)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("ecoride");

var redis = builder.AddRedis("redis", port: 6379)
    .WithLifetime(ContainerLifetime.Persistent);

var mongo = builder.AddMongoDB("mongo", port: 27018)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("fleet");

var fleet = builder.AddDotnetProject("fleet-analytics", "../EcoRide.FleetAnalytics/EcoRide.FleetAnalytics.csproj")
    .WithReference(mongo)
    .WithExternalHttpEndpoints();

builder.AddDotnetProject("trip-api", "../EcoRide.Api/EcoRide.Api.csproj")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(fleet)
    .WithEnvironment("FLEET_GRPC_URL", fleet.GetEndpoint("https"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
