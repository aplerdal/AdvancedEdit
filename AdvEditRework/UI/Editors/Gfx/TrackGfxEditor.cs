using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvEditRework.DearImGui;
using AdvEditRework.UI.Editors.Gfx;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

public enum TrackGraphic
{
    Tileset,
    Minimap,
    Cover,
    // Obstacles,
}

public class TrackGfxEditor : Editor
{
    private readonly Track _track;
    private TrackGraphic _activeGraphic;
    private TilesetEditor _tilesetEditor;

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
        _tilesetEditor = new TilesetEditor(_track.Tileset, _track.TilesetPalette);
    }

    public override void Update(bool hasFocus)
    {
        Raylib.ClearBackground(Color.White);
        GfxSelectorPanel(hasFocus);
    }

    private void GfxSelectorPanel(bool hasFocus)
    {
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;

        var position = new Vector2(4, menuBarHeight + 4);

        _tilesetEditor.Update(position, hasFocus);

        var optionsX = position.X + _tilesetEditor.RenderSize.X + 4;
        var optionsRect = new Rectangle(optionsX, menuBarHeight, windowSize.X - optionsX, windowSize.Y - position.Y);

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
                        _tilesetEditor.Dispose();
                        _tilesetEditor = graphic switch
                        {
                            TrackGraphic.Minimap => new TilesetEditor(_track.Minimap, _uiPalette, true),
                            TrackGraphic.Tileset => new TilesetEditor(_track.Tileset, _track.TilesetPalette),
                            TrackGraphic.Cover => new TilesetEditor(_track.CoverArt!, _track.CoverPalette!, 10, 8, 1),
                            _ => throw new ArgumentOutOfRangeException(nameof(graphic))
                        };
                    }
                }

            ImGui.EndCombo();
        }

        _tilesetEditor.ShowOptions();
        _tilesetEditor.ShowPaletteOptions();

        ImHelper.EndEmptyWindow();
    }

    public override void Dispose()
    {
        _tilesetEditor.Dispose();
    }
}