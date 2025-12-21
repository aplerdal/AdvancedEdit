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

    public override void Update(MapEditor editor)
    {
        var view = editor.View;
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            var trackSize = new Vector2(view.Track.Config.Size.X, view.Track.Config.Size.Y) * 128 - Vector2.One;
            if (!_dragging && editor.MouseOverMap)
            {
                _dragging = true;
                _start = Vector2.Clamp(view.MouseTilePos, Vector2.Zero, trackSize);
            }

            if (!_dragging) return;
            var p1 = _start;
            var p2 = Vector2.Clamp(view.MouseTilePos, Vector2.Zero, trackSize);
            var min = new Vector2(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var max = new Vector2(Math.Max(p1.X, p2.X) + 1, Math.Max(p1.Y, p2.Y) + 1);
            var selRect = new Rectangle(min * 8, (max - min) * 8);
            Raylib.DrawRectangleLinesEx(selRect, 2 * Settings.Shared.UIScale, Color.White);
        }
        else if (_dragging)
        {
            _dragging = false;
            var p1 = _start;
            var trackSize = new Vector2(view.Track.Config.Size.X, view.Track.Config.Size.Y) * 128 - Vector2.One;
            var p2 = Vector2.Clamp(view.MouseTilePos, Vector2.Zero, trackSize);
            var min = new Vector2(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            var max = new Vector2(Math.Max(p1.X, p2.X) + 1, Math.Max(p1.Y, p2.Y) + 1);
            var selRect = new Rectangle(min, max - min);
            var stamp = new TileEntry[(int)selRect.Width * (int)selRect.Height];
            for (int y = 0; y < (int)selRect.Height; y++)
            for (int x = 0; x < (int)selRect.Width; x++)
                stamp[y * (int)selRect.Width + x] = new TileEntry(new Vector2(x, y), editor.View.Track.Tilemap[(int)(x + selRect.X), (int)(y + selRect.Y)]);
            editor.Stamp = stamp;
            editor.SetTool(MapEditorToolType.Stamp);
        }
    }
}