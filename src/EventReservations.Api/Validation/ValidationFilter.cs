using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace EventReservations.Api.Validation;

/// <summary>
/// Filtro de endpoint que valida el primer argumento del tipo TRequest con su
/// IValidator (si está registrado). Si falla, corta con 400 y los errores por
/// campo, antes de invocar el handler. Separa validación de entrada de las
/// reglas de negocio (que viven en el dominio).
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        if (validator is not null)
        {
            var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
            if (request is not null)
            {
                var result = await validator.ValidateAsync(request);
                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
