namespace EventReservations.Domain.Common;

/// <summary>
/// Base para entidades del dominio. La igualdad se define por identidad (Id),
/// no por valor: dos entidades son la misma si comparten Id.
/// </summary>
/// <typeparam name="TId">Tipo del identificador.</typeparam>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
        => obj is Entity<TId> other
           && other.GetType() == GetType()
           && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    public override int GetHashCode() => Id.GetHashCode();
}
