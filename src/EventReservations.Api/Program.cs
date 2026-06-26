var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Endpoint de salud para verificar que la API arranca. Se ampliará en el
// incremento de API REST (endpoints, Swagger, manejo global de errores).
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
