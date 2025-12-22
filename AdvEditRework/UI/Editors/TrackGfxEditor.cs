using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvEditRework.DearImGui;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

public enum TrackGraphic
{
    Tileset,
    Minimap,
}

public class TrackGfxEditor : Editor
{
    private readonly Track _track;
    private TrackGraphic _activeGraphic = TrackGraphic.Tileset;
    private TilesetEditor _tilesetEditor;

    private readonly Palette _uiPalette = new Palette(
        [
            new(0x7C1F), new(0xFFFF), new(0xFFD2), new(0xFB6E),
            new(0xDE69), new(0x3D82), new(0x83FF), new(0x83CC),
            new(0x8256), new(0x82BF), new(0x01FF), new(0xC210),
            new(0xD6B5), new(0xE318), new(0xFFFF), new(0x0000)
        ]
    );

    public TrackGfxEditor(Track track)
    {
        _track = track;
        Raylib.SetMouseCursor(MouseCursor.Default);
        _activeGraphic = TrackGraphic.Tileset;
        _tilesetEditor = new TilesetEditor(_track.Tileset, _track.TilesetPalette);
    }

    public override void Update(bool hasFocus)
    {
        Raylib.ClearBackground(Color.White);
        GfxSelectorPanel(hasFocus);
    }

    void GfxSelectorPanel(bool hasFocus)
    {
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;

        var position = new Vector2(4, menuBarHeight + 4);

        _tilesetEditor.Update(position, hasFocus);

        var optionsX = position.X + _tilesetEditor.RenderSize.X + 4;
        var optionsRect = new Rectangle(optionsX, menuBarHeight, windowSize.X - optionsX, windowSize.Y - position.Y);

        ImHelper.BeginEmptyWindow("GfxOptionsWindow", optionsRect);

        var trackGraphics = Enum.GetValues(typeof(TrackGraphic)).Cast<TrackGraphic>();
        var graphicName = Enum.GetName(_activeGraphic);
        if (ImGui.BeginCombo("Active Graphics", graphicName))
        {
            foreach (var graphic in trackGraphics)
            {
                if (ImGui.Selectable(Enum.GetName(graphic)))
                {
                    if (_activeGraphic == graphic) continue;
                    _activeGraphic = graphic;
                    switch (graphic)
                    {
                        case TrackGraphic.Minimap:
                            _tilesetEditor = new TilesetEditor(_track.Minimap, _uiPalette);
                            break;
                        case TrackGraphic.Tileset:
                            _tilesetEditor = new TilesetEditor(_track.Tileset, _track.TilesetPalette);
                            break;
                    }
                }
            }

            ImGui.EndCombo();
        }

        ImHelper.EndEmptyWindow();
    }

    public override void Dispose()
    {
        _tilesetEditor.Dispose();
    }
}