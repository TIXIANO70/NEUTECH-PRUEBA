using Microsoft.AspNetCore.Mvc;
using Backend.Domain;
using Backend.Infrastructure;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    // Campo privado con guion bajo al inicio
    private readonly IAlertaServices _alertaService;

    // El constructor recibe 'alertaService' y se lo asigna a '_alertaService'
    public EventosController(IAlertaServices alertaService)
    {
        _alertaService = alertaService;
    }

    // POST /api/eventos   (y también /api/v1/events, la ruta del Contrato)
    //   201 = evento nuevo | 200 = duplicado idéntico | 409 = mismo record_id con otro contenido
    //   422 = no cumple el contrato | 401 = falta X-API-Key (solo si está activada en appsettings)
    [HttpPost]
    [HttpPost("/api/v1/events")]
    [RequiereApiKey]
    public IActionResult RecibirEvento([FromBody] EventoContrato evento)
    {
        var resultado = _alertaService.ProcesarEvento(evento);
        return resultado.Duplicate
            ? Ok(resultado)
            : Created($"/api/eventos/{evento.RecordId}", resultado);
    }

    // GET /api/eventos
    [HttpGet]
    public IActionResult ObtenerHistorialEventos()
    {
        var eventos = _alertaService.ObtenerEventos();
        return Ok(new { items = eventos, total = eventos.Count() });
    }
}
