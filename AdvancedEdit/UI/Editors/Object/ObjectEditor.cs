using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.RaylibExt;
using AdvancedLib.Serialization.OAM;
using AdvancedLib.Serialization.Objects;
using AdvancedLib.Serialization.Tracks;
using AdvEditRework.DearImGui;
using AdvEditRework.Shaders;
using AdvEditRework.UI.Undo;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors.Object;

internal record PlacementDrag(Vector2 StartPosition, ObjectPlacement Placement)
{
    public readonly ObjectPlacement OriginalPlacement = new()
    {
        X = Placement.X,
        Y = Placement.Y,
        Checkpoint = Placement.Checkpoint,
        ID = Placement.ID,
    };
}

public class ObjectEditor : Editor
{
    private readonly TrackView _view;
    private readonly Track _track;
    private readonly Texture2D _obstacleGfx;
    private readonly ObstacleOam _obstacleOam;
    private readonly int[] _vecPalette;
    private readonly UndoManager _undoManager;

    private PlacementDrag? _placementDrag;
    private int _selectedIndex = -1;
    private ObjectPlacement? _selectedPlacement;

    private ObjectGfxEditor? _gfxEditor;
    private bool IsPanelHovered => Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(Raylib.GetScreenWidth() * 0.75f, ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2, Raylib.GetScreenWidth() / 4f, Raylib.GetScreenHeight() - (ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2) + 4));

    private const float CellSize = 64f;

    public ObjectEditor(TrackView view, ObstacleOam oamData)
    {
        _view = view;
        _view.DrawInTrack = TrackDraw;
        _track = view.Track;
        _undoManager = new();
        Debug.Assert(_track.ObstacleGfx is not null && _track.ObstaclePalette is not null);
        _obstacleGfx = _track.ObstacleGfx.TileTexture(8, _track.ObstacleGfx.Length / 8);
        _vecPalette = new int[256 * 3 * 2]; // Space for blank colors when we read a slice of the palette
        var palette = _track.ObstaclePalette.ToIVec3();
        Array.Copy(palette, 0, _vecPalette, 0, 256 * 3);
        _obstacleOam = oamData;
    }

    public override void Update(bool hasFocus)
    {
        CheckBinds();
        if (_gfxEditor is null)
        {
            if (!IsPanelHovered)
                _view.Update();
            _view.Draw();
        }
        else
        {
            _gfxEditor.Update(hasFocus);
        }

        OptionsWindow();
    }

    private void DrawObstacleCellData(Vector2 origin, CellData data, int frame, float size)
    {
        var entry = data.Entries[frame % data.Entries.Count];
        var layout = entry.GetTileGrid();
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);

        float pixW = width * 8f;
        float pixH = height * 8f;

        float scale = MathF.Min(size / pixW, size / pixH);
        scale = MathF.Min(scale, 1f);

        float scaledW = pixW * scale;
        float scaledH = pixH * scale;

        float offsetX = origin.X + (size - scaledW) * 0.5f;
        float offsetY = origin.Y + (size - scaledH) * 0.5f;

        var slice = _vecPalette[(entry.Palette * 16 * 3)..((256 + entry.Palette * 16) * 3)];
        PaletteShader.SetPalette(slice);
        PaletteShader.Begin();

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var src = Extensions.GetTileRect(layout[x, y], 8);
            var destPos = new Vector2(offsetX + x * 8 * scale, offsetY + y * 8 * scale);

            var dest = new Rectangle(destPos.X, destPos.Y, 8 * scale, 8 * scale);
            Raylib.DrawTexturePro(_obstacleGfx, src, dest, Vector2.Zero, 0f, Color.White);
        }

        PaletteShader.End();
    }

    private void ObstacleTableView()
    {
        ImGui.PushID("ObstacleTableView");

        int frame = (int)Raylib.GetTime();
        var tableRect = new Rectangle(ImGui.GetCursorScreenPos(), new Vector2(ImGui.GetContentRegionAvail().X - 8, Raylib.GetScreenHeight() / 3f));
        var tableFlags = ImGuiTableFlags.Borders
                         | ImGuiTableFlags.ScrollY
                         | ImGuiTableFlags.SizingFixedFit;
        if (ImGui.BeginTable("ObstacleTable", 3, tableFlags, tableRect.Size))
        {
            // Column headers
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Sprite", ImGuiTableColumnFlags.WidthFixed, CellSize);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Parameter", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            for (var i = 0; i < _track.Objects.ObstacleTable.Obstacles.Count; i++)
            {
                var obstacle = _track.Objects.ObstacleTable.Obstacles[i];
                if (obstacle.Type is 0 or -8 or -16) continue;


                ImGui.TableNextRow(ImGuiTableRowFlags.None, CellSize);
                ImGui.TableSetColumnIndex(0);

                var cursorPos = ImGui.GetCursorScreenPos();

                ImGui.SetCursorScreenPos(cursorPos);
                ImGui.Dummy(new Vector2(CellSize));

                Raylib.BeginScissorMode((int)tableRect.X, (int)tableRect.Y, (int)tableRect.Width, (int)tableRect.Height);
                var area = new Rectangle(cursorPos, new Vector2(CellSize));
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), area))
                {
                    if (_selectedIndex == i) _selectedIndex = -1;
                    else _selectedIndex = i;
                }

                if (_selectedIndex == i)
                {
                    Raylib.DrawRectangleRec(area, new Color(0x8a, 0xa1, 0xf6));
                }

                if (obstacle.Type != -1)
                {
                    var cellData = _obstacleOam.GetObjectDistanceCells(obstacle.Type, obstacle.Parameter);
                    DrawObstacleCellData(cursorPos, cellData.Distances[0], frame, CellSize);
                }
                else
                {
                    var viewRec = new Rectangle(cursorPos, new Vector2(CellSize));
                    const int fontSize = 40;
                    var size = Raylib.MeasureText("?", fontSize);
                    var textPos = viewRec.Position + viewRec.Size / 2 - new Vector2(size / 2, fontSize / 2);
                    Raylib.DrawText("?", (int)textPos.X, (int)textPos.Y, fontSize, Color.Magenta);
                }

                Raylib.EndScissorMode();

                if (obstacle.Type == -1)
                {
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text("Item Box");
                    ImGui.TableSetColumnIndex(2);
                }
                else
                {
                    ImGui.TableSetColumnIndex(1);
                    ImGui.SetNextItemWidth(-1);
                    int typeVal = obstacle.Type;
                    if (ImGui.InputInt($"##type{i}", ref typeVal))
                    {
                        typeVal = Math.Clamp(typeVal, 1, short.MaxValue);
                        _track.Objects.ObstacleTable.Obstacles[i] = new Obstacle((short)typeVal, obstacle.Parameter);
                    }

                    ImGui.TableSetColumnIndex(2);
                    ImGui.SetNextItemWidth(-1);
                    int paramVal = obstacle.Parameter;
                    if (ImGui.InputInt($"##param{i}", ref paramVal))
                    {
                        _track.Objects.ObstacleTable.Obstacles[i] = new Obstacle(obstacle.Type, (short)paramVal);
                    }
                }
            }

            ImGui.EndTable();
        }

        if (ImGui.Button("New"))
        {
            _track.Objects.ObstacleTable.Obstacles.Add(new Obstacle(1, 0));
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(_selectedIndex < 2);
        if (ImGui.Button("Duplicate"))
        {
            _track.Objects.ObstacleTable.Obstacles.Add(_track.Objects.ObstacleTable[_selectedIndex].Clone());
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete"))
        {
            foreach (var placement in _track.Objects.ObstaclePlacements.Where(placement => placement.ID == _selectedIndex).ToList())
                _track.Objects.ObstaclePlacements.Remove(placement);
            _track.Objects.ObstacleTable.Obstacles.RemoveAt(_selectedIndex);
            _selectedIndex = -1;
        }

        ImGui.SameLine();
        if (ImGui.Button("Edit"))
        {
            var obs = _track.Objects.ObstacleTable.Obstacles[_selectedIndex];
            _gfxEditor?.Dispose();
            _gfxEditor = new ObjectGfxEditor(_obstacleOam.GetObjectDistanceCells(obs.Type, obs.Parameter), _track.ObstacleGfx, _track.ObstaclePalette);
        }

        ImGui.EndDisabled();
        ImGui.PopID();
    }

    private void ObstaclePreview(Obstacle? obstacle)
    {
        if (obstacle is null)
        {
            ImGui.BeginDisabled();
            ImGui.Text("Distances Preview:");
            ImGui.Dummy(new Vector2(64));
            ImGui.EndDisabled();
            return;
        }

        ImGui.Text("Distances Preview:");
        var cellData = _obstacleOam.GetObjectDistanceCells(obstacle.Type, obstacle.Parameter);
        for (var i = 0; i < cellData.Distances.Length; i++)
        {
            var dist = cellData.Distances[i];
            var cursor = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new Vector2(64));
            DrawObstacleCellData(cursor, dist, ((int)Raylib.GetTime()), 64);
            if (i != cellData.Distances.Length - 1)
                ImGui.SameLine();
        }
    }

    private void OptionsWindow()
    {
        var windowSize = new Vector2(Raylib.GetScreenWidth() / 4f, Raylib.GetScreenHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
        var position = new Vector2(4, menuBarHeight + 4);
        var optionsRect = new Rectangle(Raylib.GetScreenWidth() - windowSize.X, menuBarHeight, windowSize.X, windowSize.Y - position.Y);

        Raylib.DrawRectangleRec(optionsRect, Color.White);
        Raylib.DrawRectangleLinesEx(optionsRect, 2, Color.LightGray);
        ImHelper.BeginEmptyWindow("GfxOptionsWindow", optionsRect);

        ObstacleTableView();

        ImGui.Separator();

        ObstaclePreview(_selectedIndex < 2 ? null : _track.Objects.ObstacleTable[_selectedIndex]);

        ImGui.Separator();

        ImGui.Text("Placement Selection:");
        ImGui.BeginDisabled(_selectedIndex == -1);
        if (ImGui.Button("Create placement"))
        {
            var viewportCenter = new Vector2(Raylib.GetScreenWidth() * (3 / 4f), Raylib.GetScreenHeight()) * 0.5f;
            var worldPos = Raylib.GetScreenToWorld2D(viewportCenter, _view.Camera);
            if (_selectedIndex == 1)
            {
                var newPlacement = new ObjectPlacement((byte)_selectedIndex, (byte)(worldPos.X / 8), (byte)(worldPos.Y / 8), 0);
                _track.Objects.ItemBoxes.Add(newPlacement);
                _selectedPlacement = newPlacement;
            }
            else
            {
                var newPlacement = new ObjectPlacement((byte)_selectedIndex, (byte)(worldPos.X / 8), (byte)(worldPos.Y / 8), 0);
                _track.Objects.ObstaclePlacements.Add(newPlacement);
                _selectedPlacement = newPlacement;
            }
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(_selectedPlacement is null || ((_selectedPlacement.ID & 0x80) != 0));
        var bindPressed = (Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)) && Raylib.IsKeyPressed(KeyboardKey.D);
        if (ImGui.Button("Duplicate") || bindPressed)
        {
            Debug.Assert(_selectedPlacement is not null);
            var newPlacement = _selectedPlacement.Clone();
            newPlacement.X += 1;
            newPlacement.Y += 1;
            if (_track.Objects.ObstaclePlacements.Contains(_selectedPlacement))
                _track.Objects.ObstaclePlacements.Add(newPlacement);
            else if (_track.Objects.ItemBoxes.Contains(_selectedPlacement))
                _track.Objects.ItemBoxes.Add(newPlacement);
            _selectedPlacement = newPlacement;
        }

        ImGui.SameLine();
        bindPressed = Raylib.IsKeyPressed(KeyboardKey.Delete);
        if (ImGui.Button("Delete") || (bindPressed && _selectedPlacement is not null))
        {
            Debug.Assert(_selectedPlacement is not null);
            if (!_track.Objects.ObstaclePlacements.Remove(_selectedPlacement))
                _track.Objects.ItemBoxes.Remove(_selectedPlacement);
            _selectedPlacement = null;
        }

        ImGui.EndDisabled();

        ImHelper.EndEmptyWindow();
    }

    private void TrackDraw()
    {
        ObjectPlacement? hoveredPlacement = null;
        foreach (var placement in _track.Objects.ObstaclePlacements)
        {
            var pos = new Vector2(placement.X, placement.Y) * 8 + new Vector2(4);
            var obstacle = _track.Objects.ObstacleTable[placement.ID];
            var cellData = _obstacleOam.GetObjectDistanceCells(obstacle.Type, obstacle.Parameter);
            var area = new Rectangle(pos - new Vector2(16), new Vector2(32));
            if (_selectedPlacement == placement)
            {
                Raylib.DrawRectangleRec(area, Color.White with { A = 196 });
            }
            else if (Raylib.CheckCollisionPointRec(_view.MouseWorldPos, area))
            {
                hoveredPlacement = placement;
                Raylib.DrawRectangleRec(area, Color.White with { A = 128 });
            }

            DrawObstacleCellData(pos - new Vector2(16), cellData.Distances[0], 0, 32f);
            Raylib.DrawRectangleLinesEx(area, 1, Color.White);
        }

        foreach (var placement in _track.Objects.StartPositions)
        {
            var pos = new Vector2(placement.X, placement.Y) * 8 + new Vector2(4);
            var place = placement.ID & (~0x80);
            var area = new Rectangle(pos - new Vector2(8), new Vector2(16));
            if (_selectedPlacement == placement)
            {
                Raylib.DrawRectangleRec(area, Color.White with { A = 196 });
            }
            else if (Raylib.CheckCollisionPointRec(_view.MouseWorldPos, area))
            {
                hoveredPlacement = placement;
                Raylib.DrawRectangleRec(area, Color.White with { A = 128 });
            }

            Raylib.DrawRectangleLinesEx(area, 1, Color.White);
            var str = place <= 8 ? $"P{place}" : $"E{place - 8}";
            var color = place <= 8 ? Color.White : Color.Yellow;
            const int fontSize = 10;
            var size = Raylib.MeasureText(str, fontSize);
            Raylib.DrawText(str, (int)(pos.X - (size / 2)), (int)(pos.Y - (fontSize / 2)), fontSize, color);
        }

        foreach (var placement in _track.Objects.ItemBoxes)
        {
            var pos = new Vector2(placement.X, placement.Y) * 8 + new Vector2(4);
            var area = new Rectangle(pos - new Vector2(8), new Vector2(16));
            if (_selectedPlacement == placement)
            {
                Raylib.DrawRectangleRec(area, Color.White with { A = 196 });
            }
            else if (Raylib.CheckCollisionPointRec(_view.MouseWorldPos, area))
            {
                hoveredPlacement = placement;
                Raylib.DrawRectangleRec(area, Color.White with { A = 128 });
            }

            Raylib.DrawRectangleLinesEx(area, 1, Color.White);
            var str = "?";
            const int fontSize = 10;
            var size = Raylib.MeasureText(str, fontSize);
            Raylib.DrawText(str, (int)(pos.X - (size / 2)), (int)(pos.Y - (fontSize / 2)), fontSize, Color.Pink);
        }

        var panelHovered = IsPanelHovered;
        if (_placementDrag is not null) hoveredPlacement = _placementDrag.Placement;

        if (Raylib.IsMouseButtonDown(MouseButton.Left) && !panelHovered)
        {
            if (hoveredPlacement is null)
            {
                _selectedPlacement = null;
            }

            if (_placementDrag is null && hoveredPlacement is not null) _placementDrag = new PlacementDrag(_view.MouseTilePos, hoveredPlacement);

            if (_placementDrag is not null)
            {
                var dragDelta = _view.MouseTilePos - _placementDrag.StartPosition;
                _placementDrag.Placement.X = (byte)Math.Max(_placementDrag.OriginalPlacement.X + dragDelta.X, 0);
                _placementDrag.Placement.Y = (byte)Math.Max(_placementDrag.OriginalPlacement.Y + dragDelta.Y, 0);
            }
        }
        else if (_placementDrag is not null)
        {
            var placementRef = _placementDrag.Placement;
            var oldPlacementRef = _placementDrag.OriginalPlacement;
            var newPlacement = placementRef.Clone();
            var oldPlacement = oldPlacementRef.Clone();

            if (placementRef.Equals(oldPlacementRef))
            {
                _selectedPlacement = placementRef;
            }
            else
                _undoManager.Push(new UndoActions(
                    () =>
                    {
                        placementRef.X = newPlacement.X;
                        placementRef.Y = newPlacement.Y;
                        placementRef.ID = newPlacement.ID;
                        placementRef.Checkpoint = newPlacement.Checkpoint;
                    },
                    () =>
                    {
                        placementRef.X = oldPlacement.X;
                        placementRef.Y = oldPlacement.Y;
                        placementRef.ID = oldPlacement.ID;
                        placementRef.Checkpoint = oldPlacement.Checkpoint;
                    }
                ));

            _placementDrag = null;
        }
    }

    private void CheckBinds()
    {
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Redo();
    }

    public override void Dispose()
    {
        Raylib.UnloadTexture(_obstacleGfx);
        _gfxEditor?.Dispose();
    }
}