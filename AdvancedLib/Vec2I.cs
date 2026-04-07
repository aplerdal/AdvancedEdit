using MessagePack;

namespace AdvancedLib;

[MessagePackObject]
public class Vec2I(int x, int y) : IEquatable<Vec2I>
{
    [Key(0)] public int X { get; } = x;

    [Key(1)] public int Y { get; } = y;

    public static Vec2I Zero => new(0, 0);

    public bool Equals(Vec2I? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return X == other.X && Y == other.Y;
    }
}