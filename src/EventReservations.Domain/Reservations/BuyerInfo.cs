using System.Text.RegularExpressions;
using EventReservations.Domain.Common;

namespace EventReservations.Domain.Reservations;

/// <summary>
/// Datos del comprador de una reserva (RF-03): nombre y email.
/// Invariantes: nombre no vacío y email con formato válido. El email se
/// normaliza (recortado y en minúsculas) para evitar duplicados por mayúsculas.
/// </summary>
public sealed partial record BuyerInfo
{
    public string Name { get; }
    public string Email { get; }

    private BuyerInfo(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public static BuyerInfo Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del comprador es obligatorio.");

        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(normalizedEmail))
            throw new DomainException("El email del comprador no tiene un formato válido.");

        return new BuyerInfo(name.Trim(), normalizedEmail);
    }

    // Validación pragmática de email: algo@algo.algo, sin espacios.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
