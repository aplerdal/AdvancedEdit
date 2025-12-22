using MessagePack;

namespace AdvancedLib.Serialization.Objects;

[MessagePackObject(keyAsPropertyName: true)]
public class Obstacle(short type, short parameter) : IEquatable<Obstacle>
{
    public short Type { get; set; } = type;
    public short Parameter { get; set; } = parameter;
    public static Obstacle ItemBox => new(0, -1);

    public bool Equals(Obstacle? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Parameter == other.Parameter;
    }
}