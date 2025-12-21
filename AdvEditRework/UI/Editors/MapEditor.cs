using System.Numerics;
using AdvancedLib.Serialization.AI;
using AdvEditRework.DearImGui;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Tools;
using AdvEditRework.UI.Undo;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

public enum MapEditorToolType
{
    Draw,
    Erase,
    Select,
    Eyedropper,
    Rectangle,
    Bucket,
    Stamp,
    Wand,
}

public class MapEditor : Editor
{
    private MapEditorToolType _activeToolType = MapEditorToolType.Draw;
    public readonly UndoManager UndoManager = new();
    public readonly TrackView View;
    private readonly Texture2D _iconAtlas;
    private readonly MapEditorTool[] _tools = [new DrawTool(), new DrawTool(), new SelectionTool(), new Eyedropper(), new RectangleTool(), new BucketTool(), new StampTool(), new DrawTool()];
    public bool HasFocus { get; set; }
    public byte? SelectedTile { get; set; } = 0;
    public TileEntry[]? Stamp { get; set; }

    public bool MouseOverMap => !((Raylib.GetMousePosition().Y <= ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2) ||
                                  (Raylib.GetMousePosition().X >= Raylib.GetRenderWidth() - Settings.Shared.UIScale * 262));

    public MapEditor(TrackView view)
    {
        View = view;
        _iconAtlas = Program.TextureManager.GetTexture("tools.png");
        View.DrawInTrack = TrackSpaceUpdate;
        Raylib.SetMouseCursor(MouseCursor.Default);
    }

    public override void Update(bool hasFocus)
    {
        HasFocus = hasFocus;
        View.Draw();
        UpdateUI();
    }

    void TrackSpaceUpdate()
    {
        _tools[(int)_activeToolType].Update(this);
        CheckKeybinds();
    }

    void UpdateUI()
    {
        UpdatePanel();
    }

    void UpdatePanel()
    {
        var scale = Settings.Shared.UIScale;
        var mousePos = Raylib.GetMousePosition();
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());

        var panelWidth = scale * 262;
        var panelRect = new Rectangle(windowSize.X - panelWidth, ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2, panelWidth, windowSize.Y);
        var tabRect = new Rectangle(panelRect.X - 25 * scale, (32 + windowSize.Y) / 2.0f - 25 * scale, 25 * scale, 50 * scale);
        tabRect.X += (25 * scale);

        Raylib.DrawRectangleRec(tabRect, ImHelper.Color(ImGuiCol.WindowBg));
        Raylib.DrawRectangleLinesEx(tabRect, 1 * scale, ImHelper.Color(ImGuiCol.Border));
        Raylib.DrawRectangleRec(panelRect, ImHelper.Color(ImGuiCol.WindowBg));
        Raylib.DrawRectangleLinesEx(panelRect, 1 * scale, ImHelper.Color(ImGuiCol.Border));
        UpdateTilePicker(panelRect.Position + new Vector2(3 * scale));
        var optionsPos = panelRect.Position + new Vector2(3 * scale, 16 * 8 * 2 * scale + 6 * scale);
        var optionsRect = new Rectangle(optionsPos, panelWidth - 6 * scale, panelRect.Size.Y - optionsPos.Y - 3);
        ToolPicker(optionsPos, panelWidth - 6 * scale);
        HasFocus = Raylib.CheckCollisionPointRec(mousePos, panelRect);
    }

    void UpdateTilePicker(Vector2 position)
    {
        PaletteShader.Begin();
        int scale = Settings.Shared.UIScale * 2;
        var tileSize = 8 * scale;
        var tilesetRect = new Rectangle(position, new Vector2(16 * tileSize));
        Raylib.DrawTextureEx(View.Tileset, position, 0.0f, scale, Color.White);
        PaletteShader.End();
        if (SelectedTile.HasValue)
        {
            var tilePos = new Vector2((int)(SelectedTile.Value % 16), (int)(SelectedTile.Value / 16));
            var selectedTileRect = new Rectangle(tilesetRect.Position + tilePos * tileSize, new Vector2(tileSize));
            Raylib.DrawRectangleLinesEx(selectedTileRect, 1 * scale, Color.White);
        }

        var mousePos = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mousePos, tilesetRect))
        {
            var relMousePos = mousePos - tilesetRect.Position;
            var tilePosition = relMousePos / tileSize;
            tilePosition = new Vector2((int)tilePosition.X, (int)tilePosition.Y);
            var hoverTileRect = new Rectangle(tilesetRect.Position + (tilePosition * tileSize), tileSize, tileSize);
            hoverTileRect.Position -= new Vector2(scale);
            hoverTileRect.Size += new Vector2(2 * scale);
            Raylib.DrawRectangleLinesEx(hoverTileRect, 2 * scale, Color.White);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                SelectedTile = (byte)(tilePosition.X + 16 * tilePosition.Y);
            }
        }
    }

    void ToolPicker(Vector2 position, float width)
    {
        var scale = Settings.Shared.UIScale;
        Rectangle dest = new Rectangle(position, new Vector2(32 * scale));
        Color fillColor = new Color(0.75f, 0.75f, 0.75f);
        Color outlineColor = new Color(0.65f, 0.65f, 0.65f);
        Color shadowColor = new Color(0.50f, 0.50f, 0.50f);
        foreach (var tool in Enum.GetValues(typeof(MapEditorToolType)).Cast<MapEditorToolType>())
        {
            var atlasSrc = new Rectangle(16 * ((int)tool % 4), 16 * (int)((int)tool / 4), 16, 16);
            var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), dest);
            if (hovered || tool == _activeToolType)
            {
                Raylib.DrawRectangleRec(dest with { Y = dest.Y + 2 * scale }, fillColor);
                Raylib.DrawTexturePro(_iconAtlas, atlasSrc, dest with { Y = dest.Y + 2 * scale }, Vector2.Zero, 0, Color.White);
                Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, outlineColor);
                if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left)) _activeToolType = tool;
            }
            else
            {
                Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, shadowColor);
                Raylib.DrawRectangleRec(dest, fillColor);
                Raylib.DrawTexturePro(_iconAtlas, atlasSrc, dest, Vector2.Zero, 0, Color.White);
                Raylib.DrawRectangleLinesEx(dest, 2 * scale, outlineColor);
            }

            //Raylib.DrawRectangleRec(dest, Color.Red);
            var newDest = new Rectangle(dest.X + 32, dest.Y, dest.Size);
            if (newDest.X + newDest.Width - position.X > width) newDest = new Rectangle(position.X, position.Y + 32, dest.Size);
            dest = newDest;
        }
    }

    void CheckKeybinds()
    {
        // TODO: Customizable keybinds
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Redo();
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.V)) SetTool(MapEditorToolType.Eyedropper);
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.S)) SetTool(MapEditorToolType.Select);
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.B)) SetTool(MapEditorToolType.Draw);
        if (!ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.R)) SetTool(MapEditorToolType.Rectangle);
    }

    public void SetTool(MapEditorToolType newEditorToolType)
    {
        _activeToolType = newEditorToolType;
    }

    public override void Dispose()
    {
        //
    }
}