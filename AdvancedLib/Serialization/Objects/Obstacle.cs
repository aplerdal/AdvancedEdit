using AuroraLib.Core;
using MessagePack;

namespace AdvancedLib.Serialization.Objects;

[MessagePackObject]
public class Obstacle(short type, short parameter) : IEquatable<Obstacle>, ICloneable<Obstacle>
{
    [Key(0)] public short Type { get; set; } = type;

    [Key(1)] public short Parameter { get; set; } = parameter;
    public static Obstacle ItemBox => new(0, -1);

    public bool Equals(Obstacle? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Parameter == other.Parameter;
    }

    public Obstacle Clone()
    {
        return new Obstacle(Type, Parameter);
    }
}