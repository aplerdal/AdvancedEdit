using AuroraLib.Core.IO;

namespace AdvancedLib.Graphics;

public class Tile4Bpp : Tile
{
    public static int DataSize => 32;
    public override PixelFormat Format => PixelFormat.Bpp4;
    private readonly byte[] _indicies = new byte[64];

    public override byte this[int x, int y]
    {
        get => _indicies[x + Tile.Size * y];
        set => _indicies[x + Tile.Size * y] = (byte)(value & 0xF);
    }

    public static Tile4Bpp Empty => new();

    public override void Serialize(Stream stream)
    {
        for (int i = 0; i < 32; i++)
        {
            stream.Write((byte)(((_indicies[i * 2 + 1] & 0b1111) << 4) | (_indicies[i * 2] & 0b1111)));
        }
    }

    public override void Deserialize(Stream stream)
    {
        Span<byte> data = stackalloc byte[32];
        stream.ReadExactly(data);
        for (int i = 0; i < 32; i++)
        {
            _indicies[i * 2] = (byte)(data[i] & 0xF);
            _indicies[i * 2 + 1] = (byte)(data[i] >> 4);
        }
    }
}