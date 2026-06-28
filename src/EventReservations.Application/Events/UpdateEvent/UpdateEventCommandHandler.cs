using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.UpdateEvent;

/// <summary>
/// Edita un evento activo. Orquesta lo que requiere contexto externo (existencia
/// del venue y RN02 excluyendo el propio evento), delegando las invariantes de
/// negocio al agregado Event (RN01, RN03, fecha futura, capacidad >= ocupadas).
/// </summary>
public sealed class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand, Guid>
{
    private readonly IEventRepository _events;
    private readonly IVenueRepository _venues;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateEventCommandHandler(
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

    public async Task<Guid> HandleAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw NotFoundException.For("evento", command.EventId);

        var venue = await _venues.GetByIdAsync(command.VenueId, cancellationToken)
            ?? throw NotFoundException.For("venue", command.VenueId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // El dominio valida estado activo, RF-01, RN01, RN03 y capacidad >= ocupadas.
        @event.Update(
            command.Title,
            command.Description,
            venue,
            Capacity.Of(command.Capacity),
            Schedule.Create(command.StartsAt, command.EndsAt),
            Money.Of(command.Price),
            command.Type,
            now);

        // RN02: no puede solaparse con otro evento activo del mismo venue,
        // excluyendo el propio evento que se está editando.
        var hasOverlap = await _events.ExistsActiveOverlapAsync(
            @event.VenueId, @event.Schedule.StartsAt, @event.Schedule.EndsAt,
            excludeEventId: @event.Id, cancellationToken: cancellationToken);

        if (hasOverlap)
            throw new DomainException(
                "El venue ya tiene un evento activo con un horario superpuesto.");

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
