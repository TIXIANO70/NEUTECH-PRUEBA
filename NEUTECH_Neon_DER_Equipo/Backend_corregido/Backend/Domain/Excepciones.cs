namespace Backend.Domain;

// Errores "esperables" de la API. El middleware los convierte en HTTP con { "detail": ... }
public abstract class ExcepcionApp : Exception
{
    public int Status { get; }
    public object Detalle { get; }
    protected ExcepcionApp(int status, object detalle) : base(detalle.ToString()) { Status = status; Detalle = detalle; }
}
public sealed class ExcepcionPeticionInvalida : ExcepcionApp { public ExcepcionPeticionInvalida(object detalle) : base(422, detalle) { } }
public sealed class ExcepcionConflicto : ExcepcionApp { public ExcepcionConflicto(string detalle) : base(409, detalle) { } }
public sealed class ExcepcionNoEncontrado : ExcepcionApp { public ExcepcionNoEncontrado(string detalle) : base(404, detalle) { } }
