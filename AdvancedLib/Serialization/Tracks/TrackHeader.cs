using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Tracks;

[Flags]
public enum TrackFlags
{
    SplitTileset = 1,
    SplitTilemap = 2,
    SplitObjects = 4,
}
public class TrackHeader : ISerializable, IEquatable<TrackHeader>
{
    public long Address { get; set; }
    
    public const byte Magic = 0x01;
    public bool CompressedTileset { get; set; }
    public TrackFlags Flags { get; set; }
    public byte TrackWidth { get; set; }
    public byte TrackHeight { get; set; }
    public sbyte SharedTileset { get; set; }
    public uint TilemapOffset { get; set; }
    public uint TilesetOffset { get; set; }
    public uint TilesetPaletteOffset { get; set; }
    public uint BehaviorsOffset { get; set; }
    public uint ObstaclesOffset { get; set; }
    public uint CoinsOffset { get; set; }
    public uint ItemBoxOffset { get; set; }
    public uint StartPositionOffset { get; set; }
    public uint MinimapOffset { get; set; }
    public uint AiOffset { get; set; }
    public uint ObstacleGfxOffset { get; set; }
    public uint ObstaclePaletteOffset { get; set; }
    public sbyte SharedObstacleGfx { get; set; }
    
    public void Deserialize(Stream stream)
    {
        Address = stream.Position;
        
        stream.MatchThrow([Magic]);
        CompressedTileset = stream.ReadUInt16() != 0;
        Flags = (TrackFlags)stream.ReadUInt8();
        TrackWidth = stream.ReadUInt8();
        TrackHeight = stream.ReadUInt8();
        stream.Skip(42);
        SharedTileset = stream.ReadInt8();
        stream.Skip(15);
        TilemapOffset = stream.ReadUInt32();
        stream.Skip(60);
        TilesetOffset = stream.ReadUInt32();
        TilesetPaletteOffset = stream.ReadUInt32();
        BehaviorsOffset = stream.ReadUInt32();
        ObstaclesOffset = stream.ReadUInt32();
        CoinsOffset = stream.ReadUInt32();
        ItemBoxOffset = stream.ReadUInt32();
        StartPositionOffset = stream.ReadUInt32();
        stream.Skip(40);
        MinimapOffset = stream.ReadUInt32();
        stream.Skip(4);
        AiOffset = stream.ReadUInt32();
        stream.Skip(20);
        ObstacleGfxOffset = stream.ReadUInt32();
        ObstaclePaletteOffset = stream.ReadUInt32();
        SharedObstacleGfx = stream.ReadInt8();
        stream.Skip(19);
    }
    public void Serialize(Stream stream)
    {
        Address = stream.Position;
        
        Span<byte> padding = stackalloc byte[60];
        padding.Clear();
        stream.Write(Magic);
        stream.Write((ushort)(CompressedTileset?1:0));
        stream.Write((byte)Flags);
        stream.Write(TrackWidth);
        stream.Write(TrackHeight);
        stream.Write(padding[..42]);
        stream.Write(SharedTileset);
        stream.Write(padding[..15]);
        stream.Write(TilemapOffset);
        stream.Write(padding[..60]);
        stream.Write(TilesetOffset);
        stream.Write(TilesetPaletteOffset);
        stream.Write(BehaviorsOffset);
        stream.Write(ObstaclesOffset);
        stream.Write(CoinsOffset);
        stream.Write(ItemBoxOffset);
        stream.Write(StartPositionOffset);
        stream.Write(padding[..40]);
        stream.Write(MinimapOffset);
        stream.Write(padding[..4]);
        stream.Write(AiOffset);
        stream.Write(padding[..20]);
        stream.Write(ObstacleGfxOffset);
        stream.Write(ObstaclePaletteOffset);
        stream.Write(SharedObstacleGfx);
        stream.Write(padding[..19]);
    }
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(CompressedTileset);
        hashCode.Add((int)Flags);
        hashCode.Add(TrackWidth);
        hashCode.Add(TrackHeight);
        hashCode.Add(SharedTileset);
        hashCode.Add(TilemapOffset);
        hashCode.Add(TilesetOffset);
        hashCode.Add(TilesetPaletteOffset);
        hashCode.Add(BehaviorsOffset);
        hashCode.Add(ObstaclesOffset);
        hashCode.Add(CoinsOffset);
        hashCode.Add(ItemBoxOffset);
        hashCode.Add(StartPositionOffset);
        hashCode.Add(MinimapOffset);
        hashCode.Add(AiOffset);
        hashCode.Add(ObstacleGfxOffset);
        hashCode.Add(ObstaclePaletteOffset);
        hashCode.Add(SharedObstacleGfx);
        return hashCode.ToHashCode();
    }
    public bool Equals(TrackHeader other)
    {
        return CompressedTileset == other.CompressedTileset && Flags == other.Flags && TrackWidth == other.TrackWidth && TrackHeight == other.TrackHeight && SharedTileset == other.SharedTileset && TilemapOffset == other.TilemapOffset && TilesetOffset == other.TilesetOffset && TilesetPaletteOffset == other.TilesetPaletteOffset && BehaviorsOffset == other.BehaviorsOffset && ObstaclesOffset == other.ObstaclesOffset && CoinsOffset == other.CoinsOffset && ItemBoxOffset == other.ItemBoxOffset && StartPositionOffset == other.StartPositionOffset && MinimapOffset == other.MinimapOffset && AiOffset == other.AiOffset && ObstacleGfxOffset == other.ObstacleGfxOffset && ObstaclePaletteOffset == other.ObstaclePaletteOffset && SharedObstacleGfx == other.SharedObstacleGfx;
    }
}