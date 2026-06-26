using EventReservations.Domain.Events;
using FluentValidation;

namespace EventReservations.Application.Events.CreateEvent;

/// <summary>
/// Validación de la FORMA de la entrada (fail-fast con buenos mensajes en la API).
/// Las reglas de negocio (RN01, RN03, etc.) son responsabilidad del dominio; aquí
/// solo se valida la estructura del comando.
/// </summary>
public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
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
