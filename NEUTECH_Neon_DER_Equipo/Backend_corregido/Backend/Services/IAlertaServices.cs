using Backend.Domain;

namespace Backend.Services;

public interface IAlertaServices
{
    // Eventos entrantes
    ResultadoEvento ProcesarEvento(EventoContrato evento);
    IEnumerable<EventoContrato> ObtenerEventos();

    // ESP32
    OrdenESP32 ObtenerOrdenParaESP32();
    void RegistrarConsultaEsp32();
    void RegistrarConfirmacion(ConfirmacionESP32 confirmacion);

    // Resolver
    void ResolverAlerta();                              // apaga la alarma física y cierra las alertas activas
    ResultadoResolucion ResolverAlertaPorId(string alertId);

    // Consultas del Grupo 6 (formato Contrato)
    ColeccionRegistros ObtenerRegistros(string? cursor, int limite);
    ColeccionRegistros ObtenerRegistrosAlerta();
    ColeccionRegistros ObtenerEstado();
}
