using System.Numerics;
using AdvancedLib.RaylibExt;
using AdvancedLib.Serialization.AI;
using AdvEditRework.DearImGui;
using AdvEditRework.UI.Undo;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors.AI;

internal record CheckpointDrag(Vector2 StartPosition, Checkpoint Checkpoint, DragHandle Handle)
{
    public readonly Checkpoint OriginalCheckpoint = new()
    {
        X = Checkpoint.X,
        Y = Checkpoint.Y,
        Width = Checkpoint.Width,
        Height = Checkpoint.Height,
        Shape = Checkpoint.Shape
    };
}

internal record TargetDrag(Vector2 StartPosition, AiTarget Target)
{
    public readonly AiTarget OriginalTarget = new()
    {
        X = Target.X,
        Y = Target.Y,
        Speed = Target.Speed,
        Intersection = Target.Intersection
    };
}

public class AiEditor : Editor
{
    public readonly TrackView View;
    private readonly UndoManager _undoManager = new();
    private readonly Texture2D _shapeIcons;
    private readonly Texture2D _checkpointIcons;

    private CheckpointDrag? _checkpointDrag;
    private TargetDrag? _targetDrag;
    private Checkpoint? _selectedCheckpoint;
    private AiTarget? _selectedTarget;
    private bool _panelFocused;
    private bool _resetConfirmationShown = false;
    private AiEditorTab _tab = AiEditorTab.Checkpoint;

    private const int TargetDrawSize = 12;

    public Vector2 MouseAiBlockPos => (View.MouseTilePos / 2).ToVec2I().AsVector2();

    public AiEditor(TrackView view)
    {
        View = view;
        View.DrawInTrack = TrackDraw;
        _shapeIcons = Program.TextureManager.GetTexture("shapes.png");
        _checkpointIcons = Program.TextureManager.GetTexture("zoneIcons.png");
    }

    public override void Update(bool hasFocus)
    {
        View.Update();
        View.Draw();
        UpdatePanel();
        CheckUndo();
    }

