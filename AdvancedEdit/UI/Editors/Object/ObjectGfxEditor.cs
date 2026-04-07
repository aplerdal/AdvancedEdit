using System.Diagnostics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvancedLib.Serialization.Objects;
using AdvEditRework.DearImGui;
using AdvEditRework.UI.Editors.Gfx;
using GifLib;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.UI.Editors.Object;

public class ObjectGfxEditor : Editor
{
    private TilesetEditor _editor;
    private Palette _basePalette;
    private byte _palette;

    private ExceptionPopup? _exceptionPopup;

    public ObjectGfxEditor(DistanceCellData obstacle, Tileset tileset, Palette palette)
    {
        int width = 0, height = 0;
        _basePalette = palette;
        _palette = obstacle.Distances[0].Entries[0].Palette;
        var tilePalette = new Palette(palette[(_palette * 16)..((_palette + 1) * 16)]);
        int[] widths = new int[obstacle.Distances.Length];
        for (var i = 0; i < obstacle.Distances.Length; i++)
        {
            int colHeight = 0;
            var cellData = obstacle.Distances[i];
            int maxDistWidth = 0;
            foreach (var frame in cellData.Entries)
            {
                var grid = frame.GetTileGrid();
                colHeight += grid.GetLength(1);
                var gw = grid.GetLength(0);
                if (gw > maxDistWidth) maxDistWidth = gw;
            }

            widths[i] = maxDistWidth;
            width += maxDistWidth;
            if (colHeight > height) height = colHeight;
        }

        var layout = new int[width, height];
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
            layout[i, j] = -1;
        var xPos = 0;
        for (var i = 0; i < obstacle.Distances.Length; i++)
        {
            var cellData = obstacle.Distances[i];
            var yPos = 0;
            foreach (var frame in cellData.Entries)
            {
                var grid = frame.GetTileGrid();
                for (int y = 0; y < grid.GetLength(1); y++)
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    layout[xPos + x, yPos + y] = grid[x, y];
                }

                yPos += grid.GetLength(1);
            }

            xPos += widths[i];
        }

        _editor = new TilesetEditor(tileset, tilePalette, layout);
    }

    public override void Update(bool hasFocus)
    {
        hasFocus = hasFocus && (!(_exceptionPopup?.Open ?? false));
        _exceptionPopup?.Update();
        var quarterScreen = Raylib.GetScreenWidth() / 4f;
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
        var height = Raylib.GetScreenHeight() - menuBarHeight;
        var area = new Rectangle(0, menuBarHeight, 2 * quarterScreen, height);
        _editor.Update(area, hasFocus);
        var paletteArea = new Rectangle(2 * quarterScreen + 4, menuBarHeight, quarterScreen - 8, height / 2);
        _editor.UpdatePaletteView(paletteArea);
        var optionsArea = new Rectangle(2 * quarterScreen + 4, menuBarHeight + height / 2, quarterScreen - 8, height / 2);
        if (ImHelper.BeginEmptyWindow("gfxEditorOptions", optionsArea))
        {
            ShowOptions();
        }

        ImHelper.EndEmptyWindow();
    }

    private void ShowOptions()
    {
        ImGui.SeparatorText("Options");
        ImGui.Checkbox("Show Grid?", ref _editor.ShowGrid);

        if (ImGui.Button("Import"))
        {
            var status = Nfd.OpenDialog(out var path, TrackGfxEditor.ImageFilter, "tiles.gif");
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                var gif = GifDocument.Load(path);
                try
                {
                    gif.LoadGifToGba(ref _editor.Tileset, ref _editor.Palette, _editor.Layout);
                    UpdatePalette();
                    _editor.ReloadTileset();
                }
                catch (InvalidOperationException e)
                {
                    _exceptionPopup = new ExceptionPopup("Import Error", e);
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Export"))
        {
            var status = Nfd.SaveDialog(out var path, TrackGfxEditor.ImageFilter, "tiles.gif");
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                var gif = _editor.Tileset.ToGif(_editor.Palette, _editor.Layout);
                gif.Save(path);
            }
        }
    }

    private void UpdatePalette()
    {
        var newPal = _editor.Palette;
        var j = 0;
        for (int i = _palette * 16; i < (_palette + 1) * 16; i++)
        {
            _basePalette[i] = newPal[j++];
        }
    }

    public override void Dispose()
    {
        _editor.Dispose();
    }
}