namespace GifLib;

/// <summary>
/// A single frame within a GIF document.
/// Pixel data is stored as palette indices — one byte per pixel, row-major, top-left origin.
/// If LocalPalette is null, the frame uses the document's GlobalPalette when rendered.
/// </summary>
public sealed class GifFrame
{
    /// <summary>Frame width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>
    /// X offset of this frame within the logical screen. Usually 0 for full-canvas frames.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Y offset of this frame within the logical screen. Usually 0 for full-canvas frames.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Delay before showing the next frame, in milliseconds.
    /// GIF stores this in centiseconds (1/100s) so values are rounded to the nearest 10ms on encode.
    /// </summary>
    public int DelayMs { get; set; }

    /// <summary>
    /// Per-frame local palette. If null, the document's GlobalPalette is used.
    /// Setting this allows each frame to have a completely independent set of 256 colors.
    /// </summary>
    public GifPalette? LocalPalette { get; set; }

    /// <summary>
    /// Raw pixel indices. Length must equal Width * Height.
    /// Each value indexes into LocalPalette (if set) or the document's GlobalPalette.
    /// Duplicate colors at different indices are preserved exactly.
    /// </summary>
    public byte[] Indices { get; set; }

    /// <summary>
    /// The palette index treated as transparent. Set to null for no transparency.
    /// Any pixel whose index equals this value will be rendered as transparent.
    /// </summary>
    public byte? TransparentIndex { get; set; }

    /// <summary>
    /// Convenience: get the palette this frame actually uses (local if present, else global from document).
    /// Requires a reference to the parent document. Returns null if neither is set.
    /// </summary>
    public GifPalette? GetEffectivePalette(GifDocument document)
        => LocalPalette ?? document.GlobalPalette;

    /// <summary>
    /// Resolve the color for a given pixel index using the effective palette.
    /// Returns null if the index is the transparent index, or if no palette is available.
    /// </summary>
    public GifColor? GetColor(int pixelIndex, GifDocument document)
    {
        if (TransparentIndex.HasValue && Indices[pixelIndex] == TransparentIndex.Value)
            return null;
        var palette = GetEffectivePalette(document);
        if (palette == null) return null;
        return palette[Indices[pixelIndex]];
    }

    /// <summary>
    /// Create a new frame. Indices array is initialized to zero (first palette entry).
    /// </summary>
    public GifFrame(int width, int height)
    {
        if (width < 1 || height < 1)
            throw new ArgumentException("Frame dimensions must be at least 1x1.");
        Width  = width;
        Height = height;
        Indices = new byte[width * height];
    }

    /// <summary>
    /// Create a frame with pre-supplied index data. The array is used directly, not copied.
    /// </summary>
    public GifFrame(int width, int height, byte[] indices)
    {
        if (indices.Length != width * height)
            throw new ArgumentException($"Indices length {indices.Length} does not match {width}x{height}={width*height}.");
        Width   = width;
        Height  = height;
        Indices = indices;
    }
}