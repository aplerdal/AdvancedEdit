using System.Numerics;
using AdvEditRework.UI.Editors;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class RectangleTool : MapEditorTool
{
    private bool _dragging;
    private Vector2 _start;

    public override void Update(IToolEditable editor)
    {
        if (!editor.ActiveIndex.HasValue || !editor.Focused) return;
        
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (!_dragging)
            {
                _dragging = true;
                _start = Vector2.Clamp(editor.CellMousePos, Vector2.Zero, editor.GridSize - Vector2.One);
            }

            if (!_dragging) return;
            var p1 = _start;
            var p2 = Vector2.Clamp(editor.CellMousePos, Vector2.Zero, editor.GridSize - Vector2.One);
            var min = new Vector2(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var max = new Vector2(Math.Max(p1.X, p2.X) + 1, Math.Max(p1.Y, p2.Y) + 1);
            var selRect = new Rectangle(min * editor.CellSize, (max - min) * editor.CellSize);
            Raylib.DrawRectangleLinesEx(selRect, 2, Color.White);
        }
        else if (_dragging)
        {
            _dragging = false;
            var p1 = _start;
            var p2 = Vector2.Clamp(editor.CellMousePos, Vector2.Zero, editor.GridSize - Vector2.One);
            var min = new Vector2(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var max = new Vector2(Math.Max(p1.X, p2.X) + 1, Math.Max(p1.Y, p2.Y) + 1);
            var selRect = new Rectangle(min, max - min);
            editor.PushUndoable(editor.SetCellsUndoable(selRect, editor.ActiveIndex.Value));
        }
    }
}