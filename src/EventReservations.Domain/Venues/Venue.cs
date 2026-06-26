using EventReservations.Domain.Common;

namespace EventReservations.Domain.Venues;

/// <summary>
/// Venue (lugar). Dato de referencia preexistente (el enunciado provee 3 venues
/// fijos). Su capacidad acota la capacidad máxima de los eventos asignados (RN01).
/// Usa Id entero para coincidir con los datos de referencia (1, 2, 3).
/// </summary>
public sealed class Venue : Entity<int>
{
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public string City { get; private set; } = string.Empty;

    private Venue() { } // EF Core

    private Venue(int id, string name, int capacity, string city)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        City = city;
    }

    public static Venue Create(int id, string name, int capacity, string city)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del venue es obligatorio.");

        if (capacity <= 0)
            throw new DomainException("La capacidad del venue debe ser mayor que cero.");

        return new Venue(id, name.Trim(), capacity, (city ?? string.Empty).Trim());
    }
}
