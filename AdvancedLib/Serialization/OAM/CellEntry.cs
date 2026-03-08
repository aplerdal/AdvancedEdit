using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.OAM;

public class CellEntry : ISerializable, IEquatable<CellEntry>
{
    public ushort Attr0 { get; set; }
    public ushort Attr1 { get; set; }
    public ushort Attr2 { get; set; }


    public byte Shape
    {
        get => (byte)((Attr0 >> 14) & 0x3);
        set => Attr0 = (ushort)((Attr0 & ~0xC000) | ((value & 0x3) << 14));
    }

    public bool ColorMode8bpp
    {
        get => ((Attr0 >> 13) & 0x1) != 0;
        set => Attr0 = (ushort)((Attr0 & ~0x2000) | ((value ? 1 : 0) << 13));
    }

    public bool Mosaic
    {
        get => ((Attr0 >> 12) & 0x1) != 0;
        set => Attr0 = (ushort)((Attr0 & ~0x1000) | ((value ? 1 : 0) << 12));
    }

    public byte ObjMode
    {
        get => (byte)((Attr0 >> 10) & 0x3);
        set => Attr0 = (ushort)((Attr0 & ~0x0C00) | ((value & 0x3) << 10));
    }

    public sbyte YOffset
    {
        get => (sbyte)(Attr0 & 0xFF);
        set => Attr0 = (ushort)((Attr0 & ~0x00FF) | (byte)value);
    }

    public byte Size
    {
        get => (byte)((Attr1 >> 14) & 0x3);
        set => Attr1 = (ushort)((Attr1 & ~0xC000) | ((value & 0x3) << 14));
    }

    public bool VFlip
    {
        get => ((Attr1 >> 13) & 0x1) != 0;
        set => Attr1 = (ushort)((Attr1 & ~0x2000) | ((value ? 1 : 0) << 13));
    }

    public bool HFlip
    {
        get => ((Attr1 >> 12) & 0x1) != 0;
        set => Attr1 = (ushort)((Attr1 & ~0x1000) | ((value ? 1 : 0) << 12));
    }

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
    public byte Palette
    {
        get => (byte)((Attr2 >> 12) & 0xF);
        set => Attr2 = (ushort)((Attr2 & ~0xF000) | ((value & 0xF) << 12));
    }

    public byte Priority
    {
        get => (byte)((Attr2 >> 10) & 0x3);
        set => Attr2 = (ushort)((Attr2 & ~0x0C00) | ((value & 0x3) << 10));
    }

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
        var screen = new ushort[h, w];
        for (int row = 0; row < h; row++)
        {
            int srcRow = VFlip ? (h - 1 - row) : row;
            for (int col = 0; col < w; col++)
            {
                int srcCol = HFlip ? (w - 1 - col) : col;
                screen[row, col] = raw[srcRow, srcCol];
            }
        }

        return screen;
    }

    public bool Equals(CellEntry? other)
        => other != null && Attr0 == other.Attr0 && Attr1 == other.Attr1 && Attr2 == other.Attr2;

    public override bool Equals(object? obj) => Equals(obj as CellEntry);
    public override int GetHashCode() => HashCode.Combine(Attr0, Attr1, Attr2);
}