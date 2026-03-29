using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.OAM;

[MessagePackObject]
public class OAMCellEntry : ISerializable, IEquatable<OAMCellEntry>
{
    [Key(0)]
    public ushort Attr0 { get; set; }
    [Key(1)]
    public ushort Attr1 { get; set; }
    [Key(2)]
    public ushort Attr2 { get; set; }


    [IgnoreMember]
    public byte Shape
    {
        get => (byte)((Attr0 >> 14) & 0x3);
        set => Attr0 = (ushort)((Attr0 & ~0xC000) | ((value & 0x3) << 14));
    }

    [IgnoreMember]
    public bool ColorMode8bpp
    {
        get => ((Attr0 >> 13) & 0x1) != 0;
        set => Attr0 = (ushort)((Attr0 & ~0x2000) | ((value ? 1 : 0) << 13));
    }

    [IgnoreMember]
    public bool Mosaic
    {
        get => ((Attr0 >> 12) & 0x1) != 0;
        set => Attr0 = (ushort)((Attr0 & ~0x1000) | ((value ? 1 : 0) << 12));
    }

    [IgnoreMember]
    public byte ObjMode
    {
        get => (byte)((Attr0 >> 10) & 0x3);
        set => Attr0 = (ushort)((Attr0 & ~0x0C00) | ((value & 0x3) << 10));
    }

    [IgnoreMember]
    public sbyte YOffset
    {
        get => (sbyte)(Attr0 & 0xFF);
        set => Attr0 = (ushort)((Attr0 & ~0x00FF) | (byte)value);
    }

    [IgnoreMember]
    public byte Size
    {
        get => (byte)((Attr1 >> 14) & 0x3);
        set => Attr1 = (ushort)((Attr1 & ~0xC000) | ((value & 0x3) << 14));
    }

    [IgnoreMember]
    public bool VFlip
    {
        get => ((Attr1 >> 13) & 0x1) != 0;
        set => Attr1 = (ushort)((Attr1 & ~0x2000) | ((value ? 1 : 0) << 13));
    }

    [IgnoreMember]
    public bool HFlip
    {
        get => ((Attr1 >> 12) & 0x1) != 0;
        set => Attr1 = (ushort)((Attr1 & ~0x1000) | ((value ? 1 : 0) << 12));
    }

    [IgnoreMember]
    public short XOffset
    {
        get
        {
            int raw = Attr1 & 0x1FF;
            return (short)(raw > 255 ? raw - 512 : raw);
        }
        set
        {
            int raw = value & 0x1FF;
            Attr1 = (ushort)((Attr1 & ~0x01FF) | raw);
        }
    }

    [IgnoreMember]
    public byte Palette
    {
        get => (byte)((Attr2 >> 12) & 0xF);
        set => Attr2 = (ushort)((Attr2 & ~0xF000) | ((value & 0xF) << 12));
    }

    [IgnoreMember]
    public byte Priority
    {
        get => (byte)((Attr2 >> 10) & 0x3);
        set => Attr2 = (ushort)((Attr2 & ~0x0C00) | ((value & 0x3) << 10));
    }

    [IgnoreMember]
    public ushort TileIndex
    {
        get => (ushort)(Attr2 & 0x3FF);
        set => Attr2 = (ushort)((Attr2 & ~0x03FF) | (value & 0x3FF));
    }

    public void Deserialize(Stream stream)
    {
        Attr0 = stream.ReadUInt16();
        Attr1 = stream.ReadUInt16();
        Attr2 = stream.ReadUInt16();
    }

    public void Serialize(Stream stream)
    {
        stream.Write(Attr0);
        stream.Write(Attr1);
        stream.Write(Attr2);
    }

    private static readonly int[,] TileWidths =
    {
        { 1, 2, 4, 8 }, // Square
        { 2, 4, 4, 8 }, // Wide
        { 1, 1, 2, 4 }, // Tall
    };

    private static readonly int[,] TileHeights =
    {
        { 1, 2, 4, 8 }, // Square
        { 1, 1, 2, 4 }, // Wide
        { 2, 4, 4, 8 }, // Tall
    };

    public ushort[,] GetTileGrid()
    {
        int w = TileWidths[Shape, Size];
        int h = TileHeights[Shape, Size];
        int step = ColorMode8bpp ? 2 : 1;

        // Build the unflipped VRAM-order grid first.
        var raw = new ushort[h, w];
        for (int row = 0; row < h; row++)
        for (int col = 0; col < w; col++)
            raw[row, col] = (ushort)(TileIndex + (row * w + col) * step);

        // Mirror columns for H-flip, rows for V-flip.
        var screen = new ushort[w, h];
        for (int row = 0; row < h; row++)
        {
            int srcRow = VFlip ? (h - 1 - row) : row;
            for (int col = 0; col < w; col++)
            {
                int srcCol = HFlip ? (w - 1 - col) : col;
                screen[col, row] = raw[srcRow, srcCol];
            }
        }

        return screen;
    }

    public bool Equals(OAMCellEntry? other)
        => other != null && Attr0 == other.Attr0 && Attr1 == other.Attr1 && Attr2 == other.Attr2;

    public override bool Equals(object? obj) => Equals(obj as OAMCellEntry);
    public override int GetHashCode() => HashCode.Combine(Attr0, Attr1, Attr2);
}