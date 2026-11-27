# EcoRide — Documentación integral del taller

## 1. Qué se desarrolló

EcoRide es una solución distribuida para gestionar viajes de vehículos compartidos y analizar telemetría de su flota.

Se construyó una solución .NET 10 con:

- `EcoRide.Domain`: núcleo DDD con agregados, objetos de valor y factoría.
- `EcoRide.Domain.Tests`: pruebas unitarias xUnit y `TripBuilder`.
- `EcoRide.Application`: comandos y consultas CQRS.
- `EcoRide.Infrastructure`: EF Core, SQL Server, Dapper y migraciones.
- `EcoRide.Api`: Minimal API REST y cliente gRPC.
- `EcoRide.FleetAnalytics`: API REST, servidor gRPC, MongoDB y plugin de mantenimiento.
- `EcoRide.AppHost`: orquestación de contenedores con .NET Aspire.
- `EcoRide.ServiceDefaults`: salud, resiliencia, descubrimiento y OpenTelemetry.

## 2. Cómo se desarrolló

### Paso 1 — Dominio DDD y TDD

`Trip` protege su estado mediante propiedades de lectura y métodos de negocio. Su constructor interno impide crear instancias fuera de la factoría `TripFactory`. `Location` valida latitud y longitud.

Metodología utilizada:

1. Se creó `TripBuilder` con valores predeterminados.
2. Se definieron pruebas para inicio de viaje, doble inicio y coordenadas inválidas.
3. Se implementó el agregado hasta satisfacer las pruebas.

Comandos:

```powershell
dotnet test EcoRide.Domain.Tests/EcoRide.Domain.Tests.csproj
```

Resultado esperado: 4 pruebas exitosas.

### Paso 2 — CQRS

Escritura:

- `StartTripCommand` representa la intención.
- `StartTripCommandHandler` usa `TripFactory`, ejecuta `Start()` y persiste con EF Core.
- `ITripWriteRepository` abstrae persistencia.

Lectura:

- `GetTripHistoryQuery` solicita el historial.
- `ITripHistoryReader` expone una consulta de lectura.
- `TripHistoryReader` usa SQL plano con Dapper, sin cargar entidades EF completas.

La separación permite evolucionar, escalar y optimizar lecturas y escrituras por caminos diferentes.

### Paso 3 — Minimal APIs

Se reemplazaron controladores por rutas ligeras:

- `POST /trips/start`
- `GET /trips/history`
- `POST /trips/{tripId}/finish`
- `POST /telemetry`
- `GET /telemetry/{vehicleId}/diagnosis`

Las rutas poseen OpenAPI en ejecución de desarrollo.

### Paso 4 — Aspire

`AppHost.cs` declara:

- SQL Server en `1434`.
- Redis en `6379`.
- MongoDB en `27018`.
- El microservicio de viajes.
- Fleet Analytics.

La infraestructura se crea como recursos locales de contenedor; no requiere Compose manual.

### Paso 5 — EF Core

`EcoRideDbContext` usa Fluent API. `Location` se configura como tipo Owned en columnas de origen y destino.

La migración inicial genera la tabla `Trips`.

### Paso 6 — Persistencia políglota

- SQL Server almacena viajes estructurados.
- MongoDB almacena telemetría y estado bloqueado de vehículos.
- Redis queda disponible como caché, pendiente de integración funcional directa.

### Paso 7 — gRPC seguro

El contrato `fleet.proto` expone `LockVehicle`. El servidor valida `x-api-token` mediante `ApiTokenInterceptor`.

Importante: el token protege el método, pero no sustituye TLS. En ejecución local, usar HTTPS válido para producción. No agregar tokens reales al repositorio; usar secretos.

### Paso 8 — Integración gRPC

Al finalizar un viaje con batería menor a 15%, la API obtiene el agregado y llama al cliente gRPC Fleet Analytics. Fleet Analytics actualiza el vehículo y registra la acción en MongoDB.

