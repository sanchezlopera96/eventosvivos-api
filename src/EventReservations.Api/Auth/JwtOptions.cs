namespace EventReservations.Api.Auth;

/// <summary>
/// Opciones de autenticación JWT para el área de administración.
/// Se enlazan desde la sección "Jwt" de la configuración (variables de
/// entorno en producción; appsettings.Development.json en local).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "eventosvivos";
    public string Audience { get; init; } = "eventosvivos-admin";

    /// <summary>Clave secreta para firmar el token (HMAC-SHA256). Solo por env.</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Minutos de validez del token.</summary>
    public int ExpirationMinutes { get; init; } = 120;
}

/// <summary>
/// Credenciales del administrador. La contraseña se almacena HASHEADA con
/// BCrypt (nunca en texto plano), y todo se inyecta por configuración/env,
/// jamás en el repositorio.
/// </summary>
public sealed class AdminCredentialsOptions
{
    public const string SectionName = "AdminCredentials";

    public string Username { get; init; } = string.Empty;

    /// <summary>Hash BCrypt de la contraseña del administrador.</summary>
    public string PasswordHash { get; init; } = string.Empty;
}
