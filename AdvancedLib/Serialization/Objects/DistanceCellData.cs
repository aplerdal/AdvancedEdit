using AdvancedLib.Serialization.OAM;
using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.Objects;

[MessagePackObject]
public class DistanceCellData : ISerializable, IEquatable<DistanceCellData>
{
    [Key(0)] public CellData[] Distances { get; set; } = new CellData[4];

    public void Deserialize(Stream stream)
    {
        var address = stream.Position;
        for (int i = 0; i < 4; i++)
        {
            stream.Seek(address + i * 8, SeekOrigin.Begin);
            var ptr = new Pointer(stream.Read<uint>());
            stream.Seek(ptr);
            Distances[i] = stream.Read<CellData>();
        }
    }

    public void Serialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public bool Equals(DistanceCellData? other)
        => other != null && Distances.SequenceEqual(other.Distances);

    public override bool Equals(object? obj) => Equals(obj as DistanceCellData);
    public override int GetHashCode() => Distances.Aggregate(0, (h, e) => HashCode.Combine(h, e));
}