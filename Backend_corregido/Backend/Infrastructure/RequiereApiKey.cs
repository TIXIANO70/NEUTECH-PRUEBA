using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Infrastructure;

// Uso: [RequiereApiKey] sobre un endpoint. Pide el header  X-API-Key.
// Si en appsettings.json "Seguridad:ApiKey" está vacío, la protección queda DESACTIVADA
// (la autenticación todavía está pendiente de coordinar con los otros grupos).
public class RequiereApiKeyAttribute : TypeFilterAttribute
{
    public RequiereApiKeyAttribute() : base(typeof(ApiKeyFilter)) { }
}

public class ApiKeyFilter : IActionFilter
{
    private readonly string? _esperada;
    public ApiKeyFilter(IConfiguration config) => _esperada = config["Seguridad:ApiKey"];

    public void OnActionExecuting(ActionExecutingContext ctx)
    {
        if (string.IsNullOrEmpty(_esperada)) return;
        var enviada = ctx.HttpContext.Request.Headers["X-API-Key"].ToString();
        if (enviada != _esperada)
            ctx.Result = new ObjectResult(new { detail = "Falta X-API-Key o es incorrecta" }) { StatusCode = 401 };
    }
    public void OnActionExecuted(ActionExecutedContext ctx) { }
}
