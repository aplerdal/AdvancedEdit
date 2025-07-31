using AuroraLib.Core;
using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Allocator;

public class AllocationTable : ISerializable
{
    public List<RomSpan> Blocks { get; set; } = new List<RomSpan>();
    public const int TableSize = 0x100;
    public const int MaxBlocks = (TableSize - 2) / 6;
    /// <summary>
    /// Allocation table version
    /// </summary>
    public const int Version = 0;
    
    public AllocationTable() {}
    public AllocationTable(List<RomSpan> blocks) {}
    
    public void Serialize(Stream stream)
    {
        stream.Seek(0x400000, SeekOrigin.Begin);
        if (Blocks.Count > MaxBlocks) throw new InvalidDataException("Too many blocks allocated!");
        stream.Write((byte)Version); // Write version number
        foreach (var block in Blocks)
        {
            stream.Write((UInt24)block.Address);
            stream.Write((UInt24)block.Length);
        }
        stream.Write((byte)0);
    }

    public void Deserialize(Stream stream)
    {
        stream.Seek(0x400000, SeekOrigin.Begin);
        Blocks = new List<RomSpan>();
        if (stream.ReadUInt8() != Version) throw new Exception("Unknown allocation table version.");
        for (int i = 0; i < MaxBlocks; i++)
        {
            if (stream.PeekByte() == 0) break;
            Blocks.Add(new RomSpan(stream.ReadUInt24(), stream.ReadUInt24()));
        }
    }
}