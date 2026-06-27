using EventReservations.Application.Common;
using EventReservations.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventReservations.Api.Errors;

/// <summary>
/// Manejador global de excepciones. Traduce las excepciones conocidas a una
/// respuesta ProblemDetails con el código HTTP adecuado, sin filtrar detalles
/// internos (buena práctica OWASP: no exponer stack traces al cliente).
///   NotFoundException  -> 404
///   ConflictException  -> 409  (incluye conflictos de concurrencia por xmin)
///   DomainException    -> 422  (violación de regla de negocio)
///   resto              -> 500  (mensaje genérico)
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio no satisfecha"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Excepción no controlada");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Para 5xx no exponemos el mensaje real; para el resto, sí es informativo.
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Ha ocurrido un error inesperado."
                : exception.Message,
            Type = $"https://httpstatuses.io/{status}"
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
