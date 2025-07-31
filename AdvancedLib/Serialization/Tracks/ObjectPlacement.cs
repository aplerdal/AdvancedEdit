using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Tracks;

public class ObjectPlacement : ISerializable, IEquatable<ObjectPlacement>
{
    public byte ID { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte Zone { get; set; }

    public void Deserialize(Stream stream)
    {
        ID = stream.ReadUInt8();
        X = stream.ReadUInt8();
        Y = stream.ReadUInt8();
        Zone = stream.ReadUInt8();
    }

    public void Serialize(Stream stream)
    {
        stream.Write(ID);
        stream.Write(X);
        stream.Write(Y);
        stream.Write(Zone);
    }

    public bool Equals(ObjectPlacement other)
    {
        return ID == other.ID && X == other.X && Y == other.Y && Zone == other.Zone;
    }

    public override bool Equals(object? obj)
    {
        return obj is ObjectPlacement other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ID, X, Y, Zone);
    }
}