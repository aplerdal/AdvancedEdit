using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.Tracks;

[MessagePackObject]
public class TurnSign : ISerializable
{
    [Key(0)] public byte Zone { get; set; }
    [Key(1)] public byte Time { get; set; }
    [Key(2)] public bool Mirrored { get; set; }
    [Key(3)] public byte Sprite { get; set; }
    [Key(4)] public byte Unknown { get; set; }

    public void Serialize(Stream stream)
    {
        stream.Write(Zone);
        stream.Write(Time);
        var union = (byte)((Sprite & 0x8) | (Mirrored ? 1 << 7 : 0));
        stream.Write(union);
        stream.Write(Unknown);
    }

    public void Deserialize(Stream stream)
    {
        Zone = stream.ReadUInt8();
        Time = stream.ReadUInt8();
        var union = stream.ReadUInt8();
        Sprite = (byte)(union & 8);
        Mirrored = (union & (1 << 7)) != 0;
        Unknown = stream.ReadUInt8();
    }
}