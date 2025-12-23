using System.Numerics;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Undo;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class BucketTool : MapEditorTool
{
    public override void Update(IToolEditable editor)
    {
        if (!editor.ViewportHovered || !editor.Focused || !editor.ActiveIndex.HasValue) return;
        
        editor.DrawCell(editor.CellMousePos, editor.ActiveIndex.Value, Color.White);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && editor.ValidCell(editor.CellMousePos))
        {
            var action = FloodFill(editor, editor.CellMousePos, editor.ActiveIndex.Value);
            if (action is not null)
                editor.PushUndoable(action);
        }
    }

    private static UndoActions? FloodFill(IToolEditable editor, Vector2 pos, byte id)
    {
        var target = editor.GetCell(pos);
        if (target == id) return null;

        HashSet<Vector2> changedCells = [pos];
        Queue<Vector2> stack = new();
        stack.Enqueue(pos);

        while (stack.Count > 0)
        {
            var point = stack.Dequeue();

            int leftBound = (int)point.X;
            while (leftBound >= 0 && editor.GetCell(point with { X = leftBound }) == target)
                leftBound--;

            int rightBound = (int)point.X;
            while (rightBound < editor.GridSize.X && editor.GetCell(point with { X = rightBound }) == target)
                rightBound++;


            // Keep track of if above/below points are in a continuous span
            bool aboveInSpan = false;
            bool belowInSpan = false;
            for (int x = (int)leftBound + 1; x < rightBound; x++)
            {
                changedCells.Add(point with { X = x });

                var above = new Vector2(x, point.Y + 1);
                if (above.Y < editor.GridSize.Y && editor.GetCell(above) == target)
                {
                    if (!aboveInSpan && changedCells.Add(above))
                    {
                        stack.Enqueue(above);
                        aboveInSpan = true;
                    }
                }
                else
                {
                    aboveInSpan = false;
                }

                var below = new Vector2(x, point.Y - 1);
                if (below.Y >= 0 && editor.GetCell(below) == target)
                {
                    if (!belowInSpan && changedCells.Add(below))
                    {
                        stack.Enqueue(below);
                        belowInSpan = true;
                    }
                }
                else
                {
                    belowInSpan = false;
                }
            }
        }
        return editor.SetCellsUndoable(changedCells, id);
    }

}