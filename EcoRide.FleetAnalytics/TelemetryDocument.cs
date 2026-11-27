using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EcoRide.FleetAnalytics;

public sealed class TelemetryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("vehicleId")]
    public string VehicleId { get; set; } = string.Empty;

    [BsonElement("batteryPercentage")]
    public double BatteryPercentage { get; set; }

    [BsonElement("gForce")]
    public double GForce { get; set; }

    [BsonElement("capturedAt")]
    public DateTimeOffset CapturedAt { get; set; }

    [BsonElement("locked")]
    public bool Locked { get; set; }
}
