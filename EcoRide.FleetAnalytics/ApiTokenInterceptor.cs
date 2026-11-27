using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EcoRide.FleetAnalytics;

public sealed class ApiTokenInterceptor(IConfiguration configuration, ILogger<ApiTokenInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var entry = context.RequestHeaders.FirstOrDefault(x => x.Key == "x-api-token");
        var expected = configuration["GRPC_API_TOKEN"] ?? "eco-ride-demo-token";

        if (entry is null || entry.Value != expected)
        {
            logger.LogWarning("Rejected gRPC call without valid API token");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API token"));
        }

        logger.LogInformation("Metadata Authentication Successful. Valid API Token detected in Call Headers.");
        return await continuation(request, context);
    }
}
