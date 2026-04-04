using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.DearImGui;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Editors.Gfx;
using AdvEditRework.UI.Undo;
using GifLib;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

public enum TrackGraphic
{
    Tileset,
    Minimap,
    Cover,
}

public class TrackGfxEditor : Editor
{
    public static readonly Dictionary<string, string> ImageFilter = new() { { "GIF Image", "gif" }, { "All files", "*" } };

    private readonly Track _track;
    private TrackGraphic _activeGraphic;
    private TilesetEditor _tileEditor;
    private bool _lockedPalette;

    private BgrColor _oldPaletteColor;
    private bool _modifyingColor;

    private readonly Palette _uiPalette = new(
        [
            new BgrColor(0x7C1F), new BgrColor(0xFFFF), new BgrColor(0xFFD2), new BgrColor(0xFB6E),
            new BgrColor(0xDE69), new BgrColor(0x3D82), new BgrColor(0x83FF), new BgrColor(0x83CC),
            new BgrColor(0x8256), new BgrColor(0x82BF), new BgrColor(0x01FF), new BgrColor(0xC210),
            new BgrColor(0xD6B5), new BgrColor(0xE318), new BgrColor(0xFFFF), new BgrColor(0x0000)
        ]
    );

    public TrackGfxEditor(Track track)
    {
        _track = track;
        _activeGraphic = TrackGraphic.Tileset;
        _tileEditor = new TilesetEditor(_track.Tileset, _track.TilesetPalette);
    }

    public override void Update(bool hasFocus)
    {
        Raylib.ClearBackground(Color.White);
        GfxSelectorPanel(hasFocus);
    }

    private void GfxSelectorPanel(bool hasFocus)
    {
        var quarterScreen = Raylib.GetScreenWidth() / 4f;
        var windowSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;

        var position = new Vector2(0, menuBarHeight);
        var viewportArea = new Rectangle(position, quarterScreen * 2, windowSize.Y - position.Y);

        _tileEditor.Update(viewportArea, hasFocus);
        _tileEditor.UpdatePaletteView(new Rectangle(quarterScreen * 2 + 4, menuBarHeight, quarterScreen - 8, windowSize.Y - menuBarHeight));

        var optionsX = quarterScreen * 3;
        var optionsRect = new Rectangle(optionsX, menuBarHeight, quarterScreen, windowSize.Y - menuBarHeight);

        Raylib.DrawRectangleLinesEx(optionsRect, 2, Color.LightGray);
        ImHelper.BeginEmptyWindow("GfxOptionsWindow", optionsRect);

        var trackGraphics = Enum.GetValues(typeof(TrackGraphic)).Cast<TrackGraphic>();
        var graphicName = Enum.GetName(_activeGraphic);
        if (ImGui.BeginCombo("Active Graphics", graphicName))
        {
            foreach (var graphic in trackGraphics)
                if (graphic == TrackGraphic.Cover && _track.CoverArt is null)
                {
                    ImGui.BeginDisabled();
                    ImGui.Selectable(Enum.GetName(graphic));
                    ImGui.EndDisabled();
                }
                else
                {
                    if (ImGui.Selectable(Enum.GetName(graphic)))
                    {
                        if (_activeGraphic == graphic) continue;
                        _activeGraphic = graphic;
                        _tileEditor.Dispose();
                        _tileEditor = graphic switch
                        {
                            TrackGraphic.Minimap => new TilesetEditor(_track.Minimap, _uiPalette),
                            TrackGraphic.Tileset => new TilesetEditor(_track.Tileset, _track.TilesetPalette),
                            TrackGraphic.Cover => new TilesetEditor(_track.CoverArt!, _track.CoverPalette!, 10, 8, 1),
                            _ => throw new ArgumentOutOfRangeException(nameof(graphic))
                        };
                        _lockedPalette = graphic == TrackGraphic.Minimap;
                    }
                }

            ImGui.EndCombo();
        }

        ShowOptions();
        ShowPaletteOptions();

        ImHelper.EndEmptyWindow();
    }

    private void ShowPaletteOptions()
    {
        ImGui.SeparatorText("Palette");
        if (!_tileEditor.ActiveIndex.HasValue || _lockedPalette)
        {
            ImGui.BeginDisabled();
            float _ = 0;
            ImGui.ColorPicker3("Edit Selected Color:", ref _);
            ImGui.EndDisabled();
            return;
        }

        var color = _tileEditor.Palette[_tileEditor.ActiveIndex.Value];
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
                var capturedIndex = _tileEditor.ActiveIndex.Value;
                _tileEditor.UndoManager.Push(new UndoActions(
                    () =>
                    {
                        _tileEditor.Palette[capturedIndex] = capturedNew;
                        _tileEditor.RefreshPalette();
                    },
                    () =>
                    {
                        _tileEditor.Palette[capturedIndex] = capturedOld;
                        _tileEditor.RefreshPalette();
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

        _tileEditor.Palette[_tileEditor.ActiveIndex.Value] = newColor;
        _tileEditor.RefreshPalette();
    }

    private void ShowOptions()
    {
        ImGui.SeparatorText("Options");
        ImGui.Checkbox("Show Grid?", ref _tileEditor.ShowGrid);

        if (ImGui.Button("Import"))
        {
            var status = Nfd.OpenDialog(out var path, ImageFilter, "tiles.gif");
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                var gif = GifDocument.Load(path);
                gif.LoadGifToGBA(ref _tileEditor.Tileset, ref _tileEditor.Palette, _tileEditor.Layout);
                _tileEditor.ReloadTileset();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Export"))
        {
            var status = Nfd.SaveDialog(out var path, ImageFilter, "tiles.gif");
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                var gif = _tileEditor.Tileset.ToGif(_tileEditor.Palette, _tileEditor.Layout);
                gif.Save(path);
            }
        }
    }

    public override void Dispose()
    {
        _tileEditor.Dispose();
    }
}