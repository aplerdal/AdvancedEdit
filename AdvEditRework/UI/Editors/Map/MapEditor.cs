using System.Numerics;
using AdvancedLib.RaylibExt;
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
    Select,
    Eyedropper,
    Rectangle,
    Bucket,
    Stamp
}

public class MapEditor : Editor, IToolEditable
{
    private MapEditorToolType _activeToolType = MapEditorToolType.Draw;
    public readonly UndoManager UndoManager = new();
    public readonly TrackView View;
    private readonly Texture2D _iconAtlas;
    private readonly MapEditorTool[] _tools = [new DrawTool(), new SelectionTool(), new Eyedropper(), new RectangleTool(), new BucketTool(), new StampTool()];
    public bool Focused { get; set; }
    public byte? ActiveIndex { get; set; } = 0;

    public Vector2 CellMousePos => View.MouseTilePos;
    public int CellSize => 8;

    public Vector2 GridSize => new Vector2(View.Track.Config.Size.X, View.Track.Config.Size.Y) * 128;

    public void DrawCell(Vector2 position, byte id, Color color)
    {
        PaletteShader.Begin();
        Raylib.DrawTextureRec(View.Tileset, Extensions.GetTileRect(id, 16), position * 8, color);
        PaletteShader.End();
    }

    public bool ValidCell(Vector2 position)
    {
        return View.PointOnTrack(position);
    }

    public UndoActions SetCellsUndoable(HashSet<Vector2> positions, byte id)
    {
        return View.SetTilesUndoable(positions, id);
    }

    public UndoActions SetCellsUndoable(List<CellEntry> cells)
    {
        return View.SetTilesUndoable(cells);
    }

    public UndoActions SetCellsUndoable(Rectangle area, byte id)
    {
        return View.SetTilesUndoable(area, id);
    }

    public void PushUndoable(UndoActions action)
    {
        UndoManager.Push(action);
    }

    public byte GetCell(Vector2 position)
    {
        return View.Track.Tilemap[position];
    }

    public void OutlineCell(Vector2 position, Color color)
    {
        Raylib.DrawRectangleLinesEx(new Rectangle(position * 8 - Vector2.One, new Vector2(10)), 1, color);
    }

    public CellEntry[]? Stamp { get; set; }

    public bool ViewportHovered => !(Raylib.GetMousePosition().Y <= ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2 ||
                                     Raylib.GetMousePosition().X >= Raylib.GetScreenWidth()*(3/4f));

    public MapEditor(TrackView view)
    {
        View = view;
        _iconAtlas = Program.TextureManager.GetTexture("tools.png");
        View.DrawInTrack = TrackSpaceUpdate;
        Raylib.SetMouseCursor(MouseCursor.Default);
    }

    public override void Update(bool hasFocus)
    {
        Focused = hasFocus;
        View.Update();
        View.Draw();
        UpdatePanel();
    }

    private void TrackSpaceUpdate()
    {
        _tools[(int)_activeToolType].Update(this);
        CheckKeybinds();
    }

    private void UpdatePanel()
    {
        var mousePos = Raylib.GetMousePosition();
        var windowSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        var panelWidth = Raylib.GetScreenWidth() / 4;
        var panelRect = new Rectangle(windowSize.X - panelWidth, ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2, panelWidth, windowSize.Y);
        Focused = Raylib.CheckCollisionPointRec(mousePos, panelRect);
        
        Raylib.DrawRectangleRec(panelRect, Color.White);
        var scale = (panelWidth-6) / 128f;
        UpdateTilePicker(panelRect.Position + new Vector2(3), scale);
        var toolPickerPos = panelRect.Position + new Vector2(3, scale * 128 + 3);
        ToolPicker.Draw(toolPickerPos, panelWidth - 6, ref _activeToolType);
        var optionsPos = toolPickerPos + new Vector2(0, (panelWidth - 6)/8f);
        var optionsRec = new Rectangle(optionsPos, panelWidth-6, windowSize.Y - optionsPos.Y);
        ShowTilesetOptions(optionsRec);
    }

    private void ShowTilesetOptions(Rectangle area)
    {
        ImHelper.BeginEmptyWindow("optionsTileset", area);
        if (!ActiveIndex.HasValue)
        {
            ImGui.BeginDisabled();
            var _ = 0;
            ImGui.InputInt("Behavior", ref _);
            ImGui.EndDisabled();
        }
        else
        {
            int value = View.Track.Behaviors[ActiveIndex.Value];
            ImGui.InputInt("Behavior", ref value);
            View.Track.Behaviors[ActiveIndex.Value] = (byte)value;
        }
        ImHelper.EndEmptyWindow();
    }

    private void UpdateTilePicker(Vector2 position, float scale)
    {
        PaletteShader.Begin();
        var tileSize = 8 * scale;
        var tilesetRect = new Rectangle(position, new Vector2(16 * tileSize));
        
        Raylib.DrawTextureEx(View.Tileset, position, 0.0f, scale, Color.White);
        PaletteShader.End();
        if (ActiveIndex.HasValue)
        {
            var tilePos = new Vector2((int)(ActiveIndex.Value % 16), (int)(ActiveIndex.Value / 16));
            var selectedTileRect = new Rectangle(tilesetRect.Position + tilePos * tileSize, new Vector2(tileSize));
            Raylib.DrawRectangleLinesEx(selectedTileRect, 2, Color.White);
        }

        var mousePos = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mousePos, tilesetRect))
        {
            var relMousePos = mousePos - tilesetRect.Position;
            var tilePosition = relMousePos / tileSize;
            tilePosition = new Vector2((int)tilePosition.X, (int)tilePosition.Y);
            var hoverTileRect = new Rectangle(tilesetRect.Position + tilePosition * tileSize, tileSize, tileSize);
            hoverTileRect.Position -= new Vector2(2);
            hoverTileRect.Size += new Vector2(4);
            Raylib.DrawRectangleLinesEx(hoverTileRect, 4, Color.White);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) ActiveIndex = (byte)(tilePosition.X + 16 * tilePosition.Y);
        }
    }

    private void CheckKeybinds()
    {
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);

        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Redo();
        var settings = Settings.Shared;
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.EyedropperBind)) SetTool(MapEditorToolType.Eyedropper);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.SelectBind)) SetTool(MapEditorToolType.Select);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.DrawBind)) SetTool(MapEditorToolType.Draw);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.RectangleBind)) SetTool(MapEditorToolType.Rectangle);
        if (!ctrl && !shift && Raylib.IsKeyPressed(settings.BucketBind)) SetTool(MapEditorToolType.Bucket);
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