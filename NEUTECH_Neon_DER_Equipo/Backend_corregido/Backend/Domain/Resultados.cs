using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Backend.Domain;

public record ResultadoEvento(
    [property: JsonPropertyName("accepted")] bool Accepted,
    [property: JsonPropertyName("duplicate")] bool Duplicate,
    [property: JsonPropertyName("alert_created")] bool AlertCreated,
    [property: JsonPropertyName("alert_id")] string? AlertId);

public record ResultadoResolucion(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("already_resolved")] bool AlreadyResolved);

// Formato de colección del contrato: { items, next_cursor, has_more }
public record ColeccionRegistros(
    [property: JsonPropertyName("items")] IReadOnlyList<JsonNode?> Items,
    [property: JsonPropertyName("next_cursor")] string? NextCursor)
{
    [JsonPropertyName("has_more")] public bool HasMore => NextCursor is not null;
}
