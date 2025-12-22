using System.Numerics;
using AdvEditRework.UI.Editors;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class Eyedropper : MapEditorTool
{
    public override void Update(IToolEditable editor)
    {
        if (!editor.ViewportHovered || !editor.Focused) return;

        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            editor.ActiveIndex = editor.GetCell(editor.CellMousePos);
            editor.SetTool(MapEditorToolType.Draw);
        }

        editor.OutlineCell(editor.CellMousePos, Color.White);
    }
}