namespace GifLib;

/// <summary>
/// Reads GIF87a and GIF89a files into a GifDocument.
/// Palette indices are preserved exactly — no conversion to RGBA is performed.
/// </summary>
public static class GifReader
{
    public static GifDocument ReadFile(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadStream(stream);
    }

    public static GifDocument ReadStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        return Read(reader);
    }

    // -------------------------------------------------------------------------
    // Top-level parse
    // -------------------------------------------------------------------------

    private static GifDocument Read(BinaryReader r)
    {
        ReadHeader(r);

        // Logical Screen Descriptor
        int screenWidth  = r.ReadUInt16();
        int screenHeight = r.ReadUInt16();
        byte packed      = r.ReadByte();
        r.ReadByte(); // background color index (ignored — meaningless without rendering)
        r.ReadByte(); // pixel aspect ratio (ignored)

        bool hasGlobalPalette = (packed & 0x80) != 0;
        int  globalPaletteSize = hasGlobalPalette ? 2 << (packed & 0x07) : 0;

        var doc = new GifDocument(screenWidth, screenHeight);

        if (hasGlobalPalette)
            doc.GlobalPalette = GifPalette.ReadFrom(r, globalPaletteSize);

        // Parse blocks until trailer
        ParseBlocks(r, doc);

        return doc;
    }

    private static void ReadHeader(BinaryReader r)
    {
        var sig     = new string(r.ReadChars(3));
        var version = new string(r.ReadChars(3));
        if (sig != "GIF")
            throw new InvalidDataException($"Not a GIF file (got signature '{sig}').");
        if (version != "87a" && version != "89a")
            throw new InvalidDataException($"Unsupported GIF version '{version}'.");
    }

    // -------------------------------------------------------------------------
    // Block dispatcher
    // -------------------------------------------------------------------------

    private static void ParseBlocks(BinaryReader r, GifDocument doc)
    {
        // State carried from Graphic Control Extension to the next Image Descriptor
        byte?  pendingTransparentIndex = null;
        int    pendingDelayMs          = 0;

        while (true)
        {
            byte introducer = r.ReadByte();

            switch (introducer)
            {
                case 0x2C: // Image Descriptor
                    ReadImageDescriptor(r, doc, pendingTransparentIndex, pendingDelayMs);
                    pendingTransparentIndex = null;
                    pendingDelayMs = 0;
                    break;

                case 0x21: // Extension
                    byte label = r.ReadByte();
                    switch (label)
                    {
                        case 0xF9: // Graphic Control Extension
                            (pendingTransparentIndex, pendingDelayMs) = ReadGraphicControlExtension(r);
                            break;
                        case 0xFF: // Application Extension (e.g. Netscape loop)
                            ReadApplicationExtension(r, doc);
                            break;
                        default:
                            SkipSubBlocks(r); // unknown extension — skip safely
                            break;
                    }
                    break;

                case 0x3B: // Trailer — end of file
                    return;

                default:
                    throw new InvalidDataException($"Unknown GIF block introducer 0x{introducer:X2}.");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Image Descriptor + Image Data
    // -------------------------------------------------------------------------

    private static void ReadImageDescriptor(
        BinaryReader r,
        GifDocument doc,
        byte? transparentIndex,
        int delayMs)
    {
        int left   = r.ReadUInt16();
        int top    = r.ReadUInt16();
        int width  = r.ReadUInt16();
        int height = r.ReadUInt16();
        byte packed = r.ReadByte();

        bool hasLocalPalette = (packed & 0x80) != 0;
        bool isInterlaced    = (packed & 0x40) != 0;
        int  localPaletteSize = hasLocalPalette ? 2 << (packed & 0x07) : 0;

        GifPalette? localPalette = null;
        if (hasLocalPalette)
            localPalette = GifPalette.ReadFrom(r, localPaletteSize);

        int minimumCodeSize = r.ReadByte();
        byte[] indices = GifLzw.Decompress(r, minimumCodeSize);

        if (isInterlaced)
            indices = DeinterlaceIndices(indices, width, height);

        if (indices.Length != width * height)
            throw new InvalidDataException(
                $"Decompressed pixel count {indices.Length} does not match frame size {width}x{height}.");

        var frame = new GifFrame(width, height, indices)
        {
            Left             = left,
            Top              = top,
            DelayMs          = delayMs,
            LocalPalette     = localPalette,
            TransparentIndex = transparentIndex,
        };

        doc.Frames.Add(frame);
    }

    // -------------------------------------------------------------------------
    // Extensions
    // -------------------------------------------------------------------------

    private static (byte? transparentIndex, int delayMs) ReadGraphicControlExtension(BinaryReader r)
    {
        r.ReadByte(); // block size (always 4)
        byte packed = r.ReadByte();
        int  delayCentiseconds = r.ReadUInt16();
        byte transparentColorIndex = r.ReadByte();
        r.ReadByte(); // block terminator

        bool hasTransparency = (packed & 0x01) != 0;
        byte? transparentIndex = hasTransparency ? transparentColorIndex : null;
        int delayMs = delayCentiseconds * 10;

        return (transparentIndex, delayMs);
    }

    private static void ReadApplicationExtension(BinaryReader r, GifDocument doc)
    {
        // Read application block header (11 bytes: 8 app id + 3 auth code)
        byte headerSize = r.ReadByte();
        if (headerSize != 11)
        {
            SkipSubBlocks(r);
            return;
        }

        string appId   = new string(r.ReadChars(8));
        string authCode = new string(r.ReadChars(3));

        if (appId == "NETSCAPE" && authCode == "2.0")
        {
            // Netscape looping extension
            byte subBlockSize = r.ReadByte(); // usually 3
            if (subBlockSize >= 3)
            {
                r.ReadByte(); // sub-block id (1)
                ushort loopCount = r.ReadUInt16();
                doc.LoopCount = loopCount;
                // consume remainder of sub-block if any
                for (int i = 3; i < subBlockSize; i++) r.ReadByte();
            }
            r.ReadByte(); // block terminator
        }
        else
        {
            SkipSubBlocks(r);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void SkipSubBlocks(BinaryReader r)
    {
        while (true)
        {
            byte size = r.ReadByte();
            if (size == 0) break;
            r.ReadBytes(size);
        }
    }

    /// <summary>
    /// GIF interlacing stores rows in 4 passes: 0,8,16,... then 4,12,... then 2,6,... then 1,3,...
    /// This reorders the decompressed indices back into normal top-to-bottom order.
    /// </summary>
    private static byte[] DeinterlaceIndices(byte[] interlaced, int width, int height)
    {
        var output = new byte[width * height];
        int[] passStartRows  = { 0, 4, 2, 1 };
        int[] passRowSteps   = { 8, 8, 4, 2 };

        int srcRow = 0;
        for (int pass = 0; pass < 4; pass++)
        {
            for (int destRow = passStartRows[pass]; destRow < height; destRow += passRowSteps[pass])
            {
                Array.Copy(interlaced, srcRow * width, output, destRow * width, width);
                srcRow++;
            }
        }
        return output;
    }
}