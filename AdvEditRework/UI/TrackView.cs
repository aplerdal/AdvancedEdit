using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Undo;
using Raylib_cs;

namespace AdvEditRework.UI;

public enum EditMode
{
    Map,
    Ai,
    Graphics,
}

public class TrackView : IDisposable
{
    public Texture2D Tileset;
    private readonly int[] _shaderPalette;
    public Camera2D Camera;
    public readonly Track Track;
    private RenderTexture2D _trackTexture;
    public Vector2 MouseWorldPos { get; private set; } = Vector2.Zero;
    public Vector2 MouseTilePos { get; private set; } = Vector2.Zero;
    public delegate void DrawCallback();

    public DrawCallback? DrawInTrack;

    public TrackView(Track track)
    {
        Track = track;
        Tileset = track.Tileset.TilePaletteTexture(16, 16);
        var palette = track.TilesetPalette;
        _shaderPalette = palette.ToIVec3();
        Camera = new Camera2D
        {
            Offset = new(0),
            Target = new(0),
            Rotation = 0.0f,
            Zoom = 1.0f
        };
        DrawInTrack = null;
        RegenMapTextures();
    }

    private void RegenMapTextures()
    {
        var tilemap = Track.Tilemap;
        if (!Raylib.IsRenderTextureValid(_trackTexture) || (_trackTexture.Texture.Width != tilemap.Width * 8 || _trackTexture.Texture.Height != tilemap.Height * 8))
        {
            Raylib.UnloadRenderTexture(_trackTexture);
            _trackTexture = Raylib.LoadRenderTexture(tilemap.Width * 8, tilemap.Height * 8);
            if (!Raylib.IsRenderTextureValid(_trackTexture)) throw new InvalidOperationException("Failed to create track texture");
        }

        Raylib.BeginTextureMode(_trackTexture);
        for (int y = 0; y < tilemap.Height; y++)
        for (int x = 0; x < tilemap.Width; x++)
            Raylib.DrawTextureRec(Tileset, Extensions.GetTileRect(tilemap[x, y], 16), new Vector2(x, y) * 8, Color.White);
        Raylib.EndTextureMode();
    }

    private bool _isPanning;
    private Vector2 _lastMousePosition = Vector2.Zero;

    void UpdateCamera()
    {
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0.0f)
        {
            float zoomFactor = 1.05f;
            Vector2 mouseWorldPosBeforeZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
            Camera.Zoom *= (wheel > 0) ? zoomFactor : 1.0f / zoomFactor;
            Vector2 mouseWorldPosAfterZoom = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
            Camera.Target += mouseWorldPosBeforeZoom - mouseWorldPosAfterZoom;
        }

