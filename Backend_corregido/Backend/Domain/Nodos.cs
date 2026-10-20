namespace Backend.Domain;

// Registro inicial de nodos del contrato (grupo -> node_id y nombre del subsistema)
public static class Nodos
{
    public const string AlertNodeId = "alert_node_01";

    public static readonly IReadOnlyDictionary<int, (string NodeId, string Subsistema)> Registro =
        new Dictionary<int, (string NodeId, string Subsistema)>
        {
            [1] = ("fire_node_01", "Monitoreo de incendios forestales"),
            [2] = ("volcanic_node_01", "Nodo volcánico y calidad ambiental"),
            [3] = ("energy_node_01", "Sistema energético autónomo"),
            [4] = ("water_node_01", "Administración inteligente de agua"),
            [5] = (AlertNodeId, "Comunicaciones y alertas de emergencia"),
        };

    public static string Ahora() => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
}
