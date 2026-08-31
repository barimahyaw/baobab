namespace Baobab.SharedKernel.Domain.Primitives;

public abstract class Entity : IEquatable<Entity>
{
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        if (ReferenceEquals(left, right))
            return true;

        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Equals(other);
    }

    public override int GetHashCode() => GetType().ToString().GetHashCode(StringComparison.InvariantCulture);

    public bool Equals(Entity? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Equals(other);
    }
}