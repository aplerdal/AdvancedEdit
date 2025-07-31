using AdvancedLib.Serialization;
using AuroraLib.Core.IO;

namespace AdvancedLib.Graphics;

public class Tileset
{
    private readonly Tile[] _tiles;
    public PixelFormat PixelFormat { get; set; }
    public int Length
    {
        get => _tiles.Length;
    }
    public Tile this[int index]
    {
        get => _tiles[index];
        set => _tiles[index] = value;
    }

    /// <summary>
    /// Creates an empty tileset
    /// </summary>
    /// <param name="tiles">Number of tiles in tileset</param>
    /// <param name="pixelFormat">PixelFormat of tiles</param>
    public Tileset(int tiles, PixelFormat pixelFormat)
    {
        PixelFormat = pixelFormat;
        _tiles = new Tile[tiles];
        if (pixelFormat == PixelFormat.Bpp4)
            for (int i = 0; i < tiles; i++)
                _tiles[i] = Tile4Bpp.Empty;
        else if (pixelFormat == PixelFormat.Bpp8)
            for (int i = 0; i < tiles; i++)
                _tiles[i] = Tile8Bpp.Empty;
    }
    /// <summary>
    /// Creates a <see cref="Tileset"/> from a <see cref="Stream"/>
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> containing the tileset data</param>
    /// <param name="tiles">Number of tiles in the tileset</param>
    /// <param name="pixelFormat"><see cref="PixelFormat"/> of the tiles</param>
    public Tileset(Stream stream, int tiles, PixelFormat pixelFormat)
    {
        PixelFormat = pixelFormat;
        _tiles = new Tile[tiles];
        if (pixelFormat == PixelFormat.Bpp4)
            for (int i = 0; i < tiles; i++)
                _tiles[i] = stream.Read<Tile4Bpp>();
        else if (pixelFormat == PixelFormat.Bpp8)
            for (int i = 0; i < tiles; i++)
                _tiles[i] = stream.Read<Tile8Bpp>();
    }

    public byte[] GetData()
    {
        var stream = new MemoryPoolStream(((int)PixelFormat * 8) * _tiles.Length, true);
        Write(stream);
        return stream.ToArray();
    }
    public void Write(Stream stream)
    {
        foreach (var tile in _tiles)
        {
            stream.Write(tile);
        }
    }
    public Task WriteAsync(Stream stream) => Task.Run(() => Write(stream));
}