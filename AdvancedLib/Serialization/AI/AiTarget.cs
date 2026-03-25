using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.AI;

[MessagePackObject]
public class AiTarget : ISerializable, IEquatable<AiTarget>
{
    [Key(0)]
    public ushort X { get; set; }

    [Key(1)]
    public ushort Y { get; set; }

    [Key(2)]
    public byte Speed { get; set; }

    [Key(3)]
    public bool Intersection { get; set; }
    public static AiTarget Default => new() { X = 0, Y = 0, Speed = 1, Intersection = false };

    public void Serialize(Stream stream)
    {
        stream.Write(X);
        stream.Write(Y);
        var union = (byte)((Speed & 0x03) | (Intersection ? 1 << 7 : 0));
        stream.Write(union);
        stream.Skip(3);
    }

    public void Deserialize(Stream stream)
    {
        X = stream.ReadUInt16();
        Y = stream.ReadUInt16();
        var union = stream.ReadUInt8();
        stream.Skip(3);
        Speed = (byte)(union & 0x3);
        Intersection = (union & (1 << 7)) != 0;
    }

    public bool Equals(AiTarget? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return X == other.X && Y == other.Y && Speed == other.Speed && Intersection == other.Intersection;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AiTarget)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Speed, Intersection);
    }
}