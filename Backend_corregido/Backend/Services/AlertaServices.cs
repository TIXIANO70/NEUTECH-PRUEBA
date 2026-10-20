using System.Text.Json.Nodes;
using Backend.Domain;
using Backend.Repositories;

namespace Backend.Services;

public class AlertaServices : IAlertaServices
{
    // El repositorio es Singleton y la API recibe pedidos en paralelo (ESP32 consultando + eventos entrando).
    // Este candado evita que dos pedidos se pisen mientras leen/modifican la orden activa.
    private static readonly object Gate = new();

    private static readonly Dictionary<string, int> Severidad = new()
    {
        ["normal"] = 0, ["low"] = 1, ["medium"] = 2, ["high"] = 3, ["critical"] = 4
    };
    private static readonly HashSet<int> GruposEmisores = new() { 1, 2, 3, 4, 6 };   // el 5 somos nosotros
    private static readonly HashSet<string> TiposConfirmacion = new() { "technical_receipt", "device_execution", "human_acknowledgement" };
    private static readonly HashSet<string> ResultadosConfirmacion = new() { "success", "failure" };
    private static readonly TimeSpan VentanaEsp32 = TimeSpan.FromSeconds(15);

    private readonly IAlertaRepository _repository;
    private readonly bool _validarRegistroNodos;

    public AlertaServices(IAlertaRepository repository, IConfiguration config)
    {
        _repository = repository;
        _validarRegistroNodos = config.GetValue<bool>("Contrato:ValidarRegistroNodos");
    }

    public IEnumerable<EventoContrato> ObtenerEventos() => _repository.ObtenerEventos();

    // ---------------------------------------------------------------- eventos entrantes
    public ResultadoEvento ProcesarEvento(EventoContrato evento)
    {
        ValidadorContrato.Validar(evento, _validarRegistroNodos);
        if (evento.RecordType != "event" || !GruposEmisores.Contains(evento.Group))
            throw new ExcepcionPeticionInvalida("Se esperan eventos (record_type=event) de los grupos 1 a 4 o 6");

        lock (Gate)
        {
            // Idempotencia por record_id: igual => duplicado (200) / distinto => conflicto (409)
            if (!GuardarRegistroNuevo(evento))
                return new ResultadoEvento(true, true, false, _repository.ObtenerAlertaPorOrigen(evento.RecordId)?.AlertId);

            _repository.GuardarEvento(evento);

            string level = evento.AlertLevel.ToLower();
            // Regla del equipo: medium/high/critical. Solo si el emisor ya evaluó la alerta (metadata.alert_evaluated).
            bool dispara = evento.Metadata.AlertEvaluated && level is "medium" or "high" or "critical";
            if (!dispara) return new ResultadoEvento(true, false, false, null);

            // Un evento de menor gravedad NO puede bajar/pisar una alarma más grave que sigue activa
            var actual = _repository.ObtenerOrdenActiva();
            bool hayAlarmaActiva = actual.AlertStatus is "active" or "active_confirmed";
            if (hayAlarmaActiva && Severidad.GetValueOrDefault(actual.AlertLevel) > Severidad[level])
                return new ResultadoEvento(true, false, false, null);

            string idAlerta = $"ALT-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            string zona = evento.Group switch
            {
                1 => "INCENDIO",
                2 => "VOLCAN",
                3 => "ENERGIA",
                4 => "AGUA",
                _ => "SISTEMA"
            };
            string mensajeLcd = level switch
            {
                "critical" => "EVACUAR ZONA",
                "high"     => $"ALERTA {zona}",
                "medium"   => $"CUIDADO {zona}",
                _          => "ALERTA ACTIVA"
            };

            var alerta = new AlertaRegistrada
            {
                AlertId = idAlerta,
                SourceRecordId = evento.RecordId,
                Level = level,
                Message = evento.Data["message"].GetString()!,
                Environment = evento.Metadata.Environment,
                SourceType = evento.Metadata.SourceType,
                Status = "active"
            };
            _repository.GuardarAlerta(alerta);
            GuardarRegistroNuevo(FabricaRegistros.ParaAlerta(alerta, "active"));

            _repository.ActualizarOrden(new OrdenESP32
            {
                AlertId = idAlerta,
                AlertLevel = level,
                AlertStatus = "active",
                Message = $"Riesgo detectado en {evento.Subsystem}",
                LcdText = mensajeLcd,
                RedLed = level is "critical" or "high",
                YellowLed = level is "medium",
                GreenLed = false,
                Buzzer = level is "critical"
            });
            return new ResultadoEvento(true, false, true, idAlerta);
        }
    }

    // ---------------------------------------------------------------- ESP32
    public OrdenESP32 ObtenerOrdenParaESP32() => _repository.ObtenerOrdenActiva();

