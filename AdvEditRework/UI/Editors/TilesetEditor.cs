using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Tools;
using AdvEditRework.UI.Undo;
using AuroraLib.Core;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

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


    public int CellSize => 1;

    public byte? ActiveIndex { get; set; } = 0;
    public bool ViewportHovered => Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(_position, _viewport.Texture.Width, _viewport.Texture.Height));
    public bool Focused { get; private set; }

    public Vector2 CellMousePos
    {
        get
        {
            var vec = (Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _viewCamera) - _position / _viewCamera.Zoom);
            return new Vector2((int)vec.X, (int)vec.Y);
        }
    }

    public Vector2 GridSize => new(_texture.Width, _texture.Height);
    public CellEntry[]? Stamp { get; set; }

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
        Focused = hasFocus;
        _position = position;

        UpdateViewport(position);
        ColorPicker(position + new Vector2(ViewportSize + 4, 0));
    }

    public void ShowOptions(Rectangle area)
    {
        ToolPicker.Draw(area.Position, area.Width, ref _activeTool);
    }

    void UpdateViewport(Vector2 position)
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

        Raylib.DrawTextureRec(_viewport.Texture, new Rectangle(Vector2.Zero, _viewport.Texture.Width, -_viewport.Texture.Height), position, Color.White);
    }

    private bool _isPanning;
    private Vector2 _lastMousePosition = Vector2.Zero;

    void UpdateCamera()
    {
        var viewportRect = new Rectangle(_position, _viewport.Texture.Width, _viewport.Texture.Height);

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

    void ColorPicker(Vector2 position)
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
            if (hovered || index == ActiveIndex) Raylib.DrawRectangleLinesEx(colRect, 2, Color.White);
            if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left) && Focused) ActiveIndex = (byte)index;
        }
    }

    void CheckKeybinds()
    {
        // TODO: Customizable keybinds
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Redo();
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.V)) SetTool(MapEditorToolType.Eyedropper);
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.S)) SetTool(MapEditorToolType.Select);
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.B)) SetTool(MapEditorToolType.Draw);
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.R)) SetTool(MapEditorToolType.Rectangle);
    }

    public void Dispose()
    {
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
        Raylib.DrawRectangleLinesEx(new Rectangle(position - new Vector2(1 / 8f), new(5 / 4f)), 1/8f, color);
    }

    public bool ValidCell(Vector2 position) => (position.X >= 0 && position.Y >= 0 && position.X <= _texture.Width && position.Y <= _texture.Height);

    public byte GetCell(Vector2 position)
    {
        var tile = (int)((int)position.X / 8) + (int)(GridSize.X / 8) * (int)((int)position.Y / 8);
        return _tileset[tile][(int)position.X % 8, (int)position.Y % 8];
    }

    private void SetPixel(Vector2 position, byte id)
    {
        int indexI32 = BitConverter.ToInt32([id, id, id, 0xFF]);
        Raylib.UpdateTextureRec(_texture, new Rectangle(position, 1, 1), [indexI32]);
        var tile = (int)((int)position.X / 8) + (int)(GridSize.X / 8) * (int)((int)position.Y / 8);
        _tileset[tile][(int)position.X % 8, (int)position.Y % 8] = id;
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