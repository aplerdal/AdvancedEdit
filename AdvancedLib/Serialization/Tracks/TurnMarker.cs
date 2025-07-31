using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Tracks;

public class TurnMarker : ISerializable
{
    public byte Zone { get; set; }
    public byte Time { get; set; }
    public sbyte Sprite { get; set; }
    public byte Unknown { get; set; }
    public void Serialize(Stream stream)
    {
        stream.Write(Zone);
        stream.Write(Time);
        stream.Write(Sprite);
        stream.Write(Unknown);
    }

    public void Deserialize(Stream stream)
    {
        Zone = stream.ReadUInt8();
        Time = stream.ReadUInt8();
        Sprite = stream.ReadInt8();
        Unknown = stream.ReadUInt8();
    }
}