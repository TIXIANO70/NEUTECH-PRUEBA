using System.Text.Json;
using Backend.Domain;
using Npgsql;
using NpgsqlTypes;

namespace Backend.Repositories;

// Persistencia en las tablas públicas ya creadas por el equipo.
// No crea ni altera las tablas node, "Records", alerts o confirmation.
public sealed class NeonAlertaRepository : IAlertaRepository
{
    private readonly NpgsqlDataSource _source;
    public NeonAlertaRepository(NpgsqlDataSource source)
    {
        _source = source;
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(@"
CREATE SCHEMA IF NOT EXISTS neutech;
CREATE TABLE IF NOT EXISTS neutech.estado_esp32 (
 id INTEGER PRIMARY KEY CHECK (id=1), orden JSONB NOT NULL,
 ultima_consulta TIMESTAMP NULL);", db);
        cmd.ExecuteNonQuery();
    }

    private NpgsqlConnection Abrir() => _source.OpenConnection();

    // Un evento ya está en public."Record" al llamar GuardarEvento.
    public void GuardarEvento(EventoContrato evento) { }

    public IEnumerable<EventoContrato> ObtenerEventos()
    {
        using var db = Abrir();
       using var cmd = new NpgsqlCommand(Selectrecord + " WHERE r.record_type='event' ORDER BY r.timestamp DESC", db);
        using var rd = cmd.ExecuteReader();
        var eventos = new List<EventoContrato>();
        while (rd.Read()) eventos.Add(LeerEvento(rd));
        return eventos;
    }

    

private const string Selectrecord = @"
SELECT r.record_id,r.schema_version,r.record_type,r.timestamp,r.status,r.alert_level,
       r.data::text,r.metadata::text,r.node_id,n.""group"",n.subsystem
FROM public.""record"" r
JOIN public.node n ON n.node_id=r.node_id";

    private static EventoContrato LeerEvento(NpgsqlDataReader rd) => new()
    {
        RecordId = rd.GetString(0), SchemaVersion = rd.GetString(1), RecordType = rd.GetString(2),
        Timestamp = rd.GetString(3), Status = rd.GetString(4), AlertLevel = rd.GetString(5),
        Data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rd.GetString(6))!,
        Metadata = JsonSerializer.Deserialize<MetadataContrato>(rd.GetString(7))!,
        NodeId = rd.GetString(8), Group = rd.GetInt32(9), Subsystem = rd.GetString(10)
    };

