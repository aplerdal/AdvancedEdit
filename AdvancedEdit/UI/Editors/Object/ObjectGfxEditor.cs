using System.Diagnostics;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvancedLib.Serialization.Objects;
using AdvEditRework.DearImGui;
using AdvEditRework.UI.Editors.Gfx;
using AdvEditRework.UI.Undo;
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
    private BgrColor _oldPaletteColor;
    private bool _modifyingColor;

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
        var paletteEditPos = _editor.UpdatePaletteView(paletteArea);
        ImHelper.BeginEmptyWindow("gfxEditorOptions", new Rectangle(paletteEditPos, quarterScreen - 8, (height-menuBarHeight) - paletteEditPos.Y));
        ShowPaletteOptions();
        ShowOptions();
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
                try
                {
                    var gif = GifDocument.Load(path);
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

    private void ShowPaletteOptions()
    {
        ImGui.SeparatorText("Palette");
        if (!_editor.ActiveIndex.HasValue)
        {
            ImGui.BeginDisabled();
            float _ = 0;
            ImGui.ColorPicker3("Edit Selected Color:", ref _);
            ImGui.EndDisabled();
            return;
        }

        var color = _editor.Palette[_editor.ActiveIndex.Value];
        float[] colors = [color.R5 * 8 / 255f, color.G5 * 8 / 255f, color.B5 * 8 / 255f];
        float[] colorsOld = [color.R5 * 8 / 255f, color.G5 * 8 / 255f, color.B5 * 8 / 255f];
        unsafe
        {
            fixed (float* colorPtr = colors)
            {
                ImGui.ColorPicker3("Edit Selected Color:", colorPtr);
            }
        }

        var newColor = new BgrColor(colors[0], colors[1], colors[2]);
        if (colorsOld[0] == colors[0] && colorsOld[1] == colors[1] && colorsOld[2] == colors[2])
        {
            if (_modifyingColor)
            {
                var capturedOld = _oldPaletteColor; // capture the value now
                var capturedNew = newColor;
                var capturedIndex = _editor.ActiveIndex.Value;
                _editor.UndoManager.Push(new UndoActions(
                    () =>
                    {
                        _editor.Palette[_editor.ActiveIndex.Value] = newColor;
                        _basePalette[_editor.ActiveIndex.Value] = newColor;
                        _editor.RefreshPalette();
                    },
                    () =>
                    {
                        _editor.Palette[_editor.ActiveIndex.Value] = capturedOld;
                        _basePalette[_editor.ActiveIndex.Value] = capturedOld;
                        _editor.RefreshPalette();
                    }
                ));
                _modifyingColor = false;
            }

            return;
        }

        if (!_modifyingColor)
        {
            _oldPaletteColor = color;
            _modifyingColor = true;
        }

        _editor.Palette[_editor.ActiveIndex.Value] = newColor;
        _basePalette[_editor.ActiveIndex.Value] = newColor;
        _editor.RefreshPalette();
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