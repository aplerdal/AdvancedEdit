using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Undo;
using AuroraLib.Core;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI;

public class TilesetEditor : IDisposable
{
    private readonly Tileset _tileset;
    private readonly Palette _palette;
    private readonly Texture2D _texture;
    private readonly Image _tilesetImage;
    private int _selectedColor = 0;
    private readonly RenderTexture2D _viewport;
    private Camera2D _viewCamera;
    private readonly UndoManager _undoManager;

    private const int ViewportSize = 512;
    private const int IconSize = ViewportSize / 16;
    private const int IconsPerColumn = 16;

    public Vector2 RenderSize => new(ViewportSize + 4 + IconSize * (int)(_palette.Length / IconsPerColumn), ViewportSize);

    public TilesetEditor(Tileset tileset, Palette palette)
    {
        _tileset = tileset;
        _palette = palette;
        Debug.Assert(Math.Sqrt(tileset.Length) % 1 == 0, "Tileset size is not square. This is unsupported.");
        var sqrtLen = (int)Math.Sqrt(tileset.Length);
        _texture = _tileset.TilePaletteTexture(sqrtLen, sqrtLen);
        _tilesetImage = Raylib.LoadImageFromTexture(_texture);
        PaletteShader.SetPalette(palette.ToIVec3());
        _viewport = Raylib.LoadRenderTexture(ViewportSize, ViewportSize);
        _viewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 4);
        _undoManager = new UndoManager();
    }

    public void Update(Vector2 position, bool hasFocus)
    {
        UpdateViewport(position, hasFocus);
        ColorPicker(position + new Vector2(ViewportSize + 4, 0), hasFocus);
    }

    void UpdateViewport(Vector2 position, bool hasFocus)
    {
        if (hasFocus) UpdateCamera(position);
        Raylib.BeginTextureMode(_viewport);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginMode2D(_viewCamera);
        {
            PaletteShader.Begin();
            Raylib.DrawTexture(_texture, 0, 0, Color.White);
            PaletteShader.End();
            if (hasFocus)
            {
                var viewportRect = new Rectangle(Vector2.Zero, _viewport.Texture.Width, _viewport.Texture.Height);
                var mousePos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera) - position / _viewCamera.Zoom;
                var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), viewportRect);
                if (hovered)
                {
                    var pixelPos = new Vector2((int)mousePos.X, (int)mousePos.Y);
                    var col = _palette[_selectedColor].ToColor();
                    Raylib.DrawPixelV(pixelPos, col);
                    if (Raylib.IsMouseButtonDown(MouseButton.Left))
                    {
                        // Convert the index to a grayscale color
                        int indexI32 = BitConverter.ToInt32([(byte)(_selectedColor), (byte)(_selectedColor), (byte)(_selectedColor), 0xFF]);
                        Raylib.UpdateTextureRec(_texture, new Rectangle(pixelPos, 1, 1), [indexI32]);
                        var tile = (int)((int)pixelPos.X / 8) + (_texture.Width / 8) * (int)((int)pixelPos.Y / 8);
                        _tileset[tile][(int)pixelPos.X % 8, (int)pixelPos.Y % 8] = (byte)_selectedColor;
                    }
                }
            }
        }
        Raylib.EndMode2D();
        Raylib.EndTextureMode();

        Raylib.DrawTextureRec(_viewport.Texture, new Rectangle(Vector2.Zero, _viewport.Texture.Width, -_viewport.Texture.Height), position, Color.White);
    }

    private bool _isPanning;
    private Vector2 _lastMousePosition = Vector2.Zero;
    void UpdateCamera(Vector2 viewportPos)
    {
        var mouseOffs = viewportPos / _viewCamera.Zoom;
        var viewportRect = new Rectangle(Vector2.Zero, _viewport.Texture.Width, _viewport.Texture.Height);

        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), viewportRect);
        
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0.0f && hovered)
        {
            float zoomFactor = 1.05f;
            Vector2 mouseWorldPosBeforeZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera);
            _viewCamera.Zoom *= (wheel > 0) ? zoomFactor : 1.0f / zoomFactor;
            Vector2 mouseWorldPosAfterZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera);
            _viewCamera.Target += mouseWorldPosBeforeZoom - mouseWorldPosAfterZoom;
        }

        // Handle pan
        if (Raylib.IsMouseButtonPressed(MouseButton.Middle) && hovered)
        {
            _isPanning = true;
            _lastMousePosition = Raylib.GetMousePosition();
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.Middle))
        {
            _isPanning = false;
        }

        if (_isPanning)
        {
            Vector2 currentMousePosition = Raylib.GetMousePosition();
            Vector2 delta = Raylib.GetScreenToWorld2D(_lastMousePosition, _viewCamera) - Raylib.GetScreenToWorld2D(currentMousePosition, _viewCamera);
            _viewCamera.Target += delta;
            _lastMousePosition = currentMousePosition;
        }
        // Limit camera position to size of tileset
        // First, make sure it can all fit with current zoom level
        Debug.Assert(_texture.Width == _texture.Height); // Assuming square texture
        _viewCamera.Zoom = _viewCamera.Zoom.Clamp(ViewportSize / (float)_texture.Width, float.MaxValue);
        // Clamp view target to texture
        var viewportSizePx = new Vector2(ViewportSize / _viewCamera.Zoom);
        _viewCamera.Target = Vector2.Clamp(_viewCamera.Target, Vector2.Zero, new Vector2(_texture.Width, _texture.Height) - viewportSizePx);
    }

    void ColorPicker(Vector2 position, bool hasFocus)
    {
        var len = _palette.Length;

        for (int x = 0; x < (int)(len / IconsPerColumn); x++)
        for (int y = 0; y < IconsPerColumn; y++)
        {
            var index = x * IconsPerColumn + y;
            var colRect = new Rectangle(position.X + x * IconSize, position.Y + y * IconSize, IconSize, IconSize);
            var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), colRect);
            var color = _palette[index].ToColor();
            Raylib.DrawRectangleRec(colRect, color);
            if (hovered || index == _selectedColor) Raylib.DrawRectangleLinesEx(colRect, 2, Color.White);
            if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left) && hasFocus) _selectedColor = index;
        }
    }

    public void Dispose()
    {
        Raylib.UnloadImage(_tilesetImage);
        Raylib.UnloadTexture(_texture);
    }
}