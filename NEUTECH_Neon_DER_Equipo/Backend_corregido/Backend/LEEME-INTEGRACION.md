# NEUTECH integrado (Grupo 5)

## Ejecutar

En la carpeta `Backend_corregido/Backend` ejecutá:

```powershell
dotnet run
```

Abrí `http://localhost:5000` (panel) o `http://localhost:5000/historial.html` (historial). Swagger: `http://localhost:5000/swagger`. Requiere .NET 10 SDK.

El backend sirve la copia integrada de los archivos de `wwwroot/`, y el frontend original modificado está también en `NEUTECH-PRUEBA-main/Frontend/`. Si editás el frontend original, copiá los cambios a `wwwroot/` para verlos desde la API.

## Contrato
- GET `/api/v1/alerts` y `/api/eventos`: muestran alertas reales y origen.
- GET `/api/v1/records?limit=200`: confirmaciones recientes.
- GET `/api/v1/status`: conexión ESP32 (consultas recientes, no sensores estáticos).
- POST `/api/v1/alerts/{id}/resolve`: cerrar alerta desde la interfaz.
- POST `/api/v1/events`: ingresar evento del grupo emisor.

Las alertas se actualizan cada 5 segundos. No se vuelcan datos falsos si falla la conexión. El botón del historial solo actualiza, porque no existe endpoint para borrar. La API guarda sus datos **en memoria** y los pierde al reiniciar. `/api/v1/alerts` devuelve los últimos 100 registros de alerta, `/api/v1/records` pagina y la interfaz usa su primera página.

**Solo para entorno educativo/local:** se dejó `Seguridad:ApiKey` vacía en appsettings para que la web pueda resolver sin insertar un secreto en JS. ¡No publicar así! Para producción agregar autenticación de usuarios y autorización desde el servidor, y restringir CORS.

Los seis nodos estáticos del panel son ilustrativos, no sensores reportados por el backend; el estado del ESP32 sí viene de la API.