    private bool CheckpointShapeButton(Vector2 position, float scale, CheckpointShape shape, bool selected)
    {
        var fillColor = new Color(0.75f, 0.75f, 0.75f);
        var outlineColor = new Color(0.65f, 0.65f, 0.65f);
        var shadowColor = new Color(0.50f, 0.50f, 0.50f);
        var dest = new Rectangle(position, new Vector2(scale));
        var atlasSrc = new Rectangle(16 * (int)shape, 0, 16, 16);
        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), dest);
        if (selected || hovered)
        {
            Raylib.DrawRectangleRec(dest with { Y = dest.Y + 2 }, fillColor);
            Raylib.DrawTexturePro(_shapeIcons, atlasSrc, dest with { Y = dest.Y + 2 }, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 }, 2, outlineColor);
        }
        else
        {
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 }, 2, shadowColor);
            Raylib.DrawRectangleRec(dest, fillColor);
            Raylib.DrawTexturePro(_shapeIcons, atlasSrc, dest, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest, 2, outlineColor);
        }

        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    private bool CheckpointOptionsButton(Vector2 position, float scale, AiEditorIcon icon)
    {
        var fillColor = new Color(0.75f, 0.75f, 0.75f);
        var outlineColor = new Color(0.65f, 0.65f, 0.65f);
        var shadowColor = new Color(0.50f, 0.50f, 0.50f);
        var dest = new Rectangle(position, new Vector2(scale));
        var atlasSrc = new Rectangle(16 * ((int)icon % 4), 16 * (int)((int)icon / 4), 16, 16);
        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), dest);
        if (hovered && Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            Raylib.DrawRectangleRec(dest with { Y = dest.Y + 2 }, fillColor);
            Raylib.DrawTexturePro(_checkpointIcons, atlasSrc, dest with { Y = dest.Y + 2 }, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 }, 2, outlineColor);
        }
        else
        {
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 }, 2, shadowColor);
            Raylib.DrawRectangleRec(dest, fillColor);
            Raylib.DrawTexturePro(_checkpointIcons, atlasSrc, dest, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest, 2, outlineColor);
        }

        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    private void ShapeSelector(Vector2 position, float width)
    {
        const int checkpointTypes = 8;
        var area = ImGui.GetContentRegionAvail();
        var scale = area.X / checkpointTypes;
        var dest = position;
        var lines = 1;
        foreach (var shape in Enum.GetValues(typeof(CheckpointShape)).Cast<CheckpointShape>())
        {
            if (CheckpointShapeButton(dest, scale, shape, _selectedCheckpoint?.Shape == shape))
                if (_selectedCheckpoint is not null)
                    _undoManager.Push(ModifyCheckpointShapeUndoable(_selectedCheckpoint, shape));
            dest.X += scale;
            if (dest.X + scale - position.X > width)
            {
                dest = new Vector2(position.X, position.Y + scale);
                lines++;
            }
        }

        ImGui.Dummy(new Vector2(area.X * checkpointTypes, scale * lines));
    }

    private void CheckpointOptions(Vector2 position)
    {
        const int optionsCount = 8;
        var area = ImGui.GetContentRegionAvail();
        var scale = area.X / optionsCount;
        ImGui.Dummy(new Vector2(scale * optionsCount, scale));
        if (CheckpointOptionsButton(position + new Vector2(0 * 32, 0), scale, AiEditorIcon.NewCheckpoint)) _undoManager.Push(CreateCheckpointUndoable());

        if (CheckpointOptionsButton(position + new Vector2(1 * 32, 0), scale, AiEditorIcon.DeleteCheckpoint) || Raylib.IsKeyPressed(KeyboardKey.Delete))
            if (_selectedCheckpoint is not null)
            {
                _undoManager.Push(DeleteCheckpointUndoable(_selectedCheckpoint));
                _selectedCheckpoint = null;
            }
    }

    private void UpdatePanel()
    {
        var mousePos = Raylib.GetMousePosition();
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());

        var panelWidth = 290;
        var panelRect = new Rectangle(windowSize.X - panelWidth, ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2, panelWidth, windowSize.Y);

        Raylib.DrawRectangleRec(panelRect, ImHelper.Color(ImGuiCol.WindowBg));
        Raylib.DrawRectangleLinesEx(panelRect, 1, ImHelper.Color(ImGuiCol.Border));
        ImHelper.BeginEmptyWindow("AiEditorOptions", panelRect);

        if (ImGui.BeginTabBar("AiModeTabBar"))
        {
            if (ImGui.BeginTabItem("Checkpoints"))
            {
                _tab = AiEditorTab.Checkpoint;
                ImGui.SeparatorText("Options");
                CheckpointOptions(ImGui.GetCursorScreenPos());
                if (_selectedCheckpoint is not null)
                {
                    ImGui.SeparatorText("Selection Options");
                    ImGui.Text("Test");
                    ShapeSelector(ImGui.GetCursorScreenPos(), panelWidth - 12);
                }

                ImGui.Separator();
                if (ImGui.CollapsingHeader("Checkpoint Help"))
                    ImGui.Text(
                        """
                        Checkpoints must be laid out in numerical 
                        order to properly order racers while driving.
                        The finish line is determined by where the 
                        last checkpoint meets the 0th checkpoint, and laps
                        are counted accordingly (Watch for 
                        shortcuts!). If any checkpoints overlap, the one
                        with the higher ID takes priority. This 
                        also applies to the finish line.

                        AI targets come in 3 sets. Each set
                        represents a different route the AI
                        can take through the checkpoint.
                        Each lap, the AI choose a set of
                        targets to take around the track, and
                        when they enter a checkpoint, they drive
                        towards the selected target.
                        """);

                ImGui.EndTabItem();
            }

            for (int i = 1; i <= 3; i++)
            {
                if (ImGui.BeginTabItem(Enum.GetNames(typeof(AiEditorTab))[i]))
                {
                    _tab = (AiEditorTab)i;
                    if (_selectedTarget is not null)
                    {
                        int speed = _selectedTarget.Speed;
                        ImGui.InputInt("Speed", ref speed);
                        _selectedTarget.Speed = (byte)(speed & 0x3);
                        bool intersection = _selectedTarget.Intersection;
                        ImGui.Checkbox("Intersection", ref intersection);
                        _selectedTarget.Intersection = intersection;
                    }
                    else
                    {
                        ImGui.BeginDisabled();
                        int speed = 0;
                        bool intersection = false;
                        ImGui.InputInt("Speed", ref speed);
                        ImGui.Checkbox("Intersection", ref intersection);
                        ImGui.EndDisabled();
                    }
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        ImHelper.EndEmptyWindow();

        _panelFocused = Raylib.CheckCollisionPointRec(mousePos, panelRect);
    }

    private void CheckUndo()
    {
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) _undoManager.Redo();
    }

    private void CheckpointTrackDraw()
    {
        var hoveredCheckpoint = _checkpointDrag is not null ? _checkpointDrag.Checkpoint : _panelFocused || _resetConfirmationShown ? null : GetHoveredCheckpoint();
        var handle = hoveredCheckpoint?.GetResizeHandle(View.MouseTilePos) ?? DragHandle.None;
        DrawCheckpoints(hoveredCheckpoint);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && hoveredCheckpoint is null && !(_panelFocused || _resetConfirmationShown) && _selectedCheckpoint is not null) _selectedCheckpoint = null;

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (_checkpointDrag is null && hoveredCheckpoint is not null) _checkpointDrag = new CheckpointDrag(MouseAiBlockPos, hoveredCheckpoint, handle);

            if (_checkpointDrag is not null)
            {
                var dragDelta = MouseAiBlockPos - _checkpointDrag.StartPosition;
                if (_checkpointDrag.Handle == DragHandle.None) // Dragging entire checkpoint
                {
                    _checkpointDrag.Checkpoint.X = (ushort)Math.Clamp(_checkpointDrag.OriginalCheckpoint.X + dragDelta.X, 0, ushort.MaxValue);
                    _checkpointDrag.Checkpoint.Y = (ushort)Math.Clamp(_checkpointDrag.OriginalCheckpoint.Y + dragDelta.Y, 0, ushort.MaxValue);
                }
                else
                {
                    _checkpointDrag.Checkpoint.Resize(View.MouseTilePos, _checkpointDrag.Handle);
                }
            }
        }
        else if (_checkpointDrag is not null)
        {
            var checkpointRef = _checkpointDrag.Checkpoint;
            var newCheckpoint = new Checkpoint
            {
                X = _checkpointDrag.Checkpoint.X,
                Y = _checkpointDrag.Checkpoint.Y,
                Width = _checkpointDrag.Checkpoint.Width,
                Height = _checkpointDrag.Checkpoint.Height,
                Shape = _checkpointDrag.Checkpoint.Shape
            };
            var originalCheckpoint = new Checkpoint
            {
                X = _checkpointDrag.OriginalCheckpoint.X,
                Y = _checkpointDrag.OriginalCheckpoint.Y,
                Width = _checkpointDrag.OriginalCheckpoint.Width,
                Height = _checkpointDrag.OriginalCheckpoint.Height,
                Shape = _checkpointDrag.OriginalCheckpoint.Shape
            };

            if (newCheckpoint.Equals(originalCheckpoint))
                _selectedCheckpoint = checkpointRef;
            else
                _undoManager.Push(new UndoActions(
                    () =>
                    {
                        checkpointRef.X = newCheckpoint.X;
                        checkpointRef.Y = newCheckpoint.Y;
                        checkpointRef.Width = newCheckpoint.Width;
                        checkpointRef.Height = newCheckpoint.Height;
                        checkpointRef.Shape = newCheckpoint.Shape;
                    },
                    () =>
                    {
                        checkpointRef.X = originalCheckpoint.X;
                        checkpointRef.Y = originalCheckpoint.Y;
                        checkpointRef.Width = originalCheckpoint.Width;
                        checkpointRef.Height = originalCheckpoint.Height;
                        checkpointRef.Shape = originalCheckpoint.Shape;
                    }
                ));

            _checkpointDrag = null;
        }

        Raylib.SetMouseCursor(handle switch
        {
            DragHandle.Bottom or DragHandle.Top => MouseCursor.ResizeNs,
            DragHandle.Left or DragHandle.Right => MouseCursor.ResizeEw,
            DragHandle.TopLeft or DragHandle.BottomRight => MouseCursor.ResizeNwse,
            DragHandle.TopRight or DragHandle.BottomLeft => MouseCursor.ResizeNesw,
            _ => hoveredCheckpoint is null ? MouseCursor.Default : MouseCursor.ResizeAll
        });
    }

    private void TrackDrawTargets(int set)
    {
        var hoveredTarget = _targetDrag is not null ? _targetDrag.Target : _panelFocused || _resetConfirmationShown ? null : GetHoveredTarget(set);

        DrawCheckpointsForTargets(set, hoveredTarget);
        DrawTargets(set, hoveredTarget);
        
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && hoveredTarget is null && !(_panelFocused || _resetConfirmationShown) && _selectedTarget is not null) 
            _selectedTarget = null;

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (_targetDrag is null && hoveredTarget is not null) _targetDrag = new TargetDrag(View.MouseTilePos, hoveredTarget);

            if (_targetDrag is not null)
            {
                var dragDelta = View.MouseTilePos - _targetDrag.StartPosition;
                _targetDrag.Target.X = (ushort)Math.Max(_targetDrag.OriginalTarget.X + dragDelta.X, 0);
                _targetDrag.Target.Y = (ushort)Math.Max(_targetDrag.OriginalTarget.Y + dragDelta.Y, 0);
            }
        }
        else if (_targetDrag is not null)
        {
            var targetRef = _targetDrag.Target;
            var oldTargetRef = _targetDrag.OriginalTarget;
            var newTarget = new AiTarget
            {
                X = targetRef.X,
                Y = targetRef.Y,
                Speed = targetRef.Speed,
                Intersection = targetRef.Intersection,
            };
            var oldTarget = new AiTarget
            {
                X = oldTargetRef.X,
                Y = oldTargetRef.Y,
                Speed = oldTargetRef.Speed,
                Intersection = oldTargetRef.Intersection,
            };

            if (_targetDrag.Target.Equals(_targetDrag.OriginalTarget))
                _selectedTarget = _targetDrag.Target;
            else
                _undoManager.Push(new UndoActions(
                    () =>
                    {
                        targetRef.X = newTarget.X;
                        targetRef.Y = newTarget.Y;
                        targetRef.Speed = newTarget.Speed;
                        targetRef.Intersection = newTarget.Intersection;
                    },
                    () =>
                    {
                        targetRef.X = oldTarget.X;
                        targetRef.Y = oldTarget.Y;
                        targetRef.Speed = oldTarget.Speed;
                        targetRef.Intersection = oldTarget.Intersection;
                    }
                ));

            _targetDrag = null;
        }
    }

    private void TrackDraw()
    {
        if (_tab == AiEditorTab.Checkpoint)
            CheckpointTrackDraw();
        else
            TrackDrawTargets((int)_tab - 1);
    }

    private Checkpoint? GetHoveredCheckpoint()
    {
        if (_selectedCheckpoint is not null && _selectedCheckpoint.IsHovered(View.MouseTilePos)) return _selectedCheckpoint;
        return View.Track.Ai.Checkpoints.FirstOrDefault(checkpoint => checkpoint.IsHovered(View.MouseTilePos));
    }

    private AiTarget? GetHoveredTarget(int set)
    {
        bool TargetHovered(AiTarget? target)
        {
            if (target is null) return false;
            var rec = new Rectangle(new Vector2(target.X, target.Y) * 8 - new Vector2(TargetDrawSize / 2), new Vector2(TargetDrawSize));
            return Raylib.CheckCollisionPointRec(View.MouseWorldPos, rec);
        }

        if (_selectedTarget is not null && TargetHovered(_selectedTarget)) return _selectedTarget;
        return View.Track.Ai.TargetSets[set].FirstOrDefault(TargetHovered, null);
    }

    private void DrawCheckpoints(Checkpoint? hovered)
    {
        for (var i = 0; i < View.Track.Ai.Checkpoints.Count; i++)
        {
            var checkpoint = View.Track.Ai.Checkpoints[i];
            var color = Color.Blue;
            if (ReferenceEquals(checkpoint, hovered)) color = Color.SkyBlue;
            if (ReferenceEquals(checkpoint, _selectedCheckpoint)) color = Color.White;
            DrawCheckpoint(checkpoint, color);
            DrawLabel(checkpoint, i);
        }
    }

    private void DrawTargets(int set, AiTarget? hovered)
    {
        for (var i = 0; i < View.Track.Ai.TargetSets[set].Count; i++)
        {
            var target = View.Track.Ai.TargetSets[set][i];
            var checkpoint = View.Track.Ai.Checkpoints[i];
            var color = target.Speed switch
            {
                0 => new Color(0x5e, 0xcd, 0x00),
                1 => new Color(0xdd, 0xcc, 0x00),
                2 => new Color(0xff, 0x95, 0x2c),
                3 => new Color(0xff, 0x69, 0x69),
                _ => Color.White
            };
            if (ReferenceEquals(target, hovered)) color = Color.White;
            if (ReferenceEquals(_selectedTarget, target)) color = Color.White;
            DrawTarget(target, checkpoint, color);
        }
    }

    private void DrawCheckpointsForTargets(int set, AiTarget? hovered)
    {
        for (var i = 0; i < View.Track.Ai.Checkpoints.Count; i++)
        {
            var checkpoint = View.Track.Ai.Checkpoints[i];
            var target = View.Track.Ai.TargetSets[set][i];
            var color = target.Speed switch
            {
                0 => new Color(0x5e, 0xcd, 0x00),
                1 => new Color(0xdd, 0xcc, 0x00),
                2 => new Color(0xff, 0x95, 0x2c),
                3 => new Color(0xff, 0x69, 0x69),
                _ => Color.White
            };
            if (target == hovered) color = Color.White;
            DrawCheckpoint(checkpoint, color);
        }
    }

    private void DrawCheckpoint(Checkpoint checkpoint, Color color)
    {
        checkpoint.Draw(color with { A = 255 });
    }

    private void DrawTarget(AiTarget target, Checkpoint checkpoint, Color color)
    {
        var pos = new Vector2(target.X, target.Y) * 8;
        var area = new Rectangle(pos - new Vector2(TargetDrawSize / 2), new Vector2(TargetDrawSize));

        var rect = checkpoint.GetRect();
        rect.Position *= Checkpoint.Precision * 8;
        rect.Size *= Checkpoint.Precision * 8;
        // Ensure lines intersect drawn rectangles
        rect.Position += new Vector2(0.5f);
        rect.Size -= new Vector2(1);
        area.Position += new Vector2(0.5f);
        area.Size -= new Vector2(1);

        var checkpointMin = rect.Position;
        var checkpointMax = rect.Position + rect.Size;

        void TopLeft() => Raylib.DrawLineEx(new Vector2(checkpointMin.X, checkpointMin.Y), pos, 1, Color.White);
        void TopRight() => Raylib.DrawLineEx(new Vector2(checkpointMax.X, checkpointMin.Y), pos, 1, Color.White);
        void BottomLeft() => Raylib.DrawLineEx(new Vector2(checkpointMin.X, checkpointMax.Y), pos, 1, Color.White);
        void BottomRight() => Raylib.DrawLineEx(new Vector2(checkpointMax.X, checkpointMax.Y), pos, 1, Color.White);
        
        var overlapX = pos.X > checkpointMin.X && pos.X < checkpointMax.X;
        var overlapY = pos.Y > checkpointMin.Y && pos.Y < checkpointMax.Y;
        var leftOf = pos.X <= checkpointMin.X;
        var rightOf = pos.X >= checkpointMax.X;
        var above = pos.Y <= checkpointMin.Y;
        var below = pos.Y >= checkpointMax.Y;

        if (checkpoint.Shape != CheckpointShape.Rectangle)
        {
            if (checkpoint.Shape != CheckpointShape.TriangleBottomRight) TopLeft();
            if (checkpoint.Shape != CheckpointShape.TriangleTopRight) BottomLeft();
            if (checkpoint.Shape != CheckpointShape.TriangleBottomLeft) TopRight();
            if (checkpoint.Shape != CheckpointShape.TriangleTopLeft) BottomRight();
        }
        else if (above && leftOf)
        {
            // Top Left
            BottomLeft();
            TopRight();
        }
        else if (above && overlapX)
        {
            // Top
            TopLeft();
            TopRight();
        }
        else if (above && rightOf)
        {
            // Top Right
            TopLeft();
            BottomRight();
        }
        else if (overlapY && leftOf)
        {
            // Left
            TopLeft();
            BottomLeft();
        }
        else if (overlapX && overlapY)
        {
            // Inside
            TopLeft();
            BottomLeft();
            TopRight();
            BottomRight();
        }
        else if (overlapY && rightOf)
        {
            // Right
            TopRight();
            BottomRight();
        }
        else if (below && leftOf)
        {
            // Bottom Left
            TopLeft();
            BottomRight();
        }
        else if (below && overlapX)
        {
            // Bottom
            BottomLeft();
            BottomRight();
        }
        else if (below && rightOf)
        {
            // Bottom Right
            TopRight();
            BottomLeft();
        }

        Raylib.DrawCircleV(pos, 0.5f, Color.White);

        Raylib.DrawRectangleRec(area, color with { A = 128 });
        Raylib.DrawRectangleLinesEx(area, 1, Color.White);
    }

    private void DrawLabel(Checkpoint checkpoint, int index)
    {
        var checkpointRect = checkpoint.GetRect();
        checkpointRect.Position *= 8 * Checkpoint.Precision;
        checkpointRect.Size *= 8 * Checkpoint.Precision;

        var position = checkpointRect.Position;
        position += checkpoint.Shape switch
        {
            CheckpointShape.Rectangle => checkpointRect.Size / 2,
            CheckpointShape.TriangleBottomLeft => new Vector2(checkpointRect.Size.X / 4, 3 * checkpointRect.Size.Y / 4),
            CheckpointShape.TriangleTopRight => new Vector2(3 * checkpointRect.Size.X / 4, checkpointRect.Size.Y / 4),
            CheckpointShape.TriangleTopLeft => checkpointRect.Size / 4,
            CheckpointShape.TriangleBottomRight => 3 * checkpointRect.Size / 4,
            _ => throw new ArgumentOutOfRangeException()
        };

        var id = $"{index}";
        var size = Raylib.MeasureText(id, 20);
        position -= new Vector2(size, 20) / 2;
        Raylib.DrawText(id, (int)position.X, (int)position.Y, 20, Color.White);
    }

    private UndoActions ModifyCheckpointShapeUndoable(Checkpoint checkpoint, CheckpointShape newShape)
    {
        var oldShape = checkpoint.Shape;
        checkpoint.Shape = newShape;
        if (newShape == CheckpointShape.Rectangle && oldShape != CheckpointShape.Rectangle) checkpoint.Height = checkpoint.Width;
        return new UndoActions(
            () => checkpoint.Shape = newShape,
            () => checkpoint.Shape = oldShape
        );
    }

    private UndoActions CreateCheckpointUndoable()
    {
        var ai = View.Track.Ai;
        Checkpoint newCheckpoint;
        int newIndex;
        if (_selectedCheckpoint is not null)
        {
            newCheckpoint = new Checkpoint((ushort)(_selectedCheckpoint.X + 2), (ushort)(_selectedCheckpoint.Y + 2), _selectedCheckpoint.Width, _selectedCheckpoint.Height, CheckpointShape.Rectangle);
            newIndex = ai.Checkpoints.IndexOf(_selectedCheckpoint) + 1;
        }
        else
        {
            newCheckpoint = Checkpoint.Default;
            newIndex = ai.Checkpoints.Count;

            // Move camera over created checkpoint. We don't do this if there is a selected checkpoint as it looks too jittery.
            var newCheckpointRec = newCheckpoint.GetRect();
            var checkpointCenter = new Vector2(newCheckpointRec.X + newCheckpointRec.Width / 2, newCheckpointRec.Y + newCheckpointRec.Height / 2) * 16;
            var windowSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            var viewportSize = Raylib.GetScreenToWorld2D(windowSize - Vector2.One, View.Camera) - Raylib.GetScreenToWorld2D(Vector2.Zero, View.Camera);
            View.Camera.Target = checkpointCenter - viewportSize / 2;
        }

        ai.Checkpoints.Insert(newIndex, newCheckpoint);
        foreach (var set in ai.TargetSets) set.Insert(newIndex, AiTarget.Default);

        _selectedCheckpoint = newCheckpoint;

        return new UndoActions(
            () =>
            {
                View.Track.Ai.Checkpoints.Insert(newIndex, newCheckpoint);
                foreach (var set in View.Track.Ai.TargetSets) set.Insert(newIndex, AiTarget.Default);
            },
            () =>
            {
                if (ReferenceEquals(View.Track.Ai.Checkpoints[newIndex], _selectedCheckpoint)) _selectedCheckpoint = null;
                View.Track.Ai.Checkpoints.RemoveAt(newIndex);
                foreach (var set in View.Track.Ai.TargetSets) set.RemoveAt(newIndex);
            }
        );
    }

    private UndoActions DeleteCheckpointUndoable(Checkpoint checkpoint)
    {
        var ai = View.Track.Ai;
        var index = ai.Checkpoints.IndexOf(checkpoint);
        var originalCheckpoint = ai.Checkpoints[index];
        List<AiTarget> originalTargets = new();
        ai.Checkpoints.RemoveAt(index);
        foreach (var set in ai.TargetSets)
        {
            originalTargets.Add(set[index]);
            set.RemoveAt(index);
        }

        return new UndoActions(
            () =>
            {
                View.Track.Ai.Checkpoints.RemoveAt(index);
                foreach (var set in View.Track.Ai.TargetSets) set.RemoveAt(index);
            },
            () =>
            {
                View.Track.Ai.Checkpoints.Insert(index, originalCheckpoint);
                for (var i = 0; i < ai.TargetSets.Count; i++) ai.TargetSets[i].Insert(index, originalTargets[i]);
            }
        );
    }

    public override void Dispose()
    {
        Raylib.SetMouseCursor(MouseCursor.Default);
    }
}