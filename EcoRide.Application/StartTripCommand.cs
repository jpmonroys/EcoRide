namespace EcoRide.Application;

public sealed record StartTripCommand(string VehicleId, double Latitude, double Longitude);
