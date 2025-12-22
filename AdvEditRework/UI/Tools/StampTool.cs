using AdvEditRework.UI.Editors;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class StampTool : MapEditorTool
{
    public override void Update(IToolEditable editor)
    {
        if (!editor.Focused || !editor.ViewportHovered) return;

        if (editor.Stamp is not null)
        {
            foreach (var tile in editor.Stamp)
                editor.DrawCell(editor.CellMousePos + tile.Position, tile.Id, Color.White with { A = 192 });

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                List<CellEntry> offset = new List<CellEntry>();

                foreach (var entry in editor.Stamp)
                {
                    if (editor.ValidCell(editor.CellMousePos + entry.Position))
                        offset.Add(entry with { Position = editor.CellMousePos + entry.Position });
                }

                editor.PushUndoable(editor.SetCellsUndoable(offset));
            }
        }
    }
}