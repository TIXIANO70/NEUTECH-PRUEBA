using Backend.Domain;

namespace Backend.Infrastructure;

// Convierte las ExcepcionApp (422, 409, 404) en respuestas JSON { "detail": ... }
public class ManejadorErrores
{
    private readonly RequestDelegate _next;
    public ManejadorErrores(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (ExcepcionApp ex)
        {
            ctx.Response.StatusCode = ex.Status;
            await ctx.Response.WriteAsJsonAsync(new { detail = ex.Detalle });
        }
    }
}
