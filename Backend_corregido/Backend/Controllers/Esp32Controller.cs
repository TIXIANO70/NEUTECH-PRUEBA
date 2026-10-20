using Microsoft.AspNetCore.Mvc;
using Backend.Domain;
using Backend.Infrastructure;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")] // Ruta base: /api/esp32
public class Esp32Controller : ControllerBase
{
    private readonly IAlertaServices _alertaService;

    public Esp32Controller(IAlertaServices alertaService)
    {
        _alertaService = alertaService;
    }

    // GET /api/esp32/orden -> Consulta periódica del ESP32 por Wi-Fi
    [HttpGet("orden")]
    public IActionResult ObtenerOrden()
    {
        _alertaService.RegistrarConsultaEsp32();   // así /api/v1/status sabe que el ESP32 está vivo
        var orden = _alertaService.ObtenerOrdenParaESP32();
        return Ok(orden);
    }

    // POST /api/esp32/confirmacion -> El ESP32 reporta que ejecutó la alarma física
    [HttpPost("confirmacion")]
    public IActionResult ConfirmarEjecucion([FromBody] ConfirmacionESP32 confirmacion)
    {
        _alertaService.RegistrarConfirmacion(confirmacion);
        return Ok(new { status = "confirmacion_registrada", timestamp = DateTime.UtcNow });
    }

    // PUT /api/esp32/resolver -> Apaga la alarma activa desde la web
    [HttpPut("resolver")]
    [RequiereApiKey]
    public IActionResult ResolverAlerta()
    {
        _alertaService.ResolverAlerta();

        return Ok(new { mensaje = "Alerta resuelta. El sistema vuelve a estado NORMAL." });
    }
}