### Paso 9 — IA

`TelemetryMaintenancePlugin` interpreta batería mínima y cantidad de impactos. En este prototipo usa reglas deterministas dentro de un plugin compatible con Semantic Kernel. El reto requiere integrar un proveedor LLM real, por ejemplo Azure OpenAI, OpenAI o modelo local, antes de usarlo como diagnóstico generativo de producción.

El `PackageReference` de Semantic Kernel debe mantenerse controlado por vulnerabilidades conocidas. Se recomienda actualizar a una versión corregida antes de entregar o desplegar.

### Paso 10 — Pruebas y observabilidad

Aspire ofrece dashboard, trazas, métricas, logs y servicio de salud. El flujo completo puede seguirse entre API de viajes y Fleet Analytics.

## 3. Por qué se hizo de esta forma

- DDD protege invariantes y evita que las aplicaciones externas modifiquen el estado arbitrariamente.
- CQRS permite que las escrituras prioricen consistencia con EF Core y las lecturas velocidad con Dapper.
- Minimal APIs reducen código repetitivo de controladores en endpoints pequeños.
- EF Core transforma el modelo .NET en el esquema SQL mediante migraciones.
- MongoDB es apropiado para documentos variables de telemetría.
- gRPC usa HTTP/2 y búferes binarios, reduciendo sobrecarga frente a REST en comunicación interna.
- Aspire centraliza Docker, descubrimiento, variables y observabilidad.
- Semantic Kernel crea un punto de integración para capacidades cognitivas sin acoplar reglas a un proveedor específico.

## 4. Cómo funciona

```text
Cliente HTTP
    |
    | POST /trips/start
    v
EcoRide.Api -> StartTripCommandHandler
                    |
                    v
               Trip agregado
                    |
                    v
          EF Core -> SQL Server

Cliente finaliza viaje
    |
    v
EcoRide.Api -> Trip.Finish()
    |
    +--> Batería >= 15%: solo persiste finalización
    |
    +--> Batería < 15%: gRPC LockVehicle
                              |
                              v
                    Fleet Analytics interceptor
                              |
                              v
                       MongoDB locked=true

GET /telemetry/{id}/diagnosis -> MongoDB -> plugin de mantenimiento -> respuesta JSON
```

## 5. Requisitos de implementación

### Software

- Windows 10/11.
- Docker Desktop activo.
- .NET SDK 10.
- Aspire CLI 17.
- EF CLI `dotnet-ef` 10.
- Git.
- Visual Studio 2022/2026 o VS Code.

### Puertos

- SQL Server: 1434.
- Redis: 6379.
- MongoDB: 27018.
- Puertos HTTPS asignados dinámicamente por Aspire.

### Restauración inicial

```powershell
cd "C:\Pos\capacitacion\Curso microservicios\semana22\EcoRide"
dotnet restore EcoRide.slnx
dotnet build EcoRide.slnx
dotnet test EcoRide.Domain.Tests/EcoRide.Domain.Tests.csproj
```

## 6. Cómo ejecutarlo

1. Iniciar Docker Desktop.
2. Abrir el terminal en la carpeta EcoRide.
3. Ejecutar:

```powershell
dotnet run --project EcoRide.AppHost
```

4. Abrir el dashboard indicado por consola.
5. Verificar que SQL Server, Redis, MongoDB, Trip API y Fleet Analytics estén saludables.

Aplicar la migración:

```powershell
$env:PATH = "C:\Program Files\dotnet;$env:PATH"
& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" database update `
  --project EcoRide.Infrastructure/EcoRide.Infrastructure.csproj `
  --startup-project EcoRide.Api/EcoRide.Api.csproj `
  --context EcoRideDbContext
```

## 7. Cómo probarlo

Ejemplos usando PowerShell y el puerto real indicado por Aspire:

### Iniciar viaje

```powershell
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:PUERTO/trips/start" `
  -ContentType "application/json" `
  -Body '{"vehicleId":"scooter-abc-123","latitude":19.4326,"longitude":-99.1332}'
```

