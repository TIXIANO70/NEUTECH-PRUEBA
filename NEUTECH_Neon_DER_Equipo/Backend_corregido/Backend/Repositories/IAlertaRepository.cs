using Backend.Domain;

namespace Backend.Repositories;

// La Interfaz define que operaciones de datos podemos hacer
public interface IAlertaRepository
{
    // Eventos recibidos (historial simple)
    void GuardarEvento(EventoContrato evento);
    IEnumerable<EventoContrato> ObtenerEventos();

    // Registros normalizados (eventos + alertas + confirmaciones): idempotencia y /api/v1/records
    string? ObtenerPayloadRegistro(string recordId);
    void GuardarRegistro(string recordId, string recordType, string nodeId, string payload);
    IReadOnlyList<RegistroAlmacenado> ObtenerRegistrosDespuesDe(long seq, int limite);
    IReadOnlyList<RegistroAlmacenado> ObtenerUltimosPorTipo(string recordType, int limite);

    // Alertas generadas por el Grupo 5
    void GuardarAlerta(AlertaRegistrada alerta);
    AlertaRegistrada? ObtenerAlerta(string alertId);
    AlertaRegistrada? ObtenerAlertaPorOrigen(string sourceRecordId);
    IReadOnlyList<AlertaRegistrada> ObtenerAlertasActivas();

    // Orden física para el ESP32
    OrdenESP32 ObtenerOrdenActiva();
    void ActualizarOrden(OrdenESP32 nuevaOrden);

    // Última vez que el ESP32 consultó la API (para saber si está conectado)
    DateTime? ObtenerUltimaConsultaEsp32();
    void MarcarConsultaEsp32();
}
