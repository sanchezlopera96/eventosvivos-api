using EventReservations.Domain.Events;
using FluentValidation;

namespace EventReservations.Application.Events.UpdateEvent;

/// <summary>
/// Validación de la FORMA de la entrada. Las reglas de negocio (RN01, RN03,
/// capacidad vs ocupadas, etc.) son responsabilidad del dominio.
/// </summary>
public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        // El EventId proviene de la ruta (constraint :guid), no del cuerpo,
        // por lo que no se valida aquí (el filtro corre antes de fijarlo).
        RuleFor(x => x.Title)
            .NotEmpty().Length(Event.TitleMinLength, Event.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty().Length(Event.DescriptionMinLength, Event.DescriptionMaxLength);

        RuleFor(x => x.VenueId).GreaterThan(0);
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.Type).IsInEnum();
    }
}