    public string? ObtenerPayloadRegistro(string recordId)
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(Selectrecord + " WHERE r.record_id=@id", db);
        cmd.Parameters.AddWithValue("id", recordId);
        using var rd = cmd.ExecuteReader();
        return rd.Read() ? Backend.Services.JsonCanonico.De(LeerEvento(rd)) : null;
    }

    public void GuardarRegistro(string recordId, string recordType, string nodeId, string payload)
    {
        var reg = JsonSerializer.Deserialize<EventoContrato>(payload)!;
        using var db = Abrir();
        using var tx = db.BeginTransaction();
        // La FK de Records obliga a registrar primero el nodo.
        using (var nodo = new NpgsqlCommand(@"
INSERT INTO public.node(node_id,""group"",subsystem)
VALUES (@id,@grp,@sub)
ON CONFLICT (node_id) DO NOTHING", db, tx))
        {
            nodo.Parameters.AddWithValue("id", reg.NodeId);
            nodo.Parameters.AddWithValue("grp", reg.Group);
            nodo.Parameters.AddWithValue("sub", reg.Subsystem);
            nodo.ExecuteNonQuery();
        }
        using (var cmd = new NpgsqlCommand(@"
INSERT INTO public.""record""
(record_id,schema_version,record_type,timestamp,status,alert_level,data,metadata,node_id)
VALUES (@id,@version,@type,@time,@status,@level,@data,@metadata,@node)
ON CONFLICT (record_id) DO NOTHING", db, tx))
        {
            cmd.Parameters.AddWithValue("id", reg.RecordId);
            cmd.Parameters.AddWithValue("version", reg.SchemaVersion);
            cmd.Parameters.AddWithValue("type", reg.RecordType);
            cmd.Parameters.AddWithValue("time", reg.Timestamp);
            cmd.Parameters.AddWithValue("status", reg.Status);
            cmd.Parameters.AddWithValue("level", reg.AlertLevel);
            cmd.Parameters.Add("data", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(reg.Data);
            cmd.Parameters.Add("metadata", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(reg.Metadata);
            cmd.Parameters.AddWithValue("node", reg.NodeId);
            cmd.ExecuteNonQuery();
        }
        
        if (reg.RecordType == "confirmation")
        {
            var d = reg.Data;

            using var lockCmd = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(452003)", db, tx);
            lockCmd.ExecuteNonQuery();

            using var cmd = new NpgsqlCommand(@"
        INSERT INTO public.confirmation
        (confirmation_id,confirmation_type,resul,timestamptz,alert_id)
        SELECT COALESCE(MAX(confirmation_id),0)+1,
            @type,@result,@time,@alert
        FROM public.confirmation", db, tx);

            cmd.Parameters.AddWithValue(
                "type", d["confirmation_type"].GetString()!);
            cmd.Parameters.AddWithValue(
                "result", d["result"].GetString()!);
            cmd.Parameters.AddWithValue("time", reg.Timestamp);
            cmd.Parameters.AddWithValue(
                "alert", d["alert_id"].GetString()!);

            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // La BD no tiene secuencia de registros. La paginación usa orden determinista
    // por fecha textual ISO-8601 y record_id; el cursor es un desplazamiento.
    public IReadOnlyList<RegistroAlmacenado> ObtenerRegistrosDespuesDe(long seq, int limite)
        => ConsultarRegistros(null, seq, limite, false);
    public IReadOnlyList<RegistroAlmacenado> ObtenerUltimosPorTipo(string recordType, int limite)
        => ConsultarRegistros(recordType, 0, limite, true);

    
    private IReadOnlyList<RegistroAlmacenado> ConsultarRegistros(
        string? tipo, long offset, int limite, bool descendente)
    {
        using var db = Abrir();

        var orden = descendente ? "DESC" : "ASC";

        using var cmd = new NpgsqlCommand($@"
    SELECT
        t.seq,
        t.record_id,
        t.schema_version,
        t.record_type,
        t.timestamp,
        t.status,
        t.alert_level,
        t.data,
        t.metadata,
        t.node_id,
        t.""group"",
        t.subsystem
    FROM (
        SELECT
            row_number() OVER(
                ORDER BY r.timestamp ASC, r.record_id ASC
            ) AS seq,
            r.record_id,
            r.schema_version,
            r.record_type,
            r.timestamp,
            r.status,
            r.alert_level,
            r.data::text AS data,
            r.metadata::text AS metadata,
            r.node_id,
            n.""group"",
            n.subsystem
        FROM public.""record"" r
        JOIN public.node n ON n.node_id = r.node_id
    ) t
    WHERE t.seq > @offset
    AND (@type = '' OR t.record_type = @type)
    ORDER BY t.seq {orden}
    LIMIT @limit", db);

        cmd.Parameters.AddWithValue("offset", offset);
        cmd.Parameters.AddWithValue("type", tipo ?? "");
        cmd.Parameters.AddWithValue("limit", limite);

        using var rd = cmd.ExecuteReader();
        var lista = new List<RegistroAlmacenado>();

        while (rd.Read())
        {
            var evento = LeerEventoDesdePosicion(rd, 1);

            lista.Add(new RegistroAlmacenado(
                rd.GetInt64(0),
                evento.RecordId,
                evento.RecordType,
                evento.NodeId,
                Backend.Services.JsonCanonico.De(evento)
            ));
        }

        return lista;
    }

    private static EventoContrato LeerEventoDesdePosicion(NpgsqlDataReader rd, int i) => new()
    {
        RecordId=rd.GetString(i), SchemaVersion=rd.GetString(i+1), RecordType=rd.GetString(i+2),
        Timestamp=rd.GetString(i+3), Status=rd.GetString(i+4), AlertLevel=rd.GetString(i+5),
        Data=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(rd.GetString(i+6))!,
        Metadata=JsonSerializer.Deserialize<MetadataContrato>(rd.GetString(i+7))!,
        NodeId=rd.GetString(i+8), Group=rd.GetInt32(i+9), Subsystem=rd.GetString(i+10)
    };

    public void GuardarAlerta(AlertaRegistrada a)
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(@"
INSERT INTO public.alerts(alert_id,alert_status,message,channels,record_id)
VALUES (@id,@status,@message,@channels,@source)
ON CONFLICT (alert_id) DO UPDATE SET alert_status=EXCLUDED.alert_status", db);
        cmd.Parameters.AddWithValue("id", a.AlertId);
        cmd.Parameters.AddWithValue("status", a.Status);
        // message es VARCHAR(50) en la base proporcionada.
        cmd.Parameters.AddWithValue("message", a.Message.Length > 50 ? a.Message[..50] : a.Message);
        cmd.Parameters.AddWithValue("channels", "web,led,buzzer,lcd");
        cmd.Parameters.AddWithValue("source", a.SourceRecordId);
        cmd.ExecuteNonQuery();
    }

    
private const string SelectAlerts = @"
SELECT a.alert_id,a.record_id,r.alert_level,a.message,
       COALESCE(r.metadata->>'environment','test'),
       COALESCE(r.metadata->>'source_type','simulated'),a.alert_status
FROM public.alerts a
JOIN public.""record"" r ON r.record_id=a.record_id";

    public AlertaRegistrada? ObtenerAlerta(string alertId) => Buscar("a.alert_id", alertId);
    public AlertaRegistrada? ObtenerAlertaPorOrigen(string sourceRecordId) => Buscar("a.record_id", sourceRecordId);
    private AlertaRegistrada? Buscar(string columna, string valor)
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(SelectAlerts + $" WHERE {columna}=@v", db);
        cmd.Parameters.AddWithValue("v", valor);
        using var rd = cmd.ExecuteReader();
        return rd.Read() ? LeerAlerta(rd) : null;
    }
    private static AlertaRegistrada LeerAlerta(NpgsqlDataReader rd) => new()
    {
        AlertId=rd.GetString(0), SourceRecordId=rd.GetString(1), Level=rd.GetString(2),
        Message=rd.GetString(3), Environment=rd.GetString(4), SourceType=rd.GetString(5), Status=rd.GetString(6)
    };
    public IReadOnlyList<AlertaRegistrada> ObtenerAlertasActivas()
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(SelectAlerts+" WHERE a.alert_status='active'", db);
        using var rd = cmd.ExecuteReader();
        var items = new List<AlertaRegistrada>();
        while (rd.Read()) items.Add(LeerAlerta(rd));
        return items;
    }

    public OrdenESP32 ObtenerOrdenActiva()
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand("SELECT orden::text FROM neutech.estado_esp32 WHERE id=1", db);
        var json = cmd.ExecuteScalar() as string;
        return json is null ? new OrdenESP32 { AlertId="ALT-INIT",Message="Sin emergencias activas" }
            : JsonSerializer.Deserialize<OrdenESP32>(json)!;
    }
    public void ActualizarOrden(OrdenESP32 orden)
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(@"
INSERT INTO neutech.estado_esp32(id,orden) VALUES (1,@orden)
ON CONFLICT(id) DO UPDATE SET orden=EXCLUDED.orden", db);
        cmd.Parameters.Add("orden", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(orden);
        cmd.ExecuteNonQuery();
    }
    public DateTime? ObtenerUltimaConsultaEsp32()
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand("SELECT ultima_consulta FROM neutech.estado_esp32 WHERE id=1", db);
        var value = cmd.ExecuteScalar();
        return value is null or DBNull ? null : ((DateTime)value).ToUniversalTime();
    }
    public void MarcarConsultaEsp32()
    {
        using var db = Abrir();
        using var cmd = new NpgsqlCommand(@"
INSERT INTO neutech.estado_esp32(id,orden,ultima_consulta) VALUES (1,@orden,NOW())
ON CONFLICT(id) DO UPDATE SET ultima_consulta=NOW()", db);
        cmd.Parameters.Add("orden", NpgsqlDbType.Jsonb).Value=JsonSerializer.Serialize(new OrdenESP32 { AlertId="ALT-INIT" });
        cmd.ExecuteNonQuery();
    }
}
