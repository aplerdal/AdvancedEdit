using AdvancedLib.Graphics;
using Raylib_cs;

namespace AdvEditRework.Shaders;

public static class PaletteShader
{
    private static Shader _shader;
    private const int PaletteColors = 256;
    private static int _paletteLoc = -1;

    private const string ShaderText = @"#version 330

const int MAX_INDEXED_COLORS = 256;

// Input fragment attributes (from fragment shader)
    in vec2 fragTexCoord;
    in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;
uniform ivec3 palette[MAX_INDEXED_COLORS];
//uniform sampler2D palette; // Alternative to ivec3, palette provided as a 256x1 texture

// Output fragment color
    out vec4 finalColor;

void main()
{
    // Texel color fetching from texture sampler
    // NOTE: The texel is actually the a GRAYSCALE index color
    vec4 texelColor = texture(texture0, fragTexCoord) * fragColor;

    // Convert the (normalized) texel color RED component (GB would work, too)
    // to the palette index by scaling up from [0..1] to [0..255]
    int index = int(texelColor.r * 255.0);
    ivec3 color = palette[index];

    //finalColor = texture(palette, texelColor.xy); // Alternative to ivec3

    // Calculate final fragment color. Note that the palette color components
    // are defined in the range [0..255] and need to be normalized to [0..1]
    finalColor = vec4(color / 255.0, texelColor.a);
}";

    private static Color ToColor(this BgrColor color) => new Color(color.R << 3, color.G << 3, color.B << 3);
    public static int[] ToIVec3(this Palette palette)
    {
        int[] iVec3 = new int[palette.Length * 3];
        for (int i = 0; i < palette.Length; i++)
        {
            var color = palette[i].ToColor();
            iVec3[i * 3 + 0] = color.R;
            iVec3[i * 3 + 1] = color.G;
            iVec3[i * 3 + 2] = color.B;
        }
        return iVec3;
    }
    
    public static void Load()
    {
        _shader = Raylib.LoadShaderFromMemory(null, ShaderText);
        _paletteLoc = Raylib.GetShaderLocation(_shader, "palette");
    }

    public static void SetPalette(int[] paletteIVec)
    {
        Raylib.SetShaderValueV(_shader, _paletteLoc, paletteIVec.AsSpan(), ShaderUniformDataType.IVec3, PaletteColors);
    }

    public static void Begin()
    {
        Raylib.BeginShaderMode(_shader);
    }

    public static void End()
    {
        Raylib.EndShaderMode();
    }
}