namespace Backend.Domain;

// Alerta que generó el Grupo 5 a partir de un evento (guarda a qué evento corresponde).
public class AlertaRegistrada
{
    public required string AlertId { get; init; }
    public required string SourceRecordId { get; init; }   // record_id del evento que la originó
    public required string Level { get; init; }
    public required string Message { get; init; }
    public required string Environment { get; init; }
    public required string SourceType { get; init; }
    public required string Status { get; set; }            // active | resolved
}

// Registro normalizado ya guardado (seq = cursor de paginación de /api/v1/records)
public record RegistroAlmacenado(long Seq, string RecordId, string RecordType, string NodeId, string Payload);
