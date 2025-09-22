using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Graphics;
using AdvancedLib.Serialization.AI;
using Raylib_cs;

namespace AdvancedLib.RaylibExt;

public static class Extensions
{
    
    public static Texture2D TilePaletteTexture(this Tileset tileset, int width, int height)
    {
        Debug.Assert(width * height >= tileset.Length, "width * height >= tileset.Length");
        Image image = Raylib.GenImageColor(width * 8, height * 8, Color.White);
        for (var i = 0; i < tileset.Length; i++)
        {
            var tile = tileset[i];
            var tilePos = new Vector2((int)(i % width), (int)(i / width)) * Tile.Size;
            for (int y = 0; y < Tile.Size; y++)
            for (int x = 0; x < Tile.Size; x++)
            {
                byte index = tile[x, y];
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
        return new Color(color.R << 3, color.G << 3, color.B << 3);
    }

    public static Vec2I ToVec2I(this Vector2 v) => new((int)v.X, (int)v.Y);
    public static Vector2 AsVector2(this Vec2I v) => new(v.X, v.Y);
}