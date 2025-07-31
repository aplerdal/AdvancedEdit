namespace AdvancedLib.Serialization.Objects;

public class Obstacle(short type, short parameter) : IEquatable<Obstacle>
{
    public short Type { get; set; } = type;
    public short Parameter { get; set; } = parameter;

    public bool Equals(Obstacle? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Parameter == other.Parameter;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Obstacle)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Parameter);
    }
}