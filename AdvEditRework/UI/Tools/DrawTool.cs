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
    private int _radius = 1;
    private readonly List<CellEntry> _drawnCells = new();

    public override void Update(IToolEditable editor)
    {
        if (!editor.ViewportHovered || !editor.Focused || !editor.ActiveIndex.HasValue) return;

        var tile = editor.ActiveIndex.Value;
        var drawPoints = GetCirclePoints(editor.CellMousePos, _radius);
        PaletteShader.Begin();
        foreach (var point in drawPoints)
        {
            editor.DrawCell(point, tile, Color.White);
        }

        foreach (var cell in _drawnCells)
        {
            editor.DrawCell(cell.Position, cell.Id, Color.White);
        }

        PaletteShader.End();

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (editor.ViewportHovered)
            {
                foreach (var point in drawPoints)
                {
                    var entry = new CellEntry(point, tile);
                    if (!_drawnCells.Contains(entry) && editor.ValidCell(point))
                    {
                        editor.DrawCell(entry.Position, entry.Id, Color.White);
                        _drawnCells.Add(entry);
                    }
                }
            }
        }
        else if (_drawnCells.Count > 0)
        {
            editor.PushUndoable(editor.SetCellsUndoable(_drawnCells));
            _drawnCells.Clear();
        }
    }

    List<Vector2> GetCirclePoints(Vector2 center, int radius)
    {
        if (radius == 1) return [center];
        List<Vector2> points = new();
        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
            if (x * x + y * y <= radius * radius)
                points.Add(center + new Vector2(x, y));
        return points;
    }
}