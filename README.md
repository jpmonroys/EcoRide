# EcoRide — Gestión de Viajes e Inteligencia de Flota

EcoRide es una solución de microservicios .NET creada para gestionar viajes y analizar la telemetría de una flota de vehículos compartidos.

## Arquitectura

- `EcoRide.Domain`: DDD, agregado `Trip`, objetos de valor y factoría.
- `EcoRide.Application`: comandos y consultas CQRS.
- `EcoRide.Infrastructure`: EF Core, SQL Server y Dapper.
- `EcoRide.Api`: Minimal APIs y cliente gRPC.
- `EcoRide.FleetAnalytics`: servidor gRPC, MongoDB y diagnóstico de mantenimiento.
- `EcoRide.AppHost`: orquestación con .NET Aspire.
- `EcoRide.ServiceDefaults`: OpenTelemetry, salud y resiliencia.
- `EcoRide.Domain.Tests`: pruebas unitarias xUnit.

La documentación técnica completa está en [`DOCUMENTACION_TECNICA.md`](DOCUMENTACION_TECNICA.md).

## Requisitos

- Windows 10/11.
- Docker Desktop en ejecución.
- .NET SDK 10.0.404 o superior compatible.
- .NET Aspire CLI 17.
- EF Core CLI 10.
- Git.
- Visual Studio 2022/2026 o VS Code.

Instalar herramientas:

```powershell
dotnet tool install -g Aspire.Cli --prerelease
dotnet tool install --global dotnet-ef --version 10.0.15
```

Instalar templates Aspire si todavía no están disponibles:

```powershell
dotnet new install Aspire.ProjectTemplates
```

## Clonar o actualizar

```powershell
git clone https://github.com/jpmonroys/EcoRide.git
cd EcoRide
```

## Restaurar y compilar

Desde la carpeta que contiene `EcoRide.slnx`:

```powershell
dotnet restore EcoRide.slnx
dotnet build EcoRide.slnx
```

## Ejecutar pruebas

```powershell
dotnet test EcoRide.Domain.Tests/EcoRide.Domain.Tests.csproj
```

Las pruebas verifican el ciclo de inicio de viaje, el bloqueo ante un segundo inicio y la validación de coordenadas.

## Crear y aplicar la migración SQL

La migración inicial ya está generada en `EcoRide.Infrastructure/Migrations`.

Cuando SQL Server esté activo mediante Aspire:

```powershell
$env:PATH = "C:\Program Files\dotnet;$env:PATH"
& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" database update `
  --project EcoRide.Infrastructure/EcoRide.Infrastructure.csproj `
  --startup-project EcoRide.Api/EcoRide.Api.csproj `
  --context EcoRideDbContext
```

## Ejecutar toda la solución

1. Abrir Docker Desktop.
2. Desde la terminal:

```powershell
dotnet run --project EcoRide.AppHost
```

3. Abrir el dashboard de Aspire mostrado por la consola.
4. Verificar los recursos:
   - SQL Server en el puerto `1434`.
   - Redis en el puerto `6379`.
   - MongoDB en el puerto `27018`.
   - `trip-api`.
   - `fleet-analytics`.

## Endpoints

### Trip API

- `POST /trips/start`
- `GET /trips/history?vehicleId=...`
- `POST /trips/{tripId}/finish`

### Fleet Analytics

- `POST /telemetry`
- `GET /telemetry/{vehicleId}/diagnosis`

## Prueba rápida

Sustituir `PUERTO_API` y `PUERTO_FLEET` por los puertos publicados en Aspire.

### Iniciar viaje

```powershell
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:PUERTO_API/trips/start" `
  -ContentType "application/json" `
  -Body '{"vehicleId":"scooter-abc-123","latitude":19.4326,"longitude":-99.1332}'
```

### Consultar historial

```powershell
Invoke-RestMethod `
  -Uri "https://localhost:PUERTO_API/trips/history?vehicleId=scooter-abc-123"
```

### Registrar telemetría crítica

```powershell
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:PUERTO_FLEET/telemetry" `
  -ContentType "application/json" `
  -Body '{"vehicleId":"scooter-abc-123","batteryPercentage":9,"gForce":2.4,"capturedAt":"2026-11-26T10:14:02Z","locked":false}'
```

### Generar diagnóstico

```powershell
Invoke-RestMethod `
  -Uri "https://localhost:PUERTO_FLEET/telemetry/scooter-abc-123/diagnosis"
```

### Finalizar viaje con batería baja

```powershell
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:PUERTO_API/trips/TRIP_ID/finish" `
  -ContentType "application/json" `
  -Body '{"latitude":19.4350,"longitude":-99.1300,"batteryPercentage":9}'
```

Esta llamada activa el contrato gRPC `LockVehicle` y bloquea el vehículo en Fleet Analytics.

## Evidencias sugeridas

- Resultado de las 4 pruebas unitarias.
- Dashboard Aspire con recursos activos.
- Swagger/OpenAPI de ambos servicios.
- Respuesta de inicio e historial de viajes.
- Logs gRPC de autenticación y bloqueo.
- Documento MongoDB con `locked=true`.
- Diagnóstico de telemetría.
- Captura de trazas distribuidas.
- Tabla SQL `Trips` creada por la migración.

## Configuración segura

No almacenar contraseñas o tokens reales en `appsettings.json`.

Ejemplo de secretos locales:

```powershell
dotnet user-secrets init --project EcoRide.FleetAnalytics
dotnet user-secrets set "GRPC_API_TOKEN" "token-seguro" --project EcoRide.FleetAnalytics
dotnet user-secrets init --project EcoRide.Api
dotnet user-secrets set "GRPC_API_TOKEN" "token-seguro" --project EcoRide.Api
```

En producción, usar Azure Key Vault, variables del orquestador o un secret manager equivalente.

## Configuración Aspire

La topología se encuentra en `EcoRide.AppHost/AppHost.cs`.

Para cambiar puertos o recursos, modificar:

```csharp
builder.AddSqlServer("sql", port: 1434);
builder.AddRedis("redis", port: 6379);
builder.AddMongoDB("mongo", port: 27018);
```

## Personalización

- Reglas de viaje: `EcoRide.Domain/Trip.cs` y `TripFactory.cs`.
- Pruebas: `EcoRide.Domain.Tests/TripTests.cs`.
- Comandos CQRS: `EcoRide.Application`.
- Lecturas Dapper: `EcoRide.Infrastructure/TripHistoryReader.cs`.
- Swagger y rutas: `EcoRide.Api/Program.cs`.
- gRPC: archivos `Protos/fleet.proto`.
- MongoDB y diagnóstico: `EcoRide.FleetAnalytics/Program.cs`.
- Plugin cognitivo: `EcoRide.FleetAnalytics/TelemetryMaintenancePlugin.cs`.

## Deudas técnicas conocidas

- Semantic Kernel 1.68.0 reporta una vulnerabilidad crítica conocida en NuGet.
- La implementación cognitiva actual usa reglas deterministas, no un proveedor LLM real.
- Falta configurar correctamente certificados TLS productivos.
- Redis está declarado pero no implementa caché funcional.
- La migración no se aplica automáticamente al iniciar Aspire.
- Faltan pruebas de integración y pruebas de resiliencia.
- Falta autenticación JWT para REST.
- Algunos archivos base del template deben eliminarse.

Detalles completos y recomendaciones en [`DOCUMENTACION_TECNICA.md`](DOCUMENTACION_TECNICA.md).

## Licencia

Proyecto educativo. Asegurar revisar licencias de dependencias antes de uso comercial.
