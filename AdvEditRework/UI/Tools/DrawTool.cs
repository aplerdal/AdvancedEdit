using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.RaylibExt;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Undo;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

public class DrawTool : MapEditorTool
{
    private readonly List<TileEntry> _drawnTiles = new();
    public override void Update(MapEditor editor)
    {
        var view = editor.View;
        if (!editor.SelectedTile.HasValue) return;
        if (!editor.MouseOverMap) return;

        var tile = editor.SelectedTile.Value;

        PaletteShader.Begin();
        Raylib.DrawTextureRec(view.Tileset, Extensions.GetTileRect(tile, 16), view.MouseTilePos * 8, Color.White);
        PaletteShader.End();
        Raylib.DrawRectangleLinesEx(new Rectangle(view.MouseTilePos * 8 - Vector2.One, new(8 + 2)), 1, Color.White);
        
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (view.MouseOnTrack)
            {
                
                var entry = new TileEntry(view.MouseTilePos, tile);
                if (!_drawnTiles.Contains(entry))
                {
                    view.DrawTile(view.MouseTilePos, tile);
                    _drawnTiles.Add(entry);
                }
            }
        }
        else if (_drawnTiles.Count > 0)
        {
            editor.UndoManager.Push(view.SetTilesUndoable(_drawnTiles));
            _drawnTiles.Clear();
        }
    }
}