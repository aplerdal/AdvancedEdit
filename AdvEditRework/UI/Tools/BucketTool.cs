using AdvEditRework.UI.Editors;

namespace AdvEditRework.UI.Tools;

public class BucketTool : MapEditorTool
{
    public override void Update(IToolEditable editor)
    {
        throw new NotImplementedException();
        // if (!editor.ViewportHovered || !editor.Focused || !editor.ActiveIndex.HasValue) return;
        //
        // if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        // {
        //     // Flood fill
        //     // TODO
        //     // var action = FastFloodFill(editor, hoveredTile, tile);
        //     // if (action is not null)
        //     //     editor.UndoManager.Push(action);
        // }
    }
}