using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Project;
using AdvancedLib.RaylibExt;
using AdvancedLib.Serialization.OAM;
using AdvancedLib.Serialization.Objects;
using AdvEditRework.DearImGui;
using AdvEditRework.Shaders;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors.Object;

public class ObjectEditor :  Editor
{
    private readonly Track _track;
    private readonly Texture2D _obstacleGfx;
    private readonly ObstacleOam _obstacleOam;
    private readonly int[] _vecPalette;

    public ObjectEditor(Track track, ObstacleOam oamData)
    {
        _track = track;
        Debug.Assert(_track.ObstacleGfx is not null && _track.ObstaclePalette is not null);
        _obstacleGfx = _track.ObstacleGfx.TilePaletteTexture(8, _track.ObstacleGfx.Length / 8);
        _vecPalette = new int[256 * 3 * 2]; // Space for blank colors when we read a slice of the palette
        var palette = _track.ObstaclePalette.ToIVec3();
        Array.Copy(palette, 0, _vecPalette, 0, 256 * 3);
        _obstacleOam = oamData;
    }
    public override void Update(bool hasFocus)
    {
        Raylib.ClearBackground(Color.White);
        PaletteShader.SetPalette(_vecPalette[..(256 * 3)]);
        PaletteShader.Begin();
            Raylib.DrawTexture(_obstacleGfx, 32, 32, Color.White);
        PaletteShader.End();

        
        var list = _track.Objects.GetObstacles();
        Vector2 pos = new Vector2(32 + 8 * 8 + 4, 32);
        foreach (var obstacle in list)
        {
            if (obstacle.Type is 0 or -1 or -8 or -16) continue;
            var cellData = _obstacleOam.GetObjectDistanceCells(obstacle.Type, obstacle.Parameter);
            float yOffs = 0;
            foreach (var dist in cellData.Distances)
            {
                var size = DrawObstacleCellData(pos, dist, (int)Raylib.GetTime());
                pos.X += size.X + 4;
                if (size.Y > yOffs) yOffs = size.Y;
            }

            pos.Y += yOffs + 4;
            pos.X = 100;
        }

        OptionsWindow();
    }

    
    
    private Vector2 DrawObstacleCellData(Vector2 pos, CellData data, int frame)
    {
        var entry = data.Entries[frame % data.Entries.Count];
        var layout = entry.GetTileGrid();
        var width = layout.GetLength(0);
        var height = layout.GetLength(1);
        var slice = _vecPalette[(entry.Palette * 16 * 3)..((256 + entry.Palette * 16) * 3)];
        PaletteShader.SetPalette(slice);
        PaletteShader.Begin();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var src = Extensions.GetTileRect(layout[x, y], 8);
            
            Raylib.DrawTextureRec(_obstacleGfx, src, pos + new Vector2(y*8,x*8), Color.White);
        }
        PaletteShader.End();

        return new Vector2(height * 8, width * 8);
    }

    private void OptionsWindow()
    {
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;

        var position = new Vector2(4, menuBarHeight + 4);

        var optionsX = 0;
        var optionsRect = new Rectangle(optionsX, menuBarHeight, windowSize.X - optionsX, windowSize.Y - position.Y);

        Raylib.DrawRectangleLinesEx(optionsRect, 2, Color.LightGray);
        ImHelper.BeginEmptyWindow("GfxOptionsWindow", optionsRect);
        
        ImHelper.EndEmptyWindow();
    }

    public override void Dispose()
    {
        Raylib.UnloadTexture(_obstacleGfx);
    }
}