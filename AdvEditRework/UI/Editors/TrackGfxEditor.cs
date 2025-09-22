using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.DearImGui;
using AdvEditRework.Shaders;
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
    }
    public override void Init()
    {
        Raylib.SetMouseCursor(MouseCursor.Default);
        _tilesetEditor = new TilesetEditor(_track.Minimap, _uiPalette);
    }

    public override void Update()
    {
        Raylib.ClearBackground(Color.White);
        GfxSelectorPanel();
    }

    void GfxSelectorPanel()
    {
        var scale = Settings.Shared.UIScale;
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
        var panelRect = new Rectangle(0, menuBarHeight, windowSize.X, windowSize.Y - menuBarHeight);
        Raylib.DrawRectangleRec(panelRect, Color.LightGray);
        ImHelper.BeginEmptyWindow("GfxEditorWindow", panelRect);

        var trackGraphics = Enum.GetValues(typeof(TrackGraphic)).Cast<TrackGraphic>();
        var graphicName = Enum.GetName(_activeGraphic);
        if (ImGui.BeginCombo("Active Graphics", graphicName))
        {
            foreach (var graphic in trackGraphics)
            {
                if (ImGui.Selectable(Enum.GetName(graphic)))
                    _activeGraphic = graphic;
            }
            ImGui.EndCombo();
        }

        _tilesetEditor.Update(ImGui.GetCursorScreenPos());
        
        ImHelper.EndEmptyWindow();
    }
}