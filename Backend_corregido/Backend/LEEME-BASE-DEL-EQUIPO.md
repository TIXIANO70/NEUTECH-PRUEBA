# NEUTECH + base Neon existente (DER del equipo)

Esta versión consulta y escribe sobre las **tablas que ya existen** en PostgreSQL:

- `public.node` (`node_id`, `group`, `subsystem`)
- `public."Records"` (`record_id`, `schema_version`, `record_type`, `timestamp`, `status`, `alert_level`, `data`, `metadata`, `node_id`)
- `public.alerts` (`alert_id`, `alert_status`, `message`, `channels`, `related_record_ids`)
- `public.confirmation` (`confirmation_id`, `confirmation_type`, `result`, `timestamp`, `alert_id`)

No se borran ni se recrean esas cuatro tablas. Se crea solamente `neutech.estado_esp32` para conservar orden y última consulta física del dispositivo; el esquema `neutech` del ZIP anterior se puede conservar, pero **ya no se utiliza** para eventos ni alertas.

## Ejecutar

1. Rotá la contraseña que compartiste y copiá **la nueva URL** desde Neon.
2. En PowerShell, dentro de `Backend_corregido/Backend`:

   `$env:ConnectionStrings__DefaultConnection = 'postgresql://USUARIO:CLAVE@SERVIDOR/neondb?sslmode=require'`

   `dotnet restore`

   `dotnet run`

3. Abrí la dirección local que muestre ASP.NET Core, idealmente `http://localhost:5000`.

No hace falta agregar la contraseña a `appsettings.json` ni al JavaScript del frontend.

## Consideraciones importantes

- `related_record_ids` es `VARCHAR(255)` y FK a **un único** `public."Records".record_id` (no un array SQL). En los JSON de la API se sigue representando como array por compatibilidad con el contrato.
- `message` es `VARCHAR(50)` y en la tabla `alerts` se conserva hasta 50 caracteres; el registro JSON de alerta conserva el mensaje original.
- `channels` se guarda como la cadena `web,led,buzzer,lcd` en `VARCHAR(255)`.
- `timestamp` está guardado como `VARCHAR(50)`, no `TIMESTAMPTZ`. La API mantiene el formato ISO-8601 y ordena textualmente, lo cual asume tiempos normalizados con zona compatible.
- `confirmation_id` es `INTEGER` y las capturas no indican `IDENTITY` o `SERIAL`: esta versión obtiene el siguiente número con un bloqueo asesor transaccional (otras aplicaciones que escriban confirmaciones deben coordinar el mismo bloqueo o cambiar la columna a `IDENTITY`).
- La API de registros usa un cursor numérico basado en la posición ordenada, porque no hay `seq` en el DER; si entran registros históricos durante la paginación, podrían desplazarse resultados. Para un sistema de producción conviene agregar un ID secuencial de ingestión o cursor por `(timestamp,record_id)`.
- La configuración de seguridad para desarrollo local no debe publicarse así: configurar API keys y limitar CORS antes de desplegar.
- Asegurate de tener permisos de `SELECT`, `INSERT` y `UPDATE` sobre las tablas existentes, y `CREATE` de esquema/tablas para el estado del ESP32.

## Verificar desde Neon

```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema='public'
  AND table_name IN ('node','Records','alerts','confirmation');

SELECT COUNT(*) FROM public."Records";
SELECT COUNT(*) FROM public.alerts;
```

## Limitación de validación

No se ejecutó una conexión real a Neon ni una compilación `dotnet build` en este entorno. La integración debe probarse localmente con `dotnet restore`, `dotnet build` y el envío de un evento que cumpla el contrato.
