namespace GifLib;

/// <summary>
/// A GIF color palette. Holds 2, 4, 8, 16, 32, 64, 128, or 256 entries.
/// Indices are preserved exactly as stored — duplicate colors at different indices are kept intact.
/// </summary>
public sealed class GifPalette
{
    private readonly GifColor[] _entries;

    /// <summary>Number of entries in this palette. Always a power of two, 2–256.</summary>
    public int Count => _entries.Length;

    /// <summary>Get or set a palette entry by index. Duplicate colors at different indices are fully supported.</summary>
    public GifColor this[int index]
    {
        get => _entries[index];
        set => _entries[index] = value;
    }

    /// <summary>
    /// Create a palette from an array of colors. Length must be a power of two between 2 and 256.
    /// The array is copied — mutations to the original do not affect the palette.
    /// </summary>
    public GifPalette(GifColor[] entries)
    {
        if (!IsPowerOfTwo(entries.Length) || entries.Length < 2 || entries.Length > 256)
            throw new ArgumentException($"Palette size must be a power of two between 2 and 256, got {entries.Length}.");
        _entries = (GifColor[])entries.Clone();
    }

    /// <summary>Create an empty (all black) palette of the given power-of-two size.</summary>
    public GifPalette(int size)
    {
        if (!IsPowerOfTwo(size) || size < 2 || size > 256)
            throw new ArgumentException($"Palette size must be a power of two between 2 and 256, got {size}.");
        _entries = new GifColor[size];
    }

    /// <summary>
    /// The GIF color table size field value (0–7), where actual size = 2^(value+1).
    /// This is what gets written into the packed byte in GIF headers.
    /// </summary>
    public int GifSizeField => (int)Math.Log2(Count) - 1;

    /// <summary>Encode the palette as a flat byte array of R,G,B triples.</summary>
    public byte[] ToRawBytes()
    {
        var bytes = new byte[_entries.Length * 3];
        for (int i = 0; i < _entries.Length; i++)
            _entries[i].WriteTo(bytes, i * 3);
        return bytes;
    }

    /// <summary>Create a palette from a flat byte array of R,G,B triples.</summary>
    public static GifPalette FromRawBytes(byte[] bytes)
    {
        if (bytes.Length % 3 != 0)
            throw new ArgumentException("Raw palette bytes length must be a multiple of 3.");
        int count = bytes.Length / 3;
        var entries = new GifColor[count];
        for (int i = 0; i < count; i++)
            entries[i] = new GifColor(bytes[i * 3], bytes[i * 3 + 1], bytes[i * 3 + 2]);
        return new GifPalette(entries);
    }

    /// <summary>Create a palette by reading directly from a stream.</summary>
    internal static GifPalette ReadFrom(BinaryReader reader, int entryCount)
    {
        var entries = new GifColor[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            entries[i] = new GifColor(r, g, b);
        }
        return new GifPalette(entries);
    }

    /// <summary>Write this palette directly to a stream.</summary>
    internal void WriteTo(BinaryWriter writer)
    {
        foreach (var c in _entries)
        {
            writer.Write(c.R);
            writer.Write(c.G);
            writer.Write(c.B);
        }
    }

    /// <summary>Return a copy of the internal entries array.</summary>
    public GifColor[] ToArray() => (GifColor[])_entries.Clone();

    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
}