Guardar el `id` devuelto.

### Consultar historial

```powershell
Invoke-RestMethod `
  -Uri "https://localhost:PUERTO/trips/history?vehicleId=scooter-abc-123"
```

### Registrar telemetría crítica

```powershell
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:PUERTO_FLEET/telemetry" `
  -ContentType "application/json" `
  -Body '{"vehicleId":"scooter-abc-123","batteryPercentage":9,"gForce":2.4,"capturedAt":"2026-11-26T10:14:02Z","locked":false}'
```

### Solicitar diagnóstico

```powershell
Invoke-RestMethod `
  -Uri "https://localhost:PUERTO_FLEET/telemetry/scooter-abc-123/diagnosis"
```

### Finalizar viaje con batería baja

```powershell
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:PUERTO/trips/TRIP_ID/finish" `
  -ContentType "application/json" `
  -Body '{"latitude":19.4350,"longitude":-99.1300,"batteryPercentage":9}'
```

El resultado debe provocar la llamada gRPC y la actualización en MongoDB.

## 8. Cómo evidenciar los requisitos del taller

Capturar estas evidencias:

1. Captura de `dotnet test` con las pruebas aprobadas.
2. Captura de la solución con los ocho proyectos.
3. Captura de `Trip.cs`, `TripFactory.cs` y `TripBuilder.cs`.
4. Captura del dashboard Aspire con los cinco recursos activos.
5. Captura del Swagger/OpenAPI de Trip API.
6. Respuesta JSON de `POST /trips/start`.
7. Respuesta JSON de `GET /trips/history`.
8. Respuesta JSON de `POST /trips/{id}/finish` con batería baja.
9. Log del servidor Fleet Analytics mostrando la llamada gRPC y autenticación exitosa.
10. Evidencia del documento creado en MongoDB y el campo `locked=true`.
11. Respuesta JSON del diagnóstico de telemetría.
12. Captura de una traza distribuida desde Aspire.
13. Captura de la migración EF y tabla `Trips` creada en SQL Server.
14. Captura del repositorio GitHub con el README y el código completo.

## 9. Cómo personalizarlo

### Cambiar reglas del agregado

Modificar `Trip.cs` o `TripFactory.cs`. Agregar primero pruebas en `TripTests.cs`.

### Cambiar puertos Aspire

Editar `EcoRide.AppHost/AppHost.cs`.

### Cambiar esquema SQL

Modificar `EcoRideDbContext.cs`, ejecutar una migración y aplicar:

```powershell
& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add NuevaCambio `
  --project EcoRide.Infrastructure/EcoRide.Infrastructure.csproj `
  --startup-project EcoRide.Api/EcoRide.Api.csproj `
  --context EcoRideDbContext
