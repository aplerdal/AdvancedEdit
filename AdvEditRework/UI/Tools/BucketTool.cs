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

        Vector2 hoveredTile = editor.CellMousePos;
        var tile = editor.ActiveIndex.Value;
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            // Flood fill
            // TODO
            // var action = FastFloodFill(editor, hoveredTile, tile);
            // if (action is not null)
            //     editor.UndoManager.Push(action);
        }
    }

    private static UndoActions? FastFloodFill(MapEditor editor, Vector2 pos, byte replacement)
    {
        var map = editor.View.Track.Tilemap;
        int width = editor.View.Track.Config.Size.X;
        int height = editor.View.Track.Config.Size.Y;
        byte target = map[pos];

        // return if nothing to be done
        if (target == replacement) return null;

        HashSet<Vector2> changedPoints = new HashSet<Vector2>();
        Stack<Vector2> stack = new Stack<Vector2>();

        stack.Push(pos);

        while (stack.Count > 0)
        {
            var s = stack.Pop();

            // Find left boundary
            int left = (int)s.X;
            while (left >= 0 && map[left, (int)s.Y] == target)
                left--;

            // Find right boundary
            int right = (int)s.X;
            while (right < width && map[right, (int)s.Y] == target)
                right++;

            // Fill the scanline and collect changed points
            for (int i = left + 1; i < right; i++)
            {
                map[i, (int)s.Y] = replacement;
                changedPoints.Add(new(i, s.Y));
            }

            // Check rows above and below for unprocessed sections
            void TryPushRow(int nx, int ny)
            {
                if (ny < 0 || ny >= height) return; // Out of bounds
                bool found = false;
                for (int i = left + 1; i < right; i++)
                {
                    if (map[i, ny] == target)
                    {
                        if (!found)
                        {
                            stack.Push(new(i, ny));
                            found = true;
                        }
                    }
                    else
                    {
                        found = false;
                    }
                }
            }

            TryPushRow(left + 1, (int)s.Y - 1); // Check row above
            TryPushRow(left + 1, (int)s.Y + 1); // Check row below
        }

        return editor.View.SetTilesUndoable(changedPoints, replacement);
    }
}