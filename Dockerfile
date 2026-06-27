# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos todo y restauramos/publicamos solo el proyecto de la API.
COPY . .
RUN dotnet restore src/EventReservations.Api/EventReservations.Api.csproj
RUN dotnet publish src/EventReservations.Api/EventReservations.Api.csproj \
    -c Release -o /app/publish --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Ejecuta como usuario no-root (definido por las imágenes oficiales de .NET).
USER $APP_UID

# Kestrel escucha en HTTP dentro del contenedor; el TLS lo termina la plataforma.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EventReservations.Api.dll"]
