using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.AI;

public class AiHeader : ISerializable, IEquatable<AiHeader>
{
    public byte CheckpointCount { get; set; }
    public ushort CheckpointsOffset { get; set; }
    public ushort TargetsOffset { get; set; }

    public void Deserialize(Stream stream)
    {
        CheckpointCount = stream.ReadUInt8();
        CheckpointsOffset = stream.ReadUInt16();
        TargetsOffset = stream.ReadUInt16();
    }

    public void Serialize(Stream stream)
    {
        stream.Write(CheckpointCount);
        stream.Write(CheckpointsOffset);
        stream.Write(TargetsOffset);
    }

    public bool Equals(AiHeader? other)
    {
        return other != null && CheckpointCount == other.CheckpointCount && CheckpointsOffset == other.CheckpointsOffset && TargetsOffset == other.TargetsOffset;
    }
}