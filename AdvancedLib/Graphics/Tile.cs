using AdvancedLib.Serialization;

namespace AdvancedLib.Graphics;

public abstract class Tile : ISerializable
{
    public const int Size = 8;
    /// <summary>
    /// Get pixel of tile
    /// </summary>
    /// <param name="x">X coordinate of pixel</param>
    /// <param name="y">Y coordinate of pixel</param>
    public abstract byte this[int x, int y] { get; set; }
    /// <summary>
    /// Pixel format of tile
    /// </summary>
    public abstract PixelFormat Format { get; }
    public abstract void Serialize(Stream stream);
    public abstract void Deserialize(Stream stream);
} 