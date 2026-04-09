using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Tools;
using AdvEditRework.UI.Undo;
using AuroraLib.Core;
using Raylib_cs;

namespace AdvEditRework.UI.Editors.Gfx;

public class TilesetEditor : IDisposable, IToolEditable
{
    public Tileset Tileset;
    public Palette Palette;
    private Texture2D _texture;
    private Image _tilesetImage;
    private readonly RenderTexture2D _viewport;
    public readonly UndoManager UndoManager;
    private readonly MapEditorTool[] _tools = [new DrawTool(), new SelectionTool(), new Eyedropper(), new RectangleTool(), new BucketTool(), new StampTool()];
    private MapEditorToolType _activeTool = MapEditorToolType.Draw;

    public Camera2D ViewCamera;
    private Vector2 _position;

    public readonly int[,] Layout;

    public readonly int TilesetWidth;
    public readonly int TilesetHeight;

    public int CellSize => 1;
    public bool ShowGrid = false;

    public byte? ActiveIndex { get; set; } = 0;
    public bool ViewportHovered => Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(_position, _viewportSize, _viewportSize));
    public bool Focused { get; private set; }

    public Vector2 CellMousePos
    {
        get
        {
            var old = ViewCamera.Zoom;
            var scale = (float)_viewportSize / TargetSize;
            ViewCamera.Zoom *= scale;
            var vec = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition() - _position, ViewCamera);
            ViewCamera.Zoom = old;
            return new Vector2((int)vec.X, (int)vec.Y);
        }
    }

    public Vector2 GridSize => new Vector2(TilesetWidth, TilesetHeight) * 8;
    public CellEntry[]? Stamp { get; set; }

    private const int TargetSize = 512;
    private int _viewportSize = 512;
    private const int IconsPerColumn = 8;

    public Vector2 RenderSize => new(_viewportSize + 4 + _iconSize * IconsPerColumn, _viewportSize);

    public TilesetEditor(Tileset tileset, Palette palette)
    {
        Tileset = tileset;
        Palette = palette;
        Debug.Assert(Math.Sqrt(tileset.Length) % 1 == 0, "Tileset size is not square. Use the other constructor.");
        var sqrtLen = (int)Math.Sqrt(tileset.Length);
        TilesetWidth = sqrtLen;
        TilesetHeight = sqrtLen;
        Layout = new int[TilesetWidth, TilesetHeight];
        ushort i = 0;
        for (int y = 0; y < TilesetHeight; y++)
        for (int x = 0; x < TilesetWidth; x++)
        {
            Layout[x, y] = i++;
        }

        _texture = Tileset.TilePaletteTexture(1, Tileset.Length);
        _tilesetImage = Raylib.LoadImageFromTexture(_texture);
        _paletteIvec = palette.ToIVec3();
        _viewport = Raylib.LoadRenderTexture(TargetSize, TargetSize);
        ViewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 4);
        UndoManager = new UndoManager();
    }

    public TilesetEditor(Tileset tileset, Palette palette, int width, int height, int skip = 0)
    {
        Tileset = tileset;
        Palette = palette;
        TilesetWidth = width;
        TilesetHeight = height;
        Layout = new int[TilesetWidth, TilesetHeight];
        ushort i = (ushort)skip;
        for (int y = 0; y < TilesetHeight; y++)
        for (int x = 0; x < TilesetWidth; x++)
        {
            Layout[x, y] = i++;
        }

        ReloadTileset();
        _viewport = Raylib.LoadRenderTexture(TargetSize, TargetSize);
        ViewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 4);
        UndoManager = new UndoManager();
    }

    public TilesetEditor(Tileset tileset, Palette palette, int[,] layout)
    {
        Tileset = tileset;
        Palette = palette;
        TilesetWidth = layout.GetLength(0);
        TilesetHeight = layout.GetLength(1);
        Layout = layout;
        ReloadTileset();
        _viewport = Raylib.LoadRenderTexture(TargetSize, TargetSize);
        ViewCamera = new Camera2D(Vector2.Zero, Vector2.Zero, 0, 4);
        UndoManager = new UndoManager();
    }

    public void ReloadTileset()
    {
        if (Raylib.IsTextureValid(_texture))
        {
            Raylib.UnloadTexture(_texture);
        }

        RefreshPalette();
        Raylib.UnloadTexture(_texture);
        _texture = Tileset.TilePaletteTexture(1, Tileset.Length);
        Raylib.UnloadImage(_tilesetImage);
        _tilesetImage = Raylib.LoadImageFromTexture(_texture);
    }

    public void RefreshPalette()
    {
        _paletteIvec = Palette.ToIVec3();
    }

    public void Update(Rectangle area, bool hasFocus)
    {
        Focused = hasFocus;
        _position = area.Position;

        _viewportSize = (int)Math.Min(area.Width, area.Height);
        UpdateViewport(_position);
    }

    public void UpdatePaletteView(Rectangle area)
    {
        PaletteView(area);
        ToolPicker.Draw(area.Position + new Vector2(0, 8 + Palette.Length / 8 * _iconSize), area.Width, ref _activeTool);
    }

    private Vector2 WorldToViewport(Vector2 pos)
    {
        return Raylib.GetWorldToScreen2D(pos, ViewCamera) * ((float)_viewportSize / TargetSize) + _position;
    }

    private void UpdateViewport(Vector2 position)
    {
        if (Focused)
        {
            UpdateCamera();
            CheckKeybinds();
        }

        Raylib.BeginTextureMode(_viewport);
        Raylib.ClearBackground(Color.Blank);
        Raylib.BeginMode2D(ViewCamera);
        {
            PaletteShader.SetPalette(_paletteIvec);
            PaletteShader.Begin();
            for (int y = 0; y < TilesetHeight; y++)
            for (int x = 0; x < TilesetWidth; x++)
            {
                var index = Layout[x, y];
                if (index == -1) continue;
                var rect = Extensions.GetTileRect(index, 1);
                Raylib.DrawTextureRec(_texture, rect, 8 * new Vector2(x, y), Color.White);
            }

            PaletteShader.End();
            _tools[(int)_activeTool].Update(this);
        }
        Raylib.EndMode2D();
        Raylib.EndTextureMode();

        Raylib.DrawTexturePro(_viewport.Texture, new Rectangle(Vector2.Zero, _viewport.Texture.Width, -_viewport.Texture.Height), new Rectangle(position, _viewportSize, _viewportSize), Vector2.Zero, 0f, Color.White);
        if (ShowGrid)
        {
            Raylib.BeginScissorMode((int)position.X, (int)position.Y, _viewportSize, _viewportSize);
            {
                for (var y = 1; y < TilesetHeight; y++)
                {
                    var p1 = WorldToViewport(new Vector2(0, y * 8));
                    var p2 = WorldToViewport(new Vector2(TilesetWidth * 8, y * 8));
                    Raylib.DrawLineV(p1, p2, Color.Black);
                }

                for (var x = 1; x < TilesetWidth; x++)
                {
                    var p1 = WorldToViewport(new Vector2(x * 8, 0));
                    var p2 = WorldToViewport(new Vector2(x * 8, TilesetHeight * 8));
                    Raylib.DrawLineV(p1, p2, Color.Black);
                }
            }
            Raylib.EndScissorMode();
        }
    }

    private bool _isPanning;
    private Vector2 _lastMousePosition = Vector2.Zero;
    private int[] _paletteIvec;
    private float _iconSize;

    private void UpdateCamera()
    {
        var viewportRect = new Rectangle(_position, _viewportSize, _viewportSize);

        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), viewportRect);

        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0.0f && hovered)
        {
            var zoomFactor = 1.05f;
            var mouseWorldPosBeforeZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), ViewCamera) * (TargetSize / (float)_viewportSize);
            ViewCamera.Zoom *= wheel > 0 ? zoomFactor : 1.0f / zoomFactor;
            var mouseWorldPosAfterZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), ViewCamera) * (TargetSize / (float)_viewportSize);
            ViewCamera.Target += mouseWorldPosBeforeZoom - mouseWorldPosAfterZoom;
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
            var delta = Raylib.GetScreenToWorld2D(_lastMousePosition, ViewCamera) - Raylib.GetScreenToWorld2D(currentMousePosition, ViewCamera);
            ViewCamera.Target += delta * (TargetSize / (float)_viewportSize);
            _lastMousePosition = currentMousePosition;
        }

        // Limit camera position to size of tileset
        // First, make sure it can all fit with current zoom level
        ViewCamera.Zoom = ViewCamera.Zoom.Clamp(TargetSize / (float)(Math.Max(TilesetWidth, TilesetHeight) * 8), float.MaxValue);
        // Clamp view target to texture
        var viewportSizePx = new Vector2(TargetSize / ViewCamera.Zoom);
        ViewCamera.Target = Vector2.Clamp(ViewCamera.Target, Vector2.Zero, new Vector2(Math.Max(TilesetWidth, TilesetHeight)) * 8 - viewportSizePx);
    }

    private void PaletteView(Rectangle area)
    {
        var len = Palette.Length;
        var width = area.Width;
        _iconSize = Math.Min(width / IconsPerColumn, 32f);

        for (var y = 0; y < (int)(len / IconsPerColumn); y++)
        for (var x = 0; x < IconsPerColumn; x++)
        {
            var index = x + y * IconsPerColumn;
            var colRect = new Rectangle(area.X + x * _iconSize, area.Y + y * _iconSize, _iconSize, _iconSize);
            var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), colRect);
            var color = Palette[index].ToColor();
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
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Redo();

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
        if (!ValidCell(position)) return;
        var col = Palette[id].ToColor();
        Raylib.DrawPixelV(position, col);
    }

    public void OutlineCell(Vector2 position, Color color)
    {
        if (!ValidCell(position)) return;
        Raylib.DrawRectangleLinesEx(new Rectangle(position - new Vector2(1 / 8f), new Vector2(5 / 4f)), 1 / 8f, color);
    }

    public bool ValidCell(Vector2 position)
    {
        var inBounds = position.X >= 0 && position.Y >= 0 && position.X < TilesetWidth * 8 &&
                       position.Y < TilesetHeight * 8;
        if (!inBounds) return false;
        var tileX = ((int)position.X / 8);
        var tileY = ((int)position.Y / 8);
        var tile = Layout[tileX, tileY];
        return tile != -1;
    }

    public byte GetCell(Vector2 position)
    {
        var tileX = ((int)position.X / 8);
        var tileY = ((int)position.Y / 8);
        var tile = Layout[tileX, tileY];
        if (tile == -1) return 0;
        return Tileset[tile][(int)position.X % 8, (int)position.Y % 8];
    }

    private void SetPixel(Vector2 position, byte id)
    {
        var tileX = ((int)position.X / 8);
        var tileY = ((int)position.Y / 8);
        var tile = Layout[tileX, tileY];

        if (tile == -1) return;

        var indexI32 = BitConverter.ToInt32([id, id, id, 0xFF]);
        var texturePos = new Vector2((int)position.X % 8, tile * 8 + (int)position.Y % 8);
        Raylib.UpdateTextureRec(_texture, new Rectangle(texturePos, 1, 1), [indexI32]);
        Tileset[tile][(int)position.X % 8, (int)position.Y % 8] = id;
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
        UndoManager.Push(action);
    }
}