using System.Threading.RateLimiting;
using EventReservations.Api.Endpoints;
using EventReservations.Api.Errors;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Reservations.CreateReservation;
using EventReservations.Infrastructure;
using EventReservations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Servicios ---

builder.Services.AddInfrastructure(builder.Configuration);

// Validadores de entrada (FluentValidation), usados por el ValidationFilter.
builder.Services.AddScoped<IValidator<CreateEventCommand>, CreateEventCommandValidator>();
builder.Services.AddScoped<IValidator<CreateReservationCommand>, CreateReservationCommandValidator>();

// Manejo global de errores -> ProblemDetails.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// OpenAPI / Swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate limiting: límite por IP para mitigar abuso/DoS en una API pública.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// CORS: en producción solo los orígenes configurados; en desarrollo, abierto.
const string CorsPolicy = "spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        // Producción sin orígenes configurados: política vacía (no se permite CORS).
    }));

var app = builder.Build();

// --- Pipeline ---

app.UseExceptionHandler();

// Cabeceras de seguridad básicas.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();

// Aplica migraciones (y seed de venues) al arrancar: práctico para demo y deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Swagger: solo en desarrollo o si se habilita explícitamente por configuración.
var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseCors(CorsPolicy);

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Health");
app.MapEventEndpoints();
app.MapReservationEndpoints();

app.Run();

// Necesario para los tests de integración con WebApplicationFactory.
public partial class Program;
