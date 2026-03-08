namespace GifLib;

/// <summary>
/// Writes a GifDocument to a GIF89a file or stream.
/// Palette indices are written exactly as provided — no quantization or deduplication is performed.
/// </summary>
public static class GifWriter
{
    public static void WriteFile(GifDocument doc, string path)
    {
        using var stream = File.Create(path);
        WriteStream(doc, stream);
    }

    public static void WriteStream(GifDocument doc, Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        Write(writer, doc);
    }

    // -------------------------------------------------------------------------
    // Top-level
    // -------------------------------------------------------------------------

    private static void Write(BinaryWriter w, GifDocument doc)
    {
        Validate(doc);

        WriteHeader(w);
        WriteLogicalScreenDescriptor(w, doc);

        if (doc.GlobalPalette != null)
            doc.GlobalPalette.WriteTo(w);

        if (doc.Frames.Count > 1 && doc.LoopCount.HasValue)
            WriteNetscapeLoopExtension(w, doc.LoopCount.Value);

        foreach (var frame in doc.Frames)
            WriteFrame(w, doc, frame);

        w.Write((byte)0x3B); // Trailer
    }

    // -------------------------------------------------------------------------
    // Header + Logical Screen Descriptor
    // -------------------------------------------------------------------------

    private static void WriteHeader(BinaryWriter w)
    {
        w.Write("GIF89a".ToCharArray());
    }

    private static void WriteLogicalScreenDescriptor(BinaryWriter w, GifDocument doc)
    {
        w.Write((ushort)doc.Width);
        w.Write((ushort)doc.Height);

        byte packed = 0;
        if (doc.GlobalPalette != null)
        {
            packed |= 0x80; // global color table flag
            packed |= (byte)(doc.GlobalPalette.GifSizeField & 0x07); // size field
            // color resolution (bits 4-6): set to same as palette depth
            packed |= (byte)((doc.GlobalPalette.GifSizeField & 0x07) << 4);
        }

        w.Write(packed);
        w.Write((byte)0); // background color index
        w.Write((byte)0); // pixel aspect ratio (0 = not specified)
    }

    // -------------------------------------------------------------------------
    // Netscape loop extension
    // -------------------------------------------------------------------------

    private static void WriteNetscapeLoopExtension(BinaryWriter w, ushort loopCount)
    {
        w.Write((byte)0x21); // Extension introducer
        w.Write((byte)0xFF); // Application extension label
        w.Write((byte)11);   // Block size
        w.Write("NETSCAPE".ToCharArray());
        w.Write("2.0".ToCharArray());
        w.Write((byte)3);    // Sub-block size
        w.Write((byte)1);    // Sub-block ID
        w.Write(loopCount);
        w.Write((byte)0);    // Block terminator
    }

    // -------------------------------------------------------------------------
    // Per-frame
    // -------------------------------------------------------------------------

    private static void WriteFrame(BinaryWriter w, GifDocument doc, GifFrame frame)
    {
        var palette = frame.LocalPalette ?? doc.GlobalPalette!;

        WriteGraphicControlExtension(w, frame);
        WriteImageDescriptor(w, frame);

        if (frame.LocalPalette != null)
            frame.LocalPalette.WriteTo(w);

        // Minimum code size: max(2, ceil(log2(paletteSize)))
        int minCodeSize = Math.Max(2, (int)Math.Ceiling(Math.Log2(palette.Count)));
        GifLzw.Compress(w, frame.Indices, minCodeSize);
    }

    // -------------------------------------------------------------------------
    // Graphic Control Extension
    // -------------------------------------------------------------------------

    private static void WriteGraphicControlExtension(BinaryWriter w, GifFrame frame)
    {
        w.Write((byte)0x21); // Extension introducer
        w.Write((byte)0xF9); // Graphic Control label
        w.Write((byte)4);    // Block size (always 4)

        byte packed = 0;
        if (frame.TransparentIndex.HasValue)
            packed |= 0x01;

        w.Write(packed);
        w.Write((ushort)(frame.DelayMs / 10)); // centiseconds
        w.Write(frame.TransparentIndex ?? 0);
        w.Write((byte)0); // Block terminator
    }

    // -------------------------------------------------------------------------
    // Image Descriptor
    // -------------------------------------------------------------------------

    private static void WriteImageDescriptor(BinaryWriter w, GifFrame frame)
    {
        w.Write((byte)0x2C); // Image separator

        w.Write((ushort)frame.Left);
        w.Write((ushort)frame.Top);
        w.Write((ushort)frame.Width);
        w.Write((ushort)frame.Height);

        byte packed = 0;
        if (frame.LocalPalette != null)
        {
            packed |= 0x80; // local color table flag
            packed |= (byte)(frame.LocalPalette.GifSizeField & 0x07);
        }
        // interlace flag left as 0 (non-interlaced) — modern GIFs don't use interlacing

        w.Write(packed);
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private static void Validate(GifDocument doc)
    {
        if (doc.Frames.Count == 0)
            throw new InvalidOperationException("GifDocument has no frames.");

        for (int i = 0; i < doc.Frames.Count; i++)
        {
            var frame = doc.Frames[i];
            var palette = frame.LocalPalette ?? doc.GlobalPalette;

            if (palette == null)
                throw new InvalidOperationException(
                    $"Frame {i} has no local palette and the document has no global palette.");

            if (frame.Indices.Length != frame.Width * frame.Height)
                throw new InvalidOperationException(
                    $"Frame {i} indices length {frame.Indices.Length} does not match {frame.Width}x{frame.Height}.");

            // Check no index exceeds palette bounds
            int maxIndex = palette.Count - 1;
            for (int j = 0; j < frame.Indices.Length; j++)
            {
                if (frame.Indices[j] > maxIndex)
                    throw new InvalidOperationException(
                        $"Frame {i} pixel {j} has index {frame.Indices[j]} which exceeds palette size {palette.Count}.");
            }
        }
    }
}