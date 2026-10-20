namespace Backend.Domain;

// Mensaje que envia el ESP32 para confirmar que ya activó el hardware
public class ConfirmacionESP32
{
    public string AlertId { get; set; } = "";
    public string NodeId { get; set; } = "alert_node_01";
    public string ConfirmationType { get; set; } = "device_execution";
    public string Result { get; set; } = "success";
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}