using System.Numerics;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Editors;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class SelectionTool : MapEditorTool
{
    private bool _dragging;
    private Vector2 _start;

    public override void Update(IToolEditable editor)
    {
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            var trackSize = editor.GridSize - Vector2.One;
            if (!_dragging && editor.ViewportHovered)
            {
                _dragging = true;
                _start = Vector2.Clamp(editor.CellMousePos, Vector2.Zero, trackSize);
            }

            if (!_dragging) return;
            var p1 = _start;
            var p2 = Vector2.Clamp(editor.CellMousePos, Vector2.Zero, trackSize);
            var min = new Vector2(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var max = new Vector2(Math.Max(p1.X, p2.X) + 1, Math.Max(p1.Y, p2.Y) + 1);
            var selRect = new Rectangle(min * editor.CellSize, (max - min) * editor.CellSize);
            Raylib.DrawRectangleLinesEx(selRect, 2, Color.White);
        }
        else if (_dragging)
        {
            _dragging = false;
            var p1 = _start;
            var trackSize = editor.GridSize - Vector2.One;
            var p2 = Vector2.Clamp(editor.CellMousePos, Vector2.Zero, trackSize);
            var min = new Vector2(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var max = new Vector2(Math.Max(p1.X, p2.X) + 1, Math.Max(p1.Y, p2.Y) + 1);
            var selRect = new Rectangle(min, max - min);
            var stamp = new CellEntry[(int)selRect.Width * (int)selRect.Height];
            for (int y = 0; y < (int)selRect.Height; y++)
            for (int x = 0; x < (int)selRect.Width; x++)
                stamp[y * (int)selRect.Width + x] = new CellEntry(new Vector2(x, y), editor.GetCell(new (x + selRect.X, y + selRect.Y)));
            editor.Stamp = stamp;
            editor.SetTool(MapEditorToolType.Stamp);
        }
    }
}