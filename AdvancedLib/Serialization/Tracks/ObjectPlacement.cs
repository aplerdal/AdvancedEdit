using AuroraLib.Core;
using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.Tracks;

[MessagePackObject]
public class ObjectPlacement : ISerializable, IEquatable<ObjectPlacement>, ICloneable<ObjectPlacement>
{
    [Key(0)]
    public byte ID { get; set; }

    [Key(1)]
    public byte X { get; set; }

    [Key(2)]
    public byte Y { get; set; }

    [Key(3)]
    public byte Checkpoint { get; set; }

    public ObjectPlacement()
    {
        
    }

    public ObjectPlacement(byte id, byte x, byte y, byte checkpoint)
    {
        ID = id;
        X = x;
        Y = y;
        Checkpoint = checkpoint;
    }

    public void Deserialize(Stream stream)
    {
        ID = stream.ReadUInt8();
        X = stream.ReadUInt8();
        Y = stream.ReadUInt8();
        Checkpoint = stream.ReadUInt8();
    }

    public void Serialize(Stream stream)
    {
        stream.Write(ID);
        stream.Write(X);
        stream.Write(Y);
        stream.Write(Checkpoint);
    }

    public bool Equals(ObjectPlacement? other)
    {
        return other != null && ID == other.ID && X == other.X && Y == other.Y && Checkpoint == other.Checkpoint;
    }

    public ObjectPlacement Clone()
    {
        return new ObjectPlacement { Checkpoint = Checkpoint, ID = ID, X = X, Y = Y };
    }
}