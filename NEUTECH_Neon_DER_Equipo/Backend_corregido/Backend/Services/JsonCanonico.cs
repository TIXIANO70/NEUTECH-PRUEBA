using System.Text.Json;
using System.Text.Json.Nodes;
using Backend.Domain;

namespace Backend.Services;

// JSON con las claves ordenadas: sirve para saber si dos registros con el mismo record_id tienen el MISMO contenido.
public static class JsonCanonico
{
    public static string De(EventoContrato registro) =>
        Ordenar(JsonSerializer.SerializeToNode(registro))!.ToJsonString();

    private static JsonNode? Ordenar(JsonNode? nodo)
    {
        switch (nodo)
        {
            case JsonObject obj:
                var ordenado = new JsonObject();
                foreach (var p in obj.OrderBy(p => p.Key, StringComparer.Ordinal)) ordenado.Add(p.Key, Ordenar(p.Value));
                return ordenado;
            case JsonArray arr:
                var lista = new JsonArray();
                foreach (var x in arr) lista.Add(Ordenar(x));
                return lista;
            default:
                return nodo?.DeepClone();
        }
    }
}
