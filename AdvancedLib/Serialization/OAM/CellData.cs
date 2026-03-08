using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.OAM;

public class CellData : ISerializable, IEquatable<CellData>
{
    public List<CellEntry> Entries { get; set; } = [];

    public byte SortPriority => Entries.Count > 0 ? Entries[0].Priority : (byte)0;

    public void Deserialize(Stream stream)
    {
        ushort count = stream.ReadUInt16();
        Entries = new List<CellEntry>(count);
        for (int i = 0; i < count; i++)
        {
            var entry = new CellEntry();
            entry.Deserialize(stream);
            Entries.Add(entry);
        }
    }

    public void Serialize(Stream stream)
    {
        stream.Write((ushort)Entries.Count);
        foreach (var entry in Entries)
            entry.Serialize(stream);
    }
    
    public bool Equals(CellData? other)
        => other != null && Entries.SequenceEqual(other.Entries);

    public override bool Equals(object? obj) => Equals(obj as CellData);
    public override int GetHashCode() => Entries.Aggregate(0, (h, e) => HashCode.Combine(h, e));
}