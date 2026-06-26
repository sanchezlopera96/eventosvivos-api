namespace EventReservations.Application.Common;

/// <summary>
/// Se lanza cuando un recurso solicitado no existe. La API la traduce a HTTP 404.
/// Distinta de DomainException (violación de regla de negocio -> 409/422).
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string resource, object id)
        => new($"No se encontró {resource} con identificador '{id}'.");
}
