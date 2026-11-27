using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace EcoRide.FleetAnalytics;

public sealed class TelemetryMaintenancePlugin
{
    [KernelFunction("diagnose_telemetry")]
    [Description("Generates a predictive maintenance diagnosis from scooter telemetry JSON.")]
    public string Diagnose(
        [Description("Minimum battery percentage recorded")] double minimumBattery,
        [Description("Number of impact events above 2.1 G")] int impactEvents)
    {
        if (minimumBattery < 15 && impactEvents > 0)
            return "[REPORTE DE IA - SISTEMA DE ALERTA ECO-RIDE] Caída crítica de batería junto con impactos físicos. Riesgo alto de daño en batería o celda. Enviar inmediatamente al taller.";

        if (minimumBattery < 15)
            return "[REPORTE DE IA - SISTEMA DE ALERTA ECO-RIDE] Nivel crítico de batería. Suspender uso y revisar la batería antes de reabastecer.";

        if (impactEvents > 0)
            return "[REPORTE DE IA - SISTEMA DE ALERTA ECO-RIDE] Se registraron impactos físicos. Programar inspección preventiva del chasis y la protección de batería.";

        return "[REPORTE DE IA - SISTEMA DE ALERTA ECO-RIDE] Telemetría estable. No requiere intervención inmediata.";
    }
}
