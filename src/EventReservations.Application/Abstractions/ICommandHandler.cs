namespace EventReservations.Application.Abstractions;

/// <summary>
/// Maneja un comando (operación de escritura) y devuelve un resultado.
/// CQRS con handlers explícitos, sin bus de mediación (ADR-002).
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
