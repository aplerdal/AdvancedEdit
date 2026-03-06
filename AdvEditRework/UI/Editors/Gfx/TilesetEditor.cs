using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Tools;
using AdvEditRework.UI.Undo;
using AuroraLib.Core;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors.Gfx;

public class TilesetEditor : IDisposable, IToolEditable
{
    private readonly Tileset _tileset;
    private readonly Palette _palette;
    private readonly Texture2D _texture;
    private readonly Image _tilesetImage;
    private readonly RenderTexture2D _viewport;
    private readonly UndoManager _undoManager;
    private readonly MapEditorTool[] _tools = [new DrawTool(), new SelectionTool(), new Eyedropper(), new RectangleTool(), new BucketTool(), new StampTool()];
    private MapEditorToolType _activeTool = MapEditorToolType.Draw;

    private Camera2D _viewCamera;
    private Vector2 _position;

    private BgrColor _oldPaletteColor;
    private bool _modifyingColor = false;

    private readonly int _tilesetWidth;
    private readonly int _tilesetHeight;
    private readonly int _tilesetSkip;
    private readonly bool _paletteLocked;

    public int CellSize => 1;
    private bool _showGrid = false;

    public byte? ActiveIndex { get; set; } = 0;
    public bool ViewportHovered => Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(_position, _viewportSize, _viewportSize));
    public bool Focused { get; private set; }

    public Vector2 CellMousePos
    {
        get
        {
            var old = _viewCamera.Zoom;
            var scale = (float)_viewportSize / TargetSize;
            _viewCamera.Zoom *= scale;
            var vec = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition() - _position, _viewCamera);
            _viewCamera.Zoom = old;
            return new Vector2((int)vec.X, (int)vec.Y);
        }
    }

    public Vector2 GridSize => new(_texture.Width, _texture.Height);
    public CellEntry[]? Stamp { get; set; }

    private const int TargetSize = 512;
    private int _viewportSize = 512;
    private const int IconSize = 32;
    private const int IconsPerColumn = 8;

    public Vector2 RenderSize => new(_viewportSize + 4 + IconSize * IconsPerColumn, _viewportSize);

    public TilesetEditor(Tileset tileset, Palette palette, bool paletteLocked = false)
    {
        _tileset = tileset;
        _palette = palette;
        _paletteLocked = paletteLocked;
        Debug.Assert(Math.Sqrt(tileset.Length) % 1 == 0, "Tileset size is not square. This is unsupported.");
        var sqrtLen = (int)Math.Sqrt(tileset.Length);
        _tilesetWidth = sqrtLen;
        _tilesetHeight = sqrtLen;
        _tilesetSkip = 0;
        _texture = _tileset.TilePaletteTexture(_tilesetWidth, _tilesetHeight);
        _tilesetImage = Raylib.LoadImageFromTexture(_texture);
        PaletteShader.SetPalette(palette.ToIVec3());
        _viewport = Raylib.LoadRenderTexture(TargetSize, TargetSize);
        _viewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 4);
        _undoManager = new UndoManager();
    }

    public TilesetEditor(Tileset tileset, Palette palette, int width, int height, int skip = 0, bool paletteLocked = false)
    {
        _tileset = tileset;
        _palette = palette;
        _paletteLocked = paletteLocked;
        _tilesetWidth = width;
        _tilesetHeight = height;
        _tilesetSkip = skip;
        var size = Math.Max(_tilesetWidth, _tilesetHeight);
        _texture = _tileset.TilePaletteTexture(size, size, _tilesetSkip);
        _tilesetImage = Raylib.LoadImageFromTexture(_texture);
        PaletteShader.SetPalette(palette.ToIVec3());
        _viewport = Raylib.LoadRenderTexture(TargetSize, TargetSize);
        _viewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 4);
        _undoManager = new UndoManager();
    }

    public void Update(Vector2 position, bool hasFocus)
    {
        Focused = hasFocus;
        _position = position;

        _viewportSize = (int)(Raylib.GetRenderHeight() - position.Y - 4);
        UpdateViewport(position);
        PaletteView(position + new Vector2(_viewportSize + 4, 0));
        ToolPicker.Draw(position + new Vector2(_viewportSize + 4, 8 + _palette.Length / 8 * IconSize), IconSize * IconsPerColumn, ref _activeTool);
    }

    public void ShowOptions()
    {
        ImGui.SeparatorText("Options");
        ImGui.Checkbox("Show Grid?", ref _showGrid);
    }

    public void ShowPaletteOptions()
    {
        ImGui.SeparatorText("Palette");
        if (!ActiveIndex.HasValue || _paletteLocked)
        {
            ImGui.BeginDisabled();
            float _ = 0;
            ImGui.ColorPicker3("Edit Selected Color:", ref _);
            ImGui.EndDisabled();
            return;
        }

        var color = _palette[ActiveIndex.Value];
        float[] colors = [color.R * 8 / 255f, color.G * 8 / 255f, color.B * 8 / 255f];
        float[] colorsOld = [color.R * 8 / 255f, color.G * 8 / 255f, color.B * 8 / 255f];
        unsafe
        {
            fixed (float* colorPtr = colors)
            {
                ImGui.ColorPicker3("Edit Selected Color:", colorPtr);
            }
        }

        var newColor = new BgrColor(colors[0], colors[1], colors[2]);
        if (colorsOld[0] == colors[0] && colorsOld[1] == colors[1] && colorsOld[2] == colors[2])
        {
            if (_modifyingColor)
            {
                var capturedOld = _oldPaletteColor; // capture the value now
                var capturedNew = newColor;
                var capturedIndex = ActiveIndex.Value;
                _undoManager.Push(new UndoActions(
                    () =>
                    {
                        _palette[capturedIndex] = capturedNew;
                        PaletteShader.SetPalette(_palette.ToIVec3());
                    },
                    () =>
                    {
                        _palette[capturedIndex] = capturedOld;
                        PaletteShader.SetPalette(_palette.ToIVec3());
                    }
                ));
                _modifyingColor = false;
            }

            return;
        }

        if (!_modifyingColor)
        {
            _oldPaletteColor = color;
            _modifyingColor = true;
        }

        ;
        _palette[ActiveIndex.Value] = newColor;
        PaletteShader.SetPalette(_palette.ToIVec3());
    }

    private Vector2 WorldToViewport(Vector2 pos)
    {
        return Raylib.GetWorldToScreen2D(pos, _viewCamera) * ((float)_viewportSize / TargetSize) + _position;
    }

    private void UpdateViewport(Vector2 position)
    {
        if (Focused)
        {
            UpdateCamera();
            CheckKeybinds();
        }

        Raylib.BeginTextureMode(_viewport);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginMode2D(_viewCamera);
        {
            PaletteShader.Begin();
            Raylib.DrawTexture(_texture, 0, 0, Color.White);
            PaletteShader.End();
            _tools[(int)_activeTool].Update(this);
        }
        Raylib.EndMode2D();
        Raylib.EndTextureMode();

        Raylib.DrawTexturePro(_viewport.Texture, new Rectangle(Vector2.Zero, _viewport.Texture.Width, -_viewport.Texture.Height), new Rectangle(position, _viewportSize, _viewportSize), Vector2.Zero, 0f, Color.White);
        if (_showGrid)
        {
            Raylib.BeginScissorMode((int)position.X, (int)position.Y, _viewportSize, _viewportSize);
            {
                for (var y = 1; y < _texture.Height / 8; y++)
                {
                    var p1 = WorldToViewport(new Vector2(0, y * 8));
                    var p2 = WorldToViewport(new Vector2(_texture.Width, y * 8));
                    Raylib.DrawLineV(p1, p2, Color.Black);
                }

                for (var x = 1; x < _texture.Width / 8; x++)
                {
                    var p1 = WorldToViewport(new Vector2(x * 8, 0));
                    var p2 = WorldToViewport(new Vector2(x * 8, _texture.Height));
                    Raylib.DrawLineV(p1, p2, Color.Black);
                }
            }
            Raylib.EndScissorMode();
        }
    }

    private bool _isPanning;
    private Vector2 _lastMousePosition = Vector2.Zero;

    private void UpdateCamera()
    {
        var viewportRect = new Rectangle(_position, _viewportSize, _viewportSize);

        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), viewportRect);

        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0.0f && hovered)
        {
            var zoomFactor = 1.05f;
            var mouseWorldPosBeforeZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera) * (TargetSize / (float)_viewportSize);
            _viewCamera.Zoom *= wheel > 0 ? zoomFactor : 1.0f / zoomFactor;
            var mouseWorldPosAfterZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera) * (TargetSize / (float)_viewportSize);
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
            var currentMousePosition = Raylib.GetMousePosition();
            var delta = Raylib.GetScreenToWorld2D(_lastMousePosition, _viewCamera) - Raylib.GetScreenToWorld2D(currentMousePosition, _viewCamera);
            _viewCamera.Target += delta * (TargetSize / (float)_viewportSize);
            _lastMousePosition = currentMousePosition;
        }

        // Limit camera position to size of tileset
        // First, make sure it can all fit with current zoom level
        Debug.Assert(_texture.Width == _texture.Height); // Assuming square texture
        _viewCamera.Zoom = _viewCamera.Zoom.Clamp(TargetSize / (float)_texture.Width, float.MaxValue);
        // Clamp view target to texture
        var viewportSizePx = new Vector2(TargetSize / _viewCamera.Zoom);
        _viewCamera.Target = Vector2.Clamp(_viewCamera.Target, Vector2.Zero, new Vector2(_texture.Width, _texture.Height) - viewportSizePx);
    }

    private void PaletteView(Vector2 position)
    {
        var len = _palette.Length;

        for (var y = 0; y < (int)(len / IconsPerColumn); y++)
        for (var x = 0; x < IconsPerColumn; x++)
        {
            var index = x + y * IconsPerColumn;
            var colRect = new Rectangle(position.X + x * IconSize, position.Y + y * IconSize, IconSize, IconSize);
            var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), colRect);
            var color = _palette[index].ToColor();
            Raylib.DrawRectangleRec(colRect, color);
            if (hovered || index == ActiveIndex) Raylib.DrawRectangleLinesEx(colRect, 2, Color.White);
            if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left) && Focused) ActiveIndex = (byte)index;
        }
    }

    private void CheckKeybinds()
    {
        // TODO: Customizable keybinds
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Redo();

        var settings = Settings.Shared;
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.EyedropperBind)) SetTool(MapEditorToolType.Eyedropper);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.SelectBind)) SetTool(MapEditorToolType.Select);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.DrawBind)) SetTool(MapEditorToolType.Draw);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.RectangleBind)) SetTool(MapEditorToolType.Rectangle);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.BucketBind)) SetTool(MapEditorToolType.Bucket);
    }

    public void Dispose()
    {
        Raylib.UnloadRenderTexture(_viewport);
        Raylib.UnloadImage(_tilesetImage);
        Raylib.UnloadTexture(_texture);
    }

    public void DrawCell(Vector2 position, byte id, Color color)
    {
        var col = _palette[id].ToColor();
        Raylib.DrawPixelV(position, col);
    }

    public void OutlineCell(Vector2 position, Color color)
    {
        Raylib.DrawRectangleLinesEx(new Rectangle(position - new Vector2(1 / 8f), new Vector2(5 / 4f)), 1 / 8f, color);
    }

    public bool ValidCell(Vector2 position)
    {
        return position.X >= 0 && position.Y >= 0 && position.X <= _texture.Width && position.Y <= _texture.Height;
    }

    public byte GetCell(Vector2 position)
    {
        var tile = (int)((int)position.X / 8) + (int)(GridSize.X / 8) * (int)((int)position.Y / 8);
        if (tile < _tilesetWidth * _tilesetHeight)
            return _tileset[tile + _tilesetSkip][(int)position.X % 8, (int)position.Y % 8];
        return 0;
    }

    private void SetPixel(Vector2 position, byte id)
    {
        var tile = (int)((int)position.X / 8) + (int)(GridSize.X / 8) * (int)((int)position.Y / 8);
        if (!(tile < _tilesetWidth * _tilesetHeight)) return;

        var indexI32 = BitConverter.ToInt32([id, id, id, 0xFF]);
        Raylib.UpdateTextureRec(_texture, new Rectangle(position, 1, 1), [indexI32]);
        _tileset[tile + _tilesetSkip][(int)position.X % 8, (int)position.Y % 8] = id;
    }

    private void SetTiles(Rectangle area, byte id)
    {
        for (var y = area.Y; y < area.Y + area.Height; y++)
        for (var x = area.X; x < area.X + area.Width; x++)
            SetPixel(new Vector2(x, y), id);
    }

    private void SetTiles(byte[,] ids, Vector2 position)
    {
        for (var y = 0; y < ids.GetLength(1); y++)
        for (var x = 0; x < ids.GetLength(0); x++)
            SetPixel(position + new Vector2(x, y), ids[x, y]);
    }

    private void SetTiles(List<CellEntry> tiles)
    {
        foreach (var entry in tiles)
            SetPixel(entry.Position, entry.Id);
    }

    private void SetTiles(HashSet<Vector2> positions, byte tile)
    {
        foreach (var position in positions)
            SetPixel(position, tile);
    }

    public UndoActions SetCellsUndoable(HashSet<Vector2> positions, byte id)
    {
        var positionsClone = new HashSet<Vector2>(positions);
        var oldTiles = new List<CellEntry>(positions.Count);
        foreach (var pos in positions)
        {
            oldTiles.Add(new CellEntry(pos, GetCell(pos)));
            SetPixel(pos, id);
        }

        return new UndoActions(() => SetTiles(positionsClone, id), () => SetTiles(oldTiles));
    }

    public UndoActions SetCellsUndoable(List<CellEntry> cells)
    {
        var tilesClone = new List<CellEntry>(cells);
        var oldTiles = new List<CellEntry>(cells.Count);
        foreach (var entry in cells)
        {
            oldTiles.Add(entry with { Id = GetCell(entry.Position) });
            SetPixel(entry.Position, entry.Id);
        }

        return new UndoActions(() => SetTiles(tilesClone), () => SetTiles(oldTiles));
    }

    public UndoActions SetCellsUndoable(Rectangle area, byte id)
    {
        var oldTiles = new byte[(int)area.Width, (int)area.Height];
        for (var y = area.Y; y < area.Y + area.Height; y++)
        for (var x = area.X; x < area.X + area.Width; x++)
            oldTiles[(int)(x - area.X), (int)(y - area.Y)] = GetCell(new Vector2(x, y));
        SetTiles(area, id);
        return new UndoActions(() => SetTiles(area, id), () => SetTiles(oldTiles, area.Position));
    }

    public void SetTool(MapEditorToolType tool)
    {
        _activeTool = tool;
    }

    public void PushUndoable(UndoActions action)
    {
        _undoManager.Push(action);
    }
}