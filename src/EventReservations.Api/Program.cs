using EventReservations.Api.Endpoints;
using EventReservations.Api.Errors;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Reservations.CreateReservation;
using EventReservations.Infrastructure;
using EventReservations.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Servicios ---

// Capa de infraestructura (DbContext, repos, UnitOfWork, handlers, TimeProvider).
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

// CORS: orígenes permitidos por configuración; si no hay, en desarrollo se
// permite cualquiera (cómodo para el frontend local).
const string CorsPolicy = "spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }));

var app = builder.Build();

// --- Pipeline ---

app.UseExceptionHandler();

// Aplica migraciones (y seed de venues) al arrancar: práctico para demo y deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsPolicy);

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Health");
app.MapEventEndpoints();
app.MapReservationEndpoints();

app.Run();

// Necesario para los tests de integración con WebApplicationFactory.
public partial class Program;
