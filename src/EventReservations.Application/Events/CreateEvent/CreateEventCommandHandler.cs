using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.CreateEvent;

/// <summary>
/// Crea un evento. Orquesta lo que requiere contexto externo (existencia del
/// venue y RN02), delegando las invariantes de negocio al agregado Event.
/// </summary>
public sealed class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _events;
    private readonly IVenueRepository _venues;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateEventCommandHandler(
        IEventRepository events,
        IVenueRepository venues,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _events = events;
        _venues = venues;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken = default)
    {
        var venue = await _venues.GetByIdAsync(command.VenueId, cancellationToken)
            ?? throw NotFoundException.For("venue", command.VenueId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // El dominio valida RF-01, RN01 y RN03; los VOs validan sus invariantes.
        var @event = Event.Create(
            command.Title,
            command.Description,
            venue,
            Capacity.Of(command.Capacity),
            Schedule.Create(command.StartsAt, command.EndsAt),
            Money.Of(command.Price),
            command.Type,
            now);

        // RN02: no puede solaparse con otro evento activo del mismo venue.
        var hasOverlap = await _events.ExistsActiveOverlapAsync(
            @event.VenueId, @event.Schedule.StartsAt, @event.Schedule.EndsAt, cancellationToken);

        if (hasOverlap)
            throw new DomainException(
                "El venue ya tiene un evento activo con un horario superpuesto.");

        await _events.AddAsync(@event, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
