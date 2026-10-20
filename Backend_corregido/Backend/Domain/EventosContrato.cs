using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Domain;

// Registro normalizado v1.0.0 del Contrato.
// Sirve para los eventos que llegan (Grupos 1-4 y 6) y para los registros que genera el Grupo 5 (alertas, confirmaciones).
// "required" = si el JSON no trae el campo, se rechaza (antes quedaba con un valor por defecto y nadie se enteraba).
// Campos que no existen en el contrato => se rechazan, igual que en la API de referencia.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class EventoContrato
{
    [JsonPropertyName("schema_version")] public required string SchemaVersion { get; set; }
    [JsonPropertyName("record_id")]      public required string RecordId { get; set; }
    [JsonPropertyName("record_type")]    public required string RecordType { get; set; } // reading, state, event, alert, confirmation
    [JsonPropertyName("node_id")]        public required string NodeId { get; set; }
    [JsonPropertyName("group")]          public required int Group { get; set; }
    [JsonPropertyName("subsystem")]      public required string Subsystem { get; set; }
    [JsonPropertyName("timestamp")]      public required string Timestamp { get; set; }   // ISO 8601 con zona horaria
    [JsonPropertyName("status")]         public required string Status { get; set; }      // active, offline, error
    [JsonPropertyName("alert_level")]    public required string AlertLevel { get; set; }  // normal, low, medium, high, critical
    [JsonPropertyName("data")]           public required Dictionary<string, JsonElement> Data { get; set; }
    [JsonPropertyName("metadata")]       public required MetadataContrato Metadata { get; set; }
}

// Metadata acepta campos extra (el contrato lo permite).
public class MetadataContrato
{
    [JsonPropertyName("environment")]     public required string Environment { get; set; }   // test | operational
    [JsonPropertyName("source_type")]     public required string SourceType { get; set; }    // real | simulated | mixed
    [JsonPropertyName("alert_evaluated")] public required bool AlertEvaluated { get; set; }
    [JsonPropertyName("units")]           public Dictionary<string, string> Units { get; set; } = new();
    [JsonPropertyName("quality")]         public Dictionary<string, string> Quality { get; set; } = new();
    [JsonPropertyName("sources")]         public Dictionary<string, string>? Sources { get; set; }
    [JsonExtensionData]                   public Dictionary<string, JsonElement>? Extra { get; set; }
}
