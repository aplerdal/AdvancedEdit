using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.AI;

public class AiTarget : ISerializable
{
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public byte Speed { get; set; }
    public bool Intersection { get; set; }

    public void Serialize(Stream stream)
    {
        stream.Write(X);
        stream.Write(Y);
        byte union = (byte)(Speed & 0x03 | (Intersection ? 1 << 7 : 0));
        stream.Write(union);
        stream.Skip(3);
    }

    public void Deserialize(Stream stream)
    {
        X = stream.ReadUInt16();
        Y = stream.ReadUInt16();
        byte union = stream.ReadUInt8();
        stream.Skip(3);
        Speed = (byte)(union & 0x3);
        Intersection = (union & (1 << 7)) != 0;
    }
}