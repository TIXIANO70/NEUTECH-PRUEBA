using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Domain;

// Reglas del Contrato de Normalización (lo que en la API de referencia hacían los validators de pydantic).
public static class ValidadorContrato
{
    static readonly HashSet<string> Tipos = new() { "reading", "state", "event", "alert", "confirmation" };
    static readonly HashSet<string> Estados = new() { "active", "offline", "error" };
    static readonly HashSet<string> Niveles = new() { "normal", "low", "medium", "high", "critical" };
    static readonly HashSet<string> Entornos = new() { "test", "operational" };
    static readonly HashSet<string> Origenes = new() { "real", "simulated", "mixed" };
    static readonly HashSet<string> Calidades = new() { "valid", "missing", "invalid", "uncalibrated" };

    // El timestamp debe traer zona horaria explícita (Z o +hh:mm)
    static readonly Regex ConZonaHoraria = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:\d{2})$", RegexOptions.Compiled);

    public static void Validar(EventoContrato r, bool validarRegistroNodos)
    {
        // JSON con null explícito: cortar antes de seguir
        if (r.Data is null || r.Metadata is null)
            throw new ExcepcionPeticionInvalida(new[] { "data y metadata son obligatorios" });

        var e = new List<string>();
        var m = r.Metadata;

        if (r.SchemaVersion != "1.0.0") e.Add("schema_version debe ser '1.0.0'");
        if (string.IsNullOrWhiteSpace(r.RecordId)) e.Add("record_id no puede estar vacío");
        if (!Tipos.Contains(r.RecordType)) e.Add("record_type inválido");
        if (string.IsNullOrWhiteSpace(r.NodeId)) e.Add("node_id no puede estar vacío");
        if (r.Group is < 1 or > 6) e.Add("group debe estar entre 1 y 6");
        if (string.IsNullOrWhiteSpace(r.Subsystem)) e.Add("subsystem no puede estar vacío");
        if (!Estados.Contains(r.Status)) e.Add("status inválido (active, offline, error)");
        if (!Niveles.Contains(r.AlertLevel)) e.Add("alert_level inválido (normal, low, medium, high, critical)");

        if (r.Timestamp is null || !ConZonaHoraria.IsMatch(r.Timestamp) ||
            !DateTimeOffset.TryParse(r.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            e.Add("timestamp debe ser ISO 8601 e incluir zona horaria (ej: 2026-10-15T12:00:00Z)");

        if (!Entornos.Contains(m.Environment)) e.Add("metadata.environment inválido (test, operational)");
        if (!Origenes.Contains(m.SourceType)) e.Add("metadata.source_type inválido (real, simulated, mixed)");
        if ((m.Quality ?? new()).Values.Any(q => !Calidades.Contains(q))) e.Add("metadata.quality tiene valores inválidos");
        if (m.SourceType == "mixed" && (m.Sources is null || m.Sources.Count == 0))
            e.Add("metadata.sources es obligatorio cuando source_type=mixed");

        // Opcional (appsettings: Contrato:ValidarRegistroNodos): node_id y subsystem deben coincidir EXACTO con el registro
        if (validarRegistroNodos && Nodos.Registro.TryGetValue(r.Group, out var esperado) &&
            (r.NodeId != esperado.NodeId || r.Subsystem != esperado.Subsistema))
            e.Add("node_id y subsystem no coinciden con el registro inicial de nodos");

        if (r.RecordType == "event")
        {
            if (!TextoNoVacio(r.Data, "event_code")) e.Add("data.event_code es obligatorio para eventos");
            if (!TextoNoVacio(r.Data, "message")) e.Add("data.message es obligatorio para eventos");
            if (r.Data.TryGetValue("related_record_ids", out var ids) &&
                (ids.ValueKind != JsonValueKind.Array || ids.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String)))
                e.Add("data.related_record_ids debe ser una lista de textos");
        }

        if (e.Count > 0) throw new ExcepcionPeticionInvalida(e);
    }

    static bool TextoNoVacio(Dictionary<string, JsonElement> d, string clave) =>
        d.TryGetValue(clave, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString());
}
