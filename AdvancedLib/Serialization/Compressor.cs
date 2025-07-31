using System.IO.Compression;
using AuroraLib.Compression.Algorithms;
using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization;

public static class Compressor
{
    public static readonly LZ10 LZ10 = new() { LookAhead = false };
    public const uint MaxPartSize = 4096;

    public static void Decompress(Stream source, Stream destination) => LZ10.Decompress(source, destination);
    public static void Compress(ReadOnlySpan<byte> source, Stream destination, CompressionLevel compressionLevel = 
        CompressionLevel.Optimal) => LZ10.Compress(source, destination, compressionLevel);
    
    public static void SplitDecompress(Stream source, Stream destination, int maxParts = 16)
    {
        var baseAddress = source.Position;
        var offsets = new List<ushort>();
        var part = 0;
        while (true)
        {
            var offset = source.ReadUInt16();
            if (offset == 0 || part >= maxParts) break;
            offsets.Add(offset);
            part++;
        }

        foreach (var offset in offsets)
        {
            var address = baseAddress + offset;
            source.Seek(address, SeekOrigin.Begin);
            LZ10.Decompress(source, destination);
        }
    }
    public static void SplitCompress(ReadOnlySpan<byte> source, Stream destination, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        // Ceiling of integer division
        var blocks = (uint)(1 + (source.Length - 1) / MaxPartSize);
        var headerAddress = destination.Position;
        var headerLength = ((blocks + 15) & ~15);
        destination.Seek(2 * headerLength, SeekOrigin.Current);
        var offsets = new ushort[blocks];
        for (int i = 0; i < blocks; i++)
        {
            offsets[i] = (ushort)(destination.Position - headerAddress);
            int sourceOffset = (int)(i * MaxPartSize);
            int sourceBlockEnd = (int)Math.Min(sourceOffset + MaxPartSize, source.Length);
            LZ10.Compress(source[sourceOffset..sourceBlockEnd], destination, compressionLevel);
        }

        var endAddress = destination.Position;
        destination.Seek(headerAddress, SeekOrigin.Begin);
        foreach (var offset in offsets)
            destination.Write(offset);
        for (int i = offsets.Length; i < headerLength; i++)
            destination.Write((ushort)0);
        destination.Seek(endAddress, SeekOrigin.Begin);
    }
}