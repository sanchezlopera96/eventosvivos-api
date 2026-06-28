using Microsoft.Extensions.Options;

namespace EventReservations.Api.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token, DateTime ExpiresAt);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", (
            LoginRequest request,
            IOptions<AdminCredentialsOptions> adminOpts,
            TokenService tokens) =>
        {
            var admin = adminOpts.Value;

            // Si no hay credenciales configuradas, el login no está disponible
            // (secure by default: no se permite acceso si no se configuró).
            if (string.IsNullOrWhiteSpace(admin.Username) ||
                string.IsNullOrWhiteSpace(admin.PasswordHash))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Autenticacion no disponible",
                    detail: "Las credenciales de administracion no estan configuradas.");
            }

            // Validación de usuario + contraseña (BCrypt verifica el hash).
            var userOk = string.Equals(request.Username, admin.Username, StringComparison.Ordinal);
            var passOk = userOk && BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash);

            if (!userOk || !passOk)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Credenciales invalidas",
                    detail: "Usuario o contrasena incorrectos.");
            }

            var (token, expiresAt) = tokens.CreateToken(admin.Username);
            return Results.Ok(new LoginResponse(token, expiresAt));
        })
        .WithName("Login")
        .AllowAnonymous();

        return app;
    }
}
