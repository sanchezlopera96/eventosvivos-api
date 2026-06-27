using System.Security.Cryptography;
using System.Text;

namespace EventReservations.Api.Security;

/// <summary>
/// Protege los endpoints de administración (gestión de eventos y confirmación de
/// pago) exigiendo una API key en la cabecera 'X-Api-Key'. La comparación es de
/// tiempo constante para no filtrar información por temporización.
///
/// La clave se lee de configuración (Security:AdminApiKey), nunca del repo.
/// Si no hay clave configurada, la superficie administrativa queda deshabilitada
/// (responde 503) en lugar de quedar abierta: "seguro por defecto".
/// </summary>
public sealed class AdminApiKeyFilter : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuredKey = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Security:AdminApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Administración no disponible",
                detail: "La API administrativa no está configurada.");

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(provided) || !FixedTimeEquals(provided, configuredKey))
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No autorizado",
                detail: "API key ausente o inválida.");

        return await next(context);
    }

    private static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
