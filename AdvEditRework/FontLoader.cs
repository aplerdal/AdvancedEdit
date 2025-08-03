using System.Numerics;
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
        /*Latin   */
        " !\"#$%\'&()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[¥]^_`abcdefghijklmnopqrstuvwxyz{|}~ " +
        /*Hiragana*/"ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎをん" +
        /*Katakana*/"ァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワン" +
        /*Icons   */"\uE000\uE001\uE002\uE003\uE004\uE005\uE006";

    private const int CharacterCount = 256;
    private const int IconCount = 7;
    public static unsafe Font LoadMkscFont()
    {
        Font font = Raylib.GetFontDefault();
        Image image = Raylib.LoadImage("Resources/font.png");

        font.Texture = Raylib.LoadTextureFromImage(image);
        Raylib.SetTextureFilter(font.Texture, TextureFilter.Point);
        font.GlyphCount = CharacterCount + IconCount;
        font.Glyphs = (GlyphInfo*)Raylib.MemAlloc((uint)(font.GlyphCount * 40));
        font.Recs = (Rectangle*)Raylib.MemAlloc((uint)(font.GlyphCount * 16));

        var rect = new Rectangle(0, 0, 8, 16);
        for (int i = 0; i < CharacterCount; i++)
        {
            rect.Position = new Vector2(8 * (i % 32), 16 * (i / 32));
            font.Glyphs[i].Value = CharMap[i];
            font.Recs[i] = rect;
            font.Glyphs[i].OffsetX = 0;
            font.Glyphs[i].OffsetY = 0;
            font.Glyphs[i].AdvanceX = 0;
            font.Glyphs[i].Image = Raylib.ImageFromImage(image, rect);
        }

        rect = new Rectangle(0, 0, 16, 16);
        for (int i = 0; i < IconCount; i++)
        {
            rect.Position = new Vector2(16 * (i % 32), 16 * (i / 32) + 128);
            font.Glyphs[CharacterCount + i].Value = CharMap[CharacterCount + i];
            font.Recs[CharacterCount + i] = rect;
            font.Glyphs[CharacterCount + i].OffsetX = 0;
            font.Glyphs[CharacterCount + i].OffsetY = 0;
            font.Glyphs[CharacterCount + i].AdvanceX = 0;
            font.Glyphs[CharacterCount + i].Image = Raylib.ImageFromImage(image, rect);
        }
        Raylib.UnloadImage(image);

        font.BaseSize = 16;
        
        return font;
    }
}