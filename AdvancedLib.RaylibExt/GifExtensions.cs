using System.Numerics;
using AdvancedLib.Graphics;
using GifLib;

namespace AdvancedLib.RaylibExt;

public static class GifExtensions
{
    private static int NextPowerOfTwo(int n)
    {
        return n switch
        {
            <= 2 => 2,
            <= 4 => 4,
            <= 8 => 8,
            <= 16 => 16,
            <= 32 => 32,
            <= 64 => 64,
            <= 128 => 128,
            _ => 256
        };
    }

    private static byte[] GetImageData(this Tileset tileset, int[,] layout)
    {
        var width = layout.GetLength(0);
        var height = layout.GetLength(1);
        var data = new byte[width * Tile.Size * height * Tile.Size];
        for (int tileY = 0; tileY < height; tileY++)
        for (int tileX = 0; tileX < width; tileX++)
        {
            var index = layout[tileX, tileY];
            var tile = (index==-1)? Tile8Bpp.Empty : tileset[index];
            var tilePos = new Vector2(tileX, tileY) * Tile.Size;
            for (var y = 0; y < Tile.Size; y++)
            for (var x = 0; x < Tile.Size; x++)
            {
                data[(int)tilePos.X + x + (width * Tile.Size) * ((int)tilePos.Y + y)] = tile[x, y];
            }
        }
        return data;
    }

    private static void OverwriteFromImageData(ref Tileset tileset, byte[] data, int[,] layout)
    {
        int tileWidth = layout.GetLength(0);
        int tileHeight = layout.GetLength(1);
        int w = tileWidth * 8;

        for (int tileY = 0; tileY < tileHeight; tileY++)
        for (int tileX = 0; tileX < tileWidth; tileX++)
        for (int py = 0; py < 8; py++)
        for (int px = 0; px < 8; px++)
        {
            int srcX = tileX * 8 + px;
            int srcY = tileY * 8 + py;
            var tile = layout[tileX, tileY];
            if (tile == -1) continue;
            tileset[tile][px, py] = data[srcY * w + srcX];
        }
    }
    
    private static GifPalette ToGifPalette(this Palette palette)
    {
        var entries = new GifColor[256];

        for (int i = 0; i < palette.Length; i++)
        {
            var col = palette[i];
            entries[i] = new GifColor(col.R8, col.G8, col.B8);
        }

        return new GifPalette(entries);
    }

    private static void OverwriteGbaPalette(GifPalette gifPalette, ref Palette gba)
    {
        for (int i = 0; i < gba.Length; i++)
        {
            var col = gifPalette[i];
            gba[i] = new BgrColor(col.R, col.G, col.B);
        }
    }
    public static GifDocument ToGif(this Tileset tileset, Palette palette, int[,] layout)
    {
        var width = layout.GetLength(0);
        var height = layout.GetLength(1);
        var gif = new GifDocument(width * 8, height * 8)
        {
            GlobalPalette = palette.ToGifPalette(),
            LoopCount = null,
        };
        var frame = new GifFrame(width * 8, height * 8, tileset.GetImageData(layout))
        {
            TransparentIndex = 0,
        };
        gif.Frames.Add(frame);
        return gif;
    }

    public static void LoadGifToGba(this GifDocument gif, ref Tileset tileset, ref Palette palette, int[,] layout, bool paletteLocked = false)
    {
        if (gif.Frames.Count == 0)
            throw new InvalidOperationException("GIF has no frames.");

        var frame = gif.Frames[0];
        var gifPalette = frame.GetEffectivePalette(gif)
                         ?? throw new InvalidOperationException("GIF frame has no accessible palette.");

        if (gif.Width != layout.GetLength(0)*8 || gif.Height != layout.GetLength(1)*8)
            throw new InvalidOperationException(
                $"GIF dimensions ({gif.Width}x{gif.Height}) does not match the expected tileset size ({layout.GetLength(0) * 8}x{layout.GetLength(1) * 8}).");

        OverwriteFromImageData(ref tileset, frame.Indices, layout);
        if (!paletteLocked)
            OverwriteGbaPalette(gifPalette, ref palette);
    }
}