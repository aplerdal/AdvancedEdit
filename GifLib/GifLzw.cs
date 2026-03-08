namespace GifLib;

/// <summary>
/// GIF-flavored LZW codec.
/// GIF uses a variant of LZW where:
///   - The minimum code size is supplied per-image (usually ceil(log2(paletteSize)), min 2)
///   - A CLEAR code resets the table (value = 2^minCodeSize)
///   - An EOI code signals end of data (value = CLEAR + 1)
///   - Code width starts at minCodeSize+1 bits and grows as the table fills
///   - Data is packed LSB-first into bytes, then chunked into sub-blocks of up to 255 bytes
/// </summary>
internal static class GifLzw
{
    // -------------------------------------------------------------------------
    // Decompress
    // -------------------------------------------------------------------------

    public static byte[] Decompress(BinaryReader reader, int minimumCodeSize)
    {
        // Read all sub-blocks into a flat byte array first
        var compressed = ReadSubBlocks(reader);
        return DecompressBytes(compressed, minimumCodeSize);
    }

    private static byte[] ReadSubBlocks(BinaryReader reader)
    {
        var result = new List<byte>(4096);
        while (true)
        {
            byte blockSize = reader.ReadByte();
            if (blockSize == 0) break;
            var block = reader.ReadBytes(blockSize);
            result.AddRange(block);
        }
        return result.ToArray();
    }

    private static byte[] DecompressBytes(byte[] data, int minimumCodeSize)
    {
        int clearCode = 1 << minimumCodeSize;
        int eoiCode   = clearCode + 1;
        int codeSize  = minimumCodeSize + 1;
        int nextCode  = eoiCode + 1;

        // Code table: each entry is a sequence of indices
        var table = new List<byte[]>(4096);
        InitTable(table, clearCode);

        var output = new List<byte>(4096);

        var bits = new BitReader(data);
        byte[]? prevEntry = null;

        while (true)
        {
            int code = bits.ReadBits(codeSize);
            if (code == eoiCode) break;

            if (code == clearCode)
            {
                InitTable(table, clearCode);
                codeSize  = minimumCodeSize + 1;
                nextCode  = eoiCode + 1;
                prevEntry = null;
                continue;
            }

            byte[] entry;
            if (code < table.Count)
            {
                entry = table[code];
            }
            else if (code == nextCode && prevEntry != null)
            {
                // Special case: code not yet in table
                entry = Append(prevEntry, prevEntry[0]);
            }
            else
            {
                throw new InvalidDataException($"LZW: unexpected code {code} (table size {table.Count}).");
            }

            output.AddRange(entry);

            if (prevEntry != null && nextCode < 4096)
            {
                table.Add(Append(prevEntry, entry[0]));
                nextCode++;
                // Grow code size when table fills current width
                if (nextCode == (1 << codeSize) && codeSize < 12)
                    codeSize++;
            }

            prevEntry = entry;
        }

        return output.ToArray();
    }

    private static void InitTable(List<byte[]> table, int clearCode)
    {
        table.Clear();
        // Entries 0..clearCode-1 are single-byte literals
        for (int i = 0; i < clearCode; i++)
            table.Add(new[] { (byte)i });
        // CLEAR and EOI slots (never output)
        table.Add(Array.Empty<byte>()); // clearCode
        table.Add(Array.Empty<byte>()); // eoiCode
    }

    private static byte[] Append(byte[] prefix, byte suffix)
    {
        var result = new byte[prefix.Length + 1];
        Array.Copy(prefix, result, prefix.Length);
        result[prefix.Length] = suffix;
        return result;
    }

    // -------------------------------------------------------------------------
    // Compress
    // -------------------------------------------------------------------------

    public static void Compress(BinaryWriter writer, byte[] indices, int minimumCodeSize)
    {
        int clearCode = 1 << minimumCodeSize;
        int eoiCode   = clearCode + 1;

        writer.Write((byte)minimumCodeSize);

        var bits    = new BitWriter();
        var codeMap = new Dictionary<(int prev, byte next), int>(4096);
        int codeSize = minimumCodeSize + 1;
        int nextCode = eoiCode + 1;

        // Emit CLEAR
        bits.WriteBits(clearCode, codeSize);

        if (indices.Length == 0)
        {
            bits.WriteBits(eoiCode, codeSize);
            WriteSubBlocks(writer, bits.ToArray());
            return;
        }

        int prev = indices[0];

        for (int i = 1; i < indices.Length; i++)
        {
            byte next = indices[i];
            var key = (prev, next);

            if (codeMap.TryGetValue(key, out int found))
            {
                prev = found;
            }
            else
            {
                bits.WriteBits(prev, codeSize);

                if (nextCode < 4096)
                {
                    codeMap[key] = nextCode++;
                    if (nextCode - 1 == (1 << codeSize) && codeSize < 12)
                        codeSize++;
                }
                else
                {
                    // Table full — emit CLEAR and reset
                    bits.WriteBits(clearCode, codeSize);
                    codeMap.Clear();
                    codeSize = minimumCodeSize + 1;
                    nextCode = eoiCode + 1;
                }

                prev = next;
            }
        }

        bits.WriteBits(prev, codeSize);
        bits.WriteBits(eoiCode, codeSize);

        WriteSubBlocks(writer, bits.ToArray());
    }

    private static void WriteSubBlocks(BinaryWriter writer, byte[] data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            int blockLen = Math.Min(255, data.Length - offset);
            writer.Write((byte)blockLen);
            writer.Write(data, offset, blockLen);
            offset += blockLen;
        }
        writer.Write((byte)0); // block terminator
    }

    // -------------------------------------------------------------------------
    // Bit-level helpers
    // -------------------------------------------------------------------------

    /// <summary>LSB-first bit reader over a byte array.</summary>
    private sealed class BitReader
    {
        private readonly byte[] _data;
        private int _bytePos;
        private int _bitBuf;
        private int _bitCount;

        public BitReader(byte[] data) { _data = data; }

        public int ReadBits(int count)
        {
            while (_bitCount < count && _bytePos < _data.Length)
            {
                _bitBuf |= _data[_bytePos++] << _bitCount;
                _bitCount += 8;
            }
            int value = _bitBuf & ((1 << count) - 1);
            _bitBuf   >>= count;
            _bitCount  -= count;
            return value;
        }
    }

    /// <summary>LSB-first bit writer that accumulates into a byte list.</summary>
    private sealed class BitWriter
    {
        private readonly List<byte> _bytes = new();
        private int _bitBuf;
        private int _bitCount;

        public void WriteBits(int value, int count)
        {
            _bitBuf   |= value << _bitCount;
            _bitCount += count;
            while (_bitCount >= 8)
            {
                _bytes.Add((byte)(_bitBuf & 0xFF));
                _bitBuf   >>= 8;
                _bitCount  -= 8;
            }
        }

        public byte[] ToArray()
        {
            if (_bitCount > 0)
                _bytes.Add((byte)(_bitBuf & 0xFF));
            return _bytes.ToArray();
        }
    }
}