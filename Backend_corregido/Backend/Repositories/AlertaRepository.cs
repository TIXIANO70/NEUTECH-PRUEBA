using Backend.Domain;

namespace Backend.Repositories;

// La Clase implementa como se guardan los datos (en memoria; cuando esté la BD solo se cambia esta clase)
// Es Singleton y recibe pedidos en paralelo => todo el acceso va dentro de lock.
public class AlertaRepository : IAlertaRepository
{
    private readonly object _lock = new();
    private readonly List<EventoContrato> _eventos = new();
    private readonly List<RegistroAlmacenado> _registros = new();
    private readonly Dictionary<string, RegistroAlmacenado> _registrosPorId = new();
    private readonly Dictionary<string, AlertaRegistrada> _alertas = new();
    private long _seq;
    private DateTime? _ultimaConsultaEsp32;
    private OrdenESP32 _ordenActiva = new OrdenESP32
    {
        AlertId = "ALT-INIT",
        AlertLevel = "normal",
        AlertStatus = "resolved",
        Message = "Sin emergencias activas",
        LcdText = "SISTEMA OK",
        GreenLed = true
    };

    public void GuardarEvento(EventoContrato evento) { lock (_lock) _eventos.Add(evento); }
    public IEnumerable<EventoContrato> ObtenerEventos() { lock (_lock) return _eventos.ToList(); }

    public string? ObtenerPayloadRegistro(string recordId)
    {
        lock (_lock) return _registrosPorId.TryGetValue(recordId, out var r) ? r.Payload : null;
    }

    public void GuardarRegistro(string recordId, string recordType, string nodeId, string payload)
    {
        lock (_lock)
        {
            var registro = new RegistroAlmacenado(++_seq, recordId, recordType, nodeId, payload);
            _registros.Add(registro);
            _registrosPorId[recordId] = registro;
        }
    }

    public IReadOnlyList<RegistroAlmacenado> ObtenerRegistrosDespuesDe(long seq, int limite)
    {
        lock (_lock) return _registros.Where(r => r.Seq > seq).OrderBy(r => r.Seq).Take(limite).ToList();
    }

    public IReadOnlyList<RegistroAlmacenado> ObtenerUltimosPorTipo(string recordType, int limite)
    {
        lock (_lock) return _registros.Where(r => r.RecordType == recordType).OrderByDescending(r => r.Seq).Take(limite).ToList();
    }

    public void GuardarAlerta(AlertaRegistrada alerta) { lock (_lock) _alertas[alerta.AlertId] = alerta; }
    public AlertaRegistrada? ObtenerAlerta(string alertId) { lock (_lock) return _alertas.GetValueOrDefault(alertId); }
    public AlertaRegistrada? ObtenerAlertaPorOrigen(string sourceRecordId)
    {
        lock (_lock) return _alertas.Values.FirstOrDefault(a => a.SourceRecordId == sourceRecordId);
    }
    public IReadOnlyList<AlertaRegistrada> ObtenerAlertasActivas()
    {
        lock (_lock) return _alertas.Values.Where(a => a.Status == "active").ToList();
    }

    public OrdenESP32 ObtenerOrdenActiva() { lock (_lock) return _ordenActiva; }
    public void ActualizarOrden(OrdenESP32 nuevaOrden) { lock (_lock) _ordenActiva = nuevaOrden; }

    public DateTime? ObtenerUltimaConsultaEsp32() { lock (_lock) return _ultimaConsultaEsp32; }
    public void MarcarConsultaEsp32() { lock (_lock) _ultimaConsultaEsp32 = DateTime.UtcNow; }
}
