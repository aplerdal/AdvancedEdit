using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Graphics;
using Raylib_cs;

namespace AdvancedLib.RaylibExt;

public static class Extensions
{
    public static Texture2D TilePaletteTexture(this Tileset tileset, int width, int height, int skip = 0)
    {
        Debug.Assert(width * height + skip >= tileset.Length, "width * height >= tileset.Length");
        var image = Raylib.GenImageColor(width * 8, height * 8, Color.Black);
        for (var i = 0; i < tileset.Length - skip; i++)
        {
            var tile = tileset[i + skip];
            var tilePos = new Vector2(i % width, (int)(i / width)) * Tile.Size;
            for (var y = 0; y < Tile.Size; y++)
            for (var x = 0; x < Tile.Size; x++)
            {
                var index = tile[x, y];
                Raylib.ImageDrawPixel(ref image, (int)tilePos.X + x, (int)tilePos.Y + y, new Color(index, index, index, (byte)255));
            }
        }

        var texture = Raylib.LoadTextureFromImage(image);
        if (!Raylib.IsTextureValid(texture)) throw new Exception("Error creating tileset texture");
        Raylib.UnloadImage(image);
        return texture;
    }

    public static Texture2D TileTexture(this Tileset tileset, int width, int height, int skip = 0)
    {
        Debug.Assert(width * height + skip >= tileset.Length, "width * height >= tileset.Length");
        var image = Raylib.GenImageColor(width * 8, height * 8, new Color(0, 0, 0, 0));
        for (var i = 0; i < tileset.Length - skip; i++)
        {
            var tile = tileset[i + skip];
            var tilePos = new Vector2(i % width, (int)(i / width)) * Tile.Size;
            for (var y = 0; y < Tile.Size; y++)
            for (var x = 0; x < Tile.Size; x++)
            {
                var index = tile[x, y];
                if (index == 0) continue;
                Raylib.ImageDrawPixel(ref image, (int)tilePos.X + x, (int)tilePos.Y + y, new Color(index, index, index, (byte)255));
            }
        }

        var texture = Raylib.LoadTextureFromImage(image);
        if (!Raylib.IsTextureValid(texture)) throw new Exception("Error creating tileset texture");
        Raylib.UnloadImage(image);
        return texture;
    }

    public static Rectangle GetTileRect(int index, int width)
    {
        return new Rectangle(8 * (index % width), 8 * (int)(index / width), 8, 8);
    }

    public static Color ToColor(this BgrColor color)
    {
        return new Color(color.R5 << 3, color.G5 << 3, color.B5 << 3);
    }

    public static Vec2I ToVec2I(this Vector2 v)
    {
        return new Vec2I((int)v.X, (int)v.Y);
    }

    public static Vector2 AsVector2(this Vec2I v)
    {
        return new Vector2(v.X, v.Y);
    }
}