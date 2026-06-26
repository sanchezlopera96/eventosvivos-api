namespace EventReservations.Domain.Common;

/// <summary>
/// Se lanza cuando se viola una invariante o regla de negocio del dominio.
/// La capa de API la traduce a una respuesta HTTP apropiada (409/422) mediante
/// el manejador global de excepciones.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
