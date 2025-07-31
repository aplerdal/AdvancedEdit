using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Background;

public enum BackgroundSize
{
    Unused = 1,
    Normal = 2,
    TallFront = 4,
    TallMiddle = 8,
    TallBack = 16,
    WideFront = 32,
    WideMiddle = 64,
    WideBack = 128,
}

[Flags]
public enum CompressionMode
{
    Compressed = 1,
    Split = 2,
}

public struct BackgroundHeader : ISerializable, IEquatable<BackgroundHeader>
{
    public BackgroundSize Size { get; set; }
    public CompressionMode CompressionMode { get; set; }
    public uint TilesetOffset { get; set; }
    public uint FrontTilemapOffset { get; set; }
    public uint MiddleTilemapOffset { get; set; }
    public uint BackTilemapOffset { get; set; }
    public uint PaletteOffset { get; set; }
    
    public void Deserialize(Stream stream)
    {
        Size = (BackgroundSize)stream.ReadUInt8();
        CompressionMode = (CompressionMode)stream.ReadUInt8();
        stream.Skip(2);
        TilesetOffset = stream.ReadUInt32();
        FrontTilemapOffset = stream.ReadUInt32();
        MiddleTilemapOffset = stream.ReadUInt32();
        BackTilemapOffset = stream.ReadUInt32();
        PaletteOffset = stream.ReadUInt32();
    }
    public void Serialize(Stream stream)
    {
        stream.Write((byte)Size);
        stream.Write((byte)CompressionMode);
        stream.Write((ushort)0);
        stream.Write(TilesetOffset);
        stream.Write(FrontTilemapOffset);
        stream.Write(MiddleTilemapOffset);
        stream.Write(BackTilemapOffset);
        stream.Write(PaletteOffset);
    }
    
    public bool Equals(BackgroundHeader other)
    {
        return Size == other.Size && CompressionMode == other.CompressionMode && TilesetOffset == other.TilesetOffset && FrontTilemapOffset == other.FrontTilemapOffset && MiddleTilemapOffset == other.MiddleTilemapOffset && BackTilemapOffset == other.BackTilemapOffset && PaletteOffset == other.PaletteOffset;
    }

    public override bool Equals(object? obj)
    {
        return obj is BackgroundHeader other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Size, (int)CompressionMode, TilesetOffset, FrontTilemapOffset, MiddleTilemapOffset, BackTilemapOffset, PaletteOffset);
    }
}