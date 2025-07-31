using System.Runtime.InteropServices;
using Raylib_cs;

namespace AdvEditRework;

public static class FontLoader
{
    private static bool ColorEqual(Color color1, Color color2)
    {
        return (color1.R == color2.R) && (color1.G == color2.G) && (color1.B == color2.B) && (color1.A == color2.A);
    }

    private const string CharMap = 
        " !\"#$%\'&()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[¥]^_`abcdefghijklmnopqrstuvwxyz{|}~ "+
        "ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎをん" +
        "ァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワン";
    public static unsafe Font LoadMkscFont()
    {
        Font font = Raylib.GetFontDefault();
        Image image = Raylib.LoadImage("Resources/font.png");

        font.Texture = Raylib.LoadTextureFromImage(image);
        Raylib.SetTextureFilter(font.Texture, TextureFilter.Point);
        font.GlyphCount = 256;
        font.Glyphs = (GlyphInfo*)Raylib.MemAlloc((uint)(font.GlyphCount * 40));
        font.Recs = (Rectangle*)Raylib.MemAlloc((uint)(font.GlyphCount * 16));

        for (int i = 0; i < font.GlyphCount; i++)
        {
            var rect = new Rectangle(8 * (i % 32), 16 * (i / 32), 8, 16);
            font.Glyphs[i].Value = CharMap[i];
            font.Recs[i] = rect;
            font.Glyphs[i].OffsetX = 0;
            font.Glyphs[i].OffsetY = 0;
            font.Glyphs[i].AdvanceX = 0;
            font.Glyphs[i].Image = Raylib.ImageFromImage(image, rect);
        }
        Raylib.UnloadImage(image);

        font.BaseSize = 16;
        
        return font;
    }
}