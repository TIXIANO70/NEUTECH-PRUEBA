using System.Text.Json;

namespace Backend.Domain;

// Arma los registros que genera el propio Grupo 5 (los que el Grupo 6 consulta en /api/v1/records y /api/v1/alerts)
public static class FabricaRegistros
{
    static JsonElement J<T>(T valor) => JsonSerializer.SerializeToElement(valor);

    public static EventoContrato ParaAlerta(AlertaRegistrada a, string estadoAlerta) => new()
    {
        SchemaVersion = "1.0.0",
        RecordId = Guid.NewGuid().ToString(),
        RecordType = "alert",
        NodeId = Nodos.AlertNodeId,
        Group = 5,
        Subsystem = Nodos.Registro[5].Subsistema,
        Timestamp = Nodos.Ahora(),
        Status = "active",
        AlertLevel = a.Level,
        Data = new()
        {
            ["alert_id"] = J(a.AlertId),
            ["alert_status"] = J(estadoAlerta),
            ["message"] = J(a.Message),
            ["channels"] = J(new[] { "web", "led", "buzzer", "lcd" }),
            ["related_record_ids"] = J(new[] { a.SourceRecordId }),
        },
        Metadata = new() { Environment = a.Environment, SourceType = a.SourceType, AlertEvaluated = true },
    };

    public static EventoContrato ParaConfirmacion(AlertaRegistrada a, string tipo, string resultado) => new()
    {
        SchemaVersion = "1.0.0",
        RecordId = Guid.NewGuid().ToString(),
        RecordType = "confirmation",
        NodeId = Nodos.AlertNodeId,
        Group = 5,
        Subsystem = Nodos.Registro[5].Subsistema,
        Timestamp = Nodos.Ahora(),
        Status = "active",
        AlertLevel = a.Level,
        Data = new()
        {
            ["alert_id"] = J(a.AlertId),
            ["confirmation_type"] = J(tipo),
            ["result"] = J(resultado),
        },
        Metadata = new() { Environment = a.Environment, SourceType = a.SourceType, AlertEvaluated = true },
    };

    // Estado del nodo: "active" solo si el ESP32 consultó la API hace poco. No se finge conexión.
    public static EventoContrato EstadoNodo(bool esp32Conectado) => new()
    {
        SchemaVersion = "1.0.0",
        RecordId = "estado-" + Nodos.AlertNodeId,
        RecordType = "state",
        NodeId = Nodos.AlertNodeId,
        Group = 5,
        Subsystem = Nodos.Registro[5].Subsistema,
        Timestamp = Nodos.Ahora(),
        Status = esp32Conectado ? "active" : "offline",
        AlertLevel = "normal",
        Data = new() { ["device_connected"] = J(esp32Conectado) },
        Metadata = new() { Environment = "test", SourceType = "real", AlertEvaluated = false },
    };
}
