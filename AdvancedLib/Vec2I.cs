namespace AdvancedLib;

public class Vec2I(int x, int y) : IEquatable<Vec2I>
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;

    public static Vec2I Zero => new Vec2I(0, 0);

    public bool Equals(Vec2I? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Vec2I)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}