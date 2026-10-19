namespace Backend.Domain;

// Representa la instruccion fisica que leera el ESP32 por Wi-Fi
public record OrdenESP32
{
    public string AlertId { get; set; } = "ALT-000";
    public string AlertLevel { get; set; } = "normal";
    public string AlertStatus { get; set; } = "resolved"; // pending, active, resolved
    public string Message { get; set; } = "SISTEMA NORMAL";
    public string LcdText { get; set; } = "SISTEMA OK";
    public bool RedLed { get; set; } = false;
    public bool YellowLed { get; set; } = false;
    public bool GreenLed { get; set; } = true;
    public bool Buzzer { get; set; } = false;
}