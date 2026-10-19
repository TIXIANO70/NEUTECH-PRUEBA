using Npgsql;

namespace Backend.Infrastructure;

public static class NeonConnection
{
    public static string Normalizar(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value; // También acepta la cadena estándar de Npgsql (Host=...;Username=...)

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2) throw new InvalidOperationException("La URL de Neon debe contener usuario y contraseña.");
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            SslMode = SslMode.Require,
            Pooling = true
        };
        // El servidor de Neon exige TLS. El parámetro channel_binding de la URI
        // no forma parte de la sintaxis de connection string de Npgsql.
        return b.ConnectionString;
    }
}
