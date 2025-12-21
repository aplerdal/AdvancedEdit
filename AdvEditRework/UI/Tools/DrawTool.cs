using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Undo;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class DrawTool : MapEditorTool
{
    private int _radius = 11;
    private readonly List<TileEntry> _drawnTiles = new();
    public override void Update(MapEditor editor)
    {
        var view = editor.View;
        if (!editor.MouseOverMap || !editor.HasFocus || !editor.SelectedTile.HasValue) return;

        var tile = editor.SelectedTile.Value;
        var drawPoints = GetCirclePoints(view.MouseTilePos, _radius);
        PaletteShader.Begin();
        foreach (var point in drawPoints)
        {
            Raylib.DrawTextureRec(view.Tileset, Extensions.GetTileRect(tile, 16), point * 8, Color.White);
        }
        PaletteShader.End();
        // Raylib.DrawRectangleLinesEx(new Rectangle(view.MouseTilePos * 8 - Vector2.One, new(8 + 2)), 1, Color.White);
        
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (view.MouseOnTrack)
            {
                foreach (var point in drawPoints)
                {
                    var entry = new TileEntry(point, tile);
                    if (!_drawnTiles.Contains(entry) && view.PointOnTrack(point))
                    {
                        view.DrawTile(entry.Position, entry.Tile);
                        _drawnTiles.Add(entry);
                    }
                }
            }
        }
        else if (_drawnTiles.Count > 0)
        {
            editor.UndoManager.Push(view.SetTilesUndoable(_drawnTiles));
            _drawnTiles.Clear();
        }
    }

    List<Vector2> GetCirclePoints(Vector2 center, int radius)
    {
        if (radius == 1) return [center];
        List<Vector2> points = new();
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= radius)
                    points.Add(center + new Vector2(x, y));
        return points;
    }
}