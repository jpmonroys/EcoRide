using Dapper;
using EcoRide.Application;
using Microsoft.Data.SqlClient;

namespace EcoRide.Infrastructure;

public sealed class TripHistoryReader(string connectionString) : ITripHistoryReader
{
    public async Task<IReadOnlyList<TripHistoryDto>> GetAsync(GetTripHistoryQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        const string sql = """
            SELECT Id, VehicleId, Status, CreatedAt, StartedAt, FinishedAt
            FROM Trips
            WHERE (@VehicleId IS NULL OR VehicleId = @VehicleId)
            ORDER BY CreatedAt DESC
            """;

        var command = new CommandDefinition(sql, new { query.VehicleId }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<TripHistoryDto>(command);
        return rows.AsList();
    }
}
