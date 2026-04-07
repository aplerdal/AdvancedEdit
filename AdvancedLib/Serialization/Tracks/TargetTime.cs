using MessagePack;
using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Tracks;

[MessagePackObject]
public class TargetTime : ISerializable, IEquatable<TargetTime>
{
    [Key(0)] public ushort Character { get; set; }

    [Key(1)] public ushort Hundredths { get; set; }

    public static TargetTime[] Defaults => [new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0)];

    public TargetTime()
    {
    }

    public TargetTime(ushort character, ushort hundredths)
    {
        Character = character;
        Hundredths = hundredths;
    }

    public void Serialize(Stream stream)
    {
        stream.Write(Character);
        stream.Write(Hundredths);
    }

    public void Deserialize(Stream stream)
    {
        Character = stream.Read<ushort>();
        Hundredths = stream.Read<ushort>();
    }

    public bool Equals(TargetTime? other)
    {
        if (other is null) return false;
        return other.Character == Character && other.Hundredths == Hundredths;
    }
}