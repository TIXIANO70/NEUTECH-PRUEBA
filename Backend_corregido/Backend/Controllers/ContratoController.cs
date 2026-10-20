using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Backend.Infrastructure;
using Backend.Services;

namespace Backend.Controllers;

// Rutas que pide el Contrato de Normalización para que el Grupo 6 consulte al Grupo 5
[ApiController]
[Route("api/v1")]
public class ContratoController : ControllerBase
{
    private readonly IAlertaServices _alertaService;

    public ContratoController(IAlertaServices alertaService)
    {
        _alertaService = alertaService;
    }

    // GET /health -> prueba de vida
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { ok = true, service = "navathon-grupo5", mode = "educational" });

    // GET /api/v1/records?limit=100&cursor=... -> historial paginado (eventos + alertas + confirmaciones)
    [HttpGet("records")]
    public IActionResult ObtenerRegistros([FromQuery] string? cursor, [FromQuery, Range(1, 200)] int limit = 100)
        => Ok(_alertaService.ObtenerRegistros(cursor, limit));

    // GET /api/v1/alerts -> registros de alerta del Grupo 5 (más nuevos primero)
    [HttpGet("alerts")]
    public IActionResult ObtenerAlertasGrupo6() => Ok(_alertaService.ObtenerRegistrosAlerta());

    // GET /api/v1/status -> estado del nodo (offline si el ESP32 no consultó hace más de 15 s)
    [HttpGet("status")]
    public IActionResult ObtenerEstado() => Ok(_alertaService.ObtenerEstado());

    // POST /api/v1/alerts/{alertId}/resolve -> cierra una alerta puntual
    [HttpPost("alerts/{alertId}/resolve")]
    [RequiereApiKey]
    public IActionResult ResolverAlerta(string alertId) => Ok(_alertaService.ResolverAlertaPorId(alertId));
}