```

### Cambiar token gRPC

Usar secretos de usuario, no el archivo de configuración:

```powershell
dotnet user-secrets init --project EcoRide.FleetAnalytics
dotnet user-secrets set "GRPC_API_TOKEN" "token-seguro" --project EcoRide.FleetAnalytics
dotnet user-secrets set "GRPC_API_TOKEN" "token-seguro" --project EcoRide.Api
```

El mismo valor debe existir en ambos microservicios.

### Agregar LLM real

Integrar un cliente Azure OpenAI/OpenAI en un proyecto de infraestructura o plugin y ejecutar `InvokePromptAsync`. Mantener claves fuera del código y usar secretos o variables de entorno.

### Cambiar MongoDB

Actualizar `MONGO_CONNECTION_STRING` mediante Aspire o configuración. No codificar contraseñas en `appsettings.json`.

## 10. Deudas técnicas identificadas

- `SemanticKernel` usa 1.68.0 y NuGet informa vulnerabilidad crítica conocida.
- El plugin actual no consume un LLM real; solo aplica reglas deterministas.
- gRPC usa un token estático compartido y debe conectarse mediante TLS y secretos.
- No existe rotación ni identidad de certificados para ambiente productivo.
- No se aplica automáticamente la migración al iniciar Aspire.
- Redis está declarado pero no tiene integración funcional con caché.
- Falta integración completa de OpenTelemetry para instrumentación gRPC en el proyecto.
- Falta agregar pruebas de integración para CQRS, gRPC y MongoDB.
- Falta validación HTTP completa de todos los modelos de entrada.
- Falta implementar manejo de resiliencia ante caídas temporales de SQL, MongoDB o Fleet Analytics.
- El bloqueo gRPC solo actualiza documentos existentes; no crea telemetría nueva si no existe registro previo.
- Los puertos y nombres deben revisarse tras instalar Aspire 17 en otras máquinas.
- Los proyectos generados contienen archivos base como `WeatherForecast` y `UnitTest1`, pendientes de limpieza.
- No existe autenticación JWT para las Minimal APIs REST.
- No se agregó un `docker-compose.yml`, pero tampoco es necesario al usar Aspire.

## 11. Reflexión personal

El diseño Onion separa el núcleo de dominio de EF Core, bases de datos, transporte HTTP y proveedores de IA. Esta separación reduce cambios en cascada y permite sustituir infraestructura sin alterar las reglas de negocio.

El canal binario gRPC reduce sobrecarga en comunicaciones internas críticas y mejora la velocidad entre microservicios. Cuando se protege con TLS y metadatos autenticados, evita que información sensible sea interceptada o modificada.

.NET Aspire reduce el costo operativo al declarar contenedores y servicios en C#, proporcionar descubrimiento automático y centralizar salud, métricas, trazas y logs. Así se evita mantener scripts dispersos y se detectan cuellos de botella de forma temprana.

Las pruebas unitarias y migraciones controlan cambios antes de producción. Juntas permiten escalar globalmente con menor retrabajo, despliegues independientes y mayor estabilidad.

## Dockerización

Los microservicios cuentan con Dockerfiles multietapa usando SDK y runtime oficiales de .NET 10. Docker Compose agrega SQL Server, Redis, MongoDB, Fleet Analytics y Trip API en una red privada.

### Construcción

```powershell
docker compose build
docker compose up -d
docker compose ps
```

### Puertos Docker

- Trip API: `8081 -> 8080`.
- Fleet Analytics: `8082 -> 8080`.
- SQL Server: `1434 -> 1433`.
- Redis: `6379 -> 6379`.
- MongoDB: `27018 -> 27017`.

### Persistencia

Los volúmenes Docker mantienen SQL Server, Redis y MongoDB entre reinicios:

- `ecoride-sql`.
- `ecoride-redis`.
- `ecoride-mongo`.

### Migración automática

Trip API ejecuta `db.Database.Migrate()` antes de publicar sus endpoints. SQL Server debe estar disponible; Compose usa una comprobación de salud para asegurar el arranque previo.

### gRPC TLS

Fleet Analytics expone HTTPS con un certificado PFX de desarrollo montado desde `certs/ecoride.pfx`. El cliente Trip API confía el certificado mediante validación configurable para la ejecución Docker local.

Esta configuración es válida para laboratorio. En producción:

- Reemplazar el PFX por certificados emitidos y protegidos.
- No desactivar la validación de certificados.
- Configurar SAN para el nombre DNS real del servicio.
- Administrar tokens mediante secretos del orquestador.

### Evidencia Docker

- Captura de `docker compose ps` con los cinco servicios saludables.
- Captura de las imágenes construidas.
- Captura del inicio y finalización de viaje en la API.
- Captura de logs donde aparece `Metadata Authentication Successful`.
- Captura de logs donde aparece `Vehicle ... locked in MongoDB`.
- Captura del diagnóstico HTTPS de telemetría.