        // Handle pan
        if (Raylib.IsMouseButtonPressed(MouseButton.Middle))
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
            Vector2 delta = Raylib.GetScreenToWorld2D(_lastMousePosition, Camera) - Raylib.GetScreenToWorld2D(currentMousePosition, Camera);
            Camera.Target += delta;
            _lastMousePosition = currentMousePosition;
        }
    }

    public void Draw()
    {
        MouseWorldPos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
        MouseTilePos = new Vector2((int)(MouseWorldPos.X / 8), (int)(MouseWorldPos.Y / 8));

        PaletteShader.SetPalette(_shaderPalette);
        UpdateCamera();
        Raylib.BeginMode2D(Camera);
        {
            PaletteShader.Begin();
            Raylib.DrawTextureRec(_trackTexture.Texture, new Rectangle(0, 0, _trackTexture.Texture.Width, -_trackTexture.Texture.Height), new Vector2(0), Color.White);
            PaletteShader.End();
            DrawInTrack?.Invoke();
        }
        Raylib.EndMode2D();
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(Tileset);
        Raylib.UnloadRenderTexture(_trackTexture);
    }

    public bool PointOnTrack(Vector2 point)
    {
        return (point.X >= 0 && point.Y >= 0 && point.X < Track.Tilemap.Width && point.Y < Track.Tilemap.Height);
    }

    private void BeginTileMode()
    {
        Raylib.BeginTextureMode(_trackTexture);
    }

    /// <summary>
    /// Draws a tile to the tilemap. You must call <see cref="BeginTileMode"/> before using this and
    /// call <see cref="EndTileMode"/> after drawing all tiles.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tile"></param>
    private void InternalDrawTile(Vector2 position, byte tile)
    {
        Raylib.DrawTextureRec(Tileset, Extensions.GetTileRect(tile, 16), position * 8, Color.White);
    }

    private void EndTileMode()
    {
        Raylib.EndTextureMode();
    }

    /// <summary>
    /// Draws a tile but does NOT set the tile
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="tile"></param>
    public void DrawTile(Vector2 pos, byte tile)
    {
        BeginTileMode();
        InternalDrawTile(pos, tile);
        EndTileMode();
    }

    private void DrawSetTile(Vector2 pos, byte tile)
    {
        Track.Tilemap[pos] = tile;
        InternalDrawTile(pos, tile);
    }

    public void SetTile(Vector2 pos, byte tile)
    {
        BeginTileMode();
        DrawSetTile(pos, tile);
        EndTileMode();
    }

    private void SetTiles(Rectangle area, byte tile)
    {
        BeginTileMode();
        for (var y = area.Y; y < area.Y + area.Height; y++)
        for (var x = area.X; x < area.X + area.Width; x++)
            DrawSetTile(new Vector2(x, y), tile);
        EndTileMode();
    }

    private void SetTiles(List<CellEntry> tiles)
    {
        BeginTileMode();
        foreach (var entry in tiles)
        {
            DrawSetTile(entry.Position, entry.Id);
        }

        EndTileMode();
    }

    private void SetTiles(byte[,] tiles, Vector2 position)
    {
        BeginTileMode();
        for (var y = 0; y < tiles.GetLength(1); y++)
        for (var x = 0; x < tiles.GetLength(0); x++)
            DrawSetTile(position + new Vector2(x, y), tiles[x, y]);
        EndTileMode();
    }

    private void SetTiles(HashSet<Vector2> positions, byte tile)
    {
        BeginTileMode();
        foreach (var position in positions)
            DrawSetTile(position, tile);
        EndTileMode();
    }

    public UndoActions SetTilesUndoable(HashSet<Vector2> positions, byte tile)
    {
        var positionsClone = new HashSet<Vector2>(positions);
        var oldTiles = new List<CellEntry>(positions.Count);
        BeginTileMode();
        foreach (var pos in positions)
        {
            oldTiles.Add(new CellEntry(pos, Track.Tilemap[pos]));
            DrawSetTile(pos, tile);
        }

        EndTileMode();
        return new UndoActions(() => SetTiles(positionsClone, tile), () => SetTiles(oldTiles));
    }

    public UndoActions SetTilesUndoable(List<CellEntry> tiles)
    {
        var tilesClone = new List<CellEntry>(tiles);
        var oldTiles = new List<CellEntry>(tiles.Count);
        BeginTileMode();
        foreach (var entry in tiles)
        {
            oldTiles.Add(entry with { Id = Track.Tilemap[entry.Position] });
            DrawSetTile(entry.Position, entry.Id);
        }

        EndTileMode();
        return new UndoActions(() => SetTiles(tilesClone), () => SetTiles(oldTiles));
    }

    public UndoActions SetTilesUndoable(Rectangle area, byte tile)
    {
        var oldTiles = new byte[(int)area.Width, (int)area.Height];
        for (var y = area.Y; y < area.Y + area.Height; y++)
        for (var x = area.X; x < area.X + area.Width; x++)
            oldTiles[(int)(x - area.X), (int)(y - area.Y)] = Track.Tilemap[(int)x, (int)y];
        BeginTileMode();
        for (var y = area.Y; y < area.Y + area.Height; y++)
        for (var x = area.X; x < area.X + area.Width; x++)
            DrawSetTile(new Vector2(x, y), tile);
        EndTileMode();
        return new UndoActions(() => SetTiles(area, tile), () => SetTiles(oldTiles, area.Position));
    }
}