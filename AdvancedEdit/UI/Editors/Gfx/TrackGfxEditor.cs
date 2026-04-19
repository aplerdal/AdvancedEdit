using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvancedLib.RaylibExt;
using AdvEditRework.DearImGui;
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
    TrackName,
}

public class TrackGfxEditor : Editor
{
    public static readonly Dictionary<string, string> ImageFilter = new() { { "GIF Image", "gif" }, { "All files", "*" } };

    private readonly Track _track;
    private readonly TrackView _view;
    private TilesetEditor _tileEditor;
    private BgrColor _oldPaletteColor;
    private ExceptionPopup? _exceptionPopup;
    private TrackGraphic _activeGraphic;
    private bool _lockedPalette;
    private bool _modifyingColor;

    private bool _overlayVisible;
    private RenderTexture2D? _overlay;

    private readonly Palette _uiPalette = new(
        [
            new BgrColor(0x7C1F), new BgrColor(0xFFFF), new BgrColor(0xFFD2), new BgrColor(0xFB6E),
            new BgrColor(0xDE69), new BgrColor(0x3D82), new BgrColor(0x83FF), new BgrColor(0x83CC),
            new BgrColor(0x8256), new BgrColor(0x82BF), new BgrColor(0x01FF), new BgrColor(0xC210),
            new BgrColor(0xD6B5), new BgrColor(0xE318), new BgrColor(0xFFFF), new BgrColor(0x0000)
        ]
    );

    private readonly Palette _nameGfxPalette = new(
        [
            new BgrColor(0xCBF3), new BgrColor(0x825F), new BgrColor(0x00FD), new BgrColor(0x0000),
            new BgrColor(0x480D), new BgrColor(0xABFF), new BgrColor(0x8ED6), new BgrColor(0x0000),
            new BgrColor(0x010A), new BgrColor(0x0000), new BgrColor(0x0000), new BgrColor(0x0000),
            new BgrColor(0x0000), new BgrColor(0x0000), new BgrColor(0xFA4D), new BgrColor(0xFA4D)
        ]
    );

    public TrackGfxEditor(TrackView view)
    {
        _view = view;
        _track = view.Track;
        _activeGraphic = TrackGraphic.Tileset;
        _tileEditor = new TilesetEditor(_track.Tileset, _track.TilesetPalette);
    }

    public override void Update(bool hasFocus)
    {
        hasFocus = hasFocus && (!(_exceptionPopup?.Open ?? false));
        _exceptionPopup?.Update();
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
                            TrackGraphic.TrackName => new TilesetEditor(_track.TrackNameGfx, _nameGfxPalette, 12, 2),
                            _ => throw new ArgumentOutOfRangeException(nameof(graphic))
                        };
                        _lockedPalette = graphic == TrackGraphic.Minimap || graphic == TrackGraphic.TrackName;
                        if (_overlay.HasValue)
                            Raylib.UnloadRenderTexture(_overlay.Value);
                        _overlay = null;
                        _overlayVisible = false;
                        if (_activeGraphic == TrackGraphic.Minimap)
                        {
                            // Generate map overlay.
                            _overlay = Raylib.LoadRenderTexture(512, 512);
                            _view.DrawInTrack = null;
                        }
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

        if (_overlay.HasValue) {
            ImGui.Checkbox("Show track overlay?", ref _overlayVisible);
            if (_overlayVisible && _overlay.HasValue)
            {
                var overlay = _overlay.Value;
                var src = new Rectangle(0, 0, overlay.Texture.Width, -overlay.Texture.Height);
                var quarterScreen = Raylib.GetScreenWidth() / 4f;
                var windowSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
                var position = new Vector2(0, menuBarHeight);
                var viewportArea = new Rectangle(position, quarterScreen * 2, windowSize.Y - position.Y);
                var dest = new Rectangle(viewportArea.Position, new Vector2(MathF.Min(viewportArea.Width, viewportArea.Height)));

                var trackSize = _track.Config.Size.AsVector2();
                
                _view.Camera.Offset = Vector2.Zero;
                _view.Camera.Rotation = 0.0f;
                _view.Camera.Target = Vector2.Zero + _tileEditor.ViewCamera.Target * 32;
                _view.Camera.Zoom = 0.125f * trackSize.X * _tileEditor.ViewCamera.Zoom * 0.125f;

                Raylib.BeginTextureMode(_overlay.Value);
                _view.Draw();
                Raylib.EndTextureMode();
                
                Raylib.DrawTexturePro(overlay.Texture, src, dest, Vector2.Zero, 0.0f, Color.White with {A = 128});
            }
        }
        
        if (ImGui.Button("Import"))
        {
            var status = Nfd.OpenDialog(out var path, ImageFilter, "tiles.gif");
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                try
                {
                    var gif = GifDocument.Load(path);
                    gif.LoadGifToGba(ref _tileEditor.Tileset, ref _tileEditor.Palette, _tileEditor.Layout, _lockedPalette);
                    _tileEditor.ReloadTileset();
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
        if (_overlay.HasValue)
            Raylib.UnloadRenderTexture(_overlay.Value);
    }
}