    public void RegistrarConsultaEsp32() => _repository.MarcarConsultaEsp32();

    // El ESP32 confirma que ejecutó la alarma física
    public void RegistrarConfirmacion(ConfirmacionESP32 c)
    {
        if (!TiposConfirmacion.Contains(c.ConfirmationType))
            throw new ExcepcionPeticionInvalida("confirmation_type inválido (technical_receipt, device_execution, human_acknowledgement)");
        if (!ResultadosConfirmacion.Contains(c.Result))
            throw new ExcepcionPeticionInvalida("result inválido (success, failure)");

        lock (Gate)
        {
            var alerta = _repository.ObtenerAlerta(c.AlertId) ?? throw new ExcepcionNoEncontrado("Alerta no encontrada");
            GuardarRegistroNuevo(FabricaRegistros.ParaConfirmacion(alerta, c.ConfirmationType, c.Result));

            // Solo confirma una alarma que sigue activa (no "revive" una ya resuelta)
            var orden = _repository.ObtenerOrdenActiva();
            if (orden.AlertId == alerta.AlertId && orden.AlertStatus == "active"
                && c.ConfirmationType == "device_execution" && c.Result == "success")
                _repository.ActualizarOrden(orden with { AlertStatus = "active_confirmed" });
        }
    }

    // ---------------------------------------------------------------- resolver
    // Apaga la alarma física: vuelve a estado NORMAL (LED verde, buzzer apagado) y cierra las alertas activas
    public void ResolverAlerta()
    {
        lock (Gate)
        {
            foreach (var a in _repository.ObtenerAlertasActivas()) CerrarAlerta(a);
            var orden = _repository.ObtenerOrdenActiva();
            // Los valores por defecto de OrdenESP32 ya representan el estado normal
            _repository.ActualizarOrden(new OrdenESP32 { AlertId = orden.AlertId });
        }
    }

    public ResultadoResolucion ResolverAlertaPorId(string alertId)
    {
        lock (Gate)
        {
            var alerta = _repository.ObtenerAlerta(alertId) ?? throw new ExcepcionNoEncontrado("Alerta no encontrada");
            if (alerta.Status == "resolved") return new ResultadoResolucion(true, true);

            CerrarAlerta(alerta);
            var orden = _repository.ObtenerOrdenActiva();
            if (orden.AlertId == alertId) _repository.ActualizarOrden(new OrdenESP32 { AlertId = alertId });
            return new ResultadoResolucion(true, false);
        }
    }

    private void CerrarAlerta(AlertaRegistrada alerta)
    {
        alerta.Status = "resolved";
        _repository.GuardarAlerta(alerta);
        GuardarRegistroNuevo(FabricaRegistros.ParaAlerta(alerta, "resolved"));
    }

    // ---------------------------------------------------------------- consultas del Grupo 6
    public ColeccionRegistros ObtenerRegistros(string? cursor, int limite)
    {
        if (!long.TryParse(string.IsNullOrEmpty(cursor) ? "0" : cursor, out var despuesDe) || despuesDe < 0)
            throw new ExcepcionPeticionInvalida("cursor inválido");

        var filas = _repository.ObtenerRegistrosDespuesDe(despuesDe, limite + 1);
        bool hayMas = filas.Count > limite;
        var pagina = filas.Take(limite).ToList();
        return new ColeccionRegistros(
            pagina.Select(f => JsonNode.Parse(f.Payload)).ToList(),
            hayMas ? pagina[^1].Seq.ToString() : null);
    }

    public ColeccionRegistros ObtenerRegistrosAlerta() =>
        new(_repository.ObtenerUltimosPorTipo("alert", 100).Select(f => JsonNode.Parse(f.Payload)).ToList(), null);

    public ColeccionRegistros ObtenerEstado()
    {
        var ultima = _repository.ObtenerUltimaConsultaEsp32();
        bool conectado = ultima is not null && DateTime.UtcNow - ultima < VentanaEsp32;
        var estado = FabricaRegistros.EstadoNodo(conectado);
        return new ColeccionRegistros(new List<JsonNode?> { System.Text.Json.JsonSerializer.SerializeToNode(estado) }, null);
    }

    // ---------------------------------------------------------------- interno
    // true = se guardó nuevo; false = ya existía idéntico; lanza ExcepcionConflicto si existe con otro contenido
    private bool GuardarRegistroNuevo(EventoContrato registro)
    {
        string canonico = JsonCanonico.De(registro);
        var previo = _repository.ObtenerPayloadRegistro(registro.RecordId);
        if (previo is not null)
        {
            if (previo != canonico) throw new ExcepcionConflicto("record_id ya existe con contenido diferente");
            return false;
        }
        _repository.GuardarRegistro(registro.RecordId, registro.RecordType, registro.NodeId, canonico);
        return true;
    }
}
