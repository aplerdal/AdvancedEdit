using System.Numerics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI;

public class TilesetEditor : IDisposable
{
    private Tileset _tileset;
    private Palette _palette;
    private Texture2D _texture;
    private Image _tilesetImage;
    private int[] _paletteIVec;
    private int _selectedColor = 0;
    private RenderTexture2D _viewport;
    private Camera2D _viewCamera;

    private const int ViewportSize = 512;
    
    public TilesetEditor(Tileset tileset, Palette palette)
    {
        _tileset = tileset;
        _palette = palette;
        var sqrtLen = (int)Math.Sqrt(tileset.Length);
        _texture = _tileset.TilePaletteTexture(sqrtLen, sqrtLen);
        _tilesetImage = Raylib.LoadImageFromTexture(_texture);
        _paletteIVec = palette.ToIVec3();
        PaletteShader.SetPalette(_paletteIVec);
        _viewport = Raylib.LoadRenderTexture(ViewportSize, ViewportSize);
        _viewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 8);
    }

    public void Update(Vector2 position)
    {
        UpdateViewport(position);
        ColorPicker(position + new Vector2(ViewportSize+4, 0));
    }

    void UpdateViewport(Vector2 position)
    {
        Raylib.BeginTextureMode(_viewport);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginMode2D(_viewCamera);
        {
            PaletteShader.Begin();
            Raylib.DrawTexture(_texture, 0, 0, Color.White);
            PaletteShader.End();
            var tilesetRect = new Rectangle(0, 0, _texture.Width, _texture.Height);
            var mousePos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera) - position / _viewCamera.Zoom;
            var hovered = Raylib.CheckCollisionPointRec(mousePos, tilesetRect);
            if (hovered)
            {
                var pixelPos = new Vector2((int)mousePos.X, (int)mousePos.Y);
                var col = _palette[_selectedColor].ToColor();
                Raylib.DrawPixelV(pixelPos, col);
                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    // Convert the index to a grayscale color
                    int indexi32 = BitConverter.ToInt32([(byte)(_selectedColor), (byte)(_selectedColor), (byte)(_selectedColor), 0xFF]);
                    Raylib.UpdateTextureRec(_texture, new Rectangle(pixelPos, 1,1), [indexi32]);
                    var tile = (int)((int)pixelPos.X / 8) + 8 * (int)((int)pixelPos.Y / 8);
                    _tileset[tile][(int)pixelPos.X % 8, (int)pixelPos.Y % 8] = (byte)_selectedColor;
                }
            }
        }
        Raylib.EndMode2D();
        Raylib.EndTextureMode();
        
        Raylib.DrawTextureRec(_viewport.Texture, new Rectangle(Vector2.Zero, _viewport.Texture.Width, -_viewport.Texture.Height), position, Color.White);
    }
    void ColorPicker(Vector2 position)
    {
        const int iconSize = ViewportSize / 16;
        const int iconsPerColumn = 16;
        var len = _palette.Length;
        
        for (int x = 0; x < (int)(len / iconsPerColumn); x++)
        for (int y = 0; y < iconsPerColumn; y++)
        {
            var index = x * iconSize + y;
            var colRect = new Rectangle(position.X + x * iconSize, position.Y + y * iconSize, iconSize, iconSize);
            var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), colRect);
            var color = _palette[index].ToColor();
            Raylib.DrawRectangleRec(colRect, color);
            if (hovered || index == _selectedColor) Raylib.DrawRectangleLinesEx(colRect, 2, Color.White);
            if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left)) _selectedColor = index;
        }
    }
    
    public void Dispose()
    {
        Raylib.UnloadImage(_tilesetImage);
        Raylib.UnloadTexture(_texture);
    }
}