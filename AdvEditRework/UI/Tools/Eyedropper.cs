using System.Numerics;
using AdvEditRework.UI.Editors;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class Eyedropper : MapEditorTool
{
    public override void Update(MapEditor editor)
    {
        Vector2 hoveredTile = editor.View.MouseTilePos;
        if (!editor.View.MouseOnTrack || !editor.MouseOverMap) return;
        
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            editor.SelectedTile = editor.View.Track.Tilemap[hoveredTile];
        }
        Raylib.DrawRectangleLinesEx(new Rectangle(editor.View.MouseTilePos * 8 - Vector2.One, new(10)), 1, Color.White);
    }
}