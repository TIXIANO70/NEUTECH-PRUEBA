using Backend.Infrastructure;
using Backend.Repositories;
using Backend.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar Controladores
builder.Services.AddControllers(o =>
    {
        // Evita que la validación automática de MVC recorra por dentro los JsonElement de "data"
        o.ModelMetadataDetailsProviders.Add(
            new Microsoft.AspNetCore.Mvc.ModelBinding.SuppressChildValidationMetadataProvider(typeof(System.Text.Json.JsonElement)));
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        // JSON mal formado, campos que faltan o que sobran => 422 con { "detail": [ ... ] } (igual que el Contrato)
        o.InvalidModelStateResponseFactory = ctx => new Microsoft.AspNetCore.Mvc.UnprocessableEntityObjectResult(new
        {
            detail = ctx.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .SelectMany(e => e.Value!.Errors.Select(x =>
                {
                    var msg = string.IsNullOrEmpty(x.ErrorMessage) ? x.Exception?.Message ?? "valor inválido" : x.ErrorMessage;
                    return string.IsNullOrEmpty(e.Key) ? msg : $"{e.Key}: {msg}";
                }))
                .ToList()
        });
    });

// 2. Registrar Inyección de Dependencias 
// Neon por variable de entorno, sin credenciales en código ni en Git.
var neonUrl = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(neonUrl))
    throw new InvalidOperationException("Falta ConnectionStrings__DefaultConnection. Consultá LEEME-NEON.md.");
builder.Services.AddSingleton(NpgsqlDataSource.Create(NeonConnection.Normalizar(neonUrl)));
builder.Services.AddSingleton<IAlertaRepository, NeonAlertaRepository>();
builder.Services.AddScoped<IAlertaServices, AlertaServices>();

// 3. Registrar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Configurar Permisos CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Convierte los errores esperables (422, 409, 404) en respuestas JSON { "detail": ... }
app.UseMiddleware<ManejadorErrores>();

// 5. Habilitar la pantalla interactiva de Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Grupo 5 v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();