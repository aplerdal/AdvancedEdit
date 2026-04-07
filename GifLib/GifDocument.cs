namespace GifLib;

/// <summary>
/// A complete GIF document. Contains a logical screen, an optional global palette,
/// and an ordered list of frames.
/// </summary>
public sealed class GifDocument
{
    /// <summary>Logical screen width. All frames are composited onto this canvas.</summary>
    public int Width { get; set; }

    /// <summary>Logical screen height.</summary>
    public int Height { get; set; }

    /// <summary>
    /// Optional global color table shared by all frames that don't define a local palette.
    /// If every frame has a LocalPalette, this can be left null.
    /// At least one of GlobalPalette or each frame's LocalPalette must be set to produce a valid GIF.
    /// </summary>
    public GifPalette? GlobalPalette { get; set; }

    /// <summary>
    /// Number of times the animation loops. 0 = loop forever. Null = no loop extension written (plays once).
    /// Only meaningful for multi-frame GIFs.
    /// </summary>
    public ushort? LoopCount { get; set; } = 0;

    /// <summary>Ordered list of frames. Single-image GIFs have exactly one frame.</summary>
    public List<GifFrame> Frames { get; } = new();

    /// <summary>Create an empty document with the given canvas size.</summary>
    public GifDocument(int width, int height)
    {
        if (width < 1 || height < 1)
            throw new ArgumentException("Document dimensions must be at least 1x1.");
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Convenience: load a GIF from a file path.
    /// Equivalent to GifReader.ReadFile(path).
    /// </summary>
    public static GifDocument Load(string path) => GifReader.ReadFile(path);

    /// <summary>
    /// Convenience: load a GIF from a stream.
    /// Equivalent to GifReader.ReadStream(stream).
    /// </summary>
    public static GifDocument Load(Stream stream) => GifReader.ReadStream(stream);

    /// <summary>
    /// Convenience: save this document to a file path.
    /// Equivalent to GifWriter.WriteFile(this, path).
    /// </summary>
    public void Save(string path) => GifWriter.WriteFile(this, path);

    /// <summary>
    /// Convenience: save this document to a stream.
    /// Equivalent to GifWriter.WriteStream(this, stream).
    /// </summary>
    public void Save(Stream stream) => GifWriter.WriteStream(this, stream);
}