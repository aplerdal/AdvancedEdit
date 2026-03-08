namespace GifLib;

/// <summary>
/// A single RGB palette entry. No alpha — GIF transparency is handled via a designated transparent index.
/// </summary>
public readonly struct GifColor : IEquatable<GifColor>
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public GifColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>Encode as a packed 0xRRGGBB int.</summary>
    public int ToRgbInt() => (R << 16) | (G << 8) | B;

    /// <summary>Write this color as 3 consecutive bytes into a buffer at the given offset.</summary>
    public void WriteTo(byte[] buffer, int offset)
    {
        buffer[offset] = R;
        buffer[offset + 1] = G;
        buffer[offset + 2] = B;
    }

    public bool Equals(GifColor other) => R == other.R && G == other.G && B == other.B;
    public override bool Equals(object? obj) => obj is GifColor c && Equals(c);
    public override int GetHashCode() => ToRgbInt();
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";

    public static bool operator ==(GifColor a, GifColor b) => a.Equals(b);
    public static bool operator !=(GifColor a, GifColor b) => !a.Equals(b);
}