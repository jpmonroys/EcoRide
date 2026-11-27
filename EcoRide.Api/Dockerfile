FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EcoRide.slnx ./
COPY EcoRide.Domain/ ./EcoRide.Domain/
COPY EcoRide.Application/ ./EcoRide.Application/
COPY EcoRide.Infrastructure/ ./EcoRide.Infrastructure/
COPY EcoRide.ServiceDefaults/ ./EcoRide.ServiceDefaults/
COPY EcoRide.Api/ ./EcoRide.Api/

RUN dotnet restore EcoRide.Api/EcoRide.Api.csproj
RUN dotnet publish EcoRide.Api/EcoRide.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "EcoRide.Api.dll"]
