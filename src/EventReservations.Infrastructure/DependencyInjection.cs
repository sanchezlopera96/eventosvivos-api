using EventReservations.Application.Abstractions;
using EventReservations.Application.Events.CancelEvent;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Events.ListEvents;
using EventReservations.Application.Events.OccupancyReport;
using EventReservations.Application.Events.GetEventById;
using EventReservations.Application.Reservations.CancelReservation;
using EventReservations.Application.Reservations.ConfirmPayment;
using EventReservations.Application.Reservations.CreateReservation;
using EventReservations.Application.Reservations.GetReservationById;
using EventReservations.Application.Reservations.ListReservations;
using EventReservations.Domain.Reservations;
using EventReservations.Infrastructure.Persistence;
using EventReservations.Infrastructure.Persistence.Queries;
using EventReservations.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventReservations.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Default'.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                // Resiliencia: reintenta ante fallos transitorios de conexión
                // (p. ej. la BD aún no está lista al desplegar en la nube).
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        // Reloj inyectable (lo usan los handlers de comandos).
        services.AddSingleton(TimeProvider.System);

        // Persistencia
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Handlers de comandos (escritura)
        services.AddScoped<ICommandHandler<CreateEventCommand, Guid>, CreateEventCommandHandler>();
        services.AddScoped<ICommandHandler<CancelEventCommand, int>, CancelEventCommandHandler>();
        services.AddScoped<ICommandHandler<CreateReservationCommand, Guid>, CreateReservationCommandHandler>();
        services.AddScoped<ICommandHandler<ConfirmPaymentCommand, string>, ConfirmPaymentCommandHandler>();
        services.AddScoped<ICommandHandler<CancelReservationCommand, CancellationOutcome>, CancelReservationCommandHandler>();

        // Handlers de consultas (lectura)
        services.AddScoped<IQueryHandler<ListEventsQuery, IReadOnlyList<EventListItemDto>>, ListEventsQueryHandler>();
        services.AddScoped<IQueryHandler<GetEventByIdQuery, EventDetailDto>, GetEventByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetReservationByIdQuery, ReservationDetailDto>, GetReservationByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListReservationsQuery, IReadOnlyList<ReservationListItemDto>>, ListReservationsQueryHandler>();
        services.AddScoped<IQueryHandler<OccupancyReportQuery, OccupancyReportDto>, OccupancyReportQueryHandler>();

        return services;
    }
}
