namespace EventReservations.Application.Abstractions;

/// <summary>
/// Maneja una consulta (operación de lectura) y devuelve un resultado.
/// Las consultas proyectan datos directamente, sin pasar por el dominio (ADR-002).
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
