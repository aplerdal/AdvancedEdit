namespace AdvancedLib.Graphics;

public class Tile8Bpp : Tile
{
    public static int DataSize => 64;
    public override PixelFormat Format => PixelFormat.Bpp8;
    private readonly byte[] _indicies = new byte[64];
    public override byte this[int x, int y]
    {
        get => _indicies[x + Tile.Size * y];
        set => _indicies[x + Tile.Size * y] = value;
    }

    public static Tile8Bpp Empty => new();

    public override void Serialize(Stream stream)
    {
        stream.Write(_indicies);
    }

    public override void Deserialize(Stream stream)
    {
        stream.ReadExactly(_indicies);
    }
}