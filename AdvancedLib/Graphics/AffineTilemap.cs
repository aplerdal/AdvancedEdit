using System.Numerics;

namespace AdvancedLib.Graphics;

/// <summary>
/// A class containing a GBA affine tilemap
/// </summary>
public class AffineTilemap
{
    private byte[] _indicies;
    public int Width { get; set; }
    public int Height { get; set; }

    public byte this[int x, int y]
    {
        get => _indicies[x + y * Width];
        set => _indicies[x + y * Width] = value;
    }

    public byte this[Vector2 pos]
    {
        get => _indicies[(int)pos.X + (int)pos.Y * Width];
        set => _indicies[(int)pos.X + (int)pos.Y * Width] = value;
    }

    /// <summary>
    /// Initialize empty <see cref="AffineTilemap"/>
    /// </summary>
    /// <param name="width">width of the tilemap</param>
    /// <param name="height">height of the tilemap</param>
    public AffineTilemap(int width, int height)
    {
        Width = width;
        Height = height;
        _indicies = new byte[width * height];
    }

    /// <summary>
    /// Load an <see cref="AffineTilemap"/> from a stream
    /// </summary>
    /// <param name="stream">Stream object containing the tilemap</param>
    /// <param name="width">Width of the tilemap</param>
    /// <param name="height">Height of the tilemap</param>
    public AffineTilemap(Stream stream, int width, int height)
    {
        Width = width;
        Height = height;
        _indicies = new byte[width * height];
        stream.ReadExactly(_indicies);
    }

    public byte[] GetData() => _indicies;

    public void Write(Stream stream)
    {
        stream.Write(_indicies);
    }

    public Task WriteAsync(Stream stream) => Task.Run(() => Write(stream));
}