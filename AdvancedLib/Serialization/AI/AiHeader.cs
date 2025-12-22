using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.AI;

public class AiHeader : ISerializable, IEquatable<AiHeader>
{
    public byte ZoneCount { get; set; }
    public ushort ZonesOffset { get; set; }
    public ushort TargetsOffset { get; set; }

    public void Deserialize(Stream stream)
    {
        ZoneCount = stream.ReadUInt8();
        ZonesOffset = stream.ReadUInt16();
        TargetsOffset = stream.ReadUInt16();
    }

    public void Serialize(Stream stream)
    {
        stream.Write(ZoneCount);
        stream.Write(ZonesOffset);
        stream.Write(TargetsOffset);
    }

    public bool Equals(AiHeader? other)
    {
        return other != null && ZoneCount == other.ZoneCount && ZonesOffset == other.ZonesOffset && TargetsOffset == other.TargetsOffset;
    }
}