using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Editors;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class StampTool : MapEditorTool
{
    public override void Update(MapEditor editor)
    {
        var view = editor.View;
        if (editor.Stamp is not null)
        {
            PaletteShader.Begin();
            foreach (var tile in editor.Stamp)
                Raylib.DrawTextureRec(view.Tileset, Extensions.GetTileRect(tile.Tile, 16), 8 * (view.MouseTilePos + tile.Position), Color.White with { A = 192 });
            PaletteShader.End();

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var offsetEntries = ((TileEntry[])editor.Stamp.Clone()).ToList();
                for (var i = 0; i < offsetEntries.Count; i++)
                {
                    offsetEntries[i] = offsetEntries[i] with { Position = view.MouseTilePos + offsetEntries[i].Position};
                }

                editor.UndoManager.Push(editor.View.SetTilesUndoable(offsetEntries));
            }
        }
    }